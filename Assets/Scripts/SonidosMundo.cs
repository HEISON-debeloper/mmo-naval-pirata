using System.Collections.Generic;
using UnityEngine;

// Sonidos del mundo con grabaciones reales (CC0, de OpenGameArt; ver Resources/Audio/CREDITOS.txt).
// Cada carpeta de Resources/Audio es un grupo y cada archivo una variante: se elige una al azar y se le cambia
// un poco el tono para que no suene repetido.
//   Efectos (3D, suenan desde donde pasan): cañonazos, impactos, balas al agua, choques, hundimientos, espada,
//   hacha, pico, árboles que caen, pasos, chapuzones, brazadas, ancla, comer.
//   Ambiente: viento (más fuerte navegando rápido), agua contra el casco, olas en la orilla, gaviotas cerca de las islas,
//   crujidos de la madera a bordo y el fuego de la cocina bajo cubierta.
// Volúmenes en Opciones → Audio (Efectos y Ambiente).
[DefaultExecutionOrder(100)]
public class SonidosMundo : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (I == null) new GameObject("Sonidos del mundo").AddComponent<SonidosMundo>(); }

    static SonidosMundo I;
    static readonly Dictionary<string, AudioClip[]> grupos = new Dictionary<string, AudioClip[]>();
    static readonly Dictionary<string, float> ultimoDe = new Dictionary<string, float>();
    const int Fuentes = 32;
    AudioSource[] pool;
    int sig;
    AudioSource viento, agua, fuego;
    float tOlas = 2f, tGaviota = 4f, tCrujido = 3f, tPaso, tBrazada;
    Vector3 posAntes;
    bool nadabaAntes, hayPosAntes;

    void Awake()
    {
        I = this;
        DontDestroyOnLoad(gameObject);
        pool = new AudioSource[Fuentes];
        for (int i = 0; i < Fuentes; i++)
        {
            var g = new GameObject("Fuente " + i);
            g.transform.SetParent(transform, false);
            var f = g.AddComponent<AudioSource>();
            f.playOnAwake = false; f.spatialBlend = 1f; f.dopplerLevel = 0f;
            f.rolloffMode = AudioRolloffMode.Logarithmic;
            pool[i] = f;
        }
        viento = Bucle("Viento", "viento", false);
        agua = Bucle("Agua", "agua_casco", false);
        fuego = Bucle("Fuego de la cocina", "fuego", true);
        fuego.minDistance = 1.2f; fuego.maxDistance = 14f;
    }

    AudioSource Bucle(string nombre, string grupo, bool en3D)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        var f = g.AddComponent<AudioSource>();
        AudioClip[] c = Grupo(grupo);
        f.clip = c.Length > 0 ? c[0] : null;
        f.loop = true; f.playOnAwake = false; f.volume = 0f; f.dopplerLevel = 0f;
        f.spatialBlend = en3D ? 1f : 0f;
        f.rolloffMode = AudioRolloffMode.Linear;
        if (f.clip != null) { f.time = Random.Range(0f, f.clip.length * 0.9f); f.Play(); }
        return f;
    }

    public static AudioClip[] Grupo(string g)
    {
        if (!grupos.TryGetValue(g, out AudioClip[] c)) { c = Resources.LoadAll<AudioClip>("Audio/" + g); grupos[g] = c; }
        return c;
    }

    // Un sonido en un punto del mundo. alcance: hasta dónde se oye bien (metros). separacion: no repetir el grupo antes de eso (s).
    public static void Uno(string g, Vector3 pos, float vol = 1f, float tono = 1f, float alcance = 40f, float separacion = 0f, bool ambiente = false)
    {
        if (I == null) return;
        if (separacion > 0f)
        {
            float ahora = Time.time;
            if (ultimoDe.TryGetValue(g, out float u) && ahora - u < separacion) return;
            ultimoDe[g] = ahora;
        }
        AudioClip[] c = Grupo(g);
        if (c.Length == 0) return;
        AudioSource f = I.pool[I.sig];
        I.sig = (I.sig + 1) % Fuentes;
        f.transform.position = pos;
        f.clip = c[Random.Range(0, c.Length)];
        f.spatialBlend = 1f;
        f.minDistance = Mathf.Max(1f, alcance * 0.12f);
        f.maxDistance = alcance * 4f;
        f.volume = Mathf.Clamp01(vol) * (ambiente ? Preferencias.VolumenAmbiente : Preferencias.VolumenEfectos);
        f.pitch = tono * Random.Range(0.93f, 1.07f);
        f.Play();
    }

    // Un sonido propio del jugador (comer, su espada): sin posición, siempre igual de cerca
    public static void Propio(string g, float vol = 1f, float tono = 1f)
    {
        if (I == null) return;
        AudioClip[] c = Grupo(g);
        if (c.Length == 0) return;
        AudioSource f = I.pool[I.sig];
        I.sig = (I.sig + 1) % Fuentes;
        f.clip = c[Random.Range(0, c.Length)];
        f.spatialBlend = 0f;
        f.volume = Mathf.Clamp01(vol) * Preferencias.VolumenEfectos;
        f.pitch = tono * Random.Range(0.93f, 1.07f);
        f.Play();
    }

    // ---------- Ambiente y pasos ----------
    void Update()
    {
        Personaje p = Personaje.Instancia;
        Camera cam = Camera.main;
        float amb = Preferencias.VolumenAmbiente, dt = Time.deltaTime;
        if (p == null || cam == null || MenuPrincipal.Visible)
        {
            Fundir(viento, 0f, dt); Fundir(agua, 0f, dt); Fundir(fuego, 0f, dt);
            hayPosAntes = false;
            return;
        }
        Vector3 oido = cam.transform.position;
        ControlBarco barco = p.estado != Personaje.Estado.Libre ? p.BarcoActual : null;
        Rigidbody rbB = barco != null ? barco.GetComponent<Rigidbody>() : null;
        float velBarco = rbB != null ? rbB.linearVelocity.magnitude : 0f;
        bool abajo = p.Bajo;

        // Isla más cercana (para olas y gaviotas)
        Isla isla = null; float dIsla = float.MaxValue;
        foreach (Isla i in Isla.Todas)
        {
            if (i == null) continue;
            Vector3 d = i.transform.position - oido; d.y = 0f;
            float borde = d.magnitude - i.radio * 1.2f;
            if (borde < dIsla) { dIsla = borde; isla = i; }
        }
        bool enTierra = p.estado == Personaje.Estado.Libre && p.EnTierra;

        // Viento: arriba en el mar más fuerte; navegando rápido aún más; bajo cubierta apenas
        float v = enTierra ? 0.12f : 0.22f + Mathf.Clamp01(velBarco / 12f) * 0.35f;
        if (abajo) v *= 0.2f;
        Fundir(viento, v * amb, dt);
        // Agua contra el casco: a bordo (más fuerte abajo, se oye el casco) o nadando
        float a = barco != null ? (abajo ? 0.55f : 0.3f) + Mathf.Clamp01(velBarco / 10f) * 0.3f : p.Nadando ? 0.4f : 0f;
        agua.pitch = 0.9f + Mathf.Clamp01(velBarco / 12f) * 0.25f;
        Fundir(agua, a * amb, dt);
        // Fuego de la cocina: solo bajo cubierta en tu barco, sonando desde el fogón
        Interior it = barco != null ? barco.GetComponent<Interior>() : null;
        bool cocina = abajo && it != null && it.HayFogon && p.Nivel == 1;
        if (cocina) fuego.transform.position = barco.transform.TransformPoint(it.FogonLocal);
        Fundir(fuego, cocina ? 0.8f * amb : 0f, dt);

        // Olas en la orilla: cerca de una isla, desde el punto de la playa más cercano
        tOlas -= dt;
        if (tOlas <= 0f)
        {
            tOlas = Random.Range(2.5f, 5.5f);
            if (isla != null && dIsla < 70f)
            {
                Vector3 c = isla.transform.position, d = oido - c; d.y = 0f;
                Vector3 orilla = c + d.normalized * isla.radio * 1.2f;
                orilla.y = Olas.Altura(orilla);
                Uno("olas", orilla, 0.75f, 1f, 30f, 0f, true);
            }
        }
        // Gaviotas: sobre las islas y su costa
        tGaviota -= dt;
        if (tGaviota <= 0f)
        {
            tGaviota = Random.Range(7f, 16f);
            if (isla != null && dIsla < 120f && !abajo)
            {
                Vector3 g = oido + new Vector3(Random.Range(-25f, 25f), Random.Range(10f, 22f), Random.Range(-25f, 25f));
                Uno("gaviotas", g, 0.5f, 1f, 35f, 0f, true);
            }
        }
        // Crujidos de la madera a bordo: más seguidos y fuertes con el barco en marcha y bajo cubierta
        tCrujido -= dt;
        if (tCrujido <= 0f)
        {
            tCrujido = Random.Range(3f, 9f) / (1f + velBarco / 8f);
            if (barco != null)
            {
                Vector3 local = new Vector3(Random.Range(-2f, 2f), abajo ? 1.5f : -0.5f, Random.Range(-6f, 6f));
                Uno("crujido", barco.transform.TransformPoint(local), abajo ? 0.45f : 0.22f, Random.Range(0.75f, 1f), 12f, 0f, true);
            }
        }

        // Pasos, brazadas y chapuzón al caer al agua
        Vector3 pos = p.transform.position;
        Vector3 mov = hayPosAntes ? pos - posAntes : Vector3.zero;
        if (rbB != null) mov -= rbB.GetPointVelocity(pos) * dt;   // lo que se mueve el barco no cuenta como pasos
        mov.y = 0f;
        float rapidez = dt > 0f ? mov.magnitude / dt : 0f;
        posAntes = pos; hayPosAntes = true;
        bool caminando = (p.estado == Personaje.Estado.Cubierta || (p.estado == Personaje.Estado.Libre && p.EnTierra && !p.Nadando));
        if (caminando && rapidez > 0.6f && rapidez < 15f)
        {
            tPaso -= dt;
            if (tPaso <= 0f)
            {
                tPaso = Mathf.Clamp(1.6f / rapidez, 0.25f, 0.6f);
                bool madera = p.estado == Personaje.Estado.Cubierta;
                Uno("pasos", pos, madera ? 0.55f : 0.35f, madera ? 0.85f : 1.1f, 10f);
            }
        }
        else tPaso = 0.1f;
        if (p.Nadando && !nadabaAntes && hayPosAntes) Uno("chapuzon", pos, 0.9f, 0.9f, 30f);
        if (p.Nadando && rapidez > 0.4f)
        {
            tBrazada -= dt;
            if (tBrazada <= 0f) { tBrazada = Random.Range(0.75f, 1f); Uno("brazada", pos, 0.35f, 1f, 12f); }
        }
        nadabaAntes = p.Nadando;
    }

    static void Fundir(AudioSource f, float objetivo, float dt)
    {
        if (f == null || f.clip == null) return;
        f.volume = Mathf.MoveTowards(f.volume, objetivo, dt * 0.6f);
    }
}
