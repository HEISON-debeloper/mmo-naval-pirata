using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Fabricación (Tab): categorías a la izquierda, recetas en el centro y la receta elegida a la derecha con sus materiales
// (lo que tienes y lo que falta), requisitos, cantidad y el botón de fabricar. Búsqueda, favoritos (estrella),
// "solo lo que puedo hacer" y la última categoría usada se recuerdan.
// Teclado: ↑/↓ elegir receta, Enter fabricar, Mayús+Enter fabricar 5, Q/E cambiar de categoría.
public class VentanaFabricacion : Ventana
{
    static VentanaFabricacion i;
    public static VentanaFabricacion I => i ??= new VentanaFabricacion();

    const int Favoritos = -1;
    int categoria;                         // índice de Fabricacion.Categoria, o Favoritos
    Fabricacion.Receta elegida;
    readonly VisualElement columnaCategorias, detalle, cabeceraBarco;
    readonly ScrollView lista;
    readonly Label textoBarco, vacio;
    readonly Componentes.CampoBusqueda buscar;
    readonly Button soloPosibles;
    readonly Dictionary<int, (Button boton, Label cuenta)> botonesCategoria = new Dictionary<int, (Button, Label)>();
    readonly List<Fila> filas = new List<Fila>();
    readonly Componentes.Contador cantidad;

    class Fila
    {
        public Fabricacion.Receta receta;
        public VisualElement raiz;
        public Label nombre, linea, alcanza;
        public Icono icono;
        public Button estrella;
    }

    Fabricacion F => Fabricacion.I;

    VentanaFabricacion() : base("fabricacion", "Fabricación", "martillo", 1000f, 620f)
    {
        categoria = Mathf.Clamp(Preferencias.CategoriaFabricar, Favoritos, (int)Fabricacion.Categoria.Creativo);

        var barra = Estilo.Fila(8f); barra.style.marginBottom = 10f;
        buscar = new Componentes.CampoBusqueda("Buscar receta o material…", 280f);
        buscar.alCambiar = _ => Reconstruir();
        barra.Add(buscar);
        soloPosibles = Componentes.Ficha("Solo lo que puedo hacer", () => { Preferencias.FiltroSoloPosibles = !Preferencias.FiltroSoloPosibles; Preferencias.Guardar(); soloPosibles.EnableInClassList("ficha--activa", Preferencias.FiltroSoloPosibles); Reconstruir(); }, "check", Preferencias.FiltroSoloPosibles);
        soloPosibles.style.marginLeft = 10f;
        barra.Add(soloPosibles);
        barra.Add(Estilo.Hueco(0f, 0f, true));
        var ayuda = Estilo.Fila(10f);
        ayuda.Add(Componentes.Indicacion("↑↓", "elegir", 0.8f)); ayuda.Add(Estilo.Hueco(10f));
        ayuda.Add(Componentes.Indicacion("Enter", "fabricar", 0.8f)); ayuda.Add(Estilo.Hueco(10f));
        ayuda.Add(Componentes.Indicacion("Q/E", "categoría", 0.8f));
        barra.Add(ayuda);
        Contenido.Add(barra);

        var cuerpo = Estilo.Fila(0f, Align.Stretch);
        cuerpo.style.flexGrow = 1f; cuerpo.style.flexShrink = 1f;
        Contenido.Add(cuerpo);

        // Categorías
        columnaCategorias = Estilo.Columna();
        columnaCategorias.style.width = 190f; columnaCategorias.style.flexShrink = 0f; columnaCategorias.style.marginRight = 12f;
        cuerpo.Add(columnaCategorias);

        // Recetas
        var centro = Estilo.Columna(); centro.style.flexGrow = 1f; centro.style.flexShrink = 1f; centro.style.marginRight = 12f;
        cabeceraBarco = Estilo.Fila(6f);
        cabeceraBarco.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f); Estilo.Redondear(cabeceraBarco, Estilo.Radio); Estilo.Relleno(cabeceraBarco, 6f, 10f);
        cabeceraBarco.style.marginBottom = 8f;
        cabeceraBarco.Add(Iconos.Crear("barco", 16f, Estilo.Oro));
        textoBarco = Estilo.Pequena("", Estilo.Texto); textoBarco.style.marginLeft = 6f; textoBarco.style.flexShrink = 1f;
        cabeceraBarco.Add(textoBarco);
        centro.Add(cabeceraBarco);
        lista = Componentes.Lista();
        centro.Add(lista);
        vacio = Estilo.Pequena("");
        vacio.style.marginTop = 12f;
        centro.Add(vacio);
        cuerpo.Add(centro);

        // Detalle
        detalle = Componentes.Tarjeta(null, null, 0.6f);
        detalle.style.width = 300f; detalle.style.flexShrink = 0f;
        cuerpo.Add(detalle);
        cantidad = new Componentes.Contador(1, 1, 99);
        cantidad.alCambiar = _ => DibujarDetalle(true);

    }

    protected override float Refrescos => 4f;

    protected override void AlAbrir()
    {
        ConstruirCategorias();
        Reconstruir();
    }

    public void IrACategoria(Fabricacion.Categoria c)
    {
        categoria = (int)c;
        buscar.Limpiar();
        if (Abierta) { ConstruirCategorias(); Reconstruir(); }
    }

    public void Buscar(string texto)
    {
        categoria = int.MinValue;   // todas
        ConstruirCategorias();
        buscar.Limpiar();
        schedule.Execute(() => { var campo = buscar.Q<TextField>(); if (campo != null) campo.value = texto; }).ExecuteLater(10);
    }

    public override void Actualizar()
    {
        if (F == null) return;
        foreach (var f in filas) PintarFila(f);
        ContarCategorias();
        DibujarDetalle(false);
        if (categoria == (int)Fabricacion.Categoria.Barco || categoria == (int)Fabricacion.Categoria.Armamento) textoBarco.text = F.ResumenBarco() ?? "Sube a tu barco para mejorarlo";
    }

    // ---------- Categorías ----------
    void ConstruirCategorias()
    {
        columnaCategorias.Clear();
        botonesCategoria.Clear();
        if (categoria == (int)Fabricacion.Categoria.Creativo && !ModoCreativo.Activo) categoria = 0;
        AgregarCategoria(Favoritos, "Favoritos", "estrella");
        AgregarCategoria(int.MinValue, "Todo", "pergamino");
        for (int c = 0; c < Fabricacion.NombresCategoria.Length; c++)
        {
            if (c == (int)Fabricacion.Categoria.Creativo && !ModoCreativo.Activo) continue;
            AgregarCategoria(c, Fabricacion.NombresCategoria[c], Fabricacion.IconosCategoria[c]);
        }
        ContarCategorias();
    }

    void AgregarCategoria(int c, string nombre, string icono)
    {
        var b = new Button(() => { Sonidos.Reproducir(Sonidos.Tipo.Clic); Elegir(c); }) { text = "" };
        b.AddToClassList("fila-lista");
        if (c == categoria) b.AddToClassList("fila-lista--elegida");
        b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center;
        b.style.height = 38f; Estilo.Margen(b, 0f); b.style.marginBottom = 4f; Estilo.Relleno(b, 0f, 10f);
        b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 1f;
        Estilo.Redondear(b, Estilo.Radio);
        b.Add(Iconos.Crear(icono, 18f, Estilo.Oro));
        var l = Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
        l.style.marginLeft = 8f; l.style.flexGrow = 1f; l.pickingMode = PickingMode.Ignore;
        b.Add(l);
        var cuenta = Componentes.Insignia("0");
        b.Add(cuenta);
        Tooltip.Asignar(b, () => "Recetas que puedes hacer ahora en " + nombre);
        b.RegisterCallback<PointerEnterEvent>(_ => Sonidos.Reproducir(Sonidos.Tipo.Pasar, 0.4f));
        columnaCategorias.Add(b);
        botonesCategoria[c] = (b, cuenta);
    }

    void ContarCategorias()
    {
        if (F == null) return;
        var cuenta = new Dictionary<int, int>();
        foreach (var k in botonesCategoria.Keys) cuenta[k] = 0;
        foreach (var r in F.Todas)
        {
            if (!Visible(r, false)) continue;
            if (F.Impedimento(r) != null) continue;
            int c = (int)r.categoria;
            if (cuenta.ContainsKey(c)) cuenta[c]++;
            cuenta[int.MinValue]++;
            if (Preferencias.EsFavorito(r.id)) cuenta[Favoritos]++;
        }
        foreach (var kv in botonesCategoria)
        {
            int n = cuenta.TryGetValue(kv.Key, out int x) ? x : 0;
            kv.Value.cuenta.text = n.ToString();
            kv.Value.cuenta.style.backgroundColor = n > 0 ? Estilo.Bien : new Color(1f, 1f, 1f, 0.15f);
        }
    }

    void Elegir(int c)
    {
        categoria = c;
        if (c != int.MinValue) { Preferencias.CategoriaFabricar = c; Preferencias.Guardar(); }
        foreach (var kv in botonesCategoria) kv.Value.boton.EnableInClassList("fila-lista--elegida", kv.Key == c);
        Reconstruir();
    }

    // ---------- Recetas ----------
    bool Visible(Fabricacion.Receta r, bool conFiltros)
    {
        if (r.categoria == Fabricacion.Categoria.Creativo && !ModoCreativo.Activo) return false;
        if (!conFiltros) return true;
        if (categoria == Favoritos && !Preferencias.EsFavorito(r.id)) return false;
        if (categoria >= 0 && (int)r.categoria != categoria) return false;
        string q = buscar.Texto.Trim().ToLowerInvariant();
        if (q != "")
        {
            bool coincide = r.nombre.ToLowerInvariant().Contains(q) || (r.descripcion ?? "").ToLowerInvariant().Contains(q);
            if (!coincide) foreach (var c in r.costo()) if (Objetos.Nombre(c.Key).ToLowerInvariant().Contains(q)) { coincide = true; break; }
            if (!coincide) return false;
        }
        if (Preferencias.FiltroSoloPosibles && F.Impedimento(r) != null) return false;
        return true;
    }

    void Reconstruir()
    {
        if (F == null) return;
        lista.Clear(); filas.Clear();
        bool infoBarco = categoria == (int)Fabricacion.Categoria.Barco || categoria == (int)Fabricacion.Categoria.Armamento;
        cabeceraBarco.style.display = infoBarco ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (var r in F.Todas)
        {
            if (!Visible(r, true)) continue;
            var f = CrearFila(r);
            filas.Add(f);
            lista.Add(f.raiz);
        }
        // Las que se pueden hacer primero; luego las favoritas
        if (filas.Count > 0 && elegida != null && !filas.Exists(x => x.receta == elegida)) elegida = null;
        if (elegida == null && filas.Count > 0) elegida = filas[0].receta;
        vacio.text = categoria == Favoritos && buscar.Texto == "" ? "Aún no tienes favoritos: marca recetas con la estrella." : "Ninguna receta coincide.";
        vacio.style.display = filas.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        Actualizar();
        DibujarDetalle(true);
    }

    Fila CrearFila(Fabricacion.Receta r)
    {
        var f = new Fila { receta = r };
        var raiz = Componentes.FilaLista();
        raiz.style.minHeight = 50f;
        f.icono = Iconos.Crear(r.icono ?? "martillo", 26f, Estilo.Oro);
        f.icono.style.marginRight = 10f;
        raiz.Add(f.icono);
        var col = Estilo.Columna(); col.style.flexGrow = 1f; col.style.flexShrink = 1f; col.pickingMode = PickingMode.Ignore;
        f.nombre = Estilo.Etiqueta(r.nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteNegrita); f.nombre.pickingMode = PickingMode.Ignore;
        col.Add(f.nombre);
        f.linea = Estilo.Pequena(""); f.linea.pickingMode = PickingMode.Ignore;
        f.linea.style.whiteSpace = WhiteSpace.NoWrap; f.linea.style.overflow = Overflow.Hidden; f.linea.style.textOverflow = TextOverflow.Ellipsis;
        col.Add(f.linea);
        raiz.Add(col);
        f.alcanza = Estilo.Pequena(""); f.alcanza.style.marginLeft = 8f; f.alcanza.pickingMode = PickingMode.Ignore;
        raiz.Add(f.alcanza);
        f.estrella = Componentes.BotonIcono(Preferencias.EsFavorito(r.id) ? "estrella" : "estrella_vacia", () =>
        {
            Preferencias.AlternarFavorito(r.id);
            Componentes.IconoDe(f.estrella).Nombre = Preferencias.EsFavorito(r.id) ? "estrella" : "estrella_vacia";
            ContarCategorias();
        }, "Favorito", 16f, Estilo.Oro);
        raiz.Add(f.estrella);
        raiz.RegisterCallback<ClickEvent>(e =>
        {
            if (e.target is Button) return;
            Sonidos.Reproducir(Sonidos.Tipo.Clic);
            Seleccionar(r);
            if (e.clickCount == 2) Fabricar(1);
        });
        raiz.RegisterCallback<PointerEnterEvent>(_ => Sonidos.Reproducir(Sonidos.Tipo.Pasar, 0.4f));
        f.raiz = raiz;
        PintarFila(f);
        return f;
    }

    void PintarFila(Fila f)
    {
        var r = f.receta;
        string imp = F.Impedimento(r);
        bool puede = imp == null;
        f.raiz.EnableInClassList("fila-lista--elegida", r == elegida);
        f.raiz.EnableInClassList("fila-lista--imposible", !puede);
        f.nombre.style.color = puede ? Estilo.Texto : Estilo.TextoTenue;
        f.icono.Color = puede ? Estilo.Oro : Estilo.TextoApagado;
        string req = F.Requisito(r);
        string texto = req != null ? Estilo.Col(req, Estilo.Ambar) : CostoCorto(r);
        if (f.linea.text != texto) f.linea.text = texto;
        int veces = r.unaVez || req != null ? 0 : F.VecesQueAlcanza(r);
        f.alcanza.text = ModoCreativo.Activo || veces <= 1 ? "" : "×" + Mathf.Min(veces, 99);
    }

    string CostoCorto(Fabricacion.Receta r)
    {
        var inv = F.Inv;
        string s = "";
        foreach (var c in r.costo())
        {
            bool ok = ModoCreativo.Activo || inv.Cantidad(c.Key) >= c.Value;
            s += (s == "" ? "" : "  ") + Estilo.Col(c.Value + " " + Objetos.Corto(c.Key), ok ? Estilo.Bien : Estilo.Mal);
        }
        if (s == "") return r.detalle != null ? (r.detalle() ?? r.descripcion) : r.descripcion;
        return s;
    }

    void Seleccionar(Fabricacion.Receta r)
    {
        elegida = r;
        cantidad.Valor = 1;
        foreach (var f in filas) f.raiz.EnableInClassList("fila-lista--elegida", f.receta == r);
        DibujarDetalle(true);
        var fila = filas.Find(x => x.receta == r);
        if (fila != null) lista.ScrollTo(fila.raiz);
    }

    // ---------- Detalle ----------
    string firmaDetalle;

    void DibujarDetalle(bool forzar)
    {
        if (F == null) return;
        var r = elegida;
        string imp = r != null ? F.Impedimento(r) : null;
        string det = r != null && r.detalle != null ? r.detalle() : null;
        string firma = r == null ? "" : r.id + "|" + imp + "|" + det + "|" + CostoCorto(r) + "|" + cantidad.Valor + "|" + F.VecesQueAlcanza(r);
        if (!forzar && firma == firmaDetalle) return;
        firmaDetalle = firma;
        detalle.Clear();
        if (r == null)
        {
            var t = Estilo.Etiqueta("Elige una receta.", Estilo.TamTexto, Estilo.TextoTenue);
            t.style.marginTop = 20f; t.style.unityTextAlign = TextAnchor.MiddleCenter;
            detalle.Add(t);
            return;
        }
        var marco = new VisualElement();
        marco.style.height = 96f; marco.style.alignItems = Align.Center; marco.style.justifyContent = Justify.Center;
        marco.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f); Estilo.Redondear(marco, Estilo.Radio); marco.style.marginBottom = 8f;
        Color col = r.resultado != null ? Objetos.De(r.resultado).color : Estilo.Oro;
        marco.Add(Iconos.Crear(r.resultado != null ? Objetos.De(r.resultado).icono : r.icono ?? "martillo", 56f, col));
        detalle.Add(marco);
        detalle.Add(Estilo.Titulo(r.nombre, Estilo.TamTitulo));
        var cat = Estilo.Pequena(Fabricacion.NombresCategoria[(int)r.categoria]); cat.style.marginBottom = 6f;
        detalle.Add(cat);
        detalle.Add(Estilo.Etiqueta(r.descripcion ?? "", Estilo.TamTextoChico, Estilo.Texto));
        if (!string.IsNullOrEmpty(det)) { var d = Estilo.Etiqueta(det, Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteMedia); d.style.marginTop = 4f; detalle.Add(d); }

        // Materiales
        var costo = r.costo();
        if (costo.Count > 0)
        {
            var cab = Estilo.Etiqueta("MATERIALES", Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita);
            cab.style.letterSpacing = 1.5f; cab.style.marginTop = 10f; cab.style.marginBottom = 4f;
            detalle.Add(cab);
            int veces = r.unaVez ? 1 : cantidad.Valor;
            foreach (var c in costo)
            {
                int tiene = F.Inv.Cantidad(c.Key), hace = c.Value * veces;
                bool ok = ModoCreativo.Activo || tiene >= hace;
                var info = Objetos.De(c.Key);
                var fila = Estilo.Fila(6f); fila.style.marginBottom = 3f;
                fila.Add(Iconos.Crear(info.icono, 18f, info.color));
                var n = Estilo.Etiqueta(info.nombre, Estilo.TamTextoChico, Estilo.Texto); n.style.marginLeft = 6f; n.style.flexGrow = 1f;
                fila.Add(n);
                var v = Estilo.Etiqueta(tiene + " / " + hace, Estilo.TamTextoChico, ok ? Estilo.Bien : Estilo.Mal, Estilo.FuenteNegrita);
                fila.Add(v);
                Tooltip.Asignar(fila, Objetos.Descripcion(c.Key, tiene));
                detalle.Add(fila);
            }
            if (ModoCreativo.Activo) { var g = Estilo.Pequena("Gratis en modo creativo", Estilo.Oro); g.style.marginTop = 2f; detalle.Add(g); }
        }
        else { var s = Estilo.Pequena("Sin costo"); s.style.marginTop = 8f; detalle.Add(s); }

        string req = F.Requisito(r);
        if (req != null)
        {
            var aviso = Estilo.Fila(6f); aviso.style.marginTop = 10f;
            aviso.style.backgroundColor = new Color(Estilo.Ambar.r, Estilo.Ambar.g, Estilo.Ambar.b, 0.12f); Estilo.Redondear(aviso, 4f); Estilo.Relleno(aviso, 6f, 8f);
            aviso.Add(Iconos.Crear("alerta", 16f, Estilo.Ambar));
            var t = Estilo.Etiqueta(req, Estilo.TamTextoChico, Estilo.Ambar); t.style.marginLeft = 6f; t.style.flexShrink = 1f;
            aviso.Add(t);
            detalle.Add(aviso);
        }

        detalle.Add(Estilo.Hueco(0f, 0f, true));
        // Cantidad y botón
        int max = r.unaVez ? 1 : Mathf.Clamp(F.VecesQueAlcanza(r), 1, 99);
        if (!r.unaVez)
        {
            cantidad.Maximo = max;
            var fc = Estilo.Fila(6f); fc.style.marginTop = 10f;
            var et = Estilo.Pequena("Cantidad"); et.style.marginRight = 8f; et.style.flexGrow = 1f;
            fc.Add(et); fc.Add(cantidad);
            detalle.Add(fc);
        }
        var boton = Componentes.Boton(r.unaVez || cantidad.Valor <= 1 ? "Fabricar" : "Fabricar " + cantidad.Valor, () => Fabricar(r.unaVez ? 1 : cantidad.Valor), r.icono ?? "martillo", Componentes.Variante.Primario, 42f);
        boton.style.marginTop = 10f;
        boton.SetEnabled(imp == null);
        if (imp != null) Tooltip.Asignar(boton, imp);
        detalle.Add(boton);
    }

    void Fabricar(int n)
    {
        if (elegida == null || F == null) return;
        int hechas = F.Fabricar(elegida, n);
        Sonidos.Reproducir(hechas > 0 ? (elegida.categoria == Fabricacion.Categoria.Barco ? Sonidos.Tipo.Exito : Sonidos.Tipo.Fabricar) : Sonidos.Tipo.Error);
        cantidad.Valor = 1;
        // Cambiar de barco cierra la ventana (el barco nuevo se arma y vas al timón)
        if (hechas > 0 && elegida.categoria == Fabricacion.Categoria.Barco && elegida.unaVez && !elegida.nombre.Contains("olores")) { Ventanas.Cerrar(this); return; }
        Actualizar();
        DibujarDetalle(true);
    }

    // ---------- Teclado (cada cuadro, si es la ventana de arriba) ----------
    public override void CadaCuadro()
    {
        Keyboard k = Keyboard.current;
        if (k == null || Teclas.Capturando || Ventanas.Superior != this) return;
        int idx = filas.FindIndex(x => x.receta == elegida);
        if (k.downArrowKey.wasPressedThisFrame && filas.Count > 0) Seleccionar(filas[Mathf.Min(filas.Count - 1, idx + 1)].receta);
        if (k.upArrowKey.wasPressedThisFrame && filas.Count > 0) Seleccionar(filas[Mathf.Max(0, idx - 1)].receta);
        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
            Fabricar(k.shiftKey.isPressed ? 5 : (elegida != null && !elegida.unaVez ? cantidad.Valor : 1));
        if (k.qKey.wasPressedThisFrame) CambiarCategoria(-1);
        if (k.eKey.wasPressedThisFrame) CambiarCategoria(1);
    }

    void CambiarCategoria(int d)
    {
        var claves = new List<int>(botonesCategoria.Keys);
        int k = claves.IndexOf(categoria);
        k = (k + d + claves.Count) % claves.Count;
        Sonidos.Reproducir(Sonidos.Tipo.Clic);
        Elegir(claves[k]);
    }
}
