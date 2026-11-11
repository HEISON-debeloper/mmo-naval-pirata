using System.Collections.Generic;
using UnityEngine;

// Pecios: todo barco que se hunde cerca de ti queda en el mar para siempre. Se ve el palo roto asomando del agua
// (con su verga y jirones de vela) y queda marcado en la carta. Con el tiempo, las rutas de combate se vuelven cementerios.
// Se guardan con la partida (hasta 300; si hay más, se olvidan los más viejos).
public class Pecios : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() { if (I == null) new GameObject("Pecios").AddComponent<Pecios>(); }

    [System.Serializable]
    public class Pecio { public string nombre; public Vector3 pos; public float largo, rumbo; public int dia; }

    public static Pecios I { get; private set; }
    public static readonly List<Pecio> Todos = new List<Pecio>();
    const int Maximo = 300;
    const float DistanciaVer = 600f;    // se arman los restos visibles a menos de esto
    const float DistanciaTestigo = 700f; // solo se recuerdan los que hundiste o viste hundirse

    readonly Dictionary<Pecio, GameObject> visibles = new Dictionary<Pecio, GameObject>();
    static Material madera, vela;
    float tRevisar;

    void Awake() { I = this; OrigenFlotante.AlMover += Correr; }
    void Correr(Vector3 c) { foreach (Pecio p in Todos) p.pos -= c; }   // los restos visibles se mueven solos (son objetos del mundo)

    void OnDestroy()
    {
        if (I == this) I = null;
        OrigenFlotante.AlMover -= Correr;
        foreach (var kv in visibles) if (kv.Value != null) Destroy(kv.Value);
        visibles.Clear();
    }

    // Lo llama VidaBarco al hundirse un barco
    public static void Registrar(GameObject barco, float largo)
    {
        Personaje p = Personaje.Instancia;
        if (barco == null || p == null) return;
        Vector3 pos = barco.transform.position;
        if (Vector3.Distance(p.transform.position, pos) > DistanciaTestigo) return;
        string nombre = barco.name.Replace("(Clone)", "").Trim();
        Todos.Add(new Pecio { nombre = nombre, pos = new Vector3(pos.x, 0f, pos.z), largo = Mathf.Max(4f, largo), rumbo = barco.transform.eulerAngles.y, dia = 0 });
        while (Todos.Count > Maximo) { Pecio viejo = Todos[0]; Todos.RemoveAt(0); I?.Quitar(viejo); }
        Notificaciones.Mostrar("Pecio de " + nombre + ": quedó marcado en tu carta", Notificaciones.Tipo.Descubrimiento, 4f, "barco");
    }

    // Al cargar la partida
    public static void Fijar(List<Pecio> lista)
    {
        I?.QuitarTodos();
        Todos.Clear();
        if (lista != null) foreach (Pecio p in lista) { p.pos = OrigenFlotante.ALocal(p.pos); Todos.Add(p); }
    }

    void QuitarTodos() { foreach (var kv in visibles) if (kv.Value != null) Destroy(kv.Value); visibles.Clear(); }
    void Quitar(Pecio p) { if (visibles.TryGetValue(p, out GameObject g)) { if (g != null) Destroy(g); visibles.Remove(p); } }

    void Update()
    {
        tRevisar -= Time.deltaTime;
        if (tRevisar > 0f) return;
        tRevisar = 1f;
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 c = cam.transform.position; c.y = 0f;
        foreach (Pecio p in Todos)
        {
            bool cerca = (p.pos - c).sqrMagnitude < DistanciaVer * DistanciaVer;
            bool hay = visibles.TryGetValue(p, out GameObject g) && g != null;
            if (cerca && !hay) visibles[p] = Armar(p);
            else if (!cerca && hay) { Destroy(g); visibles.Remove(p); }
        }
    }

    // Lo que asoma del agua: el palo mayor roto e inclinado, una verga caída y jirones de vela
    GameObject Armar(Pecio p)
    {
        if (madera == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            madera = new Material(sh) { name = "Madera de pecio", color = new Color(0.16f, 0.11f, 0.07f) };
            vela = new Material(sh) { name = "Vela podrida", color = new Color(0.42f, 0.38f, 0.30f) };
            if (vela.HasProperty("_Cull")) vela.SetFloat("_Cull", 0f);   // jirones finos: se ven de los dos lados
        }
        float k = Mathf.Clamp(p.largo / 30f, 0.5f, 1.6f);   // barcos grandes, palos más gruesos
        var raiz = new GameObject("Pecio de " + p.nombre);
        raiz.transform.SetPositionAndRotation(p.pos, Quaternion.Euler(0f, p.rumbo, 0f));
        var semilla = new System.Random(p.pos.GetHashCode());
        float inclina = 12f + (float)semilla.NextDouble() * 22f;

        var palo = Pieza(PrimitiveType.Cylinder, "Palo roto", raiz.transform, madera);
        palo.localScale = new Vector3(0.45f * k, 6f * k, 0.45f * k);   // un cilindro mide 2: 12 m de palo
        palo.localRotation = Quaternion.Euler(inclina, 0f, 8f);
        palo.localPosition = palo.localRotation * new Vector3(0f, -2f * k, 0f);   // la mayor parte bajo el agua

        Vector3 punta = palo.localPosition + palo.localRotation * new Vector3(0f, 6f * k, 0f);
        var verga = Pieza(PrimitiveType.Cylinder, "Verga caída", raiz.transform, madera);
        verga.localScale = new Vector3(0.22f * k, 2.6f * k, 0.22f * k);
        verga.localRotation = Quaternion.Euler(inclina, 0f, 8f) * Quaternion.Euler(0f, 0f, 70f);
        verga.localPosition = punta - palo.localRotation * new Vector3(0f, 1.2f * k, 0f);

        for (int i = 0; i < 2; i++)
        {
            var jiron = Pieza(PrimitiveType.Quad, "Jirón de vela", raiz.transform, vela);
            jiron.localScale = new Vector3((1.2f + i * 0.6f) * k, (1.6f - i * 0.5f) * k, 1f);
            jiron.localRotation = verga.localRotation * Quaternion.Euler(0f, 90f, 90f + i * 25f);
            jiron.localPosition = verga.localPosition + palo.localRotation * new Vector3((i == 0 ? 0.6f : -0.9f) * k, -0.9f * k, 0.1f);
        }
        return raiz;
    }

    static Transform Pieza(PrimitiveType tipo, string nombre, Transform padre, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        Object.Destroy(g.GetComponent<Collider>());   // solo se ven: no estorban al navegar
        g.transform.SetParent(padre, false);
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }
}
