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
    Dictionary<string, float> mercado = new Dictionary<string, float>(); // lo que movió la oferta (1 = normal)

    // El mercado de cada isla se recuerda (por su semilla) aunque la isla se descargue al alejarte:
    // así no se puede bajar y subir los precios yéndose y volviendo. También se guarda con la partida.
    public static readonly Dictionary<int, (Dictionary<string, float> mercado, float hora)> Recordados = new Dictionary<int, (Dictionary<string, float>, float)>();
    float tRecuperar;
    bool avisado;

    void OnEnable() => Todos.Add(this);
    void OnDisable() { Todos.Remove(this); if (semilla != 0) Recordados[semilla] = (mercado, Time.time); }

    void Awake()
    {
        var rnd = new System.Random(semilla);
        foreach (string k in PrecioBase.Keys) { region[k] = 0.7f + (float)rnd.NextDouble() * 0.7f; mercado[k] = 1f; }
        if (semilla == 0) return;   // aún sin semilla (Iniciar la pone y vuelve a llamar a Awake)
        if (Recordados.TryGetValue(semilla, out var r))
        {
            mercado = r.mercado;
            foreach (string k in PrecioBase.Keys) if (!mercado.ContainsKey(k)) mercado[k] = 1f;
            // Lo que se recuperó mientras no estabas (1 % cada 20 s)
            float pasos = Mathf.Max(0f, Time.time - r.hora) / 20f;
            var claves = new List<string>(mercado.Keys);
            foreach (string k in claves) mercado[k] = Mathf.MoveTowards(mercado[k], 1f, Recuperacion * pasos);
        }
        Recordados[semilla] = (mercado, Time.time);
    }

    // Desde el guardado: el mercado de esa isla como quedó
    public static void Restaurar(int s, Dictionary<string, float> dic)
    {
        Recordados[s] = (dic, Time.time);
        foreach (Mercader m in Todos) if (m != null && m.semilla == s) m.mercado = dic;
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

    // ---------- Ventas y compras por cantidad (ventana del mercado) ----------
    // Lo que se mueve de una vez: cada lote mueve el precio un 3 %, así vender 50 maderas baja el precio igual que antes 5 tratos de 10
    public static readonly string[] SeVende = { Inventario.Madera, Inventario.Palo, Inventario.Fibra, Inventario.Piedra, Inventario.Cuerda, Inventario.Mineral, Inventario.Lingote, Inventario.Canon, Inventario.Coco, Inventario.Fruta };
    public static readonly string[] SeCompra = { Inventario.Canon, Inventario.Lingote, Inventario.Cuerda, Inventario.Madera, Inventario.Coco, Inventario.Fruta };
    public static int Lote(string tipo) =>
        tipo == Inventario.Lingote || tipo == Inventario.Canon ? 1
        : tipo == Inventario.Cuerda || tipo == Inventario.Mineral || tipo == Inventario.Coco || tipo == Inventario.Fruta ? 5 : 10;

    // Total de vender (o comprar) n unidades; con aplicar = true además mueve los precios del mercado
    public int Total(string tipo, int n, bool vender, bool aplicar)
    {
        if (n <= 0 || !PrecioBase.ContainsKey(tipo)) return 0;
        float r = region.TryGetValue(tipo, out float x) ? x : 1f;
        float mk = mercado.TryGetValue(tipo, out float y) ? y : 1f;
        int lote = Lote(tipo), total = 0;
        while (n > 0)
        {
            int q = Mathf.Min(lote, n);
            float precio = PrecioBase[tipo] * r * mk * q * (vender ? 1f : Margen);
            total += Mathf.Max(1, Mathf.RoundToInt(precio));
            mk = vender ? Mathf.Max(0.5f, mk - CambioPorTrato) : Mathf.Min(1.6f, mk + CambioPorTrato);
            n -= q;
        }
        if (aplicar) mercado[tipo] = mk;
        return total;
    }

    // Precio de una unidad ahora mismo (para mostrar)
    public float Unidad(string tipo, bool vender) => PrecioBase.ContainsKey(tipo) ? PrecioBase[tipo] * Factor(tipo) * (vender ? 1f : Margen) : 0f;

    // Cuántas unidades se pueden comprar con este oro
    public int MaxCompra(string tipo, int oro)
    {
        if (!PrecioBase.ContainsKey(tipo)) return 0;
        float r = region.TryGetValue(tipo, out float x) ? x : 1f;
        float mk = mercado.TryGetValue(tipo, out float y) ? y : 1f;
        int lote = Lote(tipo), n = 0;
        while (n < 999)
        {
            int q = lote;
            while (q > 0 && Mathf.Max(1, Mathf.RoundToInt(PrecioBase[tipo] * r * mk * q * Margen)) > oro) q--;
            if (q <= 0) break;
            oro -= Mathf.Max(1, Mathf.RoundToInt(PrecioBase[tipo] * r * mk * q * Margen));
            n += q;
            if (q < lote) break;
            mk = Mathf.Min(1.6f, mk + CambioPorTrato);
        }
        return Mathf.Min(n, 999);
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
        if (cerca && !avisado) Notificaciones.Mostrar("Mercader de " + isla + ": " + Teclas.Nombre(Accion.Fabricar) + " para vender y comprar", Notificaciones.Tipo.Comercio, 5f, "mercader");
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
