using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Opciones: Controles (cambiar cualquier tecla, restablecer), Audio, Gráficos, Accesibilidad e Interfaz.
// Todo se aplica al momento y se guarda.
public class VentanaOpciones : Ventana
{
    static VentanaOpciones i;
    public static VentanaOpciones I => i ??= new VentanaOpciones();

    readonly Componentes.Pestanas pestanas;
    readonly VisualElement controles, audio, graficos, accesibilidad, interfaz;
    Accion? capturando;
    readonly Dictionary<Accion, Button> botonesTecla = new Dictionary<Accion, Button>();

    VentanaOpciones() : base("opciones", "Opciones", "engranaje", 780f, 600f)
    {
        pestanas = new Componentes.Pestanas();
        controles = Lista(pestanas.Agregar("Controles", "teclado"));
        audio = Lista(pestanas.Agregar("Audio", "altavoz"));
        graficos = Lista(pestanas.Agregar("Gráficos", "ojo"));
        accesibilidad = Lista(pestanas.Agregar("Accesibilidad", "persona"));
        interfaz = Lista(pestanas.Agregar("Interfaz", "pergamino"));
        Contenido.Add(pestanas);
    }

    static VisualElement Lista(VisualElement pagina)
    {
        var s = Componentes.Lista(); pagina.Add(s);
        var c = Estilo.Columna(); c.style.paddingRight = 10f; s.Add(c);
        return c;
    }

    protected override float Refrescos => 0f;

    protected override void AlAbrir() => Construir();
    protected override void AlCerrar() { capturando = null; Teclas.Capturando = false; Preferencias.Guardar(); }

    static VisualElement Cabecera(string t)
    {
        var l = Estilo.Etiqueta(t.ToUpperInvariant(), Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita);
        l.style.letterSpacing = 1.5f; l.style.marginTop = 10f; l.style.marginBottom = 6f;
        return l;
    }

    void Construir()
    {
        // ---------- Controles ----------
        controles.Clear(); botonesTecla.Clear();
        var nota = Estilo.Pequena("Haz clic en una tecla y pulsa la nueva. Esc cancela. Si la tecla ya se usaba, las dos acciones se intercambian.");
        nota.style.marginBottom = 6f;
        controles.Add(nota);
        string grupo = null;
        foreach (var (accion, nombre, g) in Teclas.Lista)
        {
            if (g != grupo) { grupo = g; controles.Add(Cabecera(g)); }
            var f = Componentes.FilaLista(false); f.pickingMode = PickingMode.Position;
            var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia); l.style.flexGrow = 1f;
            f.Add(l);
            if (!Teclas.EsPorDefecto(accion))
            {
                var r = Componentes.BotonIcono("flecha_izq", () => { Teclas.Asignar(accion, Teclas.PorDefecto(accion)); Construir(); }, "Volver a " + Teclas.NombreTecla(Teclas.PorDefecto(accion)), 12f, Estilo.TextoTenue);
                f.Add(r);
            }
            var a = accion;
            var b = Componentes.Boton(Teclas.Nombre(accion), () => Capturar(a), null, Componentes.Variante.Normal, 30f);
            b.style.width = 120f; b.style.marginLeft = 6f;
            botonesTecla[accion] = b;
            f.Add(b);
            controles.Add(f);
        }
        var rest = Componentes.Boton("Restablecer todas las teclas", () => Ventanas.Confirmar("Restablecer teclas", "Todas las teclas vuelven a las de fábrica.", "Restablecer", () => { Teclas.Restablecer(); Construir(); }), "flecha_izq", Componentes.Variante.Fantasma);
        rest.style.marginTop = 10f;
        controles.Add(rest);

        // ---------- Audio ----------
        audio.Clear();
        audio.Add(Cabecera("Volumen"));
        audio.Add(Deslizador("Volumen general", 0f, 1f, Preferencias.Volumen, Pct, v => Preferencias.Volumen = v));
        audio.Add(Deslizador("Efectos (cañones, espada, pasos…)", 0f, 1f, Preferencias.VolumenEfectos, Pct, v => { Preferencias.VolumenEfectos = v; SonidosMundo.Propio("espada_aire", 0.6f); }));
        audio.Add(Deslizador("Ambiente (mar, viento, gaviotas)", 0f, 1f, Preferencias.VolumenAmbiente, Pct, v => Preferencias.VolumenAmbiente = v));
        audio.Add(Deslizador("Sonidos de la interfaz", 0f, 1f, Preferencias.VolumenInterfaz, Pct, v => { Preferencias.VolumenInterfaz = v; Sonidos.Reproducir(Sonidos.Tipo.Clic); }));
        audio.Add(Interruptor("Sonidos al tocar botones y ventanas", Preferencias.SonidosInterfaz, v => Preferencias.SonidosInterfaz = v));

        // ---------- Gráficos ----------
        graficos.Clear();
        graficos.Add(Cabecera("Calidad"));
        var nombres = new List<string>(QualitySettings.names);
        int actual = Preferencias.Calidad >= 0 ? Preferencias.Calidad : QualitySettings.GetQualityLevel();
        var dc = new Componentes.Desplegable("Nivel de calidad", nombres, actual);
        dc.alCambiar = k => { Preferencias.Calidad = k; Aplicar(); };
        graficos.Add(dc);
        graficos.Add(Deslizador("Resolución de render", 0.5f, 1.5f, Preferencias.EscalaRender, Pct, v => { Preferencias.EscalaRender = v; Aplicar(); }, 0.05f, "Menos de 100 % va más fluido; más de 100 % se ve más nítido"));
        graficos.Add(Interruptor("Sombras", Preferencias.Sombras, v => { Preferencias.Sombras = v; Aplicar(); }));
        graficos.Add(Interruptor("Sincronización vertical (VSync)", Preferencias.VSync, v => { Preferencias.VSync = v; Aplicar(); }));
        var fps = new List<string> { "Sin límite", "30", "60", "90", "120", "144" };
        int[] valores = { 0, 30, 60, 90, 120, 144 };
        var df = new Componentes.Desplegable("Límite de cuadros por segundo", fps, Mathf.Max(0, System.Array.IndexOf(valores, Preferencias.LimiteFps)));
        df.alCambiar = k => { Preferencias.LimiteFps = valores[k]; Aplicar(); };
        graficos.Add(df);
        if (!Application.isMobilePlatform) graficos.Add(Interruptor("Pantalla completa", Preferencias.PantallaCompleta, v => { Preferencias.PantallaCompleta = v; Aplicar(); }));

        // ---------- Accesibilidad ----------
        accesibilidad.Clear();
        accesibilidad.Add(Cabecera("Lectura"));
        accesibilidad.Add(Deslizador("Tamaño del texto", 0.8f, 1.4f, Preferencias.TamanoTexto, Pct, v => Preferencias.TamanoTexto = v, 0.05f, "Se nota al volver a abrir las ventanas"));
        accesibilidad.Add(Interruptor("Alto contraste", Preferencias.AltoContraste, v => { Preferencias.AltoContraste = v; Preferencias.Guardar(); }, "Bordes y textos más marcados"));
        accesibilidad.Add(Cabecera("Movimiento"));
        accesibilidad.Add(Interruptor("Reducir movimiento", Preferencias.ReducirMovimiento, v => { Preferencias.ReducirMovimiento = v; Preferencias.Guardar(); }, "Sin animaciones ni parpadeos en la interfaz"));
        accesibilidad.Add(Deslizador("Velocidad de las animaciones", 0f, 1f, Preferencias.Animaciones, Pct, v => { Preferencias.Animaciones = v; Preferencias.Guardar(); }, 0.1f));

        // ---------- Interfaz ----------
        interfaz.Clear();
        interfaz.Add(Cabecera("Tamaño"));
        interfaz.Add(Deslizador("Escala de la interfaz", 0.7f, 1.6f, Preferencias.EscalaUI, Pct, v => Preferencias.EscalaUI = v, 0.05f));
        var aplicarEscala = Componentes.Boton("Aplicar escala", () => Preferencias.Guardar(), "check", Componentes.Variante.Fantasma, 30f);
        aplicarEscala.style.alignSelf = Align.FlexStart;
        interfaz.Add(aplicarEscala);
        interfaz.Add(Deslizador("Opacidad de los paneles", 0.4f, 1f, Preferencias.Transparencia, Pct, v => Preferencias.Transparencia = v, 0.05f, "Se nota al volver a abrir las ventanas"));
        interfaz.Add(Cabecera("Qué se ve en pantalla"));
        interfaz.Add(Interruptor("Brújula", Preferencias.HudBrujula, v => { Preferencias.HudBrujula = v; Preferencias.Guardar(); }));
        interfaz.Add(Interruptor("Mochila rápida", Preferencias.HudMochila, v => { Preferencias.HudMochila = v; Preferencias.Guardar(); }));
        interfaz.Add(Interruptor("Tarjeta de tu barco", Preferencias.HudBarco, v => { Preferencias.HudBarco = v; Preferencias.Guardar(); }));
        interfaz.Add(Interruptor("Barco enemigo cercano", Preferencias.HudEnemigo, v => { Preferencias.HudEnemigo = v; Preferencias.Guardar(); }));
        interfaz.Add(Interruptor("Atajos abajo", Preferencias.HudAtajos, v => { Preferencias.HudAtajos = v; Preferencias.Guardar(); }));
        var rp = Componentes.Boton("Volver a centrar todas las ventanas", () => { ReiniciarPosiciones(); Notificaciones.Info("Las ventanas se abrirán centradas"); }, "objetivo", Componentes.Variante.Fantasma);
        rp.style.marginTop = 10f; rp.style.alignSelf = Align.FlexStart;
        interfaz.Add(rp);
    }

    static string Pct(float v) => Mathf.RoundToInt(v * 100f) + " %";

    static void Aplicar() { Preferencias.AplicarAlJuego(); Preferencias.Guardar(); }

    static void ReiniciarPosiciones() => Preferencias.OlvidarPosiciones();

    static Componentes.Deslizador Deslizador(string n, float min, float max, float v, System.Func<float, string> f, System.Action<float> cambio, float paso = 0.05f, string ayuda = null)
    {
        var d = new Componentes.Deslizador(n, min, max, v, f, paso, ayuda);
        d.alCambiar = x => cambio(x);
        d.RegisterCallback<PointerUpEvent>(_ => Preferencias.Guardar(), TrickleDown.TrickleDown);
        return d;
    }

    static Componentes.Interruptor Interruptor(string n, bool v, System.Action<bool> cambio, string ayuda = null)
    {
        var t = new Componentes.Interruptor(n, v, ayuda);
        t.alCambiar = x => { cambio(x); Preferencias.Guardar(); };
        return t;
    }

    // ---------- Captura de teclas ----------
    void Capturar(Accion a)
    {
        capturando = a;
        Teclas.Capturando = true;
        Componentes.CambiarTexto(botonesTecla[a], "Pulsa una tecla…");
    }

    public override void CadaCuadro()
    {
        if (capturando == null) return;
        Key? k = Teclas.TeclaPulsadaAhora();
        if (k == null) return;
        Accion a = capturando.Value;
        capturando = null;
        // Esc cancela (y no cierra la ventana este cuadro)
        schedule.Execute(() => Teclas.Capturando = false).ExecuteLater(50);
        if (k.Value == Key.Escape) { Componentes.CambiarTexto(botonesTecla[a], Teclas.Nombre(a)); return; }
        Teclas.Asignar(a, k.Value);
        Sonidos.Reproducir(Sonidos.Tipo.Exito);
        Construir();
    }
}
