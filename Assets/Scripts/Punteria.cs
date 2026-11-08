using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Puntería de cañones al estilo Black Flag.
// Al timón, mantén clic derecho y mira hacia un lado del barco (babor, estribor, proa o popa):
// aparece el "fantasma" con el arco de cada bala y el punto donde va a caer. Mirar más arriba
// o más abajo cambia el alcance. Clic izquierdo dispara ese lado.
// En el celular el fantasma sale solo cuando miras hacia un lado que tiene cañones.
public class Punteria : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("Puntería").AddComponent<Punteria>();

    public static bool Apuntando { get; private set; }
    public static LadoCanon LadoActual { get; private set; }
    // Dónde caen las balas (centro de la zona): la cámara lo sigue mientras apuntas
    public static Vector3 PuntoCaida { get; private set; }
    // Hacia dónde mira el costado elegido (perpendicular al barco en babor y estribor)
    public static Vector3 DireccionLado { get; private set; } = Vector3.forward;
    // Si el lado al que apuntas tiene cañones (la cámara solo se engancha en ese caso)
    public static bool HayCanonesEnLado { get; private set; }
    // Prueba: deja la puntería activa sin mantener el clic derecho (tecla T en TeclasPrueba)
    public static bool forzar;

    // Alcance: elevación extra según la inclinación de la cámara (mirar al horizonte = lejos)
    // Alcance: mover el mouse hacia arriba (o arrastrar hacia arriba en el celular) sube el tiro
    // Grados: mirar hacia el cielo = arco alto y lejos; hacia el barco = al agua, cerca
    public const float ElevMax = 42f, ElevMin = -35f;
    public static float Elevacion;   // ángulo actual del tiro (la cámara de puntería se inclina con él)
    const float SensMouse = 0.07f, SensDedo = 0.1f;
    float elevApunte;
    const float PasoTiempo = 0.05f;
    const int PasosMax = 160;

    readonly List<LineRenderer> lineas = new List<LineRenderer>();
    readonly List<Transform> marcas = new List<Transform>();
    readonly List<Vector3> puntos = new List<Vector3>();
    Material matLinea, matMarca;
    Canones ultimo;
    bool capturadoAntes;

    void LateUpdate() => capturadoAntes = CamaraBarco.CursorCapturado;

    static bool EsMovil => Application.isMobilePlatform;

    void Update()
    {
        Personaje p = Personaje.Instancia;
        Canones cn = p != null && p.estado == Personaje.Estado.Timon && p.BarcoActual != null ? p.BarcoActual.GetComponent<Canones>() : null;
        Camera cam = Camera.main;
        if (ultimo != null && ultimo != cn) ultimo.anguloApunte = float.NaN;
        ultimo = cn;
        if (cn == null || cam == null || Fabricacion.Abierto) { Apagar(cn); return; }

        // El lado se elige con hacia dónde gira la cámara (mouse a los lados)
        CamaraBarco cb = cam.GetComponent<CamaraBarco>();
        Vector3 mira = cb != null ? cb.DireccionHorizontal : cam.transform.forward;
        LadoCanon lado = LadoDe(cn.transform, mira);
        Vector3 fw = Vector3.ProjectOnPlane(cn.transform.forward, Vector3.up).normalized, rt = Vector3.Cross(Vector3.up, fw);
        DireccionLado = lado == LadoCanon.Babor ? -rt : lado == LadoCanon.Estribor ? rt : lado == LadoCanon.Proa ? fw : -fw;
        bool boton = (Mouse.current != null && Mouse.current.rightButton.isPressed || forzar) && CamaraBarco.CursorCapturado;
        bool auto = EsMovil && cn.EnLado(lado) > 0;
        if (!boton && !auto) { Apagar(cn); return; }

        // Cada vez que empiezas a apuntar, el tiro arranca en el punto más cercano al barco
        if (!Apuntando) elevApunte = AnguloMasCercano(cn, lado);
        Apuntando = true;
        LadoActual = lado;
        HayCanonesEnLado = cn.EnLado(lado) > 0;

        // Mouse arriba = tiro más alto y más lejos; mouse abajo = más corto
        float dy = 0f;
        if (Mouse.current != null && CamaraBarco.CursorCapturado) dy += Mouse.current.delta.ReadValue().y * SensMouse;
        var pantalla = Touchscreen.current;
        if (pantalla != null && pantalla.primaryTouch.isInProgress) dy += pantalla.primaryTouch.delta.ReadValue().y * SensDedo;
        elevApunte = Mathf.Clamp(elevApunte + dy, ElevMin, ElevMax);
        cn.anguloApunte = elevApunte;
        Elevacion = elevApunte;

        Dibujar(cn, lado);

        // (el clic que vuelve a capturar el cursor no dispara)
        if (boton && capturadoAntes && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (cn.EnLado(lado) == 0) p.Avisar(cn.RanurasLado(lado) == 0 ? "No hay cañones de " + Canones.Nombre(lado) + " en este barco" : "No tienes cañones a " + Canones.Nombre(lado) + ". Instálalos en Tab → Tu barco");
            else if (!cn.Disparar(lado)) p.Avisar("Recargando " + Canones.Nombre(lado) + "…");
        }
    }

    void Apagar(Canones cn)
    {
        Apuntando = false;
        HayCanonesEnLado = false;
        if (cn != null) cn.anguloApunte = float.NaN;
        Mostrar(0);
    }

    // Ángulo con el que la bala cae lo más cerca posible del barco (sin pegarle al propio casco)
    float AnguloMasCercano(Canones cn, LadoCanon lado)
    {
        Transform[] r = cn.Ranuras(lado);
        if (r == null || r.Length == 0 || r[0] == null) return ElevMin;
        float guardado = cn.anguloApunte;
        float elegido = ElevMin;
        for (float a = ElevMin; a <= 10f; a += 5f)
        {
            cn.anguloApunte = a;
            Vector3 fin = Trayectoria(cn, cn.Salida(r[0]), cn.VelocidadInicial(r[0]));
            Vector3 d = fin - r[0].position; d.y = 0f;
            if (d.magnitude > 3f) { elegido = a; break; }
        }
        cn.anguloApunte = guardado;
        return elegido;
    }

    // Hacia qué lado del barco mira la cámara
    public static LadoCanon LadoDe(Transform barco, Vector3 mira)
    {
        Vector3 f = Vector3.ProjectOnPlane(mira, Vector3.up);
        Vector3 b = Vector3.ProjectOnPlane(barco.forward, Vector3.up);
        float a = Vector3.SignedAngle(b, f, Vector3.up);
        if (a > -45f && a < 45f) return LadoCanon.Proa;
        if (a >= 45f && a <= 135f) return LadoCanon.Estribor;
        if (a <= -45f && a >= -135f) return LadoCanon.Babor;
        return LadoCanon.Popa;
    }

    // 1 cañón: una barra fina que apunta. 2 o más: una cinta entre el primero y el último,
    // que se abre más mientras más cañones tenga ese lado (como en Black Flag).
    void Dibujar(Canones cn, LadoCanon lado)
    {
        Transform[] r = cn.Ranuras(lado);
        int instalados = cn.EnLado(lado);
        bool listo = cn.Recarga(lado) <= 0f;
        Color c = instalados == 0 ? new Color(0.7f, 0.7f, 0.7f, 0.35f)
                : listo ? new Color(1f, 0.95f, 0.8f, 0.75f) : new Color(1f, 0.55f, 0.3f, 0.6f);
        BoxCollider bc = cn.GetComponent<BoxCollider>();
        float ancho = Mathf.Clamp(bc != null ? bc.size.z / 60f : 0.3f, 0.12f, 0.45f);
        if (r == null || r.Length == 0) { Mostrar(0); return; }

        if (instalados <= 1)
        {
            // Barra: un solo cañón (o fantasma gris si no hay ninguno)
            Mostrar(1);
            Cinta(false);
            Vector3 fin = Trayectoria(cn, cn.Salida(r[0]), cn.VelocidadInicial(r[0]));
            PuntoCaida = fin;
            Linea(lineas[0], puntos, c, ancho * 0.6f, ancho * 1.4f);
            Transform m = marcas[0];
            m.position = fin + Vector3.up * 0.15f;
            m.rotation = Quaternion.identity;
            float tam = 2.2f + ancho * 6f;
            m.localScale = new Vector3(tam, 0.02f, tam);
            m.GetComponent<Renderer>().material.color = new Color(c.r, c.g * 0.7f, c.b * 0.5f, c.a * 0.8f);
            return;
        }

        // Cinta: los bordes salen de los cañones instalados que están más adelante y más atrás del barco
        // (con varias filas, se usa la fila de arriba de cada extremo para que los dos bordes sean parejos)
        Mostrar(2);
        int n = Mathf.Min(instalados, r.Length);
        int iA = 0, iB = 0;
        float zMin = float.MaxValue, zMax = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (r[i] == null) continue;
            Vector3 l = cn.transform.InverseTransformPoint(r[i].position);
            if (l.z < zMin - 0.01f || (Mathf.Abs(l.z - zMin) <= 0.01f && r[i].position.y > r[iA].position.y)) { zMin = l.z; iA = i; }
            if (l.z > zMax + 0.01f || (Mathf.Abs(l.z - zMax) <= 0.01f && r[i].position.y > r[iB].position.y)) { zMax = l.z; iB = i; }
        }
        Vector3 inicioA = cn.Salida(r[iA]), inicioB = cn.Salida(r[iB]);
        float altura = Mathf.Max(inicioA.y, inicioB.y);
        inicioA.y = altura; inicioB.y = altura;
        Trayectoria(cn, inicioA, cn.VelocidadInicial(r[iA]));
        Remuestrear(puntos, bordeA);
        Trayectoria(cn, inicioB, cn.VelocidadInicial(r[iB]));
        Remuestrear(puntos, bordeB);

        // Se abre un poco hacia el final: más cañones, cinta algo más ancha (con tope, para que no se dispare)
        Vector3 lat = cn.transform.forward; lat.y = 0f; lat.Normalize();
        if (Vector3.Dot(bordeB[Muestras - 1] - bordeA[Muestras - 1], lat) < 0f) lat = -lat;
        float apertura = Mathf.Min(0.8f + 0.25f * (n - 1), 5f);
        for (int k = 0; k < Muestras; k++)
        {
            float t = (float)k / (Muestras - 1);
            bordeA[k] -= lat * apertura * t;
            bordeB[k] += lat * apertura * t;
        }

        Cinta(true);
        ArmarCinta(c);
        Linea(lineas[0], bordeA, c, ancho * 0.4f, ancho * 0.9f);
        Linea(lineas[1], bordeB, c, ancho * 0.4f, ancho * 0.9f);

        // Zona de caída: una franja entre los dos bordes
        Vector3 a = bordeA[Muestras - 1], b = bordeB[Muestras - 1];
        PuntoCaida = (a + b) / 2f;
        Transform zona = marcas[0];
        zona.position = (a + b) / 2f + Vector3.up * 0.15f;
        Vector3 d = b - a; d.y = 0f;
        zona.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward);
        zona.localScale = new Vector3(3f + ancho * 4f, 0.02f, d.magnitude + 3f);
        zona.GetComponent<Renderer>().material.color = new Color(c.r, c.g * 0.7f, c.b * 0.5f, c.a * 0.7f);
        if (marcas[1].gameObject.activeSelf) marcas[1].gameObject.SetActive(false);
    }

    const int Muestras = 40;
    readonly Vector3[] bordeA = new Vector3[Muestras], bordeB = new Vector3[Muestras];
    readonly Vector3[] vertsCinta = new Vector3[Muestras * 2];
    readonly Color[] coloresCinta = new Color[Muestras * 2];
    MeshFilter cintaMf;
    Mesh cintaMesh;

    static void Linea(LineRenderer lr, IList<Vector3> pts, Color c, float a0, float a1)
    {
        lr.positionCount = pts.Count;
        for (int k = 0; k < pts.Count; k++) lr.SetPosition(k, pts[k]);
        lr.startColor = new Color(c.r, c.g, c.b, c.a * 0.4f);
        lr.endColor = c;
        lr.startWidth = a0;
        lr.endWidth = a1;
    }

    // Reparte la trayectoria en Muestras puntos (así los dos bordes de la cinta se emparejan)
    static void Remuestrear(List<Vector3> src, Vector3[] dst)
    {
        for (int k = 0; k < dst.Length; k++)
        {
            float f = (float)k / (dst.Length - 1) * (src.Count - 1);
            int i0 = Mathf.FloorToInt(f), i1 = Mathf.Min(i0 + 1, src.Count - 1);
            dst[k] = Vector3.Lerp(src[i0], src[i1], f - i0);
        }
    }

    void Cinta(bool ver)
    {
        if (cintaMf == null)
        {
            if (!ver) return;
            GameObject g = new GameObject("Cinta de puntería");
            g.transform.SetParent(transform, false);
            cintaMf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matLinea;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            cintaMesh = new Mesh { name = "Cinta" };
            cintaMesh.MarkDynamic();
            var tris = new int[(Muestras - 1) * 6];
            for (int k = 0, t = 0; k < Muestras - 1; k++)
            {
                int a = k * 2, b = a + 1, c2 = a + 2, d = a + 3;
                tris[t++] = a; tris[t++] = c2; tris[t++] = b;
                tris[t++] = b; tris[t++] = c2; tris[t++] = d;
            }
            cintaMesh.vertices = vertsCinta;
            cintaMesh.triangles = tris;
            cintaMf.sharedMesh = cintaMesh;
        }
        if (cintaMf.gameObject.activeSelf != ver) cintaMf.gameObject.SetActive(ver);
    }

    void ArmarCinta(Color c)
    {
        for (int k = 0; k < Muestras; k++)
        {
            float t = (float)k / (Muestras - 1);
            vertsCinta[k * 2] = bordeA[k];
            vertsCinta[k * 2 + 1] = bordeB[k];
            Color ck = new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, c.a * 0.45f, t));
            coloresCinta[k * 2] = ck;
            coloresCinta[k * 2 + 1] = ck;
        }
        cintaMesh.vertices = vertsCinta;
        cintaMesh.colors = coloresCinta;
        cintaMesh.RecalculateBounds();
    }

    // Sigue la bala paso a paso (misma gravedad que el juego) hasta que toca el agua o algo sólido
    Vector3 Trayectoria(Canones cn, Vector3 p, Vector3 v)
    {
        puntos.Clear();
        puntos.Add(p);
        Vector3 g = Physics.gravity;
        for (int i = 0; i < PasosMax; i++)
        {
            Vector3 q = p + v * PasoTiempo + 0.5f * g * PasoTiempo * PasoTiempo;
            v += g * PasoTiempo;
            if (Physics.Linecast(p, q, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore) && !cn.EsPropio(hit.collider)
                && hit.collider.GetComponentInParent<Personaje>() == null)
            {
                puntos.Add(hit.point);
                return hit.point;
            }
            if (v.y < 0f && q.y < Olas.Altura(q))
            {
                Vector3 agua = new Vector3(q.x, Olas.Altura(q), q.z);
                puntos.Add(agua);
                return agua;
            }
            puntos.Add(q);
            p = q;
        }
        return p;
    }

    // Asegura n líneas y marcas visibles; oculta las demás
    void Mostrar(int n)
    {
        if (n == 0) Cinta(false);
        while (lineas.Count < n) Nueva();
        for (int i = 0; i < lineas.Count; i++)
        {
            bool ver = i < n;
            if (lineas[i].enabled != ver) lineas[i].enabled = ver;
            if (marcas[i].gameObject.activeSelf != ver) marcas[i].gameObject.SetActive(ver);
        }
    }

    void Nueva()
    {
        if (matLinea == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            matLinea = new Material(sh);
            matMarca = new Material(sh);
        }
        GameObject g = new GameObject("Fantasma " + lineas.Count);
        g.transform.SetParent(transform, false);
        LineRenderer lr = g.AddComponent<LineRenderer>();
        lr.sharedMaterial = matLinea;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;
        lineas.Add(lr);

        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m.name = "Caída " + marcas.Count;
        DestroyImmediate(m.GetComponent<Collider>());
        m.transform.SetParent(transform, false);
        Renderer rm = m.GetComponent<Renderer>();
        rm.material = matMarca;
        rm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m.SetActive(false);
        marcas.Add(m.transform);
    }
}
