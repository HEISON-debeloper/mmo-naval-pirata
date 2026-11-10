using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Mercado (Tab junto a un mercader): pestañas Vender y Comprar. Cada artículo muestra lo que llevas, el precio por unidad
// de esta isla ("barato aquí" / "caro aquí"), una cantidad (con "Máx") y el total antes de confirmar.
// Vender mucho de lo mismo baja el precio (cada lote un 3 %), comprar lo sube; se recupera con el tiempo.
public class VentanaComercio : Ventana
{
    static VentanaComercio i;
    public static VentanaComercio I => i ??= new VentanaComercio();

    readonly Componentes.Pestanas pestanas;
    readonly VisualElement listaVender, listaComprar;
    readonly Label oro, isla, tripEstado, tripNota;
    readonly Button contratar, despedir;
    readonly List<Articulo> articulos = new List<Articulo>();
    string elegirTipo; bool elegirVender;

    class Articulo
    {
        public string tipo; public bool vender;
        public VisualElement raiz;
        public Label tiene, unidad, etiqueta, total;
        public Componentes.Contador cantidad;
        public Button boton;
    }

    static Personaje P => Personaje.Instancia;
    static Inventario Inv => P != null ? P.GetComponent<Inventario>() : null;
    static Mercader M => P != null ? Mercader.Cercano(P.transform.position) : null;

    VentanaComercio() : base("comercio", "Mercado", "mercader", 820f, 600f)
    {
        var cab = Estilo.Fila(8f); cab.style.marginBottom = 8f;
        isla = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
        isla.style.flexGrow = 1f;
        cab.Add(isla);
        cab.Add(Iconos.Crear("moneda", 20f, Estilo.Oro));
        oro = Estilo.Etiqueta("0", Estilo.TamTitulo * 0.85f, Estilo.Oro, Estilo.FuenteNegrita); oro.style.marginLeft = 6f;
        cab.Add(oro);
        Contenido.Add(cab);
        var nota = Estilo.Pequena("Cada isla paga distinto: lleva lo barato de una a donde es caro. Vender mucho de lo mismo baja el precio un rato.");
        nota.style.marginBottom = 8f;
        Contenido.Add(nota);

        pestanas = new Componentes.Pestanas();
        var pv = pestanas.Agregar("Vender", "balanza");
        var pc = pestanas.Agregar("Comprar", "moneda");
        var pt = pestanas.Agregar("Tripulación", "tripulacion");
        Contenido.Add(pestanas);
        var sv = Componentes.Lista(); pv.Add(sv);
        listaVender = Estilo.Columna(); sv.Add(listaVender);
        var sc = Componentes.Lista(); pc.Add(sc);
        listaComprar = Estilo.Columna(); sc.Add(listaComprar);

        // Tripulación: contratar y despedir marineros
        var ct = Estilo.Columna(); ct.style.paddingTop = 8f; pt.Add(ct);
        tripEstado = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Texto, Estilo.FuenteNegrita);
        ct.Add(tripEstado);
        tripNota = Estilo.Pequena(""); tripNota.style.marginTop = 6f; tripNota.style.whiteSpace = WhiteSpace.Normal;
        ct.Add(tripNota);
        var bt = Estilo.Fila(10f); bt.style.marginTop = 12f;
        contratar = Componentes.Boton("Contratar (" + Tripulacion.PrecioContrato + " oro)", () =>
        {
            Tripulacion tr = Tripulacion.Mia;
            if (tr == null) return;
            string no = tr.PuedeContratar();
            if (no != null) { Notificaciones.Alerta(no); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            tr.Contratar();
            Notificaciones.Mostrar("Contrataste un marinero (" + tr.marineros + "/" + tr.Maximo + ")", Notificaciones.Tipo.Comercio, 4f, "moneda");
            Actualizar();
        }, "moneda", Componentes.Variante.Primario, 36f);
        despedir = Componentes.Boton("Despedir uno", () =>
        {
            Tripulacion tr = Tripulacion.Mia;
            if (tr != null && tr.Despedir()) Notificaciones.Info("Despediste un marinero (" + tr.marineros + ")");
            Actualizar();
        }, "cerrar", Componentes.Variante.Fantasma, 36f);
        Tooltip.Asignar(contratar, () => Tripulacion.Mia?.PuedeContratar() ?? "Contratar un marinero");
        bt.Add(contratar); bt.Add(despedir);
        ct.Add(bt);

        foreach (string t in Mercader.SeVende) Crear(t, true, listaVender);
        foreach (string t in Mercader.SeCompra) Crear(t, false, listaComprar);
    }

    protected override float Refrescos => 4f;

    // Abrir directamente con un artículo (desde la mochila)
    public void Elegir(string tipo, bool vender) { elegirTipo = tipo; elegirVender = vender; }

    protected override void AlAbrir()
    {
        Mercader m = M;
        pestanas.Elegir(elegirTipo != null && !elegirVender ? 1 : 0, false);
        foreach (var a in articulos) { a.cantidad.Valor = 1; a.raiz.EnableInClassList("fila-lista--elegida", a.tipo == elegirTipo && a.vender == elegirVender); }
        elegirTipo = null;
        Actualizar();
        if (m == null) Notificaciones.Alerta("No hay un mercader cerca");
    }

    public override void Actualizar()
    {
        Mercader m = M; Inventario inv = Inv;
        if (m == null || inv == null) { if (Abierta) Ventanas.Cerrar(this); return; }
        Titulo = "Mercado de " + m.isla;
        isla.text = "Mercader de " + Estilo.Col(m.isla, Estilo.Oro);
        int o = inv.Cantidad(Inventario.Oro);
        oro.text = o.ToString("N0");
        foreach (var a in articulos) Pintar(a, m, inv, o);
        PintarTripulacion();
    }

    void PintarTripulacion()
    {
        Tripulacion tr = Tripulacion.Mia;
        if (tr == null) return;
        tripEstado.text = tr.Estado;
        tripNota.text = "Cada marinero cuesta " + Tripulacion.PrecioContrato + " oro al contratarlo y cobra " + Tripulacion.Sueldo +
            " de oro al día. Comen 1 coco o fruta por cada 2 cada 6 horas (de la bodega o de tu mochila).\n" +
            "Con un marinero por cada 2 cañones recargas hasta un 30 % más rápido; con el barco lleno, las velas empujan un 10 % más; y pelean contra los piratas que te abordan.\n" +
            "Sin paga o sin comida baja la moral, y con la moral muy baja se van en el puerto." +
            (tr.marineros > 0 ? "\nAhora: recarga ×" + tr.FactorRecarga.ToString("0.00") + " · velas ×" + tr.FactorVelas.ToString("0.00") : "");
        string no = tr.PuedeContratar();
        contratar.SetEnabled(no == null);
        despedir.SetEnabled(tr.marineros > 0);
    }

    void Crear(string tipo, bool vender, VisualElement donde)
    {
        var info = Objetos.De(tipo);
        var a = new Articulo { tipo = tipo, vender = vender };
        var f = Componentes.FilaLista();
        f.style.minHeight = 56f;
        f.Add(Iconos.Crear(info.icono, 30f, info.color));
        var col = Estilo.Columna(); col.style.width = 200f; col.style.marginLeft = 10f; col.pickingMode = PickingMode.Ignore;
        var n = Estilo.Etiqueta(info.nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteNegrita); n.pickingMode = PickingMode.Ignore;
        col.Add(n);
        a.tiene = Estilo.Pequena(""); col.Add(a.tiene);
        f.Add(col);
        var colP = Estilo.Columna(); colP.style.width = 120f; colP.pickingMode = PickingMode.Ignore;
        a.unidad = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteMedia); colP.Add(a.unidad);
        a.etiqueta = Estilo.Pequena(""); colP.Add(a.etiqueta);
        f.Add(colP);
        a.cantidad = new Componentes.Contador(1, 1, 999);
        a.cantidad.alCambiar = _ => { Mercader m = M; Inventario inv = Inv; if (m != null && inv != null) Pintar(a, m, inv, inv.Cantidad(Inventario.Oro)); };
        f.Add(a.cantidad);
        a.total = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Oro, Estilo.FuenteNegrita);
        a.total.style.width = 90f; a.total.style.unityTextAlign = TextAnchor.MiddleRight; a.total.style.marginLeft = 10f;
        f.Add(a.total);
        a.boton = Componentes.Boton(vender ? "Vender" : "Comprar", () => Hacer(a), vender ? "balanza" : "moneda", Componentes.Variante.Primario, 34f);
        a.boton.style.marginLeft = 10f; a.boton.style.width = 120f;
        f.Add(a.boton);
        Tooltip.Asignar(col, Objetos.Descripcion(tipo, 0));
        a.raiz = f;
        donde.Add(f);
        articulos.Add(a);
    }

    void Pintar(Articulo a, Mercader m, Inventario inv, int oroTengo)
    {
        int tengo = inv.Cantidad(a.tipo);
        a.tiene.text = "Llevas " + tengo;
        a.unidad.text = m.Unidad(a.tipo, a.vender).ToString("0.#") + " oro c/u";
        string e = m.Etiqueta(a.tipo);
        a.etiqueta.text = e == "" ? "precio normal" : e;
        a.etiqueta.style.color = e == "barato aquí" ? Estilo.Bien : e == "caro aquí" ? Estilo.Ambar : Estilo.TextoTenue;
        int max = a.vender ? tengo : (ModoCreativo.Activo ? 999 : m.MaxCompra(a.tipo, oroTengo));
        if (!a.vender && !Inventario.SinPeso(a.tipo)) max = Mathf.Min(max, inv.Espacio);   // lo que quepa en la mochila
        a.cantidad.Maximo = Mathf.Max(1, max);
        int n = a.cantidad.Valor;
        int total = m.Total(a.tipo, n, a.vender, false);
        a.total.text = (a.vender ? "+" : "−") + total + " oro";
        a.total.style.color = a.vender ? Estilo.Bien : (max >= n || ModoCreativo.Activo ? Estilo.Oro : Estilo.Mal);
        bool puede = max >= 1 && n <= max;
        a.boton.SetEnabled(puede);
        Componentes.CambiarTexto(a.boton, a.vender ? "Vender" : "Comprar");
        a.raiz.EnableInClassList("fila-lista--imposible", !puede);
    }

    void Hacer(Articulo a)
    {
        Mercader m = M; Inventario inv = Inv;
        if (m == null || inv == null) return;
        int n = a.cantidad.Valor;
        if (a.vender)
        {
            n = Mathf.Min(n, inv.Cantidad(a.tipo));
            if (n <= 0) { Notificaciones.Alerta("No llevas " + Objetos.Nombre(a.tipo).ToLower()); return; }
            int ganas = m.Total(a.tipo, n, true, true);
            inv.Quitar(a.tipo, n);
            inv.Fijar(Inventario.Oro, inv.Cantidad(Inventario.Oro) + ganas);
            Notificaciones.Mostrar("Vendiste " + n + " " + Objetos.Plural(a.tipo, n) + ": +" + ganas + " oro", Notificaciones.Tipo.Comercio, 4f, "moneda");
        }
        else
        {
            if (!Inventario.SinPeso(a.tipo) && n > inv.Espacio) { Notificaciones.Alerta("No te cabe en la mochila (" + inv.Carga + " / " + Inventario.Capacidad + ")"); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            int cuesta = m.Total(a.tipo, n, false, false);
            if (!ModoCreativo.Activo && inv.Cantidad(Inventario.Oro) < cuesta) { Notificaciones.Alerta("Te falta oro: cuesta " + cuesta); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            m.Total(a.tipo, n, false, true);
            if (!ModoCreativo.Activo) inv.Quitar(Inventario.Oro, cuesta);
            inv.Fijar(a.tipo, inv.Cantidad(a.tipo) + n);
            Notificaciones.Mostrar("Compraste " + n + " " + Objetos.Plural(a.tipo, n) + (ModoCreativo.Activo ? " (gratis, creativo)" : ": −" + cuesta + " oro"), Notificaciones.Tipo.Comercio, 4f, Objetos.De(a.tipo).icono);
        }
        a.cantidad.Valor = 1;
        Actualizar();
    }
}
