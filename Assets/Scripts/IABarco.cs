using UnityEngine;

// Inteligencia del barco enemigo: patrulla, se acerca, se pone de costado y dispara andanadas.
// Esquiva islas. Si queda dañado (menos de la mitad del casco) arría velas.
// Si el jugador sube a bordo, la tripulación pelea. Sin tripulación, el barco queda capturado.
[RequireComponent(typeof(ControlBarco))]
public class IABarco : MonoBehaviour
{
    [Tooltip("Ya no se usa: ataca al barco propio del jugador cuando este está cerca de él.")]
    public Rigidbody barcoJugador;
    public int tripulacion = 4;
    public Material materialTripulante;
    public Material materialEspada;

    public float distanciaAtaque = 170f;
    [Tooltip("Solo ataca si tu barco está a menos de esta distancia (los del mundo infinito no te persiguen desde lejos).")]
    public float distanciaAlerta = 100000f;
    [Tooltip("Si es del mundo infinito: patrulla alrededor de este punto en vez de alrededor del jugador.")]
    public bool patrullaPropia;
    public Vector3 casa;
    public float radioPatrulla = 250f;
    public float distanciaDisparo = 100f;
    [Tooltip("Por debajo de esta fracción del casco, arría velas (queda a la deriva).")]
    public float umbralDanado = 0.5f;
    [Tooltip("Probabilidad de que, al atacar, intente abordarte en vez de solo cañonear.")]
    public float probAbordaje = 0.4f;
    [Tooltip("Cuántos piratas pueden estar a la vez en tu barco.")]
    public int maxAbordando = 3;
    bool decidido, quiereAbordar, avisoAbordaje;
    float tAbordaje;

    // Todos los enemigos activos (más rápido que buscarlos con FindObjectsByType cada frame)
    public static readonly System.Collections.Generic.List<IABarco> Todos = new System.Collections.Generic.List<IABarco>();
    void OnEnable() => Todos.Add(this);
    // Todos los enemigos, también los dormidos por estar lejos (ver Rendimiento)
    public static readonly System.Collections.Generic.List<IABarco> Registrados = new System.Collections.Generic.List<IABarco>();
    void Awake() { Registrados.Add(this); OrigenFlotante.AlMover += Correr; }
    void Correr(Vector3 c) { casa -= c; puntoRuta -= c; }   // el origen flotante corrió el mundo
    void OnDisable() => Todos.Remove(this);

    Tripulante[] tripulantes = new Tripulante[0];

    public bool Capturado { get; private set; }
    public string EstadoTexto { get; private set; } = "Patrullando";

    ControlBarco cb;
    Canones canones;
    VidaBarco vida;
    float w, h, L;
    Vector3 puntoRuta;

    void Start()
    {
        cb = GetComponent<ControlBarco>();
        canones = GetComponent<Canones>();
        vida = GetComponent<VidaBarco>();
        Cubierta.Medidas(GetComponent<BoxCollider>(), out w, out h, out L);
        cb.controlActivo = false;
        if (canones != null) canones.controlJugador = false;
        if (vida != null) vida.alReaparecer += Reiniciar;
        puntoRuta = NuevoPunto();
        CrearTripulacion();
        if (GetComponent<Botin>() == null) gameObject.AddComponent<Botin>();   // lo que lleva en la bodega
        Invoke(nameof(ArmarExtremos), 0.2f);   // después de que Canones reparta los de costado
    }

    // Los barcos grandes también llevan cañones de proa y barriles en la popa
    void ArmarExtremos()
    {
        if (canones == null || canones.enBabor + canones.enEstribor < 8) return;
        canones.Fijar(canones.enBabor, canones.enEstribor,
            Mathf.Max(canones.enProa, Mathf.Min(2, canones.RanurasLado(LadoCanon.Proa))),
            Mathf.Max(canones.enPopa, Mathf.Min(1, canones.RanurasLado(LadoCanon.Popa))));
    }

    void OnDestroy()
    {
        OrigenFlotante.AlMover -= Correr;
        Registrados.Remove(this);
        if (vida != null) vida.alReaparecer -= Reiniciar;
        // Los piratas que saltaron a otro barco no se quedan huérfanos para siempre
        foreach (Tripulante t in tripulantes) if (t != null) Destroy(t.gameObject);
    }

    void Reiniciar()
    {
        Capturado = false;
        cb.controlActivo = false;
        if (canones != null) canones.controlJugador = false;
        CrearTripulacion();
    }

    // El enemigo más cercano a un punto (para las teclas de prueba y el modo creativo)
    public static IABarco MasCercano(Vector3 punto)
    {
        IABarco mejor = null;
        float dMejor = float.MaxValue;
        foreach (IABarco e in Todos)
        {
            float d = Vector3.Distance(e.transform.position, punto);
            if (d < dMejor) { dMejor = d; mejor = e; }
        }
        return mejor;
    }

    public int TripulantesVivos()
    {
        int n = 0;
        foreach (Tripulante t in tripulantes) if (t != null && !t.Muerto) n++;
        return n;
    }

    float tMirar, ladoIsla;

    void Update()
    {
        if (vida != null && vida.Hundido) { EstadoTexto = "Hundiéndose"; cb.timonExterno = 0f; return; }
        if (MenuPrincipal.Visible) { cb.timonExterno = 0f; return; }   // con el menú principal abierto los piratas no atacan

        if (!Capturado && TripulantesVivos() == 0)
        {
            Capturado = true;
            cb.FijarVelas(0);
            cb.timonExterno = 0f;
            if (Personaje.Instancia != null) Personaje.Instancia.Avisar("¡" + name.Replace(" enemigo", "").Replace(" pirata", "") + " capturado! Ve a su timón y presiona F");
        }
        if (Capturado) { EstadoTexto = "Capturado"; cb.timonExterno = 0f; return; }

        Personaje p = Personaje.Instancia;
        bool jugadorABordo = p != null && p.SobreBarco(cb);
        bool danado = vida != null && vida.vida < vida.vidaMaxima * umbralDanado;

        Vector3 pos = transform.position;
        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float rumbo = transform.eulerAngles.y;
        Rigidbody objetivo = Objetivo();
        Vector3 hacia = objetivo != null ? objetivo.position - pos : Vector3.zero;
        hacia.y = 0f;
        float dist = hacia.magnitude;

        if (jugadorABordo)
        {
            EstadoTexto = "¡Abordaje!";
            cb.FijarVelas(0);
            cb.timonExterno = 0f;
            return;
        }

        if (danado)
        {
            EstadoTexto = "Dañado, a la deriva";
            cb.FijarVelas(0);
            cb.timonExterno = 0f;
            IntentarDisparar(hacia, dist);
            return;
        }

        float deseado;
        if (dist < distanciaAtaque && objetivo != null)
        {
            EstadoTexto = "Atacando";
            float aJugador = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            // Al empezar el ataque decide si cañonear de costado o ir directo a abordarte
            if (!decidido) { decidido = true; quiereAbordar = Random.value < probAbordaje && TripulantesVivos() >= 2; }
            float a1 = aJugador + 90f, a2 = aJugador - 90f;
            deseado = Mathf.Abs(Mathf.DeltaAngle(rumbo, a1)) < Mathf.Abs(Mathf.DeltaAngle(rumbo, a2)) ? a1 : a2;
            if (dist > 90f) deseado = aJugador + Mathf.DeltaAngle(aJugador, deseado) * 0.45f;
            if (dist < 35f) deseado = aJugador + 180f;
            cb.FijarVelas(2);
            if (quiereAbordar && TripulantesEnCasa() > 0)
            {
                // Abordaje: proa directo a ti y, al quedar pegados, sus piratas saltan a tu cubierta
                EstadoTexto = "Abordando";
                deseado = aJugador;
                BoxCollider suyo = objetivo.GetComponent<BoxCollider>();
                float umbral = 8f + 0.5f * (L + (suyo != null ? suyo.size.z : 10f));
                cb.FijarVelas(dist < umbral * 1.8f ? 1 : 2);
                if (dist < umbral) Abordar(objetivo);
            }
            IntentarDisparar(hacia, dist);
        }
        else
        {
            EstadoTexto = "Patrullando";
            decidido = false;
            Vector3 r = puntoRuta - pos; r.y = 0f;
            if (r.magnitude < 40f) { puntoRuta = NuevoPunto(); r = puntoRuta - pos; }
            deseado = Mathf.Atan2(r.x, r.z) * Mathf.Rad2Deg;
            cb.FijarVelas(1);
        }

        // Esquivar islas: mira 70 m al frente (5 veces por segundo alcanza; un barco no gira tan rápido)
        tMirar -= Time.deltaTime;
        if (tMirar <= 0f)
        {
            tMirar = 0.2f;
            ladoIsla = 0f;
            if (Physics.SphereCast(pos + Vector3.up, 6f, adelante, out RaycastHit hit, 70f, ~0, QueryTriggerInteraction.Ignore)
                && hit.rigidbody == null)
                ladoIsla = Vector3.Cross(adelante, hit.point - pos).y > 0f ? 1f : -1f;
        }
        if (ladoIsla != 0f) deseado = rumbo + (ladoIsla > 0f ? -75f : 75f);

        cb.timonExterno = Mathf.Clamp(Mathf.DeltaAngle(rumbo, deseado) / 30f, -1f, 1f);
    }

    // Para probar: este pirata va a abordarte sí o sí
    public void ForzarAbordaje() { decidido = true; quiereAbordar = true; }

    int TripulantesEnCasa()
    {
        int n = 0;
        foreach (Tripulante t in tripulantes) if (t != null && !t.Muerto && t.Barco == cb) n++;
        return n;
    }

    // Manda un pirata a tu barco cada pocos segundos (hasta maxAbordando a la vez), solo si estás a bordo
    void Abordar(Rigidbody objetivo)
    {
        Personaje p = Personaje.Instancia;
        ControlBarco suyo = objetivo.GetComponent<ControlBarco>();
        if (p == null || suyo == null || !p.SobreBarco(suyo)) return;
        tAbordaje -= Time.deltaTime;
        if (tAbordaje > 0f) return;
        tAbordaje = 3f;
        int alla = 0;
        foreach (Tripulante t in tripulantes) if (t != null && !t.Muerto && t.Barco == suyo) alla++;
        if (alla >= maxAbordando) return;
        foreach (Tripulante t in tripulantes)
        {
            if (t == null || t.Muerto || t.Barco != cb) continue;
            t.Saltar(suyo);
            if (!avisoAbordaje) { avisoAbordaje = true; p.Avisar("¡Te abordan! Suelta el timón (F) y pelea con la espada (clic)"); }
            return;
        }
    }

    void IntentarDisparar(Vector3 hacia, float dist)
    {
        if (canones == null || dist > distanciaDisparo || dist < 1f) return;
        Vector3 n = hacia / dist;
        // Ángulo balístico para esa distancia (como hace el jugador al apuntar), con un poco de error
        float v = canones.velocidadBala, g = Mathf.Abs(Physics.gravity.y);
        float seno = Mathf.Clamp(g * dist / (v * v), 0f, 1f);
        canones.anguloApunte = 0.5f * Mathf.Asin(seno) * Mathf.Rad2Deg + Random.Range(-1.5f, 1.5f);
        Vector3 der = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        if (Vector3.Dot(-der, n) > 0.96f) canones.Disparar(true);
        if (Vector3.Dot(der, n) > 0.96f) canones.Disparar(false);
        // También usa los cañones de proa (de frente, al perseguir) y suelta barriles por la popa si lo siguen de cerca
        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (Vector3.Dot(adelante, n) > 0.97f) canones.Disparar(LadoCanon.Proa);
        if (Vector3.Dot(-adelante, n) > 0.9f && dist < 45f) canones.Disparar(LadoCanon.Popa);
    }

    // A quién atacar: el barco del jugador (el que construyó o capturó) solo si el jugador está a bordo o cerca.
    // Si el jugador no tiene barco o anda lejos de él, el bergantín patrulla.
    Rigidbody Objetivo()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return null;
        // El barco donde vas (el tuyo o uno capturado); si estás abordando a otro pirata, tu barco propio
        ControlBarco suyo = null;
        ControlBarco abordo = p.estado != Personaje.Estado.Libre ? p.BarcoActual : null;
        if (abordo != null && abordo != cb)
        {
            IABarco otro = abordo.GetComponent<IABarco>();
            if (otro == null || otro.Capturado) suyo = abordo;
        }
        if (suyo == null && p.barco != null && p.barco != cb) suyo = p.barco;
        if (suyo == null) return null;
        VidaBarco vs = suyo.GetComponent<VidaBarco>();
        if (vs != null && vs.Hundido) return null;
        if (Vector3.Distance(p.transform.position, suyo.transform.position) > 80f) return null;
        if (Vector3.Distance(transform.position, suyo.transform.position) > distanciaAlerta) return null;
        return suyo.GetComponent<Rigidbody>();
    }

    Vector3 NuevoPunto()
    {
        if (patrullaPropia)
        {
            float ap = Random.Range(0f, Mathf.PI * 2f), dp = Random.Range(radioPatrulla * 0.3f, radioPatrulla);
            return casa + new Vector3(Mathf.Cos(ap) * dp, 0f, Mathf.Sin(ap) * dp);
        }
        Personaje p = Personaje.Instancia;
        Vector3 c = p != null ? p.transform.position : transform.position;
        float a = Random.Range(0f, Mathf.PI * 2f), d = Random.Range(150f, 400f);
        return c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
    }

    void CrearTripulacion()
    {
        double crono = Cronos.Ahora;
        try { CrearTripulacionYa(); } finally { Cronos.Anotar("tripulación de un barco pirata", crono); }
    }

    void CrearTripulacionYa()
    {
        foreach (Tripulante t in GetComponentsInChildren<Tripulante>()) Destroy(t.gameObject);
        foreach (Tripulante t in tripulantes) if (t != null) Destroy(t.gameObject);   // también los que saltaron a otro barco
        decidido = false; avisoAbordaje = false;
        tripulantes = new Tripulante[tripulacion];
        for (int i = 0; i < tripulacion; i++)
        {
            GameObject g = new GameObject("Tripulante " + (i + 1));
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Cubierta.PuntoAleatorio(w, h, L, Cubierta.EsPlana(this));
            Visual(PrimitiveType.Capsule, g.transform, new Vector3(0, 0.9f, 0), Vector3.zero, new Vector3(0.6f, 0.9f, 0.6f), materialTripulante);
            Visual(PrimitiveType.Cube, g.transform, new Vector3(0, 1.55f, 0.3f), Vector3.zero, new Vector3(0.12f, 0.08f, 0.14f), materialTripulante);
            GameObject piv = new GameObject("Espada");
            piv.transform.SetParent(g.transform, false);
            piv.transform.localPosition = new Vector3(0.38f, 1.15f, 0.1f);
            Visual(PrimitiveType.Cube, piv.transform, new Vector3(0, 0, 0.45f), Vector3.zero, new Vector3(0.06f, 0.06f, 0.9f), materialEspada);
            Tripulante t = g.AddComponent<Tripulante>();
            t.espada = piv.transform;
            // El primero es el capitán; los siguientes (hasta 3), cañoneros. Cada uno con su aspecto y su nombre
            var azar = new System.Random(GetInstanceID() * 31 + i);
            t.rol = i == 0 ? Tripulante.Rol.Capitan : i <= 3 ? Tripulante.Rol.Canonero : Tripulante.Rol.Marinero;
            t.nombre = NombreDe(t.rol, azar);
            Apariencia.Construir(g.transform, Apariencia.Aleatoria(azar, t.rol), false, t.rol);
            tripulantes[i] = t;
            Rendimiento.Capa(g, Rendimiento.CapaBarcoDetalle);   // de lejos no se dibujan
        }
    }

    static readonly string[] Nombres = { "Rosa", "Bartolo", "Mar", "Tiburcio", "Sol", "Anacleto", "Perla", "Ramiro", "Brisa", "Celeste",
        "Fausto", "Coral", "Lucio", "Marina", "Andrés", "Alexis", "Dani", "Cris", "Sasha", "Noa" };
    static readonly string[] Apodos = { "el Tuerto", "Pata de Palo", "la Brava", "Barbarroja", "Diente de Oro", "la Tormenta", "el Manco", "Ojo de Halcón", "la Sirena", "Mil Nudos" };

    static string NombreDe(Tripulante.Rol rol, System.Random r)
    {
        string n = Nombres[r.Next(Nombres.Length)] + " " + Apodos[r.Next(Apodos.Length)];
        return (rol == Tripulante.Rol.Capitan ? "capitán " : rol == Tripulante.Rol.Canonero ? "cañonero " : "") + n;
    }

    static void Visual(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 rot, Vector3 escala, Material m)
    {
        GameObject v = GameObject.CreatePrimitive(tipo);
        Destroy(v.GetComponent<Collider>());
        v.transform.SetParent(padre, false);
        v.transform.localPosition = pos;
        v.transform.localRotation = Quaternion.Euler(rot);
        v.transform.localScale = escala;
        if (m != null) v.GetComponent<Renderer>().sharedMaterial = m;
    }
}
