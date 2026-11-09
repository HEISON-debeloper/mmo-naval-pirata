using UnityEngine;

// Capa de compatibilidad del HUD antiguo (IMGUI). La interfaz nueva está en Scripts/UI (HudNuevo, Ventanas…).
// Aquí quedan la paleta, los estilos y las piezas de dibujo que todavía usan las pantallas IMGUI que faltan por renovar
// (el menú de fabricación y el mapa), y el OnGUI que las dibuja. Cuando todas estén renovadas, este archivo se va.
[DefaultExecutionOrder(100)]
public class Hud : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("HUD (IMGUI)").AddComponent<Hud>();

    public static float Escala { get; private set; } = 1f;
    public static float Ancho => Screen.width / Escala;
    public static float Alto => Screen.height / Escala;

    // ---------- Paleta (la misma de Estilo, para que las pantallas viejas no desentonen) ----------
    public static readonly Color Fondo = new Color(0.16f, 0.11f, 0.08f, 0.94f);
    public static readonly Color FondoClaro = new Color(1f, 1f, 1f, 0.07f);
    public static readonly Color Laton = new Color(0.55f, 0.42f, 0.23f, 0.9f);
    public static Color Oro => Estilo.Oro;
    public static Color Texto => Estilo.Texto;
    public static Color Tenue => Estilo.TextoTenue;
    public static Color Bien => Estilo.Bien;
    public static Color Mal => Estilo.Mal;
    public static Color Ambar => Estilo.Ambar;
    public static Color ColVida => Estilo.ColVida;
    public static Color ColHambre => Estilo.ColHambre;
    public static Color ColSed => Estilo.ColSed;
    public static Color ColCasco => Estilo.ColCasco;

    public static GUIStyle Normal, Pequeno, Titulo, Centrado, Derecha, Tecla;
    static int skinId;

    static void Estilos()
    {
        if (Normal != null && skinId == GUI.skin.GetInstanceID()) return;
        skinId = GUI.skin.GetInstanceID();
        Normal = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = false, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        Normal.normal.textColor = Texto;
        if (Estilo.FuenteTexto != null) Normal.font = Estilo.FuenteTexto;
        Pequeno = new GUIStyle(Normal) { fontSize = 11 };
        Pequeno.normal.textColor = Tenue;
        Titulo = new GUIStyle(Normal) { fontSize = 14, fontStyle = FontStyle.Bold };
        Titulo.normal.textColor = Oro;
        if (Estilo.FuenteTitulo != null) { Titulo.font = Estilo.FuenteTitulo; Titulo.fontStyle = FontStyle.Normal; }
        Centrado = new GUIStyle(Normal) { alignment = TextAnchor.MiddleCenter };
        Derecha = new GUIStyle(Normal) { alignment = TextAnchor.MiddleRight };
        Tecla = new GUIStyle(Normal) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Tecla.normal.textColor = new Color(0.08f, 0.09f, 0.10f);
    }

    // ---------- Piezas de dibujo ----------
    public static void Relleno(Rect r, Color c)
    {
        Color antes = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    public static void Caja(Rect r, bool borde = true)
    {
        Color antes = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, Fondo.a);
        GUI.DrawTexture(r, Estilo.TexMadera, ScaleMode.StretchToFill);
        GUI.color = antes;
        if (!borde) return;
        Relleno(new Rect(r.x, r.y, r.width, 1.5f), Laton);
        Relleno(new Rect(r.x, r.yMax - 1.5f, r.width, 1.5f), Laton);
        Relleno(new Rect(r.x, r.y, 1.5f, r.height), Laton);
        Relleno(new Rect(r.xMax - 1.5f, r.y, 1.5f, r.height), Laton);
    }

    public static void Barra(Rect r, float frac, Color c, string etiqueta, string valor)
    {
        Relleno(r, new Color(0f, 0f, 0f, 0.45f));
        Relleno(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), c);
        GUI.Label(new Rect(r.x + 6f, r.y, r.width - 12f, r.height), etiqueta, Normal);
        GUI.Label(new Rect(r.x + 6f, r.y, r.width - 12f, r.height), valor, Derecha);
    }

    public static void BadgeTecla(Rect r, string tecla)
    {
        Relleno(r, Oro);
        GUI.Label(r, tecla, Tecla);
    }

    public static string Col(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

    void OnGUI()
    {
        Personaje p = Personaje.Instancia;
        if (p == null || MenuPrincipal.Visible) return;
        Estilos();
        Escala = Mathf.Clamp(Screen.height / 720f, 0.8f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(Escala, Escala, 1f));
        GUI.depth = 0;

        Navegacion nav = Navegacion.Instancia;
        if (nav != null && Navegacion.MapaAbierto) nav.DibujarMapa(p);
        else
        {
            Fabricacion fab = p.GetComponent<Fabricacion>();
            if (Fabricacion.MenuAbierto && fab != null) fab.DibujarMenu();
        }
        GUI.matrix = Matrix4x4.identity;
    }
}
