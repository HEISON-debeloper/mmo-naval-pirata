using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// El lienzo de toda la interfaz nueva (UI Toolkit). Se crea solo al arrancar: PanelSettings, UIDocument y EventSystem
// por código, sin nada en la escena. Capas de abajo arriba: fondo (menú principal), HUD, ventanas, modal, avisos, tooltip.
// Respeta la zona segura del teléfono y la escala elegida en Opciones.
public class Lienzo : MonoBehaviour
{
    public static Lienzo I { get; private set; }
    public static UIDocument Doc { get; private set; }
    public static PanelSettings Ajustes { get; private set; }
    public static VisualElement Raiz { get; private set; }       // raíz del documento (toda la pantalla)
    public static VisualElement Area { get; private set; }       // dentro de la zona segura
    public static VisualElement CapaFondo { get; private set; }  // menú principal, pausa (cubre todo)
    public static VisualElement CapaHud { get; private set; }
    public static VisualElement CapaVentanas { get; private set; }
    public static VisualElement CapaModal { get; private set; }
    public static VisualElement CapaAvisos { get; private set; }
    public static VisualElement CapaTooltip { get; private set; }
    public static bool Listo => Raiz != null && Raiz.panel != null;

    public static bool EsMovil => Application.isMobilePlatform;
    const float AnchoReferencia = 1600f, AltoReferencia = 900f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear()
    {
        if (I != null) return;
        var g = new GameObject("Interfaz");
        I = g.AddComponent<Lienzo>();
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Ajustes = ScriptableObject.CreateInstance<PanelSettings>();
        Ajustes.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Tema");
        Ajustes.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        Ajustes.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        Ajustes.match = 0.5f;
        Ajustes.sortingOrder = 10;
        Ajustes.clearColor = false;
        AplicarEscala();

        Doc = gameObject.AddComponent<UIDocument>();
        Doc.panelSettings = Ajustes;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        Ventanas.Reiniciar();
        Raiz = Doc.rootVisualElement;
        Raiz.name = "raiz";
        Raiz.style.flexGrow = 1f;
        Raiz.pickingMode = PickingMode.Ignore;
        var hoja = Resources.Load<StyleSheet>("UI/Estilos");
        if (hoja != null) Raiz.styleSheets.Add(hoja);
        else Debug.LogWarning("No se encontró Resources/UI/Estilos.uss");

        Area = new VisualElement { name = "area", pickingMode = PickingMode.Ignore };
        Area.style.flexGrow = 1f;
        Raiz.Add(Area);

        CapaFondo = Capa("capa-fondo");
        CapaHud = Capa("capa-hud");
        CapaVentanas = Capa("capa-ventanas");
        CapaModal = Capa("capa-modal");
        CapaAvisos = Capa("capa-avisos");
        CapaTooltip = Capa("capa-tooltip");

        Raiz.RegisterCallback<GeometryChangedEvent>(_ => AplicarZonaSegura());
        Preferencias.alCambiar += AlCambiarPreferencias;
        AlCambiarPreferencias();
        Preferencias.AplicarAlJuego();
    }

    VisualElement Capa(string nombre)
    {
        var c = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
        c.style.position = Position.Absolute;
        c.style.left = c.style.right = c.style.top = c.style.bottom = 0f;
        Area.Add(c);
        return c;
    }

    void OnDestroy()
    {
        Preferencias.alCambiar -= AlCambiarPreferencias;
        if (I == this) I = null;
    }

    // Escala de la interfaz: resolución de referencia dividida entre la escala elegida (más escala = todo más grande).
    // En el teléfono la interfaz es un 30 % más grande por defecto para que se pueda tocar.
    static void AplicarEscala()
    {
        float e = Preferencias.EscalaUI * (EsMovil ? 1.3f : 1f);
        Ajustes.referenceResolution = new Vector2Int(Mathf.RoundToInt(AnchoReferencia / e), Mathf.RoundToInt(AltoReferencia / e));
    }

    void AlCambiarPreferencias()
    {
        AplicarEscala();
        Raiz.EnableInClassList("sin-animaciones", Preferencias.ReducirMovimiento || Preferencias.Animaciones <= 0.01f);
        Raiz.EnableInClassList("alto-contraste", Preferencias.AltoContraste);
        Raiz.EnableInClassList("movil", EsMovil);
        AplicarZonaSegura();
    }

    // Píxeles de pantalla → unidades del panel
    public static float PorPixel => Raiz != null && Screen.width > 0 && Raiz.resolvedStyle.width > 0f ? Raiz.resolvedStyle.width / Screen.width : 1f;

    void AplicarZonaSegura()
    {
        if (Area == null) return;
        Rect s = Screen.safeArea;
        float k = PorPixel;
        Area.style.paddingLeft = s.xMin * k;
        Area.style.paddingRight = (Screen.width - s.xMax) * k;
        Area.style.paddingBottom = s.yMin * k;
        Area.style.paddingTop = (Screen.height - s.yMax) * k;
    }

    // Tamaño útil (zona segura) en unidades del panel
    public static Vector2 Tamano => Area != null ? new Vector2(Area.resolvedStyle.width - Area.resolvedStyle.paddingLeft - Area.resolvedStyle.paddingRight,
                                                              Area.resolvedStyle.height - Area.resolvedStyle.paddingTop - Area.resolvedStyle.paddingBottom) : new Vector2(AnchoReferencia, AltoReferencia);

    // Posición del puntero en unidades del panel (origen arriba a la izquierda, dentro de Area)
    public static Vector2 Puntero
    {
        get
        {
            Vector2 px = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 p = RuntimePanelUtils.ScreenToPanel(Raiz.panel, new Vector2(px.x, Screen.height - px.y));
            if (Area != null) p -= new Vector2(Area.resolvedStyle.paddingLeft, Area.resolvedStyle.paddingTop);
            return p;
        }
    }

    // ¿El puntero está sobre algún elemento de la interfaz que reciba clics? (para que un clic en un botón no sea un espadazo)
    public static bool PunteroSobreUI
    {
        get
        {
            if (!Listo) return false;
            Vector2 px = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            VisualElement e = Raiz.panel.Pick(RuntimePanelUtils.ScreenToPanel(Raiz.panel, new Vector2(px.x, Screen.height - px.y)));
            return e != null && e != Raiz && e != Area;
        }
    }

    void Update()
    {
        Ventanas.Actualizar();
        Pausa.ControlarTiempo();
        Notificaciones.Actualizar();
        AtajosUI.Actualizar();
        if (Teclas.De(Accion.Pausa) != Key.Escape && Teclas.Pulsada(Accion.Pausa, true) && Personaje.Instancia != null && !MenuPrincipal.Visible) Ventanas.Alternar(Pausa.I);
    }

    // Animación sencilla de un valor 0→1 (usa el reloj sin escala para que funcione en pausa)
    public static void Animar(VisualElement e, int ms, System.Action<float> paso, System.Action fin = null)
    {
        if (ms <= 0 || e == null || e.panel == null) { paso?.Invoke(1f); fin?.Invoke(); return; }
        var a = e.experimental.animation.Start(0f, 1f, ms, (_, v) => paso(v));
        if (fin != null) a.OnCompleted(fin);
    }

    public static float Suave(float t) => t * t * (3f - 2f * t);               // entrada y salida suaves
    public static float Rebote(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - Mathf.Sin(t * Mathf.PI) * 0.25f); }
}
