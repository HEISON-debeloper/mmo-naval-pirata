using System.Collections.Generic;
using UnityEngine;

// Pactos malditos. El Caribe arrastra una maldición antigua: en algunas islas lejanas hay altares de piedra con una
// luz verde, y quien llega ahí puede hacer un pacto con el mar. Siempre es una ventaja con su contrapartida.
// Los pactos se atan a tu barco (hasta 3): si el barco se hunde, el mar cobra la deuda y se pierden todos.
// Cada altar ofrece un solo pacto y se apaga cuando lo aceptas. Se guardan con la partida.
public class Pactos : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() { if (I == null) new GameObject("Pactos").AddComponent<Pactos>(); }

    public class Pacto { public string id, nombre, ventaja, precio, lema; }

    public static readonly Pacto[] Lista =
    {
        new Pacto { id = "viento", nombre = "Viento del Ahogado", ventaja = "Las velas empujan un 20 % más.",
                    precio = "El casco se pudre: pierde un poco de vida cada rato (nunca baja de 40 %).", lema = "Los que se ahogaron soplan tus velas… y se llevan astillas." },
        new Pacto { id = "polvora", nombre = "Pólvora de Hueso", ventaja = "Tus balas hacen un 35 % más de daño.",
                    precio = "Los cañones tardan un 25 % más en recargar.", lema = "Huesos molidos de marinos viejos: pesan, pero rompen." },
        new Pacto { id = "coral", nombre = "Casco de Coral", ventaja = "Tu barco recibe un 35 % menos de daño.",
                    precio = "Las velas empujan un 15 % menos: el coral pesa.", lema = "El arrecife te abraza y no te suelta." },
        new Pacto { id = "kraken", nombre = "Sed del Kraken", ventaja = "Los cañones recargan un 30 % más rápido.",
                    precio = "Tu sed baja el doble de rápido.", lema = "Sus brazos cargan la pólvora, pero te secan la boca." },
        new Pacto { id = "oro", nombre = "Oro Maldito", ventaja = "Los barcos que se hunden sueltan el doble de botín.",
                    precio = "Tu hambre baja un 50 % más rápido y la tripulación pierde moral en cada comida.", lema = "Todo el oro brilla… y todo el oro pesa." },
        new Pacto { id = "marea", nombre = "Bendición de la Marea", ventaja = "El casco se repara solo, despacio.",
                    precio = "Tus balas hacen un 25 % menos de daño.", lema = "El mar cura lo que no quiere hundir… todavía." },
    };
    public const int Maximo = 3;

    public static Pactos I { get; private set; }
    public static readonly List<string> Activos = new List<string>();      // atados a tu barco
    public static readonly HashSet<string> Usados = new HashSet<string>(); // altares ya apagados (id de la isla)

    public static bool Tiene(string id) => Activos.Contains(id);
    public static Pacto De(string id) { foreach (Pacto p in Lista) if (p.id == id) return p; return null; }

    // ---------- Efectos (solo en tu barco) ----------
    static bool EsMio(GameObject barco)
    {
        Personaje p = Personaje.Instancia;
        return Activos.Count > 0 && barco != null && p != null && p.barco != null && p.barco.gameObject == barco;
    }
    public static float Velas(GameObject b) => !EsMio(b) ? 1f : (Tiene("viento") ? 1.2f : 1f) * (Tiene("coral") ? 0.85f : 1f);
    public static float Recarga(GameObject b) => !EsMio(b) ? 1f : (Tiene("polvora") ? 1.25f : 1f) * (Tiene("kraken") ? 0.7f : 1f);
    public static float DanoBala(GameObject b) => !EsMio(b) ? 1f : (Tiene("polvora") ? 1.35f : 1f) * (Tiene("marea") ? 0.75f : 1f);
    public static float DanoRecibido(GameObject b) => EsMio(b) && Tiene("coral") ? 0.65f : 1f;
    public static float Sed => Tiene("kraken") ? 2f : 1f;
    public static float Hambre => Tiene("oro") ? 1.5f : 1f;
    public static float Botin => Tiene("oro") ? 2f : 1f;

    public static string Resumen()
    {
        if (Activos.Count == 0) return "";
        var n = new List<string>();
        foreach (string id in Activos) { Pacto p = De(id); if (p != null) n.Add(p.nombre); }
        return string.Join(", ", n);
    }

    public static void Fijar(List<string> activos, List<string> usados)
    {
        Activos.Clear(); Usados.Clear();
        if (activos != null) foreach (string a in activos) if (De(a) != null && Activos.Count < Maximo) Activos.Add(a);
        if (usados != null) foreach (string u in usados) Usados.Add(u);
        I?.RefrescarAltares();
    }

    // ---------- Altares ----------
    readonly Dictionary<Isla, Altar> altares = new Dictionary<Isla, Altar>();
    class Altar { public GameObject obj; public Transform orbe; public Renderer luz; public string isla; public Pacto pacto; public bool apagado; }
    static Material piedra, verde, apagada;
    float tRevisar, tPudre;
    bool hundidoAntes, avisado;
    Altar cerca;

    void Awake() { I = this; }
    void OnDestroy() { if (I == this) I = null; foreach (var kv in altares) if (kv.Value.obj != null) Destroy(kv.Value.obj); }

    static int Hash(string s) { unchecked { int h = 23; foreach (char c in s) h = h * 31 + c; return h & 0x7fffffff; } }

    // Uno de cada 4 islas, lejos del inicio (a más de 1,2 km), tiene altar
    static bool TieneAltar(Isla i) => i.transform.position.magnitude > 1200f && Hash(i.Id) % 4 == 1;

    void Update()
    {
        Personaje p = Personaje.Instancia;
        float dt = Time.deltaTime;
        if (p == null || MenuPrincipal.Visible) return;

        // El barco se hundió: el mar cobra la deuda
        VidaBarco vb = p.barco != null ? p.barco.GetComponent<VidaBarco>() : null;
        bool hundido = vb != null && vb.Hundido;
        if (hundido && !hundidoAntes && Activos.Count > 0)
        {
            Activos.Clear();
            SonidosMundo.Uno("trueno", p.transform.position, 0.8f, 0.7f, 200f);
            Notificaciones.Mostrar("El mar cobró su deuda: tu barco se hundió y perdiste todos tus pactos", Notificaciones.Tipo.Peligro, 7f);
        }
        hundidoAntes = hundido;

        // Viento del Ahogado pudre el casco; la Marea lo repara
        if (vb != null && !hundido)
        {
            if (Tiene("viento")) { tPudre += dt; if (tPudre > 15f) { tPudre = 0f; if (vb.vida > vb.vidaMaxima * 0.4f) vb.vida = Mathf.Max(vb.vidaMaxima * 0.4f, vb.vida - vb.vidaMaxima * 0.01f); } }
            if (Tiene("marea") && vb.vida < vb.vidaMaxima) vb.vida = Mathf.Min(vb.vidaMaxima, vb.vida + vb.vidaMaxima * 0.005f * dt);
        }

        tRevisar -= dt;
        if (tRevisar <= 0f) { tRevisar = 2f; RefrescarAltares(); }

        // El orbe flota y late; junto a un altar encendido, F ofrece el pacto
        cerca = null;
        foreach (var kv in altares)
        {
            Altar a = kv.Value;
            if (a.obj == null) continue;
            if (a.orbe != null) a.orbe.localPosition = new Vector3(0f, 2.3f + Mathf.Sin(Time.time * 1.6f) * 0.12f, 0f);
            Vector3 d = a.obj.transform.position - p.transform.position; d.y = 0f;
            if (!a.apagado && d.magnitude < 3.5f && p.estado == Personaje.Estado.Libre) cerca = a;
        }
        if (cerca != null && !avisado) { avisado = true; p.Avisar(Teclas.Nombre(Accion.Usar) + ": escuchar al altar maldito"); }
        if (cerca == null) avisado = false;
        if (cerca != null && Teclas.Pulsada(Accion.Usar) && !Ventanas.HayModal) VentanaPacto.Ofrecer(cerca.pacto, cerca.isla, cerca.obj.transform.position);
    }

    // Para la tecla de prueba: el altar encendido más cercano
    public static bool AltarCercano(Vector3 desde, out Vector3 donde)
    {
        donde = Vector3.zero;
        if (I == null) return false;
        float mejor = float.MaxValue; bool hay = false;
        foreach (var kv in I.altares)
        {
            if (kv.Value.obj == null || kv.Value.apagado) continue;
            float d = Vector3.Distance(kv.Value.obj.transform.position, desde);
            if (d < mejor) { mejor = d; donde = kv.Value.obj.transform.position; hay = true; }
        }
        return hay;
    }

    internal void RefrescarAltares()
    {
        var quitar = new List<Isla>();
        foreach (var kv in altares) if (kv.Key == null || kv.Value.obj == null) quitar.Add(kv.Key);
        foreach (Isla q in quitar) { if (altares[q].obj != null) Destroy(altares[q].obj); altares.Remove(q); }
        foreach (Isla i in Isla.Todas)
        {
            if (i == null || altares.ContainsKey(i) || !TieneAltar(i)) continue;
            Altar a = Armar(i);
            if (a != null) altares[i] = a;
        }
        foreach (var kv in altares) Encender(kv.Value, !Usados.Contains(kv.Value.isla));
    }

    void Encender(Altar a, bool si)
    {
        a.apagado = !si;
        if (a.luz != null) a.luz.sharedMaterial = si ? verde : apagada;
    }

    Altar Armar(Isla isla)
    {
        if (piedra == null)
        {
            Personaje p = Personaje.Instancia;
            Renderer r = p != null ? p.GetComponentInChildren<Renderer>() : null;
            if (r == null || r.sharedMaterial == null) return null;
            piedra = Modelos.DeColor(r.sharedMaterial, new Color(0.18f, 0.2f, 0.2f));
            verde = Modelos.DeColor(r.sharedMaterial, new Color(0.25f, 1f, 0.45f), true);
            apagada = Modelos.DeColor(r.sharedMaterial, new Color(0.3f, 0.32f, 0.3f));
        }
        // Un punto en tierra firme de la isla (se prueban varios, de afuera hacia el centro)
        int h = Hash(isla.Id);
        float ang = (h % 360) * Mathf.Deg2Rad;
        Vector3 c = isla.transform.position;
        Vector3? punto = null;
        foreach (float k in new[] { 0.6f, 0.45f, 0.3f, 0.15f })
        {
            Vector3 arriba = c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * isla.radio * k + Vector3.up * 300f;
            if (Physics.Raycast(arriba, Vector3.down, out RaycastHit hit, 600f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Isla>() == isla && hit.point.y > 0.6f)
            { punto = hit.point; break; }
        }
        if (punto == null) return null;

        var raiz = new GameObject("Altar maldito");
        raiz.transform.SetParent(isla.transform, true);
        raiz.transform.SetPositionAndRotation(punto.Value, Quaternion.Euler(0f, h % 90, 0f));
        Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(0f, 0.2f, 0f), new Vector3(1.8f, 0.4f, 1.8f), piedra, true);
        Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 1.2f, 0.7f), piedra, true).localRotation = Quaternion.Euler(0f, 45f, 0f);
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.PI / 2f + 0.4f;
            Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(Mathf.Cos(a) * 1.8f, 0.7f, Mathf.Sin(a) * 1.8f), new Vector3(0.35f, 1.4f + (k % 2) * 0.5f, 0.3f), piedra, true)
                .localRotation = Quaternion.Euler(k * 6f - 8f, a * Mathf.Rad2Deg, 4f);
        }
        Transform orbe = Pieza(PrimitiveType.Sphere, raiz.transform, new Vector3(0f, 2.3f, 0f), Vector3.one * 0.55f, verde, false);
        var luz = orbe.gameObject.AddComponent<Light>();
        luz.color = new Color(0.3f, 1f, 0.5f); luz.range = 9f; luz.intensity = 1.6f;
        return new Altar { obj = raiz, orbe = orbe, luz = orbe.GetComponent<Renderer>(), isla = isla.Id, pacto = Lista[(h / 7) % Lista.Length] };
    }

    static Transform Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material m, bool solida)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        if (!solida) Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }

    // Lo llama la ventana del pacto
    public static string Aceptar(Pacto p, string isla, Vector3 pos)
    {
        Personaje pj = Personaje.Instancia;
        if (pj == null || pj.barco == null) return "Los pactos se atan a un barco: necesitas uno propio";
        if (Tiene(p.id)) return "Ya tienes ese pacto";
        if (Activos.Count >= Maximo) return "Tu barco ya carga " + Maximo + " pactos: el mar no presta más";
        Activos.Add(p.id);
        Usados.Add(isla);
        I?.RefrescarAltares();
        SonidosMundo.Uno("trueno", pos, 0.9f, 0.6f, 150f);
        Notificaciones.Mostrar("Pacto sellado: " + p.nombre + ". " + p.ventaja, Notificaciones.Tipo.Descubrimiento, 6f);
        return null;
    }
}
