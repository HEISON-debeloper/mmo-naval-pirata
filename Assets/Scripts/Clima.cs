using UnityEngine;

// Clima por zonas: el mar está dividido en zonas de 1,5 km y cada una tiene su propio tiempo, que cambia cada
// ~10 minutos: despejado, nublado, lluvia o tormenta. Mar adentro (lejos del inicio) las tormentas son más frecuentes.
// Cambia las olas (los barcos se mueven más), el cielo, la niebla y la luz; con lluvia caen gotas y suena la lluvia;
// en tormenta hay relámpagos y truenos. Cambia poco a poco (unos 20 s) al entrar a otra zona o al cambiar el tiempo.
public class Clima : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() { if (FindFirstObjectByType<Clima>() == null) new GameObject("Clima").AddComponent<Clima>(); }

    public enum Tiempo { Despejado, Nublado, Lluvia, Tormenta }
    public static Tiempo Actual { get; private set; } = Tiempo.Despejado;
    public static string Nombre => Actual == Tiempo.Despejado ? "Despejado" : Actual == Tiempo.Nublado ? "Nublado" : Actual == Tiempo.Lluvia ? "Lluvia" : "Tormenta";

    // Lo que leen el cielo (CicloDia) y el resto: 0 = nada, 1 = máximo
    public static float Nubes, LluviaFuerza, Destello;
    public static bool Calmado;
    public static int Forzado = -1;   // tecla de prueba X: fija un tiempo (-1 = el de la zona)   // "mar en calma" del modo creativo

    const float TamanoZona = 1500f, Periodo = 600f;
    float nubesObj, lluviaObj, olasObj = 0.85f, olas = 0.85f;
    float tTrueno = 10f, tDestello;
    Tiempo anterior = Tiempo.Despejado;
    bool primero = true;
    ParticleSystem gotas;
    AudioSource sonLluvia, sonTrueno;

    void Start()
    {
        // Gotas: una caja de partículas que sigue a la cámara
        var g = new GameObject("Lluvia");
        g.transform.SetParent(transform, false);
        gotas = g.AddComponent<ParticleSystem>();
        gotas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = gotas.main;
        main.loop = true; main.startLifetime = 1.2f; main.startSpeed = 24f; main.startSize = 0.018f;
        main.maxParticles = 4000; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.8f, 0.85f, 0.92f, 0.55f);
        var forma = gotas.shape;
        forma.shapeType = ParticleSystemShapeType.Box; forma.scale = new Vector3(40f, 40f, 1f);   // plana (el giro la acuesta)
        forma.rotation = new Vector3(90f, 0f, 0f);   // emite hacia abajo
        var em = gotas.emission; em.rateOverTime = 0f;
        var r = g.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 1f; r.velocityScale = 0.05f;
        r.maxParticleSize = 0.006f;   // de cerca no se vuelven barras gruesas
        Shader sh = Resources.Load<Shader>("Shaders/Lluvia");   // transparente y en Resources (entra en la compilación)
        if (sh == null) sh = Shader.Find("Pirata/Lluvia");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        r.sharedMaterial = new Material(sh);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gotas.Play();

        sonLluvia = Fuente("lluvia", true);
        sonTrueno = Fuente("trueno", false);
    }

    AudioSource Fuente(string grupo, bool bucle)
    {
        var f = gameObject.AddComponent<AudioSource>();
        AudioClip[] c = SonidosMundo.Grupo(grupo);
        f.clip = c.Length > 0 ? c[0] : null;
        f.loop = bucle; f.playOnAwake = false; f.spatialBlend = 0f; f.volume = 0f;
        if (bucle && f.clip != null) f.Play();
        return f;
    }

    void Update()
    {
        Personaje p = Personaje.Instancia;
        Camera cam = Camera.main;
        float dt = Time.deltaTime;
        if (p != null && !MenuPrincipal.Visible)
        {
            Tiempo t = Forzado >= 0 ? (Tiempo)Forzado : Elegir(p.transform.position);
            if (t != anterior && !primero) Avisar(t);
            anterior = t; Actual = t; primero = false;
            switch (t)
            {
                case Tiempo.Despejado: nubesObj = 0f; lluviaObj = 0f; olasObj = 0.85f; break;
                case Tiempo.Nublado: nubesObj = 0.45f; lluviaObj = 0f; olasObj = 1.1f; break;
                case Tiempo.Lluvia: nubesObj = 0.7f; lluviaObj = 0.55f; olasObj = 1.5f; break;
                default: nubesObj = 0.9f; lluviaObj = 1f; olasObj = 2.1f; break;
            }
        }
        // Cambios suaves (unos 20 s)
        Nubes = Mathf.MoveTowards(Nubes, nubesObj, dt / 20f);
        LluviaFuerza = Mathf.MoveTowards(LluviaFuerza, lluviaObj, dt / 15f);
        olas = Mathf.MoveTowards(olas, olasObj, dt / 25f);
        Olas.intensidad = Calmado ? 0.15f : olas;

        // Gotas y sonido (bajo cubierta no llueve, pero se oye)
        bool abajo = p != null && p.Bajo;
        if (gotas != null)
        {
            if (cam != null) gotas.transform.position = cam.transform.position + Vector3.up * 14f + cam.transform.forward * 8f;
            var em = gotas.emission;
            em.rateOverTime = abajo ? 0f : LluviaFuerza * 2600f;
        }
        float amb = Preferencias.VolumenAmbiente;
        if (sonLluvia != null) sonLluvia.volume = LluviaFuerza * (abajo ? 0.35f : 0.8f) * amb;

        // Relámpagos y truenos en la tormenta
        Destello = Mathf.MoveTowards(Destello, 0f, dt * 5f);
        if (Actual == Tiempo.Tormenta && LluviaFuerza > 0.7f)
        {
            tTrueno -= dt;
            if (tTrueno <= 0f)
            {
                tTrueno = Random.Range(8f, 25f);
                Destello = abajo ? 0.3f : 1f;
                if (sonTrueno != null && sonTrueno.clip != null)
                {
                    sonTrueno.volume = (abajo ? 0.5f : 0.9f) * amb;
                    sonTrueno.pitch = Random.Range(0.85f, 1.1f);
                    sonTrueno.PlayDelayed(Random.Range(0.3f, 2.5f));   // el trueno llega después del relámpago
                }
            }
        }
    }

    // El tiempo de una zona en este periodo (siempre el mismo para la misma zona y momento)
    static Tiempo Elegir(Vector3 pos)
    {
        int zx = Mathf.FloorToInt(pos.x / TamanoZona), zz = Mathf.FloorToInt(pos.z / TamanoZona);
        int periodo = Mathf.FloorToInt(Time.time / Periodo);
        float lejos = new Vector2(pos.x, pos.z).magnitude;
        if (lejos < 500f) return periodo % 4 == 3 ? Tiempo.Nublado : Tiempo.Despejado;   // la isla de inicio: casi siempre buen tiempo
        var rnd = new System.Random(unchecked(zx * 73856093 ^ zz * 19349663 ^ periodo * 83492791));
        double tirada = rnd.NextDouble();
        float tormenta = Mathf.Clamp(0.06f + lejos / 30000f, 0.06f, 0.3f);   // más tormentas mar adentro
        if (tirada < tormenta) return Tiempo.Tormenta;
        if (tirada < tormenta + 0.18) return Tiempo.Lluvia;
        if (tirada < tormenta + 0.45) return Tiempo.Nublado;
        return Tiempo.Despejado;
    }

    static void Avisar(Tiempo t)
    {
        string texto = t == Tiempo.Tormenta ? "Se acerca una tormenta: el mar se pone bravo"
                     : t == Tiempo.Lluvia ? "Empieza a llover"
                     : t == Tiempo.Nublado ? "El cielo se nubla"
                     : "Se despeja el cielo";
        Notificaciones.Mostrar(texto, t == Tiempo.Tormenta ? Notificaciones.Tipo.Alerta : Notificaciones.Tipo.Info, 5f);
    }
}
