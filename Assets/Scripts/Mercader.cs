using System.Collections.Generic;
using UnityEngine;

// Mercader de una isla: compra lo que traes y vende lo que necesitas, a cambio de oro.
// Como en Albion, cada isla tiene sus precios: lo que abunda en una es barato y en otra es caro,
// así que vale la pena llevar cosas de una isla a otra. Si vendes mucho de lo mismo, el precio baja
// (y se recupera con el tiempo); si compras mucho, sube.
// Lo arma GeneradorIsla: siempre en la isla inicial y en más o menos una de cada tres islas.
public class Mercader : MonoBehaviour
{
    public static readonly List<Mercader> Todos = new List<Mercader>();
    public const float Alcance = 6f;
    public const string Oro = "Oro";

    // Precio base de una unidad, en oro
    public static readonly Dictionary<string, float> PrecioBase = new Dictionary<string, float>
    {
        { Inventario.Madera, 2f }, { Inventario.Palo, 1f }, { Inventario.Fibra, 1f }, { Inventario.Piedra, 1f },
        { Inventario.Cuerda, 4f }, { Inventario.Mineral, 5f }, { Inventario.Lingote, 14f }, { Inventario.Canon, 60f },
        { Inventario.Coco, 3f }, { Inventario.Fruta, 3f },
    };
    const float Margen = 1.3f;          // el mercader vende más caro de lo que compra
    const float CambioPorTrato = 0.03f; // cada venta baja el precio 3 % (cada compra lo sube)
    const float Recuperacion = 0.01f;   // vuelve 1 % hacia lo normal cada 20 s

    public string isla = "la isla";
    public int semilla;

    readonly Dictionary<string, float> region = new Dictionary<string, float>();  // carácter de la isla (0.7 a 1.4)
    readonly Dictionary<string, float> mercado = new Dictionary<string, float>(); // lo que movió la oferta (1 = normal)
    float tRecuperar;
    bool avisado;

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Awake()
    {
        var rnd = new System.Random(semilla);
        foreach (string k in PrecioBase.Keys) { region[k] = 0.7f + (float)rnd.NextDouble() * 0.7f; mercado[k] = 1f; }
    }

    // Se llama después de poner la semilla (al crearlo desde código)
    public void Iniciar(string nombreIsla, int s)
    {
        isla = nombreIsla; semilla = s;
        Awake();
    }

    public static Mercader Cercano(Vector3 p)
    {
        foreach (Mercader m in Todos)
        {
            if (m == null) continue;
            Vector3 d = m.transform.position - p; d.y = 0f;
            if (d.magnitude <= Alcance && Mathf.Abs(m.transform.position.y - p.y) < 4f) return m;
        }
        return null;
    }

    float Factor(string tipo) => (region.TryGetValue(tipo, out float r) ? r : 1f) * (mercado.TryGetValue(tipo, out float m) ? m : 1f);

    // Lo que te paga por n unidades
    public int PrecioVenta(string tipo, int n) => Mathf.Max(1, Mathf.RoundToInt(PrecioBase[tipo] * Factor(tipo) * n));
    // Lo que te cobra por n unidades
    public int PrecioCompra(string tipo, int n) => Mathf.Max(1, Mathf.RoundToInt(PrecioBase[tipo] * Factor(tipo) * Margen * n));

    // "barato aquí", "caro aquí" o nada
    public string Etiqueta(string tipo)
    {
        float f = Factor(tipo);
        if (f < 0.85f) return "barato aquí";
        if (f > 1.2f) return "caro aquí";
        return "";
    }

    public void Vendiste(string tipo) => mercado[tipo] = Mathf.Max(0.5f, mercado[tipo] - CambioPorTrato);
    public void Compraste(string tipo) => mercado[tipo] = Mathf.Min(1.6f, mercado[tipo] + CambioPorTrato);

    void Update()
    {
        tRecuperar += Time.deltaTime;
        if (tRecuperar >= 20f)
        {
            tRecuperar = 0f;
            var claves = new List<string>(mercado.Keys);
            foreach (string k in claves) mercado[k] = Mathf.MoveTowards(mercado[k], 1f, Recuperacion);
        }

        // Aviso al llegar
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Vector3 d = transform.position - p.transform.position; d.y = 0f;
        bool cerca = d.magnitude <= Alcance;
        if (cerca && !avisado) p.Avisar("Mercader de " + isla + ": Tab para vender y comprar");
        avisado = cerca;
    }

    // Un puesto en la playa: mesa, toldo y el mercader
    public static Mercader Crear(Transform isla, Vector3 posLocal, float rumbo, string nombreIsla, int semilla, Material madera, Material toldo, Material ropa)
    {
        GameObject g = new GameObject("Mercader");
        g.transform.SetParent(isla, false);
        g.transform.localPosition = posLocal;
        g.transform.localRotation = Quaternion.Euler(0f, rumbo, 0f);
        Transform t = g.transform;
        Pieza(PrimitiveType.Cube, "Mesa", t, new Vector3(0f, 0.5f, 1.2f), new Vector3(2.4f, 0.12f, 0.9f), madera, true);
        Pieza(PrimitiveType.Cube, "Pata", t, new Vector3(-1.1f, 0.25f, 1.2f), new Vector3(0.12f, 0.5f, 0.8f), madera, false);
        Pieza(PrimitiveType.Cube, "Pata", t, new Vector3(1.1f, 0.25f, 1.2f), new Vector3(0.12f, 0.5f, 0.8f), madera, false);
        foreach (float x in new[] { -1.3f, 1.3f })
        foreach (float z in new[] { 0.3f, 1.9f })
            Pieza(PrimitiveType.Cylinder, "Poste", t, new Vector3(x, 1.2f, z), new Vector3(0.1f, 1.2f, 0.1f), madera, false);
        Pieza(PrimitiveType.Cube, "Toldo", t, new Vector3(0f, 2.45f, 1.1f), new Vector3(2.9f, 0.08f, 2.0f), toldo, false);
        Pieza(PrimitiveType.Capsule, "Mercader", t, new Vector3(0f, 0.9f, 0.4f), new Vector3(0.6f, 0.9f, 0.6f), ropa, true);
        Pieza(PrimitiveType.Cylinder, "Sombrero", t, new Vector3(0f, 1.85f, 0.4f), new Vector3(0.7f, 0.05f, 0.7f), madera, false);
        // Mercancía sobre la mesa
        Pieza(PrimitiveType.Cube, "Caja", t, new Vector3(-0.6f, 0.75f, 1.2f), new Vector3(0.5f, 0.4f, 0.5f), madera, false);
        Pieza(PrimitiveType.Sphere, "Fruta", t, new Vector3(0.5f, 0.68f, 1.2f), new Vector3(0.3f, 0.25f, 0.3f), toldo, false);
        Mercader m = g.AddComponent<Mercader>();
        m.Iniciar(nombreIsla, semilla);
        return m;
    }

    static void Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat, bool colision)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!colision) Object.DestroyImmediate(g.GetComponent<Collider>());
    }
}
