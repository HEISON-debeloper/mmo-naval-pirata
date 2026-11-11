using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Base de todas las ventanas del juego: panel de madera con barra de título, botón de cerrar, arrastre con el ratón,
// posición recordada, animación de abrir/cerrar, Esc para cerrar. Las ventanas concretas heredan y llenan Contenido.
public abstract class Ventana : VisualElement
{
    public readonly string Clave;          // nombre interno (para recordar la posición)
    public bool Modal;                     // oscurece el fondo y bloquea lo demás
    public bool Arrastrable = true;
    public bool RecordarPosicion = true;
    public bool CerrableConEsc = true;
    public bool Abierta { get; private set; }
    public System.Action alAbrir, alCerrar;

    protected readonly VisualElement Contenido;
    readonly VisualElement barra;
    readonly Label titulo;
    readonly Icono icono;
    readonly VisualElement sombra;
    readonly VisualElement remaches;
    bool arrastrando; Vector2 inicioPuntero, inicioPos; int punteroId = -1;
    bool centradaAlAbrir = true;
    float ultimaActualizacion;

    protected Ventana(string clave, string tituloTexto, string iconoNombre, float ancho, float alto = 0f)
    {
        Clave = clave;
        name = "ventana-" + clave;
        AddToClassList("ventana");
        style.position = Position.Absolute;
        style.width = ancho;
        if (alto > 0f) style.height = alto;
        style.maxWidth = Length.Percent(100f);
        style.maxHeight = Length.Percent(100f);
        style.display = DisplayStyle.None;
        pickingMode = PickingMode.Position;
        focusable = true;

        // Sombra blanda detrás del panel
        sombra = new VisualElement { pickingMode = PickingMode.Ignore };
        sombra.style.position = Position.Absolute;
        sombra.style.left = -6f; sombra.style.right = -6f; sombra.style.top = -4f; sombra.style.bottom = -10f;
        sombra.style.backgroundColor = Estilo.Sombra;
        Estilo.Redondear(sombra, Estilo.Radio + 6f);
        Add(sombra);

        var panel = new VisualElement { name = "panel" };
        panel.style.flexGrow = 1f;
        Estilo.PanelMadera(panel, 0.96f * Preferencias.Transparencia + (1f - Preferencias.Transparencia) * 0.75f);
        Add(panel);

        // Barra de título: franja de cuero con icono, título en Cinzel y botón de cerrar
        barra = new VisualElement { name = "barra" };
        barra.style.flexDirection = FlexDirection.Row;
        barra.style.alignItems = Align.Center;
        barra.style.height = 42f;
        Estilo.Fondo(barra, Estilo.TexCuero, Color.white, 1f);
        barra.style.borderBottomWidth = 1.5f;
        barra.style.borderBottomColor = Estilo.Bronce;
        barra.style.borderTopLeftRadius = barra.style.borderTopRightRadius = Estilo.Radio - 1f;
        Estilo.Relleno(barra, 0f, 12f);
        panel.Add(barra);

        icono = Iconos.Crear(string.IsNullOrEmpty(iconoNombre) ? "pergamino" : iconoNombre, 22f, Estilo.Oro);
        icono.style.marginRight = 10f;
        barra.Add(icono);
        titulo = Estilo.Titulo(tituloTexto, Estilo.TamTitulo);
        titulo.style.flexGrow = 1f;
        titulo.style.unityTextOverflowPosition = TextOverflowPosition.End;
        titulo.style.textOverflow = TextOverflow.Ellipsis;
        titulo.style.whiteSpace = WhiteSpace.NoWrap;
        barra.Add(titulo);
        var cerrar = Componentes.BotonIcono("cerrar", () => Ventanas.Cerrar(this), "Cerrar (Esc)", 20f, Estilo.TextoTenue);
        barra.Add(cerrar);

        Contenido = new VisualElement { name = "contenido" };
        Contenido.style.flexGrow = 1f;
        Contenido.style.flexShrink = 1f;
        Estilo.Relleno(Contenido, 12f, 14f);
        panel.Add(Contenido);

        remaches = new VisualElement { pickingMode = PickingMode.Ignore };
        remaches.style.position = Position.Absolute;
        remaches.style.left = remaches.style.right = remaches.style.top = remaches.style.bottom = 0f;
        Estilo.Remaches(remaches);
        Add(remaches);

        barra.RegisterCallback<PointerDownEvent>(AlPulsarBarra);
        barra.RegisterCallback<PointerMoveEvent>(AlMoverBarra);
        barra.RegisterCallback<PointerUpEvent>(AlSoltarBarra);
        barra.RegisterCallback<PointerCaptureOutEvent>(_ => arrastrando = false);
        RegisterCallback<PointerDownEvent>(_ => Ventanas.AlFrente(this), TrickleDown.TrickleDown);
    }

    public string Titulo { get => titulo.text; set => titulo.text = value; }
    public void CambiarIcono(string nombre) => icono.Nombre = nombre;

    // ---------- Abrir y cerrar ----------
    internal void AbrirInterno()
    {
        if (Abierta) return;
        Abierta = true;
        style.display = DisplayStyle.Flex;
        AddToClassList("ventana--cerrada");
        BringToFront();
        AlAbrir();
        alAbrir?.Invoke();
        // Posición: la recordada o centrada
        schedule.Execute(() =>
        {
            if (RecordarPosicion && Preferencias.PosicionVentana(Clave, out Vector2 p)) Colocar(p);
            else if (centradaAlAbrir) Centrar();
            RemoveFromClassList("ventana--cerrada");
            Focus();
        }).ExecuteLater(1);
        Sonidos.Reproducir(Sonidos.Tipo.Abrir);
    }

    internal void CerrarInterno()
    {
        if (!Abierta) return;
        Abierta = false;
        AlCerrar();
        alCerrar?.Invoke();
        AddToClassList("ventana--cerrada");
        int ms = Estilo.Ms(140);
        if (ms <= 0) style.display = DisplayStyle.None;
        else schedule.Execute(() => { if (!Abierta) style.display = DisplayStyle.None; }).ExecuteLater(ms);
        Sonidos.Reproducir(Sonidos.Tipo.Cerrar);
    }

    protected virtual void AlAbrir() { }
    protected virtual void AlCerrar() { }
    // Se llama cada cuadro mientras está abierta; las ventanas refrescan aquí lo que cambia (cantidades, barras)
    public virtual void Actualizar() { }
    // Se llama cada cuadro mientras está abierta (teclado de la ventana)
    public virtual void CadaCuadro() { }
    // Cuántas veces por segundo refrescar (0 = solo al abrir)
    protected virtual float Refrescos => 8f;

    internal void Tic()
    {
        CadaCuadro();
        if (Refrescos <= 0f) return;
        if (Time.unscaledTime - ultimaActualizacion < 1f / Refrescos) return;
        ultimaActualizacion = Time.unscaledTime;
        Actualizar();
    }

    // ---------- Posición ----------
    public void Centrar()
    {
        centradaAlAbrir = true;
        Vector2 t = Lienzo.Tamano;
        float w = resolvedStyle.width > 0f ? resolvedStyle.width : style.width.value.value;
        float h = resolvedStyle.height > 0f ? resolvedStyle.height : (style.height.keyword == StyleKeyword.Undefined ? style.height.value.value : 300f);
        Colocar(new Vector2((t.x - w) / 2f, (t.y - h) / 2f));
    }

    public void Colocar(Vector2 p)
    {
        Vector2 t = Lienzo.Tamano;
        float w = resolvedStyle.width > 0f ? resolvedStyle.width : style.width.value.value;
        float h = resolvedStyle.height > 0f ? resolvedStyle.height : 200f;
        // Nunca fuera de la pantalla (siempre queda al menos la barra visible)
        p.x = Mathf.Clamp(p.x, -w + 80f, Mathf.Max(0f, t.x - 80f));
        p.y = Mathf.Clamp(p.y, 0f, Mathf.Max(0f, t.y - 42f));
        style.left = p.x; style.top = p.y;
    }

    void AlPulsarBarra(PointerDownEvent e)
    {
        if (!Arrastrable || e.button != 0) return;
        if (e.target is Button) return;   // el botón de cerrar
        arrastrando = true; punteroId = e.pointerId;
        inicioPuntero = e.position; inicioPos = new Vector2(resolvedStyle.left, resolvedStyle.top);
        barra.CapturePointer(e.pointerId);
        e.StopPropagation();
    }

    void AlMoverBarra(PointerMoveEvent e)
    {
        if (!arrastrando || e.pointerId != punteroId) return;
        Vector2 d = (Vector2)e.position - inicioPuntero;
        Colocar(inicioPos + d);
    }

    void AlSoltarBarra(PointerUpEvent e)
    {
        if (!arrastrando || e.pointerId != punteroId) return;
        arrastrando = false;
        barra.ReleasePointer(e.pointerId);
        centradaAlAbrir = false;
        if (RecordarPosicion) Preferencias.GuardarPosicionVentana(Clave, new Vector2(resolvedStyle.left, resolvedStyle.top));
    }

    // ---------- Ayudas para las ventanas hijas ----------
    protected VisualElement Seccion(string tituloSeccion, string iconoNombre = null)
    {
        var s = Estilo.Columna();
        s.style.marginBottom = Estilo.Espacio;
        var cab = Estilo.Fila(6f);
        if (iconoNombre != null) { var ic = Iconos.Crear(iconoNombre, 16f, Estilo.Oro); ic.style.marginRight = 6f; cab.Add(ic); }
        var l = Estilo.Etiqueta(tituloSeccion, Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteNegrita);
        l.style.letterSpacing = 1.5f;
        cab.Add(l);
        var sep = Estilo.Separador(); sep.style.flexGrow = 1f; sep.style.marginLeft = 8f; sep.style.marginTop = sep.style.marginBottom = 0f;
        cab.Add(sep);
        cab.style.marginBottom = 6f;
        s.Add(cab);
        Contenido.Add(s);
        return s;
    }
}

// Gestor: cuáles ventanas están abiertas, Esc, ventanas modales (confirmaciones), tooltips.
public static class Ventanas
{
    static readonly List<Ventana> todas = new List<Ventana>();
    static readonly List<Ventana> abiertas = new List<Ventana>();
    static VisualElement oscurecedor;
    static Ventana modalActual;
    public static event System.Action alCambiar;

    // Alguna ventana que se queda con el teclado y el ratón (el juego deja de reaccionar a las teclas y suelta el cursor)
    public static bool HayModal => modalActual != null;
    public static bool HayAbiertas => abiertas.Count > 0;
    public static bool AlgunaAbierta => abiertas.Count > 0 || modalActual != null || MenuPrincipal.Visible || Pausa.Visible;
    public static int Cuantas => abiertas.Count;
    public static Ventana Superior => abiertas.Count > 0 ? abiertas[abiertas.Count - 1] : null;

    // Al recargar la escena (nueva partida) el lienzo es otro: se olvidan las ventanas del anterior
    public static void Reiniciar()
    {
        todas.Clear(); abiertas.Clear(); modalActual = null; oscurecedor = null;
        Tooltip.Ocultar();
    }

    public static void Registrar(Ventana v)
    {
        if (todas.Contains(v)) return;
        todas.Add(v);
        (v.Modal ? Lienzo.CapaModal : Lienzo.CapaVentanas).Add(v);
    }

    public static void Abrir(Ventana v)
    {
        if (v == null) return;
        Registrar(v);
        if (v.Abierta) { AlFrente(v); return; }
        if (v.Modal)
        {
            if (modalActual != null && modalActual != v) Cerrar(modalActual);
            modalActual = v;
            Oscurecer(true);
        }
        else abiertas.Add(v);
        Estilo.ReaplicarTexto(v);   // por si cambió el tamaño del texto mientras estaba cerrada
        v.AbrirInterno();
        alCambiar?.Invoke();
    }

    public static void Cerrar(Ventana v)
    {
        if (v == null || !v.Abierta) return;
        v.CerrarInterno();
        abiertas.Remove(v);
        if (modalActual == v) { modalActual = null; Oscurecer(false); }
        alCambiar?.Invoke();
    }

    public static void Alternar(Ventana v)
    {
        if (v.Abierta) Cerrar(v); else Abrir(v);
    }

    public static void CerrarTodas()
    {
        foreach (var v in new List<Ventana>(abiertas)) Cerrar(v);
        if (modalActual != null) Cerrar(modalActual);
    }

    public static bool EstaAbierta<T>() where T : Ventana
    {
        foreach (var v in abiertas) if (v is T) return true;
        return modalActual is T;
    }

    public static void AlFrente(Ventana v)
    {
        if (v == null || !v.Abierta || v.Modal) return;
        abiertas.Remove(v); abiertas.Add(v);
        v.BringToFront();
    }

    static void Oscurecer(bool si)
    {
        if (oscurecedor == null)
        {
            oscurecedor = new VisualElement { name = "oscurecedor" };
            oscurecedor.style.position = Position.Absolute;
            oscurecedor.style.left = oscurecedor.style.right = oscurecedor.style.top = oscurecedor.style.bottom = 0f;
            oscurecedor.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            oscurecedor.pickingMode = PickingMode.Position;    // se traga los clics de fuera
            Lienzo.CapaModal.Insert(0, oscurecedor);
        }
        oscurecedor.style.display = si ? DisplayStyle.Flex : DisplayStyle.None;
        Lienzo.CapaModal.pickingMode = si ? PickingMode.Position : PickingMode.Ignore;
    }

    // Lo llama Lienzo cada cuadro
    static readonly List<Ventana> copia = new List<Ventana>();
    internal static void Actualizar()
    {
        // Copia sin crear basura (una ventana puede cerrarse durante su Tic)
        copia.Clear(); copia.AddRange(abiertas);
        foreach (var v in copia) v.Tic();
        modalActual?.Tic();
        Tooltip.Actualizar();

        // Esc: cierra el modal, si no la ventana de arriba
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && !Teclas.Capturando)
        {
            if (modalActual != null) { if (modalActual.CerrableConEsc) Cerrar(modalActual); return; }
            if (abiertas.Count > 0) { Cerrar(Superior); return; }
            // Sin nada abierto: Esc abre la pausa (salvo en el menú principal o con el taller abierto)
            if (Personaje.Instancia != null && !MenuPrincipal.Visible && !Taller.Abierto && Teclas.De(Accion.Pausa) == Key.Escape) Abrir(Pausa.I);
        }
    }

    // ---------- Confirmación modal ----------
    public static void Confirmar(string titulo, string mensaje, string textoSi, System.Action alConfirmar, bool peligro = false, string textoNo = "Cancelar")
    {
        var c = new VentanaConfirmar(titulo, mensaje, textoSi, textoNo, alConfirmar, peligro);
        Abrir(c);
    }

    class VentanaConfirmar : Ventana
    {
        public VentanaConfirmar(string titulo, string mensaje, string textoSi, string textoNo, System.Action alConfirmar, bool peligro)
            : base("confirmar", titulo, peligro ? "alerta" : "info", 440f)
        {
            Modal = true; RecordarPosicion = false; Arrastrable = false;
            var texto = Estilo.Etiqueta(mensaje);
            texto.style.marginBottom = 16f;
            Contenido.Add(texto);
            var fila = Estilo.Fila(10f);
            fila.style.justifyContent = Justify.FlexEnd;
            fila.Add(Componentes.Boton(textoNo, () => Ventanas.Cerrar(this), null, Componentes.Variante.Fantasma));
            var si = Componentes.Boton(textoSi, () => { Ventanas.Cerrar(this); alConfirmar?.Invoke(); }, null, peligro ? Componentes.Variante.Peligro : Componentes.Variante.Primario);
            fila.Add(si);
            Contenido.Add(fila);
            alCerrar = () => schedule.Execute(() => RemoveFromHierarchy()).ExecuteLater(200);
        }
        protected override float Refrescos => 0f;
    }
}

// Tooltip: un globo de pergamino junto al puntero al dejar el ratón sobre algo (o al mantener pulsado en el teléfono)
public static class Tooltip
{
    static VisualElement globo; static Label texto;
    static VisualElement objetivo; static System.Func<string> fuente;
    static float desde; static bool visible;
    const float Espera = 0.35f;

    public static void Asignar(VisualElement e, string t) => Asignar(e, () => t);
    public static void Asignar(VisualElement e, System.Func<string> t)
    {
        e.RegisterCallback<PointerEnterEvent>(_ => { objetivo = e; fuente = t; desde = Time.unscaledTime; });
        e.RegisterCallback<PointerLeaveEvent>(_ => { if (objetivo == e) Ocultar(); });
        e.RegisterCallback<PointerDownEvent>(_ => { if (!Lienzo.EsMovil && objetivo == e) Ocultar(); });
        e.RegisterCallback<DetachFromPanelEvent>(_ => { if (objetivo == e) Ocultar(); });
    }

    static void Asegurar()
    {
        if (globo != null && globo.panel != null) return;
        globo = new VisualElement { name = "tooltip", pickingMode = PickingMode.Ignore };
        globo.style.position = Position.Absolute;
        globo.style.maxWidth = 320f;
        Estilo.PanelPergamino(globo);
        Estilo.Relleno(globo, 6f, 10f);
        globo.style.display = DisplayStyle.None;
        texto = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Tinta, Estilo.FuenteMedia);
        globo.Add(texto);
        Lienzo.CapaTooltip.Add(globo);
    }

    internal static void Actualizar()
    {
        if (objetivo == null) return;
        if (objetivo.panel == null) { Ocultar(); return; }

        if (!visible && Time.unscaledTime - desde >= Espera)
        {
            string t = fuente?.Invoke();
            if (string.IsNullOrEmpty(t)) return;
            Asegurar();
            texto.text = t;
            globo.style.display = DisplayStyle.Flex;
            visible = true;
        }
        if (visible)
        {
            Vector2 p = Lienzo.Puntero + new Vector2(14f, 18f);
            Vector2 t = Lienzo.Tamano;
            float w = globo.resolvedStyle.width, h = globo.resolvedStyle.height;
            if (p.x + w > t.x) p.x = Lienzo.Puntero.x - w - 8f;
            if (p.y + h > t.y) p.y = Lienzo.Puntero.y - h - 8f;
            globo.style.left = p.x; globo.style.top = p.y;
            string nuevo = fuente?.Invoke();
            if (nuevo != null && nuevo != texto.text) texto.text = nuevo;
        }
    }

    public static void Ocultar()
    {
        objetivo = null; fuente = null; visible = false;
        if (globo != null) globo.style.display = DisplayStyle.None;
    }
}
