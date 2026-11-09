using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Avisos en pantalla (globos a la derecha que entran deslizándose y se van solos), anuncios grandes en el centro
// ("¡Abordaje!") y el registro de todo lo ocurrido para la bitácora (G). Los objetos recogidos seguidos se agrupan ("+6 Madera").
public static class Notificaciones
{
    public enum Tipo { Info, Exito, Alerta, Peligro, Objeto, Descubrimiento, Comercio, Barco }

    public class Entrada
    {
        public string texto, icono;
        public Tipo tipo;
        public float hora;           // Time.time al ocurrir
        public System.DateTime reloj;
        public int veces = 1;
    }

    public static readonly List<Entrada> Registro = new List<Entrada>();
    public static event System.Action<Entrada> alRegistrar;
    public static int SinLeer { get; private set; }
    public static void MarcarLeidas() => SinLeer = 0;
    const int MaxRegistro = 300;

    class Globo
    {
        public VisualElement raiz; public Label texto; public string clave; public float hasta; public Entrada entrada; public bool saliendo;
    }

    static readonly List<Globo> globos = new List<Globo>();
    static VisualElement columna, anuncio;
    static Label anuncioTitulo, anuncioTexto;
    static float anuncioHasta;
    const int MaxGlobos = 5;
    const float AnchoGlobo = 340f;

    public static Color ColorDe(Tipo t)
    {
        switch (t)
        {
            case Tipo.Exito: return Estilo.Bien;
            case Tipo.Alerta: return Estilo.Ambar;
            case Tipo.Peligro: return Estilo.Mal;
            case Tipo.Objeto: return Estilo.Oro;
            case Tipo.Descubrimiento: return Estilo.Info;
            case Tipo.Comercio: return Estilo.OroVivo;
            case Tipo.Barco: return Estilo.ColCasco;
            default: return Estilo.Texto;
        }
    }

    public static string IconoDe(Tipo t)
    {
        switch (t)
        {
            case Tipo.Exito: return "check";
            case Tipo.Alerta: return "alerta";
            case Tipo.Peligro: return "calavera";
            case Tipo.Objeto: return "caja";
            case Tipo.Descubrimiento: return "isla";
            case Tipo.Comercio: return "moneda";
            case Tipo.Barco: return "barco";
            default: return "info";
        }
    }

    static void Asegurar()
    {
        if (columna != null && columna.panel != null) return;
        if (!Lienzo.Listo) return;
        columna = new VisualElement { name = "avisos", pickingMode = PickingMode.Ignore };
        columna.style.position = Position.Absolute;
        columna.style.right = 16f; columna.style.top = 150f;
        columna.style.width = AnchoGlobo;
        columna.style.flexDirection = FlexDirection.Column;
        columna.style.alignItems = Align.FlexEnd;
        Lienzo.CapaAvisos.Add(columna);

        anuncio = new VisualElement { name = "anuncio", pickingMode = PickingMode.Ignore };
        anuncio.style.position = Position.Absolute;
        anuncio.style.left = 0f; anuncio.style.right = 0f; anuncio.style.top = Length.Percent(22f);
        anuncio.style.alignItems = Align.Center;
        anuncio.style.display = DisplayStyle.None;
        anuncioTitulo = Estilo.Titulo("", Estilo.TamTituloGrande * 1.4f, Estilo.OroVivo);
        anuncioTitulo.style.unityTextAlign = TextAnchor.MiddleCenter;
        anuncioTitulo.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.9f), offset = new Vector2(0f, 2f), blurRadius = 6f };
        anuncioTitulo.style.letterSpacing = 3f;
        anuncio.Add(anuncioTitulo);
        anuncioTexto = Estilo.Etiqueta("", Estilo.TamTitulo, Estilo.Texto, Estilo.FuenteMedia);
        anuncioTexto.style.unityTextAlign = TextAnchor.MiddleCenter;
        anuncioTexto.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.9f), offset = new Vector2(0f, 1f), blurRadius = 4f };
        anuncioTexto.style.marginTop = 6f;
        anuncio.Add(anuncioTexto);
        Lienzo.CapaAvisos.Add(anuncio);
    }

    // ---------- Avisos ----------
    public static void Mostrar(string texto, Tipo tipo = Tipo.Info, float segundos = 4f, string icono = null, string clave = null)
    {
        if (string.IsNullOrEmpty(texto)) return;
        var e = new Entrada { texto = texto, tipo = tipo, icono = icono ?? IconoDe(tipo), hora = Time.time, reloj = System.DateTime.Now };
        Registrar(e);
        Asegurar();
        if (columna == null) return;

        // Si ya hay un globo con la misma clave, se actualiza en vez de apilar otro igual
        if (clave != null)
            foreach (var g in globos)
                if (g.clave == clave && !g.saliendo) { g.texto.text = texto; g.hasta = Time.unscaledTime + segundos; g.entrada.texto = texto; Rebotar(g.raiz); return; }

        var globo = Crear(e, clave);
        globo.hasta = Time.unscaledTime + segundos;
        globos.Add(globo);
        while (globos.Count > MaxGlobos) Quitar(globos[0]);
        switch (tipo)
        {
            case Tipo.Peligro: Sonidos.Reproducir(Sonidos.Tipo.Alerta); break;
            case Tipo.Alerta: Sonidos.Reproducir(Sonidos.Tipo.Error, 0.6f); break;
            case Tipo.Exito: Sonidos.Reproducir(Sonidos.Tipo.Exito); break;
            case Tipo.Descubrimiento: Sonidos.Reproducir(Sonidos.Tipo.Descubrir); break;
            case Tipo.Comercio: Sonidos.Reproducir(Sonidos.Tipo.Moneda); break;
            case Tipo.Objeto: Sonidos.Reproducir(Sonidos.Tipo.Transferir, 0.7f); break;
        }
    }

    // Adivina el tipo de un aviso por su texto (para los Avisar antiguos repartidos por el juego)
    public static Tipo Adivinar(string t)
    {
        string s = t.ToLowerInvariant();
        if (s.StartsWith("descubriste")) return Tipo.Descubrimiento;
        if (s.Contains("derrot") || s.Contains("hundi") || s.StartsWith("¡")) return Tipo.Peligro;
        if (s.Contains("guardad") || s.Contains("capturado") || s.StartsWith("+") || s.Contains("listo")) return Tipo.Exito;
        if (s.Contains("vendiste") || s.Contains("compraste") || s.Contains(" oro")) return Tipo.Comercio;
        if (s.StartsWith("no ") || s.StartsWith("primero") || s.Contains("no puedes") || s.Contains("falta") || s.Contains("aún no") || s.Contains("hace falta") || s.Contains("necesitas")) return Tipo.Alerta;
        return Tipo.Info;
    }

    public static void Info(string t) => Mostrar(t, Tipo.Info);
    public static void Exito(string t) => Mostrar(t, Tipo.Exito);
    public static void Alerta(string t) => Mostrar(t, Tipo.Alerta);
    public static void Peligro(string t) => Mostrar(t, Tipo.Peligro, 5f);

    // Objeto recogido: "+3 Madera" (se agrupa con los anteriores del mismo tipo)
    static readonly Dictionary<string, int> acumulado = new Dictionary<string, int>();
    static readonly Dictionary<string, float> acumuladoDesde = new Dictionary<string, float>();
    public static void Objeto(string tipo, int n, bool mostrar = true)
    {
        if (n == 0) return;
        if (!mostrar)
        {
            // Solo a la bitácora (el HUD ya lo enseña junto a la mochila)
            Registrar(new Entrada { texto = (n > 0 ? "+" : "") + n + " " + Objetos.Nombre(tipo), tipo = Tipo.Objeto, icono = Objetos.De(tipo).icono, hora = Time.time, reloj = System.DateTime.Now });
            return;
        }
        if (!acumulado.ContainsKey(tipo) || Time.unscaledTime - acumuladoDesde[tipo] > 4f) acumulado[tipo] = 0;
        acumulado[tipo] += n; acumuladoDesde[tipo] = Time.unscaledTime;
        var info = Objetos.De(tipo);
        string s = (acumulado[tipo] > 0 ? "+" : "") + acumulado[tipo] + " " + info.nombre;
        Mostrar(s, Tipo.Objeto, 3.5f, info.icono, "obj:" + tipo);
    }

    // Anuncio grande y centrado (abordaje, derrota, isla nueva)
    public static void Anuncio(string titulo, string subtitulo = null, float segundos = 3f, Tipo tipo = Tipo.Info)
    {
        Registrar(new Entrada { texto = titulo + (string.IsNullOrEmpty(subtitulo) ? "" : " — " + subtitulo), tipo = tipo, icono = IconoDe(tipo), hora = Time.time, reloj = System.DateTime.Now });
        Asegurar();
        if (anuncio == null) return;
        anuncioTitulo.text = titulo;
        anuncioTitulo.style.color = ColorDe(tipo) == Estilo.Texto ? Estilo.OroVivo : ColorDe(tipo);
        anuncioTexto.text = subtitulo ?? "";
        anuncioTexto.style.display = string.IsNullOrEmpty(subtitulo) ? DisplayStyle.None : DisplayStyle.Flex;
        anuncio.style.display = DisplayStyle.Flex;
        anuncio.style.opacity = 0f;
        anuncio.style.scale = new Scale(new Vector3(1.08f, 1.08f, 1f));
        anuncioHasta = Time.unscaledTime + segundos;
        Lienzo.Animar(anuncio, Estilo.Ms(260), v => { anuncio.style.opacity = v; float s = Mathf.Lerp(1.08f, 1f, Lienzo.Suave(v)); anuncio.style.scale = new Scale(new Vector3(s, s, 1f)); });
        if (tipo == Tipo.Peligro) Sonidos.Reproducir(Sonidos.Tipo.Alerta); else Sonidos.Reproducir(Sonidos.Tipo.Descubrir, 0.8f);
    }

    static void Registrar(Entrada e)
    {
        // Mensajes idénticos seguidos se cuentan en vez de repetirse
        if (Registro.Count > 0)
        {
            Entrada u = Registro[Registro.Count - 1];
            if (u.texto == e.texto && u.tipo == e.tipo && Time.time - u.hora < 30f) { u.veces++; u.hora = e.hora; u.reloj = e.reloj; alRegistrar?.Invoke(u); return; }
        }
        Registro.Add(e);
        if (Registro.Count > MaxRegistro) Registro.RemoveAt(0);
        SinLeer++;
        alRegistrar?.Invoke(e);
    }

    static Globo Crear(Entrada e, string clave)
    {
        var raiz = new VisualElement { pickingMode = PickingMode.Ignore };
        raiz.AddToClassList("aviso");
        raiz.AddToClassList("aviso--oculto");
        raiz.style.flexDirection = FlexDirection.Row;
        raiz.style.alignItems = Align.Center;
        raiz.style.width = AnchoGlobo;
        raiz.style.marginBottom = 6f;
        Estilo.PanelMadera(raiz, 0.92f);
        raiz.style.overflow = Overflow.Hidden;
        Color c = ColorDe(e.tipo);
        var franja = new VisualElement { pickingMode = PickingMode.Ignore };
        franja.style.width = 4f; franja.style.alignSelf = Align.Stretch; franja.style.backgroundColor = c;
        franja.style.marginRight = 8f;
        raiz.Add(franja);
        var ic = Iconos.Crear(e.icono, 20f, c);
        ic.style.marginTop = ic.style.marginBottom = 8f; ic.style.marginRight = 8f;
        raiz.Add(ic);
        var l = Estilo.Etiqueta(e.texto, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
        l.style.flexGrow = 1f; l.style.flexShrink = 1f; l.style.marginRight = 10f;
        l.style.marginTop = l.style.marginBottom = 7f;
        l.pickingMode = PickingMode.Ignore;
        raiz.Add(l);
        columna.Add(raiz);
        raiz.schedule.Execute(() => raiz.RemoveFromClassList("aviso--oculto")).ExecuteLater(16);
        return new Globo { raiz = raiz, texto = l, clave = clave, entrada = e };
    }

    static void Rebotar(VisualElement e)
    {
        if (Preferencias.ReducirMovimiento) return;
        e.style.scale = new Scale(new Vector3(1.04f, 1.04f, 1f));
        e.schedule.Execute(() => e.style.scale = new Scale(Vector3.one)).ExecuteLater(90);
    }

    static void Quitar(Globo g)
    {
        if (g.saliendo) return;
        g.saliendo = true;
        g.raiz.AddToClassList("aviso--oculto");
        int ms = Estilo.Ms(220);
        var raiz = g.raiz;
        if (ms <= 0) raiz.RemoveFromHierarchy();
        else raiz.schedule.Execute(() => raiz.RemoveFromHierarchy()).ExecuteLater(ms + 20);
        globos.Remove(g);
    }

    internal static void Actualizar()
    {
        if (globos.Count > 0)
            for (int i = globos.Count - 1; i >= 0; i--)
                if (Time.unscaledTime >= globos[i].hasta) Quitar(globos[i]);
        if (anuncio != null && anuncio.resolvedStyle.display == DisplayStyle.Flex && Time.unscaledTime >= anuncioHasta)
        {
            anuncioHasta = float.MaxValue;
            Lienzo.Animar(anuncio, Estilo.Ms(400), v => anuncio.style.opacity = 1f - v, () => { if (anuncioHasta == float.MaxValue) anuncio.style.display = DisplayStyle.None; });
        }
        // Las ventanas tapan la columna de avisos a la derecha: cuando hay una abierta, los avisos bajan al borde inferior
        if (columna != null)
        {
            bool ventana = Ventanas.Cuantas > 0;
            columna.style.top = ventana ? (StyleLength)StyleKeyword.Auto : (StyleLength)150f;
            columna.style.bottom = ventana ? (StyleLength)90f : (StyleLength)StyleKeyword.Auto;
        }
    }

    public static void Limpiar()
    {
        foreach (var g in new List<Globo>(globos)) Quitar(g);
    }

    public static string Hora(Entrada e) => e.reloj.ToString("HH:mm");
}
