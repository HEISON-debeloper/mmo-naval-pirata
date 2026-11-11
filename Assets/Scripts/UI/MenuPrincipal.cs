using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menú principal cinematográfico: al arrancar, la cámara gira despacio alrededor de tu barco (o de ti) mientras el título
// y el menú aparecen sobre una viñeta oscura. Continuar (si hay partida), Nueva partida (con confirmación), Opciones,
// Créditos y Salir. Muestra cuándo se guardó la partida. Se vuelve aquí desde la pausa.
public static class MenuPrincipal
{
    static VisualElement raiz, columna;
    static Label infoPartida;
    static bool mostradoAlArrancar;
    public static bool Visible { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void AlCargar()
    {
        Visible = false; raiz = null;
        // Solo al abrir el juego (no al recargar la escena para una partida nueva)
        if (mostradoAlArrancar) return;
        mostradoAlArrancar = true;
        new GameObject("Menú principal (arranque)").AddComponent<Arranque>();
    }

    class Arranque : MonoBehaviour
    {
        int cuadros;
        void Update()
        {
            if (++cuadros < 3 || !Lienzo.Listo) return;   // el lienzo se arma el primer cuadro
            Mostrar();
            Destroy(gameObject);
        }
    }

    public static void Mostrar()
    {
        if (!Lienzo.Listo) return;
        Ventanas.CerrarTodas();
        if (raiz == null || raiz.panel == null) Construir();
        Visible = true;
        raiz.style.display = DisplayStyle.Flex;
        CamaraBarco.Orbitar = true;
        ActualizarInfo();
        raiz.style.opacity = 0f;
        Lienzo.Animar(raiz, Mathf.Max(1, Estilo.Ms(900)), v => raiz.style.opacity = v);
        columna.style.translate = new Translate(0f, -20f);
        Lienzo.Animar(columna, Estilo.Ms(1100), v => columna.style.translate = new Translate(0f, -20f * (1f - Lienzo.Suave(v))));
        Sonidos.Reproducir(Sonidos.Tipo.Descubrir, 0.7f);
    }

    public static void Ocultar()
    {
        if (raiz == null) return;
        CamaraBarco.Orbitar = false;
        Visible = false;
        Lienzo.Animar(raiz, Estilo.Ms(450), v => raiz.style.opacity = 1f - v, () => { if (!Visible) raiz.style.display = DisplayStyle.None; });
        Preferencias.VistoMenuPrincipal = true; Preferencias.Guardar();
        Sonidos.Reproducir(Sonidos.Tipo.Abrir);
        if (!Preferencias.VistoMenuPrincipal || !Guardado.HayPartida)
            Notificaciones.Mostrar("Pulsa " + Teclas.Nombre(Accion.Ayuda) + " para ver los controles", Notificaciones.Tipo.Info, 6f, "libro");
    }

    static void Construir()
    {
        raiz = new VisualElement { name = "menu-principal" };
        raiz.style.position = Position.Absolute;
        raiz.style.left = raiz.style.right = raiz.style.top = raiz.style.bottom = 0f;
        raiz.pickingMode = PickingMode.Position;
        // Viñeta: oscuro a la izquierda (donde va el menú), el mar a la vista a la derecha
        var vineta = new VisualElement { pickingMode = PickingMode.Ignore };
        vineta.style.position = Position.Absolute; vineta.style.left = vineta.style.right = vineta.style.top = vineta.style.bottom = 0f;
        vineta.style.backgroundImage = new StyleBackground(Degradado());
        vineta.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
        raiz.Add(vineta);

        columna = Estilo.Columna();
        columna.style.position = Position.Absolute; columna.style.left = 80f; columna.style.top = 0f; columna.style.bottom = 0f;
        columna.style.justifyContent = Justify.Center; columna.style.width = 460f;
        raiz.Add(columna);

        var sombra = new TextShadow { color = new Color(0f, 0f, 0f, 0.9f), offset = new Vector2(0f, 3f), blurRadius = 8f };
        var titulo = Estilo.Titulo("MMO NAVAL", 64f * Preferencias.TamanoTexto, Estilo.OroVivo);
        titulo.style.letterSpacing = 6f; titulo.style.textShadow = sombra;
        columna.Add(titulo);
        var titulo2 = Estilo.Titulo("PIRATA", 64f * Preferencias.TamanoTexto, Estilo.Oro);
        titulo2.style.letterSpacing = 18f; titulo2.style.marginTop = -14f; titulo2.style.textShadow = sombra;
        columna.Add(titulo2);
        var lema = Estilo.Etiqueta("Construye tu barco, cruza el mar, reina en las islas.", Estilo.TamTexto * 1.1f, Estilo.Texto, Estilo.FuenteMedia);
        lema.style.marginTop = 4f; lema.style.marginBottom = 34f;
        lema.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.9f), offset = new Vector2(0f, 1f), blurRadius = 4f };
        columna.Add(lema);

        void B(string texto, string icono, System.Action a, Componentes.Variante v = Componentes.Variante.Normal, string nombre = null)
        {
            var b = Componentes.Boton(texto, a, icono, v, 50f);
            b.style.width = 300f; b.style.marginBottom = 10f;
            if (nombre != null) b.name = nombre;
            columna.Add(b);
        }
        B("Continuar", "play", Ocultar, Componentes.Variante.Primario, "continuar");
        B("Nueva partida", "bandera", NuevaPartida, Componentes.Variante.Normal, "nueva");
        B("Recuperar la partida anterior", "pergamino", RecuperarAnterior, Componentes.Variante.Normal, "anterior");
        B("Opciones", "engranaje", () => Ventanas.Abrir(VentanaOpciones.I));
        B("Créditos", "pergamino", () => Ventanas.Abrir(VentanaCreditos.I));
        B("Salir", "salir", Salir, Componentes.Variante.Fantasma);

        infoPartida = Estilo.Pequena("");
        infoPartida.style.marginTop = 14f;
        infoPartida.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.9f), offset = new Vector2(0f, 1f), blurRadius = 3f };
        columna.Add(infoPartida);

        var version = Estilo.Pequena(Version.Texto + " · " + Version.Nombre);
        version.style.position = Position.Absolute; version.style.right = 20f; version.style.bottom = 14f;
        raiz.Add(version);
        Lienzo.CapaFondo.Add(raiz);
    }

    static void ActualizarInfo()
    {
        bool hay = Guardado.HayPartida;
        var c = raiz.Q<Button>("continuar");
        if (c != null) Componentes.CambiarTexto(c, hay ? "Continuar" : "Jugar");
        var n = raiz.Q<Button>("nueva");
        if (n != null) n.style.display = hay ? DisplayStyle.Flex : DisplayStyle.None;
        var an = raiz.Q<Button>("anterior");
        if (an != null) an.style.display = Guardado.HayAnterior ? DisplayStyle.Flex : DisplayStyle.None;
        if (hay)
        {
            var f = System.IO.File.GetLastWriteTime(Guardado.Ruta);
            infoPartida.text = "Partida guardada el " + f.ToString("dd/MM/yyyy") + " a las " + f.ToString("HH:mm");
        }
        else infoPartida.text = "Empiezas en una isla, sin nada. Recoge, fabrica y haz tu primera balsa.";
    }

    static void NuevaPartida()
    {
        Ventanas.Confirmar("Nueva partida", "Empiezas de cero en una isla, sin nada.\n\nLa partida actual (barco, mochila, islas descubiertas y pintura) queda guardada aparte: la puedes recuperar desde este menú.", "Empezar de cero", () =>
        {
            var g = Object.FindFirstObjectByType<Guardado>();
            if (g != null && g.Listo && Guardado.HayPartida) g.GuardarAhora();   // lo último que hiciste también queda guardado aparte
            if (g != null) g.BorrarPartida();
            else Guardado.Apartar();
            Recargar();
        }, true);
    }

    // Cambia a la partida anterior (la apartada con "Nueva partida") y la carga
    static void RecuperarAnterior()
    {
        Ventanas.Confirmar("Recuperar la partida anterior", "Vuelves a la partida que tenías antes de empezar de cero. La partida actual queda guardada aparte (puedes volver a cambiar).", "Recuperar", () =>
        {
            var g = Object.FindFirstObjectByType<Guardado>();
            if (g != null && g.Listo && Guardado.HayPartida) g.GuardarAhora();
            Guardado.RecuperarAnterior();
            Recargar();
        });
    }

    static void Recargar()
    {
        {
            Time.timeScale = 1f;
            Visible = false;
            CamaraBarco.Orbitar = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public static void Salir()
    {
        var g = Object.FindFirstObjectByType<Guardado>();
        if (g != null && g.Listo && Guardado.HayPartida) g.GuardarAhora();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Degradado: negro casi opaco a la izquierda → transparente a la derecha, con sombra arriba y abajo
    static Texture2D degradado;
    static Texture2D Degradado()
    {
        if (degradado != null) return degradado;
        const int w = 256, h = 128;
        degradado = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1), v = y / (float)(h - 1);
                float a = Mathf.Lerp(0.88f, 0.05f, Mathf.SmoothStep(0f, 1f, u * 1.4f));
                float bordes = Mathf.Pow(Mathf.Abs(v - 0.5f) * 2f, 3f) * 0.5f;
                px[y * w + x] = new Color(0.03f, 0.02f, 0.015f, Mathf.Clamp01(a + bordes));
            }
        degradado.SetPixels(px); degradado.Apply();
        return degradado;
    }
}

// Créditos
public class VentanaCreditos : Ventana
{
    static VentanaCreditos i;
    public static VentanaCreditos I => i ??= new VentanaCreditos();
    VentanaCreditos() : base("creditos", "Créditos", "pergamino", 520f)
    {
        Modal = true; RecordarPosicion = false;
        void L(string t, string v) { var f = Estilo.Columna(); f.style.marginBottom = 10f; f.Add(Estilo.Etiqueta(t, Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteNegrita)); f.Add(Estilo.Etiqueta(v, Estilo.TamTexto, Estilo.Texto)); Contenido.Add(f); }
        L("Creado por", "HEISON");
        L("Programación, modelos y arte procedural", "HEISON con Claude (Anthropic)");
        L("Motor", "Unity 6 (URP) · Blender para los modelos");
        L("Tipografías", "Cinzel (Natanael Gama) y Alegreya Sans (Huerta Tipográfica), licencia SIL Open Font License");
        L("Versión", Version.Texto + " · " + Version.Nombre);
        var b = Componentes.Boton("Cerrar", () => Ventanas.Cerrar(this), null, Componentes.Variante.Primario);
        b.style.marginTop = 6f;
        Contenido.Add(b);
    }
    protected override float Refrescos => 0f;
}
