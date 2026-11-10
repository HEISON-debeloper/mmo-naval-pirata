using UnityEngine;
using UnityEngine.Rendering;

// Malla de agua low poly que sigue al objetivo y se mueve con las olas.
// Optimizada: la altura se calcula una vez por esquina de la cuadrícula (no por cada vértice repetido),
// la calma junto a las islas se guarda y solo se recalcula cuando el agua se mueve de casilla,
// y las normales (caras planas) se calculan a mano en vez de con RecalculateNormals.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AguaOlas : MonoBehaviour
{
    [Tooltip("Lo que el agua sigue (normalmente el barco del jugador).")]
    public Transform seguir;
    [Tooltip("Tamaño del mar visible, en metros.")]
    public float tamano = 800f;
    [Tooltip("Cuadros por lado. Más = olas más suaves, pero más costoso.")]
    public int resolucion = 100;

    Mesh malla;
    Vector3[] verts, normales;
    float[] alturas, calma, gx, gz;
    float celda, ox = float.NaN, oz = float.NaN;
    int lado;

    // Olas en la tarjeta de video: el shader mueve los vértices y aquí solo se pasa el tiempo, la fuerza
    // y la calma (en una textura que se rehace al mover el agua de casilla). Si no se puede, se hace aquí como antes.
    bool enGPU;
    Texture2D texCalma;
    static readonly int idT = Shader.PropertyToID("_OlasT"), idInten = Shader.PropertyToID("_OlasInten"),
                        idGPU = Shader.PropertyToID("_OlasGPU"), idRejilla = Shader.PropertyToID("_OlasRejilla"),
                        idCalma = Shader.PropertyToID("_OlasCalma");

    void Awake()
    {
        celda = tamano / resolucion;
        lado = resolucion + 1;
        alturas = new float[lado * lado];
        calma = new float[lado * lado];
        gx = new float[lado];
        gz = new float[lado];
        float mitad = tamano / 2f;
        for (int i = 0; i < lado; i++) { gx[i] = i * celda - mitad; gz[i] = i * celda - mitad; }

        int n = resolucion * resolucion * 6;
        verts = new Vector3[n];
        normales = new Vector3[n];
        int[] tris = new int[n];
        int k = 0;
        for (int z = 0; z < resolucion; z++)
        for (int x = 0; x < resolucion; x++)
        {
            // Cada triángulo con sus propios vértices = aspecto facetado (low poly)
            verts[k] = new Vector3(gx[x], 0, gz[z]);
            verts[k + 1] = new Vector3(gx[x], 0, gz[z + 1]);
            verts[k + 2] = new Vector3(gx[x + 1], 0, gz[z + 1]);
            verts[k + 3] = new Vector3(gx[x], 0, gz[z]);
            verts[k + 4] = new Vector3(gx[x + 1], 0, gz[z + 1]);
            verts[k + 5] = new Vector3(gx[x + 1], 0, gz[z]);
            for (int j = 0; j < 6; j++) { tris[k + j] = k + j; normales[k + j] = Vector3.up; }
            k += 6;
        }
        malla = new Mesh { name = "Agua", indexFormat = IndexFormat.UInt32 };
        malla.vertices = verts;
        malla.normals = normales;
        malla.triangles = tris;
        malla.MarkDynamic();
        // Límites fijos (las olas no pasan de ±3 m): así no hay que recalcularlos cada frame
        malla.bounds = new Bounds(Vector3.zero, new Vector3(tamano, 6f, tamano));
        GetComponent<MeshFilter>().sharedMesh = malla;

        // Material del agua que no se dibuja dentro de los cascos (si el shader no está, se queda el de antes)
        Shader sh = Shader.Find("Pirata/Agua");
        MeshRenderer r = GetComponent<MeshRenderer>();
        if (sh != null && r != null && r.sharedMaterial != null && r.sharedMaterial.shader != sh)
        {
            Material viejo = r.sharedMaterial;
            Material nuevo = new Material(sh) { name = "Agua (sin entrar a los barcos)" };
            if (viejo.HasProperty("_BaseColor")) nuevo.SetColor("_BaseColor", viejo.GetColor("_BaseColor"));
            if (viejo.HasProperty("_Smoothness")) nuevo.SetFloat("_Smoothness", viejo.GetFloat("_Smoothness"));
            r.sharedMaterial = nuevo;
        }
        enGPU = r != null && r.sharedMaterial != null && r.sharedMaterial.shader == sh && sh != null
                && SystemInfo.SupportsTextureFormat(TextureFormat.RFloat);
        if (enGPU)
        {
            texCalma = new Texture2D(lado, lado, TextureFormat.RFloat, false, true)
            { name = "Calma del agua", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Shader.SetGlobalTexture(idCalma, texCalma);
            Shader.SetGlobalVector(idRejilla, new Vector4(tamano / 2f, 1f / celda, 1f / lado, 0f));
        }
        Shader.SetGlobalFloat(idGPU, enGPU ? 1f : 0f);
    }

    void OnDestroy()
    {
        if (texCalma != null) Destroy(texCalma);
        Shader.SetGlobalFloat(idGPU, 0f);
    }

    // ---------- Cascos: el agua no se dibuja dentro de los barcos cercanos ----------
    const int MaxCascos = 8;
    static readonly int idALocal = Shader.PropertyToID("_CascoALocal"), idForma = Shader.PropertyToID("_CascoForma"),
                        idForma2 = Shader.PropertyToID("_CascoForma2"), idN = Shader.PropertyToID("_CascosN");
    readonly Matrix4x4[] matrices = new Matrix4x4[MaxCascos];
    readonly Vector4[] formas = new Vector4[MaxCascos], formas2 = new Vector4[MaxCascos];
    readonly System.Collections.Generic.List<(float d, ControlBarco b)> cercanos = new System.Collections.Generic.List<(float, ControlBarco)>();

    void Cascos()
    {
        Camera cam = Camera.main;
        Vector3 c = cam != null ? cam.transform.position : transform.position;
        cercanos.Clear();
        foreach (ControlBarco b in ControlBarco.Todos)
        {
            if (b == null) continue;
            float d = (b.transform.position - c).sqrMagnitude;
            if (d < 400f * 400f) cercanos.Add((d, b));
        }
        cercanos.Sort((a, b) => a.d.CompareTo(b.d));
        int n = 0;
        foreach (var (_, b) in cercanos)
        {
            if (n >= MaxCascos) break;
            if (Cubierta.EsPlana(b)) continue;   // la balsa no tiene casco cerrado
            VidaBarco v = b.GetComponent<VidaBarco>();
            if (v != null && v.Hundido) continue;   // el que se hunde sí se llena de agua
            BoxCollider col = b.GetComponent<BoxCollider>();
            if (col == null) continue;
            Cubierta.Medidas(col, out float w, out float h, out float L);
            matrices[n] = b.transform.worldToLocalMatrix;
            formas[n] = new Vector4(w / 2f, h, CascoProcedural.ZPopa(L), CascoProcedural.ZProa(w, L));
            formas2[n] = new Vector4(0.3f * L - 0.6f * w, 0.06f, 0f, 0f);
            n++;
        }
        Shader.SetGlobalMatrixArray(idALocal, matrices);
        Shader.SetGlobalVectorArray(idForma, formas);
        Shader.SetGlobalVectorArray(idForma2, formas2);
        Shader.SetGlobalInt(idN, n);
    }

    void LateUpdate()
    {
        Vector3 centro = seguir != null ? seguir.position : Vector3.zero;
        float nx = Mathf.Round(centro.x / celda) * celda;
        float nz = Mathf.Round(centro.z / celda) * celda;
        if (nx != ox || nz != oz || Olas.IslasCambiaron)
        {
            ox = nx; oz = nz;
            Olas.IslasCambiaron = false;
            transform.position = new Vector3(ox, 0f, oz);
            Olas.ActualizarCercanas(ox, oz, tamano * 0.75f);
            // La calma depende solo del lugar: se calcula al moverse de casilla, no en cada frame
            for (int z = 0; z < lado; z++)
            for (int x = 0; x < lado; x++)
                calma[z * lado + x] = Olas.Calma(gx[x] + ox, gz[z] + oz);
            if (enGPU) { texCalma.SetPixelData(calma, 0); texCalma.Apply(false); }
        }

        float t = Time.time;
        float inten = Olas.intensidad;
        if (enGPU)
        {
            Shader.SetGlobalFloat(idT, t);
            Shader.SetGlobalFloat(idInten, inten);
            Cascos();
            return;
        }
        for (int z = 0; z < lado; z++)
        {
            float wz = gz[z] + oz;
            for (int x = 0; x < lado; x++)
            {
                int i = z * lado + x;
                alturas[i] = Olas.OlaBase(gx[x] + ox, wz, t) * inten * calma[i];
            }
        }

        int k = 0;
        for (int z = 0; z < resolucion; z++)
        {
            int f0 = z * lado, f1 = (z + 1) * lado;
            for (int x = 0; x < resolucion; x++)
            {
                float a = alturas[f0 + x], b = alturas[f1 + x], c = alturas[f1 + x + 1], d = alturas[f0 + x + 1];
                verts[k].y = a; verts[k + 1].y = b; verts[k + 2].y = c;
                verts[k + 3].y = a; verts[k + 4].y = c; verts[k + 5].y = d;
                // Normal de cada cara: (a,b,c) y (a,c,d) sobre un cuadro de lado "celda"
                Vector3 n1 = new Vector3(b - c, celda, a - b).normalized;
                Vector3 n2 = new Vector3(a - d, celda, d - c).normalized;
                normales[k] = n1; normales[k + 1] = n1; normales[k + 2] = n1;
                normales[k + 3] = n2; normales[k + 4] = n2; normales[k + 5] = n2;
                k += 6;
            }
        }
        malla.SetVertices(verts);
        malla.SetNormals(normales);
        Cascos();
    }
}
