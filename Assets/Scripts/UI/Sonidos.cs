using System.Collections.Generic;
using UnityEngine;

// Sonidos de la interfaz, hechos por código (sin archivos de audio): clic, abrir y cerrar ventanas,
// monedas, madera, error, descubrimiento, fabricación. Cortos y discretos; se apagan en Opciones → Audio.
public class Sonidos : MonoBehaviour
{
    public enum Tipo { Clic, Abrir, Cerrar, Moneda, Madera, Error, Descubrir, Fabricar, Pasar, Exito, Alerta, Transferir }

    static Sonidos instancia;
    readonly Dictionary<Tipo, AudioClip> clips = new Dictionary<Tipo, AudioClip>();
    AudioSource[] fuentes;
    int siguiente;
    float ultimo;
    Tipo ultimoTipo;
    const int Hz = 22050;

    static void Asegurar()
    {
        if (instancia != null) return;
        var g = new GameObject("Sonidos de interfaz");
        Object.DontDestroyOnLoad(g);
        instancia = g.AddComponent<Sonidos>();
        instancia.fuentes = new AudioSource[4];
        for (int i = 0; i < instancia.fuentes.Length; i++)
        {
            var f = g.AddComponent<AudioSource>();
            f.playOnAwake = false; f.spatialBlend = 0f; f.ignoreListenerPause = true;
            instancia.fuentes[i] = f;
        }
    }

    public static void Reproducir(Tipo t, float volumen = 1f)
    {
        if (!Preferencias.SonidosInterfaz) return;
        Asegurar();
        // Un mismo sonido no se repite más de 20 veces por segundo (al pasar rápido por una lista)
        float ahora = Time.unscaledTime;
        if (t == instancia.ultimoTipo && ahora - instancia.ultimo < 0.05f) return;
        instancia.ultimo = ahora; instancia.ultimoTipo = t;
        if (!instancia.clips.TryGetValue(t, out AudioClip c)) { c = instancia.Crear(t); instancia.clips[t] = c; }
        AudioSource f = instancia.fuentes[instancia.siguiente];
        instancia.siguiente = (instancia.siguiente + 1) % instancia.fuentes.Length;
        f.pitch = t == Tipo.Pasar ? Random.Range(0.97f, 1.03f) : 1f;
        f.PlayOneShot(c, volumen * Preferencias.VolumenInterfaz);
    }

    // ---------- Síntesis ----------
    AudioClip Crear(Tipo t)
    {
        float dur; System.Func<float, float> onda;
        var rnd = new System.Random(7);
        float Ruido() => (float)rnd.NextDouble() * 2f - 1f;
        float Seno(float f, float s) => Mathf.Sin(2f * Mathf.PI * f * s);
        float Env(float s, float ataque, float caida, float total) => s < ataque ? s / ataque : Mathf.Exp(-(s - ataque) / caida) * Mathf.Clamp01((total - s) / 0.01f);
        switch (t)
        {
            case Tipo.Clic:      // toque corto de madera
                dur = 0.07f; onda = s => (Seno(1800f, s) * 0.5f + Ruido() * 0.5f) * Env(s, 0.002f, 0.012f, dur) * 0.5f; break;
            case Tipo.Pasar:     // al pasar el cursor: tic suave
                dur = 0.04f; onda = s => (Seno(2600f, s) * 0.6f + Ruido() * 0.2f) * Env(s, 0.002f, 0.008f, dur) * 0.22f; break;
            case Tipo.Abrir:     // soplo ascendente (tela, papel)
                dur = 0.22f; onda = s => Ruido() * Env(s, 0.03f, 0.08f, dur) * 0.35f * (0.5f + 0.5f * Seno(60f + s * 400f, s)); break;
            case Tipo.Cerrar:    // soplo descendente, más corto
                dur = 0.16f; onda = s => Ruido() * Env(s, 0.005f, 0.05f, dur) * 0.3f; break;
            case Tipo.Moneda:    // dos campanitas
                dur = 0.35f; onda = s => (Seno(2093f, s) * Env(s, 0.002f, 0.08f, dur) + Seno(2637f, Mathf.Max(0f, s - 0.07f)) * Env(Mathf.Max(0f, s - 0.07f), 0.002f, 0.1f, dur) * (s > 0.07f ? 1f : 0f)) * 0.35f; break;
            case Tipo.Madera:    // golpe de madera (fabricar cosas sencillas)
                dur = 0.18f; onda = s => (Seno(220f, s) * 0.6f + Seno(440f, s) * 0.3f + Ruido() * 0.3f) * Env(s, 0.003f, 0.04f, dur) * 0.6f; break;
            case Tipo.Fabricar:  // martillo sobre yunque: metal + madera
                dur = 0.3f; onda = s => (Seno(1320f, s) * 0.5f + Seno(1980f, s) * 0.25f + Ruido() * 0.3f) * Env(s, 0.002f, 0.07f, dur) * 0.55f; break;
            case Tipo.Error:     // zumbido bajo, corto
                dur = 0.2f; onda = s => (Seno(160f, s) * 0.6f + Seno(165f, s) * 0.4f) * Env(s, 0.01f, 0.12f, dur) * 0.45f; break;
            case Tipo.Alerta:    // dos notas graves
                dur = 0.4f; onda = s => (s < 0.18f ? Seno(440f, s) : Seno(330f, s)) * Env(s % 0.2f, 0.01f, 0.1f, 0.2f) * 0.4f; break;
            case Tipo.Descubrir: // arpegio ascendente
                dur = 0.7f; onda = s => { int n = Mathf.Min(3, (int)(s / 0.14f)); float f = new[] { 523f, 659f, 784f, 1047f }[n]; float ls = s - n * 0.14f; return Seno(f, s) * Env(ls, 0.01f, 0.2f, 0.5f) * 0.35f; }; break;
            case Tipo.Exito:     // dos notas alegres
                dur = 0.3f; onda = s => (s < 0.12f ? Seno(784f, s) : Seno(1047f, s)) * Env(s < 0.12f ? s : s - 0.12f, 0.01f, 0.09f, 0.2f) * 0.35f; break;
            case Tipo.Transferir: // deslizar objetos: roce corto
                dur = 0.12f; onda = s => Ruido() * Env(s, 0.01f, 0.04f, dur) * 0.3f * (0.6f + 0.4f * Seno(900f, s)); break;
            default:
                dur = 0.05f; onda = s => 0f; break;
        }
        int n = Mathf.CeilToInt(dur * Hz);
        var datos = new float[n];
        // Filtro suave para que no suene áspero
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float v = Mathf.Clamp(onda(i / (float)Hz), -1f, 1f);
            prev = prev * 0.3f + v * 0.7f;
            datos[i] = prev;
        }
        var clip = AudioClip.Create("ui_" + t, n, 1, Hz, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
