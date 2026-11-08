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
        }

        float t = Time.time;
        float inten = Olas.intensidad;
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
    }
}
