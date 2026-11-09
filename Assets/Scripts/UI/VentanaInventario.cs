using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Mochila (I): casillas con icono y cantidad, filtros por categoría, búsqueda y orden; a la derecha la ficha del objeto elegido
// con lo que se puede hacer con él (comer, guardar en la bodega, vender, ver en qué recetas se usa). Si vas en tu barco, abajo
// aparece la bodega: con un objeto elegido, otro clic lo pasa al otro lado (Mayús+clic pasa 10, doble clic pasa todo).
public class VentanaInventario : Ventana
{
    static VentanaInventario i;
    public static VentanaInventario I => i ??= new VentanaInventario();

    readonly VisualElement rejilla, rejillaBodega, seccionBodega, fichas, ficha;
    readonly Label vacio, pesoOro, tituloBodega, vacioBodega;
    readonly Componentes.CampoBusqueda buscar;
    readonly Componentes.Desplegable orden;
    readonly List<Componentes.CasillaObjeto> casillas = new List<Componentes.CasillaObjeto>();
    readonly List<Componentes.CasillaObjeto> casillasBodega = new List<Componentes.CasillaObjeto>();
    int filtro = -1;           // -1 = todo; si no, Objetos.Categoria
    string elegido;
    bool elegidoEnBodega;
    readonly Componentes.Contador cuantos;

    static Inventario Inv => Personaje.Instancia != null ? Personaje.Instancia.GetComponent<Inventario>() : null;

    public static Bodega BodegaCerca()
    {
        Personaje p = Personaje.Instancia;
        if (p == null || p.estado == Personaje.Estado.Libre || p.BarcoActual == null || p.BarcoActual != p.barco) return null;
        Bodega b = p.BarcoActual.GetComponent<Bodega>();
        if (b == null) b = p.BarcoActual.gameObject.AddComponent<Bodega>();
        return b;
    }

    VentanaInventario() : base("inventario", "Mochila", "caja", 880f, 600f)
    {
        // Barra superior: búsqueda, orden y oro
        var barra = Estilo.Fila(8f);
        barra.style.marginBottom = 8f;
        buscar = new Componentes.CampoBusqueda("Buscar en la mochila…", 240f);
        buscar.alCambiar = _ => Refrescar();
        barra.Add(buscar);
        barra.Add(Estilo.Hueco(0f, 0f, true));
        orden = new Componentes.Desplegable("Ordenar", new List<string> { "Por nombre", "Por cantidad", "Por categoría" }, Preferencias.OrdenInventario);
        orden.style.width = 290f; orden.style.marginBottom = 0f;
        orden.alCambiar = k => { Preferencias.OrdenInventario = k; Preferencias.Guardar(); Refrescar(); };
        barra.Add(orden);
        var oro = Estilo.Fila(4f); oro.style.marginLeft = 14f;
        oro.Add(Iconos.Crear("moneda", 20f, Estilo.Oro));
        pesoOro = Estilo.Etiqueta("0", Estilo.TamTitulo * 0.85f, Estilo.Oro, Estilo.FuenteNegrita); pesoOro.style.marginLeft = 6f;
        oro.Add(pesoOro);
        Tooltip.Asignar(oro, "Oro: se gana vendiendo al mercader y con el botín");
        barra.Add(oro);
        Contenido.Add(barra);

        // Fichas de categoría
        fichas = Estilo.Fila(4f); fichas.style.flexWrap = Wrap.Wrap; fichas.style.marginBottom = 8f;
        Contenido.Add(fichas);

        var cuerpo = Estilo.Fila(12f, Align.Stretch);
        cuerpo.style.flexGrow = 1f; cuerpo.style.flexShrink = 1f;
        Contenido.Add(cuerpo);

        var izq = Estilo.Columna(); izq.style.flexGrow = 1f; izq.style.flexShrink = 1f; izq.style.marginRight = 12f;
        cuerpo.Add(izq);
        var lista = Componentes.Lista();
        izq.Add(lista);
        rejilla = Estilo.Fila(0f); rejilla.style.flexWrap = Wrap.Wrap; rejilla.style.alignItems = Align.FlexStart;
        lista.Add(rejilla);
        vacio = Estilo.Pequena("");
        vacio.style.marginTop = 10f;
        lista.Add(vacio);

        // Bodega (solo a bordo de tu barco)
        seccionBodega = Estilo.Columna(); seccionBodega.style.marginTop = 12f;
        seccionBodega.Add(Estilo.Separador());
        var cabB = Estilo.Fila(6f);
        cabB.Add(Iconos.Crear("barco", 16f, Estilo.Oro));
        tituloBodega = Estilo.Etiqueta("Bodega del barco", Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteNegrita);
        tituloBodega.style.marginLeft = 6f; tituloBodega.style.flexGrow = 1f;
        cabB.Add(tituloBodega);
        cabB.Add(Componentes.Boton("Guardar comida", () => MoverComida(true), "flecha_abajo", Componentes.Variante.Fantasma, 28f));
        var sc = Componentes.Boton("Sacar comida", () => MoverComida(false), "flecha_arriba", Componentes.Variante.Fantasma, 28f); sc.style.marginLeft = 6f;
        cabB.Add(sc);
        seccionBodega.Add(cabB);
        seccionBodega.Add(Estilo.Pequena("Elige un objeto y vuelve a hacer clic para pasarlo al otro lado · Mayús+clic: 10 · doble clic: todo"));
        rejillaBodega = Estilo.Fila(0f); rejillaBodega.style.flexWrap = Wrap.Wrap; rejillaBodega.style.marginTop = 4f;
        seccionBodega.Add(rejillaBodega);
        vacioBodega = Estilo.Pequena("La bodega está vacía.");
        seccionBodega.Add(vacioBodega);
        lista.Add(seccionBodega);

        // Ficha del objeto
        ficha = Componentes.Tarjeta(null, null, 0.6f);
        ficha.style.width = 270f; ficha.style.flexShrink = 0f;
        cuerpo.Add(ficha);
        cuantos = new Componentes.Contador(1, 1, 1);
    }

    protected override float Refrescos => 4f;

    protected override void AlAbrir()
    {
        buscar.Limpiar();
        ConstruirFichas();
        fichaDe = "\0";
        Refrescar();
    }

    public override void Actualizar() => Refrescar();

    void ConstruirFichas()
    {
        fichas.Clear();
        fichas.Add(Componentes.Ficha("Todo", () => { filtro = -1; ConstruirFichas(); Refrescar(); }, "caja", filtro == -1));
        foreach (Objetos.Categoria c in System.Enum.GetValues(typeof(Objetos.Categoria)))
        {
            if (c == Objetos.Categoria.Valor) continue;
            var cc = c;
            fichas.Add(Componentes.Ficha(Objetos.NombreCategoria(c), () => { filtro = (int)cc; ConstruirFichas(); Refrescar(); }, Objetos.IconoCategoria(c), filtro == (int)c));
        }
    }

    readonly List<KeyValuePair<string, int>> cosas = new List<KeyValuePair<string, int>>();

    void Refrescar()
    {
        Inventario inv = Inv;
        if (inv == null) return;
        pesoOro.text = inv.Cantidad(Inventario.Oro).ToString("N0");
        string q = buscar.Texto.Trim().ToLowerInvariant();

        cosas.Clear();
        foreach (var kv in inv.Todo)
        {
            if (kv.Value <= 0 || kv.Key == Inventario.Oro) continue;
            var info = Objetos.De(kv.Key);
            if (filtro >= 0 && (int)info.categoria != filtro) continue;
            if (q != "" && !info.nombre.ToLowerInvariant().Contains(q) && !info.descripcion.ToLowerInvariant().Contains(q)) continue;
            cosas.Add(kv);
        }
        switch (Preferencias.OrdenInventario)
        {
            case 1: cosas.Sort((a, b) => b.Value.CompareTo(a.Value)); break;
            case 2: cosas.Sort((a, b) => Objetos.De(a.Key).orden.CompareTo(Objetos.De(b.Key).orden)); break;
            default: cosas.Sort((a, b) => string.Compare(Objetos.Nombre(a.Key), Objetos.Nombre(b.Key), System.StringComparison.CurrentCulture)); break;
        }
        Llenar(rejilla, casillas, cosas, false);
        vacio.text = q != "" || filtro >= 0 ? "Nada coincide con el filtro." : "La mochila está vacía. Recoge cosas en la isla con " + Teclas.Nombre(Accion.Usar) + ", o con el hacha y el pico.";
        vacio.style.display = cosas.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        Bodega bd = BodegaCerca();
        seccionBodega.style.display = bd != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (bd != null)
        {
            tituloBodega.text = "Bodega del barco  " + bd.Total + " / " + bd.capacidad;
            cosas.Clear();
            foreach (var kv in bd.Todo) if (kv.Value > 0) cosas.Add(kv);
            cosas.Sort((a, b) => Objetos.De(a.Key).orden.CompareTo(Objetos.De(b.Key).orden));
            Llenar(rejillaBodega, casillasBodega, cosas, true);
            vacioBodega.style.display = cosas.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (elegido != null && (elegidoEnBodega ? (bd == null || bd.Cantidad(elegido) <= 0) : inv.Cantidad(elegido) <= 0)) elegido = null;
        DibujarFicha();
    }

    void Llenar(VisualElement donde, List<Componentes.CasillaObjeto> lista, List<KeyValuePair<string, int>> datos, bool bodega)
    {
        while (lista.Count < datos.Count)
        {
            var c = new Componentes.CasillaObjeto(68f, true);
            c.alPulsar = cs => AlPulsar(cs, bodega, 1);
            c.alDoblePulsar = cs => AlPulsar(cs, bodega, int.MaxValue);
            c.alPulsarDerecho = cs => { elegido = cs.Tipo; elegidoEnBodega = bodega; Refrescar(); };
            lista.Add(c); donde.Add(c);
        }
        for (int k = 0; k < lista.Count; k++)
        {
            bool ver = k < datos.Count;
            lista[k].style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
            if (!ver) continue;
            lista[k].Fijar(datos[k].Key, datos[k].Value);
            lista[k].Elegida(elegido == datos[k].Key && elegidoEnBodega == bodega);
        }
    }

    void AlPulsar(Componentes.CasillaObjeto c, bool bodega, int n)
    {
        Bodega bd = BodegaCerca();
        var kb = UnityEngine.InputSystem.Keyboard.current;
        bool mayus = kb != null && kb.shiftKey.isPressed;
        // Con la bodega a mano: doble clic, Mayús o un segundo clic sobre lo elegido lo pasa al otro lado
        if (bd != null && (n > 1 || mayus || (elegido == c.Tipo && elegidoEnBodega == bodega)))
        {
            int cuanto = n == int.MaxValue ? c.Cantidad : mayus ? 10 : 1;
            Mover(c.Tipo, cuanto, !bodega);
            return;
        }
        elegido = c.Tipo; elegidoEnBodega = bodega;
        Refrescar();
    }

    void Mover(string tipo, int n, bool aBodega)
    {
        Bodega bd = BodegaCerca(); Inventario inv = Inv;
        Debug.Log("[UI] Mover " + tipo + " n=" + n + " aBodega=" + aBodega + " bd=" + (bd != null));
        if (bd == null || inv == null || tipo == null) return;
        int movidos = aBodega ? bd.Guardar(inv, tipo, n) : bd.Sacar(inv, tipo, n);
        if (movidos > 0) Sonidos.Reproducir(Sonidos.Tipo.Transferir);
        else { Sonidos.Reproducir(Sonidos.Tipo.Error); if (aBodega && bd.Libre == 0) Notificaciones.Alerta("La bodega está llena"); }
        Refrescar();
    }

    void MoverComida(bool aBodega)
    {
        Bodega bd = BodegaCerca(); Inventario inv = Inv;
        if (bd == null || inv == null) return;
        int n = 0;
        foreach (string t in new[] { Inventario.Coco, Inventario.Fruta })
            n += aBodega ? bd.Guardar(inv, t, int.MaxValue) : bd.Sacar(inv, t, int.MaxValue);
        if (n > 0) { Sonidos.Reproducir(Sonidos.Tipo.Transferir); Notificaciones.Info((aBodega ? "Guardaste " : "Sacaste ") + n + " de comida"); }
        else Notificaciones.Alerta(aBodega ? (bd.Libre == 0 ? "La bodega está llena" : "No llevas comida") : "La bodega no tiene comida");
        Refrescar();
    }

    string fichaDe = "\0"; bool fichaBodega; int fichaCantidad = -1; bool fichaConBodega;

    void DibujarFicha()
    {
        Inventario inv = Inv;
        Bodega bd = BodegaCerca();
        int cant = elegido == null ? 0 : elegidoEnBodega ? (bd != null ? bd.Cantidad(elegido) : 0) : inv.Cantidad(elegido);
        if (fichaDe == elegido && fichaBodega == elegidoEnBodega && fichaCantidad == cant && fichaConBodega == (bd != null)) return;
        fichaDe = elegido; fichaBodega = elegidoEnBodega; fichaCantidad = cant; fichaConBodega = bd != null;
        Debug.Log("[UI] ficha rehecha: " + elegido + " " + cant + " frame " + Time.frameCount);
        ficha.Clear();
        if (elegido == null)
        {
            var t = Estilo.Etiqueta("Elige un objeto para ver qué es y qué puedes hacer con él.", Estilo.TamTexto, Estilo.TextoTenue);
            t.style.marginTop = 20f; t.style.unityTextAlign = TextAnchor.MiddleCenter;
            ficha.Add(t);
            if (bd != null) { var b = Estilo.Pequena("A bordo de tu barco: elige un objeto y haz clic otra vez (o doble clic) para pasarlo a la bodega y de vuelta."); b.style.marginTop = 12f; ficha.Add(b); }
            return;
        }
        var info = Objetos.De(elegido);
        var marco = new VisualElement();
        marco.style.height = 110f; marco.style.alignItems = Align.Center; marco.style.justifyContent = Justify.Center;
        marco.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f); Estilo.Redondear(marco, Estilo.Radio); marco.style.marginBottom = 8f;
        marco.Add(Iconos.Crear(info.icono, 64f, info.color));
        ficha.Add(marco);
        ficha.Add(Estilo.Titulo(info.nombre, Estilo.TamTitulo));
        var sub = Estilo.Pequena(Objetos.NombreCategoria(info.categoria) + "  ·  " + (elegidoEnBodega ? "en la bodega: " : "llevas ") + cant);
        sub.style.marginBottom = 6f;
        ficha.Add(sub);
        ficha.Add(Estilo.Etiqueta(info.descripcion, Estilo.TamTextoChico, Estilo.Texto));
        if (!string.IsNullOrEmpty(info.origen)) { var o = Estilo.Pequena("Se consigue: " + info.origen); o.style.marginTop = 6f; ficha.Add(o); }
        ficha.Add(Estilo.Separador());

        var acciones = Estilo.Columna();
        Personaje p = Personaje.Instancia;
        if (!elegidoEnBodega && (elegido == Inventario.Coco || elegido == Inventario.Fruta))
            acciones.Add(Boton("Comer  (" + Teclas.Nombre(global::Accion.Comer) + ")", "hambre", () => { p.Comer(); Refrescar(); }, Componentes.Variante.Primario));
        if (bd != null)
        {
            cuantos.Maximo = Mathf.Max(1, cant);
            cuantos.Valor = Mathf.Clamp(cuantos.Valor, 1, Mathf.Max(1, cant));
            var fila = Estilo.Fila(6f); fila.style.marginBottom = 6f;
            var et = Estilo.Pequena("Cantidad"); et.style.marginRight = 8f; fila.Add(et);
            fila.Add(cuantos);
            acciones.Add(fila);
            string tipo = elegido; bool aB = !elegidoEnBodega;
            acciones.Add(Boton(elegidoEnBodega ? "Sacar de la bodega" : "Guardar en la bodega", elegidoEnBodega ? "flecha_arriba" : "flecha_abajo",
                () => Mover(tipo, cuantos.Valor, aB)));
        }
        if (!elegidoEnBodega && Mercader.Cercano(p.transform.position) != null && Mercader.PrecioBase.ContainsKey(elegido))
        {
            string tipo = elegido;
            acciones.Add(Boton("Vender al mercader", "balanza", () => { Ventanas.Cerrar(this); VentanaComercio.I.Elegir(tipo, true); Ventanas.Abrir(VentanaComercio.I); }));
        }
        // Recetas que usan este objeto
        var usos = new List<string>();
        Fabricacion f = Fabricacion.I;
        if (f != null) foreach (var r in f.Todas) { if (r.categoria == Fabricacion.Categoria.Creativo) continue; if (r.costo().ContainsKey(elegido)) usos.Add(r.nombre); }
        if (usos.Count > 0)
        {
            var u = Estilo.Pequena("Se usa en: " + string.Join(", ", usos.GetRange(0, Mathf.Min(6, usos.Count))) + (usos.Count > 6 ? "…" : ""));
            u.style.marginTop = 6f; u.style.marginBottom = 6f;
            acciones.Add(u);
            string nombre = info.nombre;
            acciones.Add(Boton("Ver recetas", "martillo", () => { Ventanas.Cerrar(this); VentanaFabricacion.I.Buscar(nombre); Ventanas.Abrir(VentanaFabricacion.I); }, Componentes.Variante.Fantasma));
        }
        ficha.Add(acciones);
    }

    static Button Boton(string texto, string icono, System.Action a, Componentes.Variante v = Componentes.Variante.Normal)
    {
        var b = Componentes.Boton(texto, a, icono, v, 34f);
        b.style.marginBottom = 6f;
        return b;
    }
}
