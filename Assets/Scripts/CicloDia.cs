using UnityEngine;

// Día y noche: un día del juego dura una hora real (como en el diseño). El sol sale y se pone, el cielo y la niebla
// cambian de color (azul de día, naranja al amanecer y al atardecer, azul oscuro de noche) y de noche alumbra la luna.
// Se puede apagar en Opciones → Gráficos (queda siempre de día). La hora se guarda con la partida.
public class CicloDia : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() { if (FindFirstObjectByType<CicloDia>() == null) new GameObject("Día y noche").AddComponent<CicloDia>(); }

    public const float SegundosPorDia = 3600f;
    public static float Hora = 9f;   // 0 a 24
    public static bool EsDeNoche => Hora < 5.5f || Hora > 19f;
    public static string Texto => Mathf.FloorToInt(Hora).ToString("00") + ":" + Mathf.FloorToInt((Hora % 1f) * 60f).ToString("00");

    Light sol;
    Color cieloDia;
    float intensidadDia = 1f;
    bool listo;
    float nieblaInicio = -1f, nieblaFin;
    int avisoFase = -1;

    // Colores del cielo a lo largo del día (hora, color)
    static readonly (float h, Color c)[] Cielo =
    {
        (0f,  new Color(0.03f, 0.05f, 0.12f)),
        (4.5f, new Color(0.04f, 0.06f, 0.14f)),
        (5.8f, new Color(0.85f, 0.52f, 0.40f)),
        (7.5f, new Color(0.62f, 0.83f, 0.88f)),
        (17f, new Color(0.62f, 0.83f, 0.88f)),
        (18.6f, new Color(0.92f, 0.55f, 0.35f)),
        (19.8f, new Color(0.10f, 0.10f, 0.22f)),
        (24f, new Color(0.03f, 0.05f, 0.12f)),
    };

    void Start()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null) intensidadDia = sol.intensity;
        Camera cam = Camera.main;
        cieloDia = cam != null ? cam.backgroundColor : new Color(0.62f, 0.83f, 0.88f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        listo = true;
    }

    void Update()
    {
        if (!listo) return;
        if (!Preferencias.CicloDia) { Aplicar(10f); return; }
        if (!MenuPrincipal.Visible) Hora = Mathf.Repeat(Hora + Time.deltaTime * 24f / SegundosPorDia, 24f);
        Aplicar(Hora);
        Avisos();
    }

    static Color ColorCielo(float h)
    {
        for (int i = 0; i < Cielo.Length - 1; i++)
            if (h >= Cielo[i].h && h <= Cielo[i + 1].h)
                return Color.Lerp(Cielo[i].c, Cielo[i + 1].c, Mathf.InverseLerp(Cielo[i].h, Cielo[i + 1].h, h));
        return Cielo[0].c;
    }

    void Aplicar(float h)
    {
        // Altura del sol: sale a las 6, arriba a las 12, se pone a las 18
        float ang = (h - 6f) / 12f * 180f;            // 0 = horizonte este, 180 = horizonte oeste
        float altura = Mathf.Sin(ang * Mathf.Deg2Rad);  // >0 de día
        Color cielo = ColorCielo(h);
        // Nubes y lluvia (Clima): cielo gris y más oscuro, niebla más cerca
        float nubes = Clima.Nubes;
        float gris = cielo.grayscale;
        cielo = Color.Lerp(cielo, new Color(gris, gris, gris * 1.05f) * 0.62f, nubes);
        cielo = Color.Lerp(cielo, new Color(0.85f, 0.88f, 1f), Clima.Destello * 0.6f);
        Camera cam = Camera.main;
        if (cam != null) cam.backgroundColor = cielo;
        RenderSettings.fogColor = cielo;
        if (nieblaInicio < 0f) { nieblaInicio = RenderSettings.fogStartDistance; nieblaFin = RenderSettings.fogEndDistance; }
        float lluvia = Clima.LluviaFuerza;
        float perfil = PerfilGrafico.Ahora.niebla;   // en gama baja la niebla llega antes (y se dibuja menos)
        RenderSettings.fogStartDistance = nieblaInicio * perfil * Mathf.Lerp(1f, 0.25f, Mathf.Max(nubes * 0.5f, lluvia));
        RenderSettings.fogEndDistance = nieblaFin * perfil * Mathf.Lerp(1f, 0.45f, Mathf.Max(nubes * 0.4f, lluvia));

        if (sol != null)
        {
            if (altura > -0.05f)
            {
                // De día: el sol cruza el cielo; más cálido cerca del horizonte
                sol.transform.rotation = Quaternion.Euler(Mathf.Clamp(ang, 3f, 177f) * 0.8f + 4f, -30f + ang * 0.5f, 0f);
                float fuerza = Mathf.Clamp01(altura * 3f);
                sol.intensity = Mathf.Lerp(0.25f, intensidadDia, fuerza) * (1f - 0.55f * Clima.Nubes) + Clima.Destello * 1.5f;
                sol.color = Color.Lerp(new Color(1f, 0.6f, 0.35f), new Color(1f, 0.96f, 0.9f), fuerza);
            }
            else
            {
                // De noche: la luna, alta y azulada, con poca luz
                float angLuna = Mathf.Repeat(ang - 180f, 360f);
                sol.transform.rotation = Quaternion.Euler(25f + Mathf.Sin(angLuna * Mathf.Deg2Rad) * 35f, 150f + angLuna * 0.3f, 0f);
                sol.intensity = 0.28f * (1f - 0.6f * Clima.Nubes) + Clima.Destello * 1.2f;
                sol.color = new Color(0.55f, 0.65f, 0.95f);
            }
        }
        // Luz del ambiente: nunca negra del todo, para poder jugar de noche
        float dia = Mathf.Clamp01(altura * 2.5f + 0.15f);
        Color ambiente = Color.Lerp(new Color(0.16f, 0.19f, 0.30f), new Color(0.58f, 0.62f, 0.66f), dia);
        ambiente *= 1f - 0.35f * Clima.Nubes;
        RenderSettings.ambientLight = ambiente + new Color(0.5f, 0.55f, 0.65f) * Clima.Destello;
    }

    void Avisos()
    {
        int fase = Hora >= 5.7f && Hora < 6.2f ? 0 : Hora >= 18.4f && Hora < 18.9f ? 1 : -1;
        if (fase == avisoFase) return;
        avisoFase = fase;
        if (fase == 0) Notificaciones.Mostrar("Amanece", Notificaciones.Tipo.Info, 4f);
        else if (fase == 1) Notificaciones.Mostrar("Cae la noche", Notificaciones.Tipo.Info, 4f);
    }
}
