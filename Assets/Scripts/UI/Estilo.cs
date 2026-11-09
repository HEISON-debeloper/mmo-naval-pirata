using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Identidad visual de toda la interfaz, en un solo lugar: colores, fuentes, tamaños y texturas
// (madera oscura, pergamino, cuero y bronce, todas hechas por código). Las ventanas y el HUD
// toman todo de aquí, así cambiar un color aquí cambia la interfaz entera.
public static class Estilo
{
    // ---------- Paleta ----------
    public static readonly Color Madera = new Color(0.16f, 0.11f, 0.08f);        // fondo de paneles
    public static readonly Color MaderaClara = new Color(0.24f, 0.17f, 0.12f);
    public static readonly Color Cuero = new Color(0.22f, 0.15f, 0.10f);         // botones
    public static readonly Color CueroClaro = new Color(0.31f, 0.22f, 0.15f);
    public static readonly Color Bronce = new Color(0.55f, 0.42f, 0.23f);        // bordes
    public static readonly Color BronceClaro = new Color(0.79f, 0.64f, 0.36f);
    public static readonly Color Oro = new Color(0.89f, 0.77f, 0.47f);           // títulos, resaltes
    public static readonly Color OroVivo = new Color(1.0f, 0.88f, 0.55f);
    public static readonly Color Pergamino = new Color(0.90f, 0.84f, 0.69f);     // paneles claros (mapa, notas)
    public static readonly Color PergaminoOscuro = new Color(0.78f, 0.70f, 0.54f);
    public static readonly Color Tinta = new Color(0.17f, 0.12f, 0.08f);         // texto sobre pergamino
    public static readonly Color Texto = new Color(0.94f, 0.90f, 0.82f);
    public static readonly Color TextoTenue = new Color(0.72f, 0.66f, 0.56f);
    public static readonly Color TextoApagado = new Color(0.52f, 0.47f, 0.40f);
    public static readonly Color Bien = new Color(0.56f, 0.82f, 0.52f);
    public static readonly Color Mal = new Color(0.94f, 0.45f, 0.36f);
    public static readonly Color Ambar = new Color(0.95f, 0.76f, 0.35f);
    public static readonly Color Info = new Color(0.52f, 0.78f, 0.92f);
    public static readonly Color Peligro = new Color(0.62f, 0.14f, 0.12f);
    public static readonly Color ColVida = new Color(0.78f, 0.21f, 0.19f);
    public static readonly Color ColHambre = new Color(0.86f, 0.53f, 0.18f);
    public static readonly Color ColSed = new Color(0.24f, 0.56f, 0.86f);
    public static readonly Color ColCasco = new Color(0.66f, 0.46f, 0.24f);
    public static readonly Color ColVelas = new Color(0.90f, 0.86f, 0.74f);
    public static readonly Color Sombra = new Color(0f, 0f, 0f, 0.55f);

    // ---------- Tamaños ----------
    public const float Radio = 6f;
    public const float Espacio = 10f;
    public static float TamTexto => 15f * Preferencias.TamanoTexto;
    public static float TamTextoChico => 13f * Preferencias.TamanoTexto;
    public static float TamTitulo => 21f * Preferencias.TamanoTexto;
    public static float TamTituloGrande => 30f * Preferencias.TamanoTexto;

    // ---------- Fuentes ----------
    static Font fuenteTitulo, fuenteTexto, fuenteTextoMedia, fuenteTextoNegrita;
    public static Font FuenteTitulo => fuenteTitulo != null ? fuenteTitulo : (fuenteTitulo = Resources.Load<Font>("UI/Fuentes/Cinzel-Bold"));
    public static Font FuenteTexto => fuenteTexto != null ? fuenteTexto : (fuenteTexto = Resources.Load<Font>("UI/Fuentes/AlegreyaSans-Regular"));
    public static Font FuenteMedia => fuenteTextoMedia != null ? fuenteTextoMedia : (fuenteTextoMedia = Resources.Load<Font>("UI/Fuentes/AlegreyaSans-Medium"));
    public static Font FuenteNegrita => fuenteTextoNegrita != null ? fuenteTextoNegrita : (fuenteTextoNegrita = Resources.Load<Font>("UI/Fuentes/AlegreyaSans-Bold"));

    public static void Fuente(VisualElement e, Font f)
    {
        if (f != null) e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(f));
    }

    // ---------- Texturas hechas por código (se crean una vez) ----------
    static Texture2D texMadera, texPergamino, texCuero, texMetal, texRuido;
    public static Texture2D TexMadera => texMadera != null ? texMadera : (texMadera = CrearMadera());
    public static Texture2D TexPergamino => texPergamino != null ? texPergamino : (texPergamino = CrearPergamino());
    public static Texture2D TexCuero => texCuero != null ? texCuero : (texCuero = CrearCuero());
    public static Texture2D TexMetal => texMetal != null ? texMetal : (texMetal = CrearMetal());

    static Texture2D Nueva(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        return t;
    }

    // Madera oscura: vetas alargadas con un poco de ruido
    static Texture2D CrearMadera()
    {
        const int n = 256;
        var t = Nueva(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float veta = Mathf.PerlinNoise(u * 2.5f + 7f, v * 28f + 3f);           // vetas horizontales
                float fino = Mathf.PerlinNoise(u * 40f, v * 160f) * 0.5f;
                float g = Mathf.Clamp01(veta * 0.75f + fino * 0.25f);
                Color c = Color.Lerp(new Color(0.13f, 0.09f, 0.06f), new Color(0.23f, 0.16f, 0.11f), g);
                px[y * n + x] = c;
            }
        t.SetPixels(px); t.Apply(true);
        return t;
    }

    // Pergamino: beige con manchas suaves y fibras
    static Texture2D CrearPergamino()
    {
        const int n = 256;
        var t = Nueva(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float mancha = Mathf.PerlinNoise(u * 4f + 11f, v * 4f + 5f);
                float fibra = Mathf.PerlinNoise(u * 60f, v * 60f + 9f);
                float g = mancha * 0.7f + fibra * 0.3f;
                Color c = Color.Lerp(new Color(0.80f, 0.72f, 0.55f), new Color(0.93f, 0.88f, 0.74f), g);
                px[y * n + x] = c;
            }
        t.SetPixels(px); t.Apply(true);
        return t;
    }

    // Cuero: grano fino oscuro
    static Texture2D CrearCuero()
    {
        const int n = 128;
        var t = Nueva(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float g = Mathf.PerlinNoise(u * 24f + 3f, v * 24f + 1f) * 0.6f + Mathf.PerlinNoise(u * 90f, v * 90f) * 0.4f;
                Color c = Color.Lerp(new Color(0.19f, 0.13f, 0.09f), new Color(0.28f, 0.20f, 0.14f), g);
                px[y * n + x] = c;
            }
        t.SetPixels(px); t.Apply(true);
        return t;
    }

    // Metal cepillado (bronce)
    static Texture2D CrearMetal()
    {
        const int n = 128;
        var t = Nueva(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float g = Mathf.PerlinNoise(u * 90f, v * 3f) * 0.5f + Mathf.PerlinNoise(u * 6f, v * 6f) * 0.5f;
                Color c = Color.Lerp(new Color(0.48f, 0.36f, 0.19f), new Color(0.80f, 0.65f, 0.38f), g);
                px[y * n + x] = c;
            }
        t.SetPixels(px); t.Apply(true);
        return t;
    }

    // ---------- Ayudas de estilo ----------
    public static void Fondo(VisualElement e, Texture2D tex, Color tinte, float alfa = 1f)
    {
        e.style.backgroundImage = new StyleBackground(tex);
        e.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        e.style.backgroundSize = new BackgroundSize(new Length(tex.width, LengthUnit.Pixel), new Length(tex.height, LengthUnit.Pixel));
        e.style.unityBackgroundImageTintColor = new Color(tinte.r, tinte.g, tinte.b, alfa);
    }

    public static void Borde(VisualElement e, Color c, float ancho = 1f, float radio = Radio)
    {
        e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = ancho;
        e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        Redondear(e, radio);
    }

    public static void Redondear(VisualElement e, float radio)
    {
        e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radio;
    }

    public static void Relleno(VisualElement e, float todos) => Relleno(e, todos, todos);
    public static void Relleno(VisualElement e, float vertical, float horizontal)
    {
        e.style.paddingTop = e.style.paddingBottom = vertical;
        e.style.paddingLeft = e.style.paddingRight = horizontal;
    }

    public static void Margen(VisualElement e, float todos) => Margen(e, todos, todos);
    public static void Margen(VisualElement e, float vertical, float horizontal)
    {
        e.style.marginTop = e.style.marginBottom = vertical;
        e.style.marginLeft = e.style.marginRight = horizontal;
    }

    // Panel de madera con borde de bronce (la base de ventanas y tarjetas)
    public static void PanelMadera(VisualElement e, float alfa = 0.94f)
    {
        float a = alfa;
        if (Preferencias.AltoContraste) a = 1f;
        Fondo(e, TexMadera, Color.white, a);
        Borde(e, Preferencias.AltoContraste ? OroVivo : Bronce, Preferencias.AltoContraste ? 2f : 1.5f);
    }

    // Panel de pergamino (texto oscuro encima)
    public static void PanelPergamino(VisualElement e, float alfa = 0.97f)
    {
        Fondo(e, TexPergamino, Color.white, alfa);
        Borde(e, new Color(0.45f, 0.33f, 0.18f), 1.5f);
    }

    public static Label Etiqueta(string texto, float tam = -1f, Color? color = null, Font fuente = null)
    {
        var l = new Label(texto);
        l.style.fontSize = tam > 0f ? tam : TamTexto;
        l.style.color = color ?? Texto;
        l.style.whiteSpace = WhiteSpace.Normal;
        l.enableRichText = true;
        Fuente(l, fuente ?? FuenteTexto);
        Margen(l, 0f); Relleno(l, 0f);
        return l;
    }

    public static Label Titulo(string texto, float tam = -1f, Color? color = null)
    {
        var l = Etiqueta(texto, tam > 0f ? tam : TamTitulo, color ?? Oro, FuenteTitulo);
        l.style.unityTextAlign = TextAnchor.MiddleLeft;
        return l;
    }

    public static Label Pequena(string texto, Color? color = null) => Etiqueta(texto, TamTextoChico, color ?? TextoTenue);

    public static string Col(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

    // Fila o columna con separación entre hijos
    public static VisualElement Fila(float espacio = Espacio, Align alinear = Align.Center)
    {
        var f = new VisualElement();
        f.style.flexDirection = FlexDirection.Row;
        f.style.alignItems = alinear;
        return f;
    }

    public static VisualElement Columna()
    {
        var c = new VisualElement();
        c.style.flexDirection = FlexDirection.Column;
        return c;
    }

    public static VisualElement Separador(bool vertical = false)
    {
        var s = new VisualElement();
        if (vertical) { s.style.width = 1f; s.style.alignSelf = Align.Stretch; s.style.marginLeft = s.style.marginRight = 6f; }
        else { s.style.height = 1f; s.style.marginTop = s.style.marginBottom = 6f; }
        s.style.backgroundColor = new Color(Bronce.r, Bronce.g, Bronce.b, 0.45f);
        return s;
    }

    public static VisualElement Hueco(float ancho = 0f, float alto = 0f, bool crecer = false)
    {
        var h = new VisualElement();
        if (ancho > 0f) h.style.width = ancho;
        if (alto > 0f) h.style.height = alto;
        if (crecer) h.style.flexGrow = 1f;
        h.pickingMode = PickingMode.Ignore;
        return h;
    }

    // Remaches de bronce en las esquinas de un panel (detalle de instrumento naval)
    public static void Remaches(VisualElement panel, float tam = 7f, float margen = 5f)
    {
        foreach (var (x, y) in new[] { (true, true), (false, true), (true, false), (false, false) })
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore };
            r.style.position = Position.Absolute;
            r.style.width = tam; r.style.height = tam;
            if (x) r.style.left = margen; else r.style.right = margen;
            if (y) r.style.top = margen; else r.style.bottom = margen;
            Redondear(r, tam / 2f);
            r.style.backgroundColor = BronceClaro;
            r.style.borderTopWidth = r.style.borderLeftWidth = 1f;
            r.style.borderTopColor = r.style.borderLeftColor = new Color(1f, 0.93f, 0.7f, 0.6f);
            r.style.borderBottomWidth = r.style.borderRightWidth = 1f;
            r.style.borderBottomColor = r.style.borderRightColor = new Color(0.2f, 0.12f, 0.05f, 0.7f);
            panel.Add(r);
        }
    }

    // Duración de animaciones según la preferencia (0 = sin animaciones)
    public static int Ms(int ms) => Preferencias.ReducirMovimiento ? 0 : Mathf.RoundToInt(ms * Preferencias.Animaciones);
}
