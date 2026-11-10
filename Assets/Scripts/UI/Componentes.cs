using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Componentes reutilizables de la interfaz: botones, barras de estado, pestañas, casillas de objetos, campo de búsqueda,
// deslizadores, interruptores, fichas, contadores, teclas. Todos toman colores y fuentes de Estilo y estados de Estilos.uss.
public static class Componentes
{
    public enum Variante { Normal, Primario, Peligro, Fantasma }

    // ---------- Botón ----------
    public static Button Boton(string texto, System.Action accion, string icono = null, Variante variante = Variante.Normal, float alto = 36f)
    {
        var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); accion?.Invoke(); });
        b.text = "";
        b.AddToClassList("boton");
        if (variante == Variante.Primario) b.AddToClassList("boton--primario");
        else if (variante == Variante.Peligro) b.AddToClassList("boton--peligro");
        else if (variante == Variante.Fantasma) b.AddToClassList("boton--fantasma");
        b.style.flexDirection = FlexDirection.Row;
        b.style.alignItems = Align.Center;
        b.style.justifyContent = Justify.Center;
        b.style.height = alto;
        b.style.minHeight = alto;
        Estilo.Margen(b, 0f);
        Estilo.Relleno(b, 0f, 14f);
        if (variante != Variante.Fantasma)
        {
            b.style.backgroundImage = new StyleBackground(variante == Variante.Primario ? Estilo.TexMetal : Estilo.TexCuero);
            b.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            b.style.backgroundSize = new BackgroundSize(new Length(128, LengthUnit.Pixel), new Length(128, LengthUnit.Pixel));
        }
        b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1.5f;
        Estilo.Redondear(b, Estilo.Radio);
        Color colorTexto = variante == Variante.Primario ? new Color(0.11f, 0.07f, 0.04f) : Estilo.Texto;
        if (!string.IsNullOrEmpty(icono))
        {
            var ic = Iconos.Crear(icono, 18f, colorTexto);
            ic.style.marginRight = string.IsNullOrEmpty(texto) ? 0f : 8f;
            b.Add(ic);
        }
        if (!string.IsNullOrEmpty(texto))
        {
            var l = Estilo.Etiqueta(texto, Estilo.TamTexto, colorTexto, Estilo.FuenteMedia);
            l.pickingMode = PickingMode.Ignore;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            l.name = "texto";
            b.Add(l);
        }
        b.RegisterCallback<PointerEnterEvent>(_ => Sonidos.Reproducir(Sonidos.Tipo.Pasar, 0.6f));
        return b;
    }

    public static void CambiarTexto(Button b, string texto)
    {
        var l = b.Q<Label>("texto");
        if (l != null) l.text = texto;
    }

    public static void Habilitar(VisualElement e, bool si)
    {
        e.SetEnabled(si);
    }

    // Botón solo con icono (cerrar, flechas, favorito)
    public static Button BotonIcono(string icono, System.Action accion, string ayuda = null, float tam = 20f, Color? color = null)
    {
        var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); accion?.Invoke(); }) { text = "" };
        b.AddToClassList("boton-icono");
        b.style.width = tam + 12f; b.style.height = tam + 12f;
        b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
        Estilo.Margen(b, 0f); Estilo.Relleno(b, 0f);
        Estilo.Redondear(b, (tam + 12f) / 2f);
        b.style.backgroundImage = null;
        var ic = Iconos.Crear(icono, tam, color ?? Estilo.Oro);
        ic.name = "icono";
        b.Add(ic);
        if (!string.IsNullOrEmpty(ayuda)) Tooltip.Asignar(b, ayuda);
        return b;
    }

    public static Icono IconoDe(Button b) => b.Q<Icono>("icono");

    // ---------- Barra de estado (vida, hambre, sed, casco, velas, recarga) ----------
    public class BarraEstado : VisualElement
    {
        readonly VisualElement relleno, brillo, perdida;
        readonly Label texto;
        readonly Color color;
        float mostrado = 1f, objetivo = 1f, anteriorValor = -1f, perdidaDesde;
        float parpadeo;
        public bool MostrarNumeros = true;

        public BarraEstado(string nombre, string icono, Color color, float ancho = 190f, float alto = 16f)
        {
            this.color = color;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.width = ancho;
            pickingMode = PickingMode.Ignore;
            if (!string.IsNullOrEmpty(icono))
            {
                var ic = Iconos.Crear(icono, alto + 6f, color);
                ic.style.marginRight = 6f;
                Add(ic);
            }
            var pista = new VisualElement { pickingMode = PickingMode.Ignore };
            pista.style.flexGrow = 1f;
            pista.style.height = alto;
            pista.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            Estilo.Borde(pista, new Color(Estilo.Bronce.r, Estilo.Bronce.g, Estilo.Bronce.b, 0.8f), 1f, alto / 2f);
            pista.style.overflow = Overflow.Hidden;
            Add(pista);
            // Lo perdido hace poco se ve en claro un momento y luego se vacía (como en juegos de acción)
            perdida = new VisualElement { pickingMode = PickingMode.Ignore };
            perdida.style.position = Position.Absolute; perdida.style.left = 0f; perdida.style.top = 0f; perdida.style.bottom = 0f;
            perdida.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0.7f);
            perdida.style.width = Length.Percent(100f);
            pista.Add(perdida);
            relleno = new VisualElement { pickingMode = PickingMode.Ignore };
            relleno.style.position = Position.Absolute; relleno.style.left = 0f; relleno.style.top = 0f; relleno.style.bottom = 0f;
            relleno.style.width = Length.Percent(100f);
            relleno.style.backgroundColor = color;
            Estilo.Redondear(relleno, alto / 2f);
            pista.Add(relleno);
            brillo = new VisualElement { pickingMode = PickingMode.Ignore };
            brillo.style.position = Position.Absolute; brillo.style.left = 2f; brillo.style.right = 2f; brillo.style.top = 1f;
            brillo.style.height = Mathf.Max(2f, alto * 0.3f);
            brillo.style.backgroundColor = new Color(1f, 1f, 1f, 0.22f);
            Estilo.Redondear(brillo, alto / 4f);
            relleno.Add(brillo);
            texto = Estilo.Etiqueta("", Estilo.TamTextoChico * 0.9f, Estilo.Texto, Estilo.FuenteNegrita);
            texto.style.position = Position.Absolute; texto.style.left = 0f; texto.style.right = 0f; texto.style.top = 0f; texto.style.bottom = 0f;
            texto.style.unityTextAlign = TextAnchor.MiddleCenter;
            texto.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.8f), offset = new Vector2(0f, 1f), blurRadius = 1f };
            pista.Add(texto);
            if (!string.IsNullOrEmpty(nombre)) Tooltip.Asignar(this, nombre);
            pickingMode = PickingMode.Position;
        }

        // valor y máximo reales; se anima suavemente hacia ellos
        public void Fijar(float valor, float maximo, string textoExtra = null)
        {
            objetivo = maximo > 0f ? Mathf.Clamp01(valor / maximo) : 0f;
            if (anteriorValor >= 0f && valor < anteriorValor - 0.5f) perdidaDesde = Time.unscaledTime;
            anteriorValor = valor;
            if (MostrarNumeros) texto.text = textoExtra ?? (Mathf.RoundToInt(valor) + " / " + Mathf.RoundToInt(maximo));
            else texto.text = textoExtra ?? "";
        }

        public void Animar()
        {
            float dt = Time.unscaledDeltaTime;
            mostrado = Preferencias.ReducirMovimiento ? objetivo : Mathf.Lerp(mostrado, objetivo, 1f - Mathf.Exp(-dt * 10f));
            relleno.style.width = Length.Percent(mostrado * 100f);
            // La parte clara de lo perdido baja despacio
            float esperaPerdida = Time.unscaledTime - perdidaDesde;
            float anchoPerdida = perdida.resolvedStyle.width;
            float objetivoPerdida = mostrado * 100f;
            float actualPct = perdida.style.width.value.value;
            if (esperaPerdida > 0.5f) actualPct = Mathf.Lerp(actualPct, objetivoPerdida, 1f - Mathf.Exp(-dt * 6f));
            if (actualPct < objetivoPerdida) actualPct = objetivoPerdida;
            perdida.style.width = Length.Percent(actualPct);
            // Parpadeo suave cuando queda poco
            if (objetivo < 0.25f && !Preferencias.ReducirMovimiento)
            {
                parpadeo += dt * 4f;
                float p = 0.75f + 0.25f * Mathf.Sin(parpadeo);
                relleno.style.backgroundColor = Color.Lerp(color, Color.white, (1f - p) * 0.6f);
            }
            else relleno.style.backgroundColor = color;
        }
    }

    // ---------- Pestañas ----------
    public class Pestanas : VisualElement
    {
        readonly List<Button> botones = new List<Button>();
        readonly List<VisualElement> paginas = new List<VisualElement>();
        readonly VisualElement cabecera, cuerpo;
        public int Actual { get; private set; } = -1;
        public System.Action<int> alCambiar;

        public Pestanas()
        {
            style.flexGrow = 1f;
            cabecera = Estilo.Fila(4f);
            cabecera.style.flexWrap = Wrap.Wrap;
            cabecera.style.marginBottom = 8f;
            Add(cabecera);
            cuerpo = new VisualElement();
            cuerpo.style.flexGrow = 1f;
            Add(cuerpo);
        }

        public VisualElement Agregar(string nombre, string icono = null, string atajo = null)
        {
            int i = botones.Count;
            var b = new Button(() => Elegir(i)) { text = "" };
            b.AddToClassList("pestana");
            b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center;
            b.style.height = 34f; Estilo.Margen(b, 0f, 2f); Estilo.Relleno(b, 0f, 12f);
            b.style.borderTopWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f; b.style.borderBottomWidth = 2f;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = Estilo.Radio;
            b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 0f;
            if (icono != null) { var ic = Iconos.Crear(icono, 16f, Estilo.Oro); ic.style.marginRight = 6f; b.Add(ic); }
            var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, null, Estilo.FuenteMedia);
            l.style.color = StyleKeyword.Null;   // lo pone la clase .pestana
            l.pickingMode = PickingMode.Ignore; l.style.whiteSpace = WhiteSpace.NoWrap;
            b.Add(l);
            if (!string.IsNullOrEmpty(atajo)) { var t = Tecla(atajo, 0.8f); t.style.marginLeft = 8f; b.Add(t); }
            b.RegisterCallback<PointerEnterEvent>(_ => Sonidos.Reproducir(Sonidos.Tipo.Pasar, 0.5f));
            cabecera.Add(b);
            botones.Add(b);
            var pag = new VisualElement();
            pag.style.flexGrow = 1f;
            pag.style.display = DisplayStyle.None;
            cuerpo.Add(pag);
            paginas.Add(pag);
            if (Actual < 0) Elegir(0, false);
            return pag;
        }

        public void Elegir(int i, bool sonido = true)
        {
            if (i < 0 || i >= paginas.Count) return;
            if (sonido && i != Actual) Sonidos.Reproducir(Sonidos.Tipo.Clic);
            Actual = i;
            for (int k = 0; k < paginas.Count; k++)
            {
                paginas[k].style.display = k == i ? DisplayStyle.Flex : DisplayStyle.None;
                botones[k].EnableInClassList("pestana--activa", k == i);
            }
            alCambiar?.Invoke(i);
        }

        public void Siguiente(int d) => Elegir((Actual + d + paginas.Count) % paginas.Count);
        public int Cuantas => paginas.Count;
        public void Mostrar(int i, bool si) { botones[i].style.display = si ? DisplayStyle.Flex : DisplayStyle.None; if (!si && Actual == i) Elegir(0, false); }
    }

    // ---------- Casilla de objeto (inventario, bodega, mercado) ----------
    public class CasillaObjeto : VisualElement
    {
        public string Tipo { get; private set; }
        public int Cantidad { get; private set; }
        readonly Icono icono; readonly Label cantidad, nombre;
        readonly VisualElement marcaNueva;
        public System.Action<CasillaObjeto> alPulsar, alPulsarDerecho, alDoblePulsar;
        float ultimoClic;

        public CasillaObjeto(float tam = 64f, bool conNombre = false)
        {
            AddToClassList("casilla");
            style.width = tam; style.height = conNombre ? tam + 18f : tam;
            style.alignItems = Align.Center; style.justifyContent = Justify.Center;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1f;
            Estilo.Redondear(this, Estilo.Radio);
            Estilo.Margen(this, 3f);
            icono = Iconos.Crear("caja", tam * 0.55f, Estilo.Oro);
            Add(icono);
            cantidad = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Texto, Estilo.FuenteNegrita);
            cantidad.style.position = Position.Absolute; cantidad.style.right = 4f; cantidad.style.bottom = conNombre ? 20f : 2f;
            cantidad.style.textShadow = new TextShadow { color = Color.black, offset = new Vector2(0f, 1f), blurRadius = 1f };
            cantidad.pickingMode = PickingMode.Ignore;
            Add(cantidad);
            if (conNombre)
            {
                nombre = Estilo.Etiqueta("", Estilo.TamTextoChico * 0.85f, Estilo.TextoTenue);
                nombre.style.position = Position.Absolute; nombre.style.bottom = 2f; nombre.style.left = 2f; nombre.style.right = 2f;
                nombre.style.unityTextAlign = TextAnchor.MiddleCenter; nombre.style.whiteSpace = WhiteSpace.NoWrap;
                nombre.style.textOverflow = TextOverflow.Ellipsis; nombre.style.overflow = Overflow.Hidden;
                nombre.pickingMode = PickingMode.Ignore;
                Add(nombre);
            }
            marcaNueva = new VisualElement { pickingMode = PickingMode.Ignore };
            marcaNueva.style.position = Position.Absolute; marcaNueva.style.top = 3f; marcaNueva.style.left = 3f;
            marcaNueva.style.width = 8f; marcaNueva.style.height = 8f; Estilo.Redondear(marcaNueva, 4f);
            marcaNueva.style.backgroundColor = Estilo.Bien; marcaNueva.style.display = DisplayStyle.None;
            Add(marcaNueva);
            RegisterCallback<ClickEvent>(e =>
            {
                if (Time.unscaledTime - ultimoClic < 0.3f && alDoblePulsar != null) { alDoblePulsar(this); ultimoClic = 0f; }
                else { Sonidos.Reproducir(Sonidos.Tipo.Clic); alPulsar?.Invoke(this); ultimoClic = Time.unscaledTime; }
            });
            RegisterCallback<PointerDownEvent>(e => { if (e.button == 1) { Sonidos.Reproducir(Sonidos.Tipo.Clic); alPulsarDerecho?.Invoke(this); } });
            RegisterCallback<PointerEnterEvent>(_ => Sonidos.Reproducir(Sonidos.Tipo.Pasar, 0.4f));
            Tooltip.Asignar(this, () => Tipo == null ? null : Objetos.Descripcion(Tipo, Cantidad));
        }

        public void Fijar(string tipo, int n)
        {
            Tipo = tipo; Cantidad = n;
            EnableInClassList("casilla--vacia", tipo == null || n <= 0);
            if (tipo == null) { icono.style.visibility = Visibility.Hidden; cantidad.text = ""; if (nombre != null) nombre.text = ""; return; }
            icono.style.visibility = Visibility.Visible;
            var info = Objetos.De(tipo);
            icono.Nombre = info.icono; icono.Color = info.color;
            cantidad.text = n > 1 ? n.ToString() : (n == 1 && info.apilable ? "1" : "");
            if (nombre != null) nombre.text = info.nombre;
        }

        public void Elegida(bool si) => EnableInClassList("casilla--elegida", si);
        public void Nueva(bool si) { marcaNueva.style.display = si ? DisplayStyle.Flex : DisplayStyle.None; EnableInClassList("casilla--nueva", si); }
    }

    // ---------- Campo de búsqueda ----------
    public class CampoBusqueda : VisualElement
    {
        readonly TextField campo;
        public System.Action<string> alCambiar;
        public string Texto => campo.value ?? "";

        public CampoBusqueda(string sugerencia = "Buscar…", float ancho = 220f)
        {
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center;
            style.width = ancho;
            var lupa = Iconos.Crear("buscar", 16f, Estilo.TextoTenue);
            lupa.style.position = Position.Absolute; lupa.style.left = 8f;
            campo = new TextField();
            campo.AddToClassList("campo");
            campo.style.flexGrow = 1f; Estilo.Margen(campo, 0f);
            Estilo.Fuente(campo, Estilo.FuenteTexto);
            campo.style.fontSize = Estilo.TamTexto;
            var input = campo.Q(className: "unity-base-text-field__input");
            if (input != null) { input.style.paddingLeft = 28f; input.style.paddingRight = 26f; input.style.height = 32f; Estilo.Fuente(input, Estilo.FuenteTexto); input.style.fontSize = Estilo.TamTexto; input.style.color = Estilo.Texto; }
            Add(campo);
            Add(lupa);
            var ayuda = Estilo.Etiqueta(sugerencia, Estilo.TamTexto, Estilo.TextoApagado);
            ayuda.style.position = Position.Absolute; ayuda.style.left = 30f; ayuda.style.top = 0f; ayuda.style.bottom = 0f;
            ayuda.style.unityTextAlign = TextAnchor.MiddleLeft; ayuda.pickingMode = PickingMode.Ignore;
            Add(ayuda);
            var limpiar = BotonIcono("cerrar", () => { campo.value = ""; campo.Focus(); }, null, 12f, Estilo.TextoTenue);
            limpiar.style.position = Position.Absolute; limpiar.style.right = 2f; limpiar.style.display = DisplayStyle.None;
            Add(limpiar);
            campo.RegisterValueChangedCallback(e =>
            {
                ayuda.style.display = string.IsNullOrEmpty(e.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                limpiar.style.display = string.IsNullOrEmpty(e.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
                alCambiar?.Invoke(e.newValue ?? "");
            });
            // Mientras se escribe, el juego no debe reaccionar a las letras
            campo.RegisterCallback<FocusInEvent>(_ => Teclas.Capturando = true);
            campo.RegisterCallback<FocusOutEvent>(_ => Teclas.Capturando = false);
            campo.RegisterCallback<DetachFromPanelEvent>(_ => Teclas.Capturando = false);   // al cerrar la ventana con el foco puesto
            campo.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { campo.Blur(); Teclas.Capturando = false; } });
        }

        public void Enfocar() => campo.Q(className: "unity-base-text-field__input")?.Focus();
        public void Limpiar() => campo.value = "";
    }

    // ---------- Deslizador con etiqueta y valor ----------
    public class Deslizador : VisualElement
    {
        readonly Slider s; readonly Label valor;
        readonly System.Func<float, string> formato;
        public System.Action<float> alCambiar;
        public float Valor { get => s.value; set { s.SetValueWithoutNotify(value); valor.text = formato(value); } }

        public Deslizador(string nombre, float min, float max, float inicial, System.Func<float, string> formato, float paso = 0f, string ayuda = null)
        {
            this.formato = formato ?? (v => v.ToString("0.##"));
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center;
            style.marginBottom = 6f;
            var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
            l.style.width = 190f; l.style.flexShrink = 0f;
            Add(l);
            s = new Slider(min, max) { value = inicial };
            s.AddToClassList("deslizador");
            s.style.flexGrow = 1f; Estilo.Margen(s, 0f, 8f);
            Add(s);
            valor = Estilo.Etiqueta(this.formato(inicial), Estilo.TamTexto, Estilo.Oro, Estilo.FuenteNegrita);
            valor.style.width = 64f; valor.style.unityTextAlign = TextAnchor.MiddleRight; valor.style.flexShrink = 0f;
            Add(valor);
            s.RegisterValueChangedCallback(e =>
            {
                float v = e.newValue;
                if (paso > 0f) { v = Mathf.Round(v / paso) * paso; s.SetValueWithoutNotify(v); }
                valor.text = this.formato(v);
                alCambiar?.Invoke(v);
            });
            if (ayuda != null) Tooltip.Asignar(l, ayuda);
        }
    }

    // ---------- Interruptor (sí / no) ----------
    public class Interruptor : VisualElement
    {
        readonly Toggle t;
        public System.Action<bool> alCambiar;
        public bool Valor { get => t.value; set => t.SetValueWithoutNotify(value); }

        public Interruptor(string nombre, bool inicial, string ayuda = null)
        {
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center; style.justifyContent = Justify.SpaceBetween;
            style.marginBottom = 6f;
            var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
            l.style.flexGrow = 1f;
            Add(l);
            t = new Toggle { value = inicial };
            t.AddToClassList("interruptor");
            Estilo.Margen(t, 0f);
            Add(t);
            t.RegisterValueChangedCallback(e => { Sonidos.Reproducir(Sonidos.Tipo.Clic); alCambiar?.Invoke(e.newValue); });
            if (ayuda != null) Tooltip.Asignar(l, ayuda);
            RegisterCallback<ClickEvent>(e => { if (e.target == l || e.target == this) t.value = !t.value; });
        }
    }

    // ---------- Desplegable ----------
    public class Desplegable : VisualElement
    {
        readonly DropdownField d;
        public System.Action<int> alCambiar;
        public int Indice { get => d.index; set => d.SetValueWithoutNotify(d.choices[Mathf.Clamp(value, 0, d.choices.Count - 1)]); }

        public Desplegable(string nombre, List<string> opciones, int inicial, string ayuda = null)
        {
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center; style.justifyContent = Justify.SpaceBetween;
            style.marginBottom = 6f;
            var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
            l.style.flexGrow = 1f;
            Add(l);
            d = new DropdownField(opciones, Mathf.Clamp(inicial, 0, opciones.Count - 1));
            d.AddToClassList("desplegable");
            d.style.width = 200f; Estilo.Margen(d, 0f);
            Estilo.Fuente(d, Estilo.FuenteTexto); d.style.fontSize = Estilo.TamTexto;
            Add(d);
            d.RegisterValueChangedCallback(_ => { Sonidos.Reproducir(Sonidos.Tipo.Clic); alCambiar?.Invoke(d.index); });
            if (ayuda != null) Tooltip.Asignar(l, ayuda);
        }
    }

    // ---------- Ficha (chip) de filtro o categoría ----------
    public static Button Ficha(string texto, System.Action accion, string icono = null, bool activa = false)
    {
        var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); accion?.Invoke(); }) { text = "" };
        b.AddToClassList("ficha");
        if (activa) b.AddToClassList("ficha--activa");
        b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center;
        b.style.height = 28f; Estilo.Margen(b, 2f); Estilo.Relleno(b, 0f, 10f);
        b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f;
        Estilo.Redondear(b, 14f);
        if (icono != null) { var ic = Iconos.Crear(icono, 14f, Estilo.Oro); ic.style.marginRight = 6f; b.Add(ic); }
        var l = Estilo.Etiqueta(texto, Estilo.TamTextoChico, null, Estilo.FuenteMedia);
        l.style.color = StyleKeyword.Null; l.pickingMode = PickingMode.Ignore; l.style.whiteSpace = WhiteSpace.NoWrap;
        b.Add(l);
        return b;
    }

    // ---------- Contador de cantidad (- 5 +, con mantener pulsado y "máx") ----------
    public class Contador : VisualElement
    {
        int valor, min, max;
        readonly Label texto;
        public System.Action<int> alCambiar;
        public int Valor { get => valor; set { valor = Mathf.Clamp(value, min, max); texto.text = valor.ToString(); } }
        public int Maximo { get => max; set { max = Mathf.Max(min, value); Valor = valor; } }

        public Contador(int inicial, int min, int max, bool conMaximo = true)
        {
            this.min = min; this.max = max; valor = Mathf.Clamp(inicial, min, max);
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center;
            Add(Repetidor("menos", () => Cambiar(-1), () => Cambiar(-10)));
            texto = Estilo.Etiqueta(valor.ToString(), Estilo.TamTexto, Estilo.Oro, Estilo.FuenteNegrita);
            texto.style.width = 44f; texto.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(texto);
            Add(Repetidor("mas", () => Cambiar(1), () => Cambiar(10)));
            if (conMaximo)
            {
                var bm = Boton("Máx", () => { Valor = this.max; alCambiar?.Invoke(valor); }, null, Variante.Fantasma, 26f);
                bm.style.marginLeft = 6f; Estilo.Relleno(bm, 0f, 8f);
                Add(bm);
            }
        }

        void Cambiar(int d) { Valor = valor + d; alCambiar?.Invoke(valor); }

        // Botón que repite mientras se mantiene pulsado (más rápido con el tiempo)
        Button Repetidor(string icono, System.Action paso, System.Action pasoGrande)
        {
            var b = BotonIcono(icono, null, null, 14f, Estilo.Texto);
            b.style.width = b.style.height = 28f;
            b.AddToClassList("boton");
            b.RemoveFromClassList("boton-icono");
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f;
            Estilo.Redondear(b, 6f);
            IVisualElementScheduledItem rep = null; int veces = 0;
            b.RegisterCallback<PointerDownEvent>(e =>
            {
                veces = 0; paso(); Sonidos.Reproducir(Sonidos.Tipo.Clic);
                rep = b.schedule.Execute(() => { veces++; if (veces > 12) pasoGrande(); else paso(); }).Every(90).StartingIn(400);
            }, TrickleDown.TrickleDown);
            System.Action parar = () => { rep?.Pause(); rep = null; };
            b.RegisterCallback<PointerUpEvent>(_ => parar(), TrickleDown.TrickleDown);
            b.RegisterCallback<PointerLeaveEvent>(_ => parar());
            b.RegisterCallback<PointerCaptureOutEvent>(_ => parar());
            b.clickable = null;   // el clic ya lo maneja PointerDown
            return b;
        }
    }

    // ---------- Dibujo de una tecla ("F", "Tab") ----------
    public static Label Tecla(string nombre, float escala = 1f)
    {
        var l = Estilo.Etiqueta(nombre, Estilo.TamTextoChico * escala, Estilo.OroVivo, Estilo.FuenteNegrita);
        l.AddToClassList("tecla");
        l.style.height = 20f * escala;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.pickingMode = PickingMode.Ignore;
        return l;
    }

    public static Label TeclaDe(Accion a, float escala = 1f) => Tecla(Teclas.Nombre(a), escala);

    // Fila "tecla + acción" para las indicaciones de contexto y la ayuda
    public static VisualElement Indicacion(string tecla, string texto, float escala = 1f)
    {
        var f = Estilo.Fila(6f);
        f.pickingMode = PickingMode.Ignore;
        f.Add(Tecla(tecla, escala));
        var l = Estilo.Etiqueta(texto, Estilo.TamTexto * escala, Estilo.Texto, Estilo.FuenteMedia);
        l.style.marginLeft = 6f; l.pickingMode = PickingMode.Ignore;
        l.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.8f), offset = new Vector2(0f, 1f), blurRadius = 2f };
        f.Add(l);
        return f;
    }

    // ---------- Tarjeta (panel pequeño con título, para el HUD y las ventanas) ----------
    public static VisualElement Tarjeta(string titulo = null, string icono = null, float alfa = 0.9f)
    {
        var t = Estilo.Columna();
        Estilo.PanelMadera(t, alfa * Preferencias.Transparencia + (1f - Preferencias.Transparencia) * 0.5f);
        Estilo.Relleno(t, 8f, 10f);
        if (!string.IsNullOrEmpty(titulo))
        {
            var cab = Estilo.Fila(6f);
            cab.style.marginBottom = 6f;
            if (icono != null) { var ic = Iconos.Crear(icono, 16f, Estilo.Oro); ic.style.marginRight = 6f; cab.Add(ic); }
            var l = Estilo.Etiqueta(titulo, Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteTitulo);
            l.style.letterSpacing = 1f;
            cab.Add(l);
            t.Add(cab);
        }
        return t;
    }

    // ---------- Lista con desplazamiento ----------
    public static ScrollView Lista(float alto = 0f)
    {
        var s = new ScrollView(ScrollViewMode.Vertical);
        s.style.flexGrow = 1f;
        if (alto > 0f) s.style.height = alto;
        s.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        s.verticalScrollerVisibility = ScrollerVisibility.Auto;
        s.mouseWheelScrollSize = 60f;
        return s;
    }

    // Fila de lista (receta, entrada de bitácora, artículo del mercado)
    public static VisualElement FilaLista(bool elegible = true)
    {
        var f = Estilo.Fila(8f);
        f.AddToClassList("fila-lista");
        f.style.borderTopWidth = f.style.borderBottomWidth = f.style.borderLeftWidth = f.style.borderRightWidth = 1f;
        Estilo.Redondear(f, Estilo.Radio);
        Estilo.Relleno(f, 6f, 10f);
        f.style.marginBottom = 4f;
        if (!elegible) f.pickingMode = PickingMode.Ignore;
        return f;
    }

    // Texto con sombra para el HUD (legible sobre el mar y el cielo)
    public static Label TextoHud(string texto, float tam = -1f, Color? color = null, Font fuente = null)
    {
        var l = Estilo.Etiqueta(texto, tam, color, fuente ?? Estilo.FuenteMedia);
        l.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.85f), offset = new Vector2(0f, 1f), blurRadius = 2f };
        l.pickingMode = PickingMode.Ignore;
        return l;
    }

    // Insignia pequeña con número o texto (contador de avisos, cantidad)
    public static Label Insignia(string texto, Color? fondo = null)
    {
        var l = Estilo.Etiqueta(texto, Estilo.TamTextoChico * 0.85f, new Color(0.1f, 0.07f, 0.04f), Estilo.FuenteNegrita);
        l.style.backgroundColor = fondo ?? Estilo.Oro;
        Estilo.Redondear(l, 9f);
        l.style.height = 18f; l.style.minWidth = 18f;
        Estilo.Relleno(l, 0f, 5f);
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.pickingMode = PickingMode.Ignore;
        return l;
    }
}
