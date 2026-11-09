using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Taller de pintura (P, a bordo de tu barco), con la interfaz nueva. A la izquierda las partes del barco (con su color y
// un punto si cambió); a la derecha el color: paleta, color libre (tono, color vivo, brillo), dibujo de velas y bandera
// con vista previa, y el mascarón. Abajo el costo y Guardar / Cancelar / Todo original. La cámara gira alrededor del barco.
public class VentanaTaller : Ventana
{
    static VentanaTaller i;
    public static VentanaTaller I => i ??= new VentanaTaller();

    int sel;            // parte elegida, o NombresParte.Length = mascarón
    bool segundo;       // editando el color del dibujo
    readonly VisualElement partes, zona;
    readonly Label costo;
    readonly Button guardar;
    readonly List<(Button b, VisualElement muestra, VisualElement punto)> botonesParte = new List<(Button, VisualElement, VisualElement)>();
    Componentes.Deslizador tono, vivo, brillo;
    VisualElement vistaColor;

    static Pintura P => Taller.PinturaActual;

    VentanaTaller() : base("taller", "Taller de pintura", "brocha", 700f, 640f)
    {
        var cuerpo = Estilo.Fila(12f, Align.Stretch); cuerpo.style.flexGrow = 1f; cuerpo.style.flexShrink = 1f;
        Contenido.Add(cuerpo);
        partes = Estilo.Columna(); partes.style.width = 210f; partes.style.flexShrink = 0f; partes.style.marginRight = 12f;
        cuerpo.Add(partes);
        var sv = Componentes.Lista(); sv.style.flexGrow = 1f;
        zona = Estilo.Columna(); zona.style.paddingRight = 8f;
        sv.Add(zona);
        cuerpo.Add(sv);

        var pie = Estilo.Fila(8f); pie.style.marginTop = 10f;
        costo = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Oro, Estilo.FuenteNegrita); costo.style.flexGrow = 1f;
        pie.Add(costo);
        var orig = Componentes.Boton("Todo original", () => { P.datos = new Pintura.Datos { mascaron = Taller.Antes != null ? Taller.Antes.mascaron : "" }; P.Aplicar(); Dibujar(); }, "flecha_izq", Componentes.Variante.Fantasma);
        pie.Add(orig);
        var cancelar = Componentes.Boton("Cancelar", () => Taller.CancelarCambios(), "cerrar"); cancelar.style.marginLeft = 8f;
        pie.Add(cancelar);
        guardar = Componentes.Boton("Guardar", () => Taller.GuardarCambios(), "check", Componentes.Variante.Primario); guardar.style.marginLeft = 8f;
        pie.Add(guardar);
        Contenido.Add(pie);
    }

    protected override float Refrescos => 4f;

    protected override void AlAbrir()
    {
        sel = 0; segundo = false;
        Titulo = "Taller — " + (P != null ? P.name : "");
        Partes();
        Dibujar();
    }

    protected override void AlCerrar()
    {
        if (Taller.Abierto) Taller.CancelarCambios();   // Esc o la X: se descartan los cambios sin guardar
    }

    public override void Actualizar()
    {
        if (P == null) return;
        int c = Taller.CostoActual;
        costo.text = ModoCreativo.Activo ? "Gratis (modo creativo)" : c == 0 ? "Sin cambios" : "Costo: " + c + " de oro";
        Inventario inv = Personaje.Instancia != null ? Personaje.Instancia.GetComponent<Inventario>() : null;
        bool alcanza = ModoCreativo.Activo || inv == null || inv.Cantidad(Inventario.Oro) >= c;
        costo.style.color = alcanza ? Estilo.Oro : Estilo.Mal;
        guardar.SetEnabled(alcanza);
        PintarPartes();
    }

    void Partes()
    {
        partes.Clear(); botonesParte.Clear();
        for (int k = 0; k <= Pintura.NombresParte.Length; k++)
        {
            int idx = k;
            string n = k < Pintura.NombresParte.Length ? Pintura.NombresParte[k] : "Mascarón de proa";
            var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); sel = idx; segundo = false; Dibujar(); }) { text = "" };
            b.AddToClassList("fila-lista");
            b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.height = 36f;
            Estilo.Margen(b, 0f); b.style.marginBottom = 4f; Estilo.Relleno(b, 0f, 8f);
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f;
            Estilo.Redondear(b, Estilo.Radio);
            var muestra = new VisualElement(); muestra.style.width = 18f; muestra.style.height = 18f; Estilo.Borde(muestra, new Color(0f, 0f, 0f, 0.6f), 1f, 4f);
            if (k == Pintura.NombresParte.Length) { muestra.style.backgroundColor = Color.clear; muestra.Add(Iconos.Crear("calavera", 16f, Estilo.Oro)); }
            b.Add(muestra);
            var l = Estilo.Etiqueta(n, Estilo.TamTextoChico, Estilo.Texto, Estilo.FuenteMedia); l.style.marginLeft = 8f; l.style.flexGrow = 1f; l.style.unityTextAlign = TextAnchor.MiddleLeft; l.pickingMode = PickingMode.Ignore;
            b.Add(l);
            var punto = new VisualElement(); punto.style.width = 8f; punto.style.height = 8f; Estilo.Redondear(punto, 4f); punto.style.backgroundColor = Estilo.Oro;
            Tooltip.Asignar(punto, "Cambiado");
            b.Add(punto);
            partes.Add(b);
            botonesParte.Add((b, muestra, punto));
        }
        PintarPartes();
    }

    void PintarPartes()
    {
        var pt = P; var antes = Taller.Antes;
        if (pt == null || antes == null) return;
        for (int k = 0; k < botonesParte.Count; k++)
        {
            var (b, muestra, punto) = botonesParte[k];
            b.EnableInClassList("fila-lista--elegida", k == sel);
            bool cambiado;
            if (k < Pintura.NombresParte.Length)
            {
                Pintura.Estilo e = pt.De((Pintura.Parte)k);
                muestra.style.backgroundColor = e.pintado ? e.color : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                cambiado = !e.Igual(antes.estilos[k]);
            }
            else cambiado = (pt.datos.mascaron ?? "") != (antes.mascaron ?? "");
            punto.style.visibility = cambiado ? Visibility.Visible : Visibility.Hidden;
        }
    }

    // ---------- Panel de la parte elegida ----------
    void Dibujar()
    {
        zona.Clear();
        if (P == null) return;
        if (sel >= Pintura.NombresParte.Length) { Mascarones(); Actualizar(); return; }
        var parte = (Pintura.Parte)sel;
        Pintura.Estilo e = P.De(parte);
        bool dibujo = Pintura.TieneDibujo(parte);

        var cab = Estilo.Fila(8f); cab.style.marginBottom = 8f;
        cab.Add(Estilo.Titulo(Pintura.NombresParte[sel], Estilo.TamTitulo * 0.9f));
        cab.Add(Estilo.Hueco(0f, 0f, true));
        var orig = Componentes.Boton(e.pintado ? "Color original" : "Color original ✓", () => { e.pintado = false; P.Aplicar(); Dibujar(); }, null, Componentes.Variante.Fantasma, 28f);
        orig.SetEnabled(e.pintado);
        cab.Add(orig);
        zona.Add(cab);

        if (dibujo)
        {
            var pes = Estilo.Fila(4f); pes.style.marginBottom = 8f;
            var c1 = Componentes.Ficha("Color principal", () => { segundo = false; Dibujar(); }, null, !segundo);
            var c2 = Componentes.Ficha("Color del dibujo", () => { segundo = true; Dibujar(); }, null, segundo);
            pes.Add(c1); pes.Add(c2);
            zona.Add(pes);
        }

        Color actual = segundo && dibujo ? e.color2 : e.color;
        zona.Add(Seccion("PALETA"));
        var paleta = Estilo.Fila(0f); paleta.style.flexWrap = Wrap.Wrap; paleta.style.marginBottom = 8f;
        foreach (var (nombre, color) in Pintura.Paleta)
        {
            var c = color;
            var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); Fijar(c); Dibujar(); }) { text = "" };
            b.style.width = 52f; b.style.height = 34f; Estilo.Margen(b, 3f);
            b.style.backgroundColor = c; b.style.backgroundImage = null;
            bool este = e.pintado && Parecido(actual, c);
            Estilo.Borde(b, este ? Estilo.OroVivo : new Color(0f, 0f, 0f, 0.6f), este ? 3f : 1f, 6f);
            Tooltip.Asignar(b, nombre);
            paleta.Add(b);
        }
        zona.Add(paleta);

        zona.Add(Seccion("COLOR LIBRE"));
        Color.RGBToHSV(actual, out float h, out float s, out float v);
        tono = new Componentes.Deslizador("Tono", 0f, 1f, h, x => Mathf.RoundToInt(x * 360f) + "°", 0f);
        vivo = new Componentes.Deslizador("Color vivo", 0f, 1f, s, x => Mathf.RoundToInt(x * 100f) + " %", 0f);
        brillo = new Componentes.Deslizador("Brillo", 0f, 1f, v, x => Mathf.RoundToInt(x * 100f) + " %", 0f);
        tono.alCambiar = vivo.alCambiar = brillo.alCambiar = _ => { Fijar(Color.HSVToRGB(tono.Valor, vivo.Valor, brillo.Valor)); vistaColor.style.backgroundColor = Color.HSVToRGB(tono.Valor, vivo.Valor, brillo.Valor); };
        zona.Add(tono); zona.Add(vivo); zona.Add(brillo);
        vistaColor = new VisualElement(); vistaColor.style.height = 18f; vistaColor.style.marginBottom = 10f;
        vistaColor.style.backgroundColor = actual; Estilo.Borde(vistaColor, new Color(0f, 0f, 0f, 0.6f), 1f, 4f);
        zona.Add(vistaColor);

        if (dibujo)
        {
            zona.Add(Seccion("DIBUJO"));
            var patrones = Estilo.Fila(0f); patrones.style.flexWrap = Wrap.Wrap;
            for (int k = 0; k < Pintura.NombresPatron.Length; k++)
            {
                int idx = k;
                var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); e.patron = idx; e.pintado = true; P.Aplicar(); Dibujar(); }) { text = "" };
                b.AddToClassList("casilla");
                if (e.patron == k && e.pintado) b.AddToClassList("casilla--elegida");
                b.style.width = 92f; b.style.height = 84f; Estilo.Margen(b, 3f); Estilo.Relleno(b, 4f);
                b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f;
                Estilo.Redondear(b, Estilo.Radio);
                b.style.flexDirection = FlexDirection.Column; b.style.alignItems = Align.Center;
                var img = new VisualElement { pickingMode = PickingMode.Ignore };
                img.style.width = 78f; img.style.height = 52f;
                Texture2D t = Pintura.Textura((Pintura.Patron)k, e.pintado ? e.color : Color.white, e.color2);
                if (t != null) { img.style.backgroundImage = new StyleBackground(t); img.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f)); }
                else img.style.backgroundColor = e.pintado ? e.color : Color.white;
                Estilo.Redondear(img, 3f);
                b.Add(img);
                var l = Estilo.Etiqueta(Pintura.NombresPatron[k], Estilo.TamTextoChico * 0.9f, Estilo.Texto); l.pickingMode = PickingMode.Ignore; l.style.marginTop = 3f;
                b.Add(l);
                patrones.Add(b);
            }
            zona.Add(patrones);
        }
        Actualizar();
    }

    static bool Parecido(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

    void Fijar(Color c)
    {
        var parte = (Pintura.Parte)sel;
        Pintura.Estilo e = P.De(parte);
        bool dibujo = Pintura.TieneDibujo(parte);
        if (segundo && dibujo) { if (!e.pintado) e.color = Color.white; e.color2 = c; }
        else e.color = c;
        e.pintado = true;
        P.Aplicar();
        PintarPartes();
    }

    static Label Seccion(string t)
    {
        var l = Estilo.Etiqueta(t, Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita);
        l.style.letterSpacing = 1.5f; l.style.marginTop = 6f; l.style.marginBottom = 4f;
        return l;
    }

    void Mascarones()
    {
        var cab = Estilo.Titulo("Mascarón de proa", Estilo.TamTitulo * 0.9f); cab.style.marginBottom = 4f;
        zona.Add(cab);
        var n = Estilo.Pequena("La figura tallada en la proa. Cambiarlo cuesta " + Taller.PrecioMascaron + " de oro.");
        n.style.marginBottom = 8f;
        zona.Add(n);
        for (int k = 0; k < Pintura.Mascarones.Length; k++)
        {
            int idx = k;
            bool elegido = P.datos.mascaron == Pintura.Mascarones[k];
            var b = Componentes.Boton(Pintura.NombresMascaron[k], () => { P.datos.mascaron = Pintura.Mascarones[idx]; P.Aplicar(); Dibujar(); },
                k == Pintura.Mascarones.Length - 1 ? "cerrar" : "calavera", elegido ? Componentes.Variante.Primario : Componentes.Variante.Normal, 40f);
            b.style.marginBottom = 6f;
            zona.Add(b);
        }
    }
}
