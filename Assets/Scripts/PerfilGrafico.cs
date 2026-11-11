using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Perfil gráfico según el aparato. En el teléfono el juego mira cuánta memoria y cuántos núcleos tiene y si la
// tarjeta gráfica habla Vulkan, y elige Bajo, Medio o Alto. El perfil fija de una vez la resolución de render,
// el suavizado, las sombras y los cuadros por segundo (que después se pueden tocar uno por uno en Opciones),
// y además lo que no tiene opción propia: HDR, luces extra, distancia de dibujo de los detalles, niebla y lluvia.
//
// Con el perfil en Automático, en el teléfono también hay resolución dinámica: si el juego no llega a los
// cuadros por segundo que pide, baja la resolución de a poco (hasta 50 %); si le sobra, la vuelve a subir.
public class PerfilGrafico : MonoBehaviour
{
    public enum Nivel { Bajo, Medio, Alto }

    public struct Valores
    {
        public float escala;     // resolución de render
        public int suavizado;    // MSAA
        public bool sombras;
        public float sombrasDistancia;
        public int fps;          // 0 = sin límite
        public bool hdr;
        public int lucesExtra;   // faroles, barriles en llamas, altares
        public float distancia;  // multiplica la distancia a la que se dejan de dibujar los detalles
        public float niebla;     // multiplica la distancia de la niebla (más niebla = se dibuja menos)
        public float particulas; // multiplica la lluvia
    }

    public static Valores De(Nivel n)
    {
        bool movil = Application.isMobilePlatform;
        switch (n)
        {
            case Nivel.Bajo:
                return new Valores { escala = 0.6f, suavizado = 1, sombras = false, sombrasDistancia = 0f, fps = 30, hdr = false,
                                     lucesExtra = 1, distancia = 0.65f, niebla = 0.8f, particulas = 0.35f };
            case Nivel.Medio:
                return new Valores { escala = movil ? 0.8f : 0.9f, suavizado = movil ? 1 : 2, sombras = true, sombrasDistancia = 50f,
                                     fps = movil ? 30 : 0, hdr = !movil, lucesExtra = 2, distancia = 0.85f, niebla = 1f, particulas = 0.7f };
            default:
                return new Valores { escala = movil ? 0.9f : 1f, suavizado = movil ? 2 : 4, sombras = true,
                                     sombrasDistancia = movil ? 70f : Rendimiento.DistanciaSombras,
                                     fps = movil ? 60 : 0, hdr = true, lucesExtra = movil ? 4 : 8, distancia = 1f, niebla = 1f, particulas = 1f };
        }
    }

    // Lo que el aparato puede, a ojo: en PC siempre Alto
    public static Nivel Detectado
    {
        get
        {
            if (detectado.HasValue) return detectado.Value;
            Nivel n;
            if (!Application.isMobilePlatform) n = Nivel.Alto;
            else
            {
                int memoria = SystemInfo.systemMemorySize;      // MB
                int nucleos = SystemInfo.processorCount;
                bool vulkan = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan
                           || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal;
                if (memoria < 3500 || nucleos < 6 || !vulkan) n = Nivel.Bajo;    // gama baja o teléfono viejo (solo OpenGL)
                else if (memoria >= 7500 && nucleos >= 8) n = Nivel.Alto;
                else n = Nivel.Medio;
            }
            detectado = n;
            return n;
        }
    }
    static Nivel? detectado;

    public static bool Automatico => Preferencias.Perfil < 0;
    public static Nivel Actual => Automatico ? Detectado : (Nivel)Mathf.Clamp(Preferencias.Perfil, 0, 2);
    public static Valores Ahora => De(Actual);

    public static string Nombre(Nivel n) => n == Nivel.Bajo ? "Bajo" : n == Nivel.Medio ? "Medio" : "Alto";

    // Al elegir un perfil, sus valores pasan a las opciones sueltas (resolución, suavizado, sombras, FPS)
    public static void Elegir(int perfil)
    {
        Preferencias.Perfil = perfil;
        Valores v = Ahora;
        Preferencias.EscalaRender = v.escala;
        Preferencias.Suavizado = v.suavizado;
        Preferencias.Sombras = v.sombras;
        Preferencias.LimiteFps = v.fps;
        escalaDinamica = -1f;
    }

    // Lo que el perfil fija y no tiene opción propia (lo llama Preferencias.AplicarAlJuego)
    public static void Aplicar(UniversalRenderPipelineAsset urp)
    {
        Valores v = Ahora;
        if (urp != null)
        {
            urp.supportsHDR = v.hdr;
            urp.maxAdditionalLightsCount = v.lucesExtra;
            if (Preferencias.Sombras) urp.shadowDistance = v.sombrasDistancia > 0f ? v.sombrasDistancia : Rendimiento.DistanciaSombras;
            if (escalaDinamica > 0f) urp.renderScale = Mathf.Min(escalaDinamica, Preferencias.EscalaRender);
        }
        // En el teléfono la sincronización vertical no cuenta: manda el límite de FPS (60 si no hay límite)
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Preferencias.LimiteFps > 0 ? Preferencias.LimiteFps : 60;
            // Física a 30 pasos por segundo (no 50): a 30 FPS eran casi dos pasos por cuadro. Los barcos se interpolan.
            // Y si un cuadro se atrasa mucho, no más de 3 pasos de física para alcanzarlo (si no, el atraso se agranda)
            Time.fixedDeltaTime = 1f / 30f;
            Time.maximumDeltaTime = 0.1f;
        }
        Rendimiento.Reconfigurar();
    }

    // ---------- Resolución dinámica (solo teléfono y perfil automático) ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (!Application.isMobilePlatform) return;
        var g = new GameObject("Resolución dinámica");
        DontDestroyOnLoad(g);
        g.AddComponent<PerfilGrafico>();
    }

    static float escalaDinamica = -1f;   // -1 = sin tocar (usa la de Opciones)
    float tiempo; int cuadros; int buenos;

    void Update()
    {
        if (!Automatico || Time.timeScale <= 0f) { tiempo = 0f; cuadros = 0; return; }
        float dt = Time.unscaledDeltaTime;
        if (dt > 0.5f) { tiempo = 0f; cuadros = 0; return; }   // una carga o una pausa: no cuenta
        tiempo += dt; cuadros++;
        if (tiempo < 4f) return;

        float fps = cuadros / tiempo;
        tiempo = 0f; cuadros = 0;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        float objetivo = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30f;
        float tope = Preferencias.EscalaRender;
        float actual = escalaDinamica > 0f ? escalaDinamica : tope;

        if (fps < objetivo * 0.85f && actual > 0.5f)
        {
            escalaDinamica = Mathf.Max(0.5f, actual - 0.05f);
            urp.renderScale = escalaDinamica;
            buenos = 0;
        }
        else if (fps >= objetivo * 0.97f && actual < tope)
        {
            // Subir con más calma que bajar (que no se quede subiendo y bajando)
            if (++buenos >= 3)
            {
                escalaDinamica = Mathf.Min(tope, actual + 0.05f);
                urp.renderScale = escalaDinamica;
                buenos = 0;
            }
        }
        else buenos = 0;
    }

    public static string EstadoDinamica =>
        escalaDinamica > 0f ? "resolución ajustada a " + Mathf.RoundToInt(escalaDinamica * 100f) + " %" : null;
}
