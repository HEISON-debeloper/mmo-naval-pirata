using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Tu barco (B): plano del barco visto desde arriba con cada porta (llena o vacía), estado y medidas, armamento por lado
// (poner y quitar cañones de la mochila), bodega con traslados, y accesos a las mejoras y al taller de pintura.
public class VentanaBarco : Ventana
{
    static VentanaBarco i;
    public static VentanaBarco I => i ??= new VentanaBarco();

    readonly Componentes.Pestanas pestanas;
    readonly Plano plano;
    readonly Label nombre, medidas, sinBarco;
    readonly VisualElement cuerpo, datos, armas, bodegaIzq, bodegaDer;
    readonly Componentes.BarraEstado casco, carga;
    readonly Dictionary<string, Label> valores = new Dictionary<string, Label>();
    readonly Dictionary<LadoCanon, (Label cuenta, Label recarga, Button mas, Button menos)> filasArmas = new Dictionary<LadoCanon, (Label, Label, Button, Button)>();
    Label barriles; Button mejorarBarriles;

    static Personaje P => Personaje.Instancia;
    static ControlBarco Barco => P != null ? (P.estado != Personaje.Estado.Libre && P.BarcoActual == P.barco ? P.barco : P.barco) : null;
    static bool ABordo => P != null && P.estado != Personaje.Estado.Libre && P.BarcoActual != null && P.BarcoActual == P.barco;

    VentanaBarco() : base("barco", "Tu barco", "barco", 940f, 620f)
    {
        sinBarco = Estilo.Etiqueta("Aún no tienes barco. Fabrica una balsa en la orilla (" + Teclas.Nombre(Accion.Fabricar) + " → Barco).", Estilo.TamTexto, Estilo.TextoTenue);
        sinBarco.style.marginTop = 30f; sinBarco.style.unityTextAlign = TextAnchor.MiddleCenter;
        Contenido.Add(sinBarco);

        cuerpo = Estilo.Fila(14f, Align.Stretch);
        cuerpo.style.flexGrow = 1f; cuerpo.style.flexShrink = 1f;
        Contenido.Add(cuerpo);

        // Izquierda: plano y nombre
        var izq = Estilo.Columna(); izq.style.width = 300f; izq.style.flexShrink = 0f; izq.style.marginRight = 14f;
        nombre = Estilo.Titulo("", Estilo.TamTitulo);
        izq.Add(nombre);
        medidas = Estilo.Pequena(""); medidas.style.marginBottom = 8f;
        izq.Add(medidas);
        plano = new Plano();
        plano.style.flexGrow = 1f; plano.style.minHeight = 300f;
        plano.style.backgroundColor = new Color(0.08f, 0.16f, 0.2f, 0.6f);
        Estilo.Borde(plano, new Color(Estilo.Bronce.r, Estilo.Bronce.g, Estilo.Bronce.b, 0.6f), 1f, Estilo.Radio);
        izq.Add(plano);
        var ley = Estilo.Fila(10f); ley.style.marginTop = 6f; ley.style.flexWrap = Wrap.Wrap;
        ley.Add(Leyenda(Estilo.Oro, "cañón")); ley.Add(Leyenda(new Color(1f, 1f, 1f, 0.35f), "porta vacía")); ley.Add(Leyenda(Estilo.ColHambre, "barriles"));
        izq.Add(ley);
        cuerpo.Add(izq);

        // Derecha: pestañas
        pestanas = new Componentes.Pestanas();
        pestanas.style.flexGrow = 1f; pestanas.style.flexShrink = 1f;
        datos = pestanas.Agregar("Estado", "escudo");
        armas = pestanas.Agregar("Armamento", "canon");
        var pb = pestanas.Agregar("Bodega", "caja");
        cuerpo.Add(pestanas);

        // ---- Estado ----
        casco = new Componentes.BarraEstado("Casco", "escudo", Estilo.ColCasco, 420f, 16f);
        casco.style.marginBottom = 10f;
        datos.Add(casco);
        foreach (var (clave, icono, etiqueta) in new[] {
            ("molde", "barco", "Tipo"), ("filas", "pisos", "Cubiertas de cañones"), ("canones", "canon", "Cañones"), ("velas", "vela", "Velas"),
            ("mejoras", "martillo", "Mejoras"), ("rumbo", "brujula", "Rumbo y velocidad"), ("ancla", "ancla", "Ancla"), ("pintura", "brocha", "Pintura") })
            datos.Add(Dato(clave, icono, etiqueta));
        var acciones = Estilo.Fila(8f); acciones.style.marginTop = 14f; acciones.style.flexWrap = Wrap.Wrap;
        acciones.Add(Componentes.Boton("Mejorar y agrandar", () => { Ventanas.Cerrar(this); VentanaFabricacion.I.IrACategoria(Fabricacion.Categoria.Barco); Ventanas.Abrir(VentanaFabricacion.I); }, "martillo", Componentes.Variante.Primario));
        var bt = Componentes.Boton("Taller de pintura (" + Teclas.Nombre(Accion.Taller) + ")", () => { Ventanas.CerrarTodas(); Taller.Abrir(); }, "brocha");
        bt.style.marginLeft = 8f;
        acciones.Add(bt);
        datos.Add(acciones);

        // ---- Armamento ----
        var notaA = Estilo.Pequena("Los cañones se fabrican en el horno (" + Teclas.Nombre(Accion.Fabricar) + " → Armamento). Aquí se ponen y se quitan: lo que quitas vuelve a la mochila.");
        notaA.style.marginBottom = 8f;
        armas.Add(notaA);
        foreach (LadoCanon l in new[] { LadoCanon.Babor, LadoCanon.Estribor, LadoCanon.Proa })
            armas.Add(FilaArma(l));
        var fb = Componentes.FilaLista(false); fb.pickingMode = PickingMode.Position;
        fb.Add(Iconos.Crear("fuego", 22f, Estilo.ColHambre));
        var cb = Estilo.Columna(); cb.style.flexGrow = 1f; cb.style.marginLeft = 10f;
        cb.Add(Estilo.Etiqueta("Barriles en llamas (popa)", Estilo.TamTexto, Estilo.Texto, Estilo.FuenteNegrita));
        barriles = Estilo.Pequena(""); cb.Add(barriles);
        fb.Add(cb);
        mejorarBarriles = Componentes.Boton("Mejorar", () => { Ventanas.Cerrar(this); VentanaFabricacion.I.IrACategoria(Fabricacion.Categoria.Armamento); Ventanas.Abrir(VentanaFabricacion.I); }, "martillo", Componentes.Variante.Fantasma, 30f);
        fb.Add(mejorarBarriles);
        armas.Add(fb);

        // ---- Bodega ----
        carga = new Componentes.BarraEstado("Carga de la bodega", "caja", Estilo.ColHambre, 520f, 14f);
        carga.style.marginBottom = 8f;
        pb.Add(carga);
        var nb = Estilo.Pequena("Solo a bordo de tu barco. La comida de la bodega se come en el mar con " + Teclas.Nombre(Accion.Comer) + " cuando no llevas nada encima.");
        nb.style.marginBottom = 8f;
        pb.Add(nb);
        var dos = Estilo.Fila(12f, Align.Stretch); dos.style.flexGrow = 1f;
        var sl = Componentes.Lista(); sl.style.flexGrow = 1f; sl.style.flexBasis = 0f;
        var sr = Componentes.Lista(); sr.style.flexGrow = 1f; sr.style.flexBasis = 0f; sr.style.marginLeft = 12f;
        bodegaIzq = Estilo.Columna(); sl.Add(bodegaIzq);
        bodegaDer = Estilo.Columna(); sr.Add(bodegaDer);
        dos.Add(sl); dos.Add(sr);
        pb.Add(dos);
        pestanas.alCambiar = _ => Actualizar();
    }

    protected override float Refrescos => 5f;

    protected override void AlAbrir() => Actualizar();

    static VisualElement Leyenda(Color c, string t)
    {
        var f = Estilo.Fila(4f); f.style.marginRight = 10f;
        var p = new VisualElement(); p.style.width = 10f; p.style.height = 10f; Estilo.Redondear(p, 5f); p.style.backgroundColor = c;
        f.Add(p);
        var l = Estilo.Pequena(t); l.style.marginLeft = 4f; f.Add(l);
        return f;
    }

    VisualElement Dato(string clave, string icono, string etiqueta)
    {
        var f = Estilo.Fila(8f); f.style.marginBottom = 6f;
        f.Add(Iconos.Crear(icono, 18f, Estilo.Oro));
        var e = Estilo.Etiqueta(etiqueta, Estilo.TamTexto, Estilo.TextoTenue, Estilo.FuenteMedia);
        e.style.width = 190f; e.style.marginLeft = 8f;
        f.Add(e);
        var v = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
        v.style.flexGrow = 1f; v.style.flexShrink = 1f;
        f.Add(v);
        valores[clave] = v;
        return f;
    }

    VisualElement FilaArma(LadoCanon l)
    {
        var f = Componentes.FilaLista(false); f.pickingMode = PickingMode.Position;
        f.style.minHeight = 54f;
        f.Add(Iconos.Crear("canon", 22f, Estilo.Oro));
        var col = Estilo.Columna(); col.style.flexGrow = 1f; col.style.marginLeft = 10f;
        string nombre = l == LadoCanon.Babor ? "Babor (izquierda)" : l == LadoCanon.Estribor ? "Estribor (derecha)" : "Proa (al frente)";
        var t = Estilo.Fila(6f);
        t.Add(Estilo.Etiqueta(nombre, Estilo.TamTexto, Estilo.Texto, Estilo.FuenteNegrita));
        if (l == LadoCanon.Babor) { var k = Componentes.TeclaDe(Accion.DispararBabor, 0.8f); k.style.marginLeft = 8f; t.Add(k); }
        if (l == LadoCanon.Estribor) { var k = Componentes.TeclaDe(Accion.DispararEstribor, 0.8f); k.style.marginLeft = 8f; t.Add(k); }
        col.Add(t);
        var rec = Estilo.Pequena(""); col.Add(rec);
        f.Add(col);
        var menos = Componentes.BotonIcono("menos", () => Cambiar(l, -1), "Quitar un cañón (vuelve a la mochila)", 16f, Estilo.Texto);
        var cuenta = Estilo.Etiqueta("", Estilo.TamTitulo * 0.8f, Estilo.Oro, Estilo.FuenteNegrita);
        cuenta.style.width = 80f; cuenta.style.unityTextAlign = TextAnchor.MiddleCenter;
        var mas = Componentes.BotonIcono("mas", () => Cambiar(l, 1), "Poner un cañón de la mochila", 16f, Estilo.Texto);
        f.Add(menos); f.Add(cuenta); f.Add(mas);
        var lleno = Componentes.Boton("Llenar", () => Cambiar(l, 999), null, Componentes.Variante.Fantasma, 28f);
        lleno.style.marginLeft = 8f;
        Tooltip.Asignar(lleno, "Pone todos los cañones que quepan y lleves");
        f.Add(lleno);
        filasArmas[l] = (cuenta, rec, mas, menos);
        return f;
    }

    void Cambiar(LadoCanon l, int d)
    {
        ControlBarco b = Barco;
        if (b == null) return;
        if (!ABordo) { Notificaciones.Alerta("Sube a tu barco para cambiar los cañones"); return; }
        Canones cn = b.GetComponent<Canones>();
        Inventario inv = P.GetComponent<Inventario>();
        if (cn == null || inv == null) return;
        int hechos = 0;
        if (d > 0)
        {
            for (int k = 0; k < d; k++)
            {
                if (!cn.HayRanura(l)) { if (hechos == 0) Notificaciones.Alerta("Ese lado está lleno"); break; }
                if (!ModoCreativo.Activo && !inv.Quitar(Inventario.Canon, 1)) { if (hechos == 0) Notificaciones.Alerta("No llevas cañones: fabrícalos en el horno"); break; }
                cn.Instalar(l); hechos++;
            }
        }
        else if (cn.Quitar(l)) { if (!ModoCreativo.Activo) inv.Fijar(Inventario.Canon, inv.Cantidad(Inventario.Canon) + 1); hechos++; }
        Sonidos.Reproducir(hechos > 0 ? Sonidos.Tipo.Madera : Sonidos.Tipo.Error);
        Actualizar();
    }

    public override void Actualizar()
    {
        ControlBarco b = Barco;
        sinBarco.style.display = b == null ? DisplayStyle.Flex : DisplayStyle.None;
        cuerpo.style.display = b == null ? DisplayStyle.None : DisplayStyle.Flex;
        if (b == null) return;
        Canones cn = b.GetComponent<Canones>();
        VidaBarco v = b.GetComponent<VidaBarco>();
        PiezasBarco pz = b.GetComponent<PiezasBarco>();
        Bodega bd = b.GetComponent<Bodega>();
        Rigidbody rb = b.GetComponent<Rigidbody>();
        Pintura pt = b.GetComponent<Pintura>();
        FormaBarco.Leer(b, out int mo, out int a, out int an, out int al);
        FormaBarco.Medidas(mo, a, an, al, out float L, out float w, out float h);
        nombre.text = b.name;
        medidas.text = L.ToString("0.#") + " m de eslora × " + w.ToString("0.#") + " m de manga × " + h.ToString("0.#") + " m de alto";
        plano.Fijar(b, cn);
        if (v != null) { casco.Fijar(v.vida, v.vidaMaxima, v.Hundido ? "¡Hundiéndose!" : null); casco.Animar(); }

        valores["molde"].text = FormaBarco.Nombre(mo, a, an, al) + Estilo.Col("   alargado " + a + " · ensanchado " + an + " · pisos +" + al, Estilo.TextoTenue);
        valores["filas"].text = FormaBarco.Filas(mo, al).ToString();
        valores["canones"].text = cn != null ? cn.Instalados + " de " + cn.Ranuras() + " portas" : "—";
        valores["velas"].text = (b.EnReversa ? "reversa" : b.NivelVelaActual + " de " + b.nivelesVela) + Estilo.Col("   " + FormaBarco.De(mo).mastiles + " mástil" + (FormaBarco.De(mo).mastiles == 1 ? "" : "es"), Estilo.TextoTenue);
        valores["mejoras"].text = pz != null ? "casco +" + pz.nivelCasco + "  ·  velas +" + pz.nivelVelas : "ninguna";
        float nudos = rb != null ? Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude * 1.944f : 0f;
        valores["rumbo"].text = Mathf.RoundToInt((b.transform.eulerAngles.y + 360f) % 360f).ToString("000") + "°  ·  " + nudos.ToString("0.0") + " nudos";
        valores["ancla"].text = b.Anclado ? Estilo.Col("echada", Estilo.Ambar) : "levada";
        valores["pintura"].text = pt != null && pt.datos != null && pt.AlgoPintado() ? "pintado a tu gusto" + (string.IsNullOrEmpty(pt.datos.mascaron) ? "" : " · mascarón " + pt.datos.mascaron) : "colores originales";

        // Armamento
        Inventario inv = P.GetComponent<Inventario>();
        int llevo = inv != null ? inv.Cantidad(Inventario.Canon) : 0;
        foreach (var kv in filasArmas)
        {
            LadoCanon l = kv.Key; var f = kv.Value;
            int ran = cn != null ? cn.RanurasLado(l) : 0, en = cn != null ? cn.EnLado(l) : 0;
            f.cuenta.text = en + " / " + ran;
            f.recarga.text = ran == 0 ? (l == LadoCanon.Proa ? "Este barco no tiene cañones de proa: agrándalo" : "Este barco no aguanta cañones")
                : "Recarga " + cn.TiempoRecarga(l).ToString("0.0") + " s" + (en > 0 ? "  ·  " + (cn.Recarga(l) <= 0f ? Estilo.Col("listo", Estilo.Bien) : Estilo.Col(cn.Recarga(l).ToString("0.0") + " s", Estilo.Ambar)) : "") + "  ·  llevas " + llevo + " en la mochila";
            f.mas.SetEnabled(ABordo && ran > 0 && en < ran && (llevo > 0 || ModoCreativo.Activo));
            f.menos.SetEnabled(ABordo && en > 0);
        }
        if (cn != null)
        {
            bool hay = cn.RanurasLado(LadoCanon.Popa) > 0;
            barriles.text = !hay ? "La balsa no tiene popa: agrándala" : cn.EnLado(LadoCanon.Popa) == 0 ? "Sin barriles: fabrícalos en Armamento" :
                cn.BarrilesPorTanda + " por tanda  ·  recarga " + Canones.RecargaBarriles.ToString("0") + " s  ·  nivel " + cn.nivelBarriles + " de " + Canones.MaxNivelBarriles;
        }

        if (pestanas.Actual == 2) ActualizarBodega(bd, inv);
    }

    // ---------- Bodega ----------
    string firmaBodega;
    void ActualizarBodega(Bodega bd, Inventario inv)
    {
        if (bd == null && ABordo) bd = P.barco.gameObject.AddComponent<Bodega>();
        if (bd == null || inv == null) { carga.Fijar(0f, 1f, "sin bodega"); carga.Animar(); return; }
        carga.Fijar(bd.Total, bd.capacidad); carga.Animar();
        string firma = ABordo + "|";
        foreach (var kv in inv.Todo) firma += kv.Key + kv.Value;
        firma += "|";
        foreach (var kv in bd.Todo) firma += kv.Key + kv.Value;
        if (firma == firmaBodega) return;
        firmaBodega = firma;
        bodegaIzq.Clear(); bodegaDer.Clear();
        var t1 = Estilo.Etiqueta("MOCHILA", Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita); t1.style.letterSpacing = 1.5f; t1.style.marginBottom = 4f;
        bodegaIzq.Add(t1);
        var t2 = Estilo.Etiqueta("BODEGA  " + bd.Total + " / " + bd.capacidad, Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita); t2.style.letterSpacing = 1.5f; t2.style.marginBottom = 4f;
        bodegaDer.Add(t2);
        foreach (var kv in Ordenar(inv.Todo)) if (kv.Key != Inventario.Oro && kv.Value > 0) bodegaIzq.Add(FilaTraslado(kv.Key, kv.Value, true, bd, inv));
        foreach (var kv in Ordenar(bd.Todo)) if (kv.Value > 0) bodegaDer.Add(FilaTraslado(kv.Key, kv.Value, false, bd, inv));
        if (bodegaDer.childCount == 1) bodegaDer.Add(Estilo.Pequena("Vacía."));
    }

    static List<KeyValuePair<string, int>> Ordenar(IEnumerable<KeyValuePair<string, int>> e)
    {
        var l = new List<KeyValuePair<string, int>>(e);
        l.Sort((x, y) => Objetos.De(x.Key).orden.CompareTo(Objetos.De(y.Key).orden));
        return l;
    }

    VisualElement FilaTraslado(string tipo, int n, bool aBodega, Bodega bd, Inventario inv)
    {
        var info = Objetos.De(tipo);
        var f = Componentes.FilaLista(false); f.pickingMode = PickingMode.Position;
        f.Add(Iconos.Crear(info.icono, 20f, info.color));
        var l = Estilo.Etiqueta(info.nombre, Estilo.TamTextoChico, Estilo.Texto, Estilo.FuenteMedia); l.style.marginLeft = 8f; l.style.flexGrow = 1f;
        f.Add(l);
        var c = Estilo.Etiqueta(n.ToString(), Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteNegrita); c.style.marginRight = 8f;
        f.Add(c);
        void Mover(int k)
        {
            if (!ABordo) { Notificaciones.Alerta("Sube a tu barco para usar la bodega"); return; }
            int m = aBodega ? bd.Guardar(inv, tipo, k) : bd.Sacar(inv, tipo, k);
            if (m > 0) Sonidos.Reproducir(Sonidos.Tipo.Transferir);
            else { Sonidos.Reproducir(Sonidos.Tipo.Error); if (aBodega && bd.Libre == 0) Notificaciones.Alerta("La bodega está llena"); }
            firmaBodega = null;
            Actualizar();
        }
        var b1 = Componentes.Boton(aBodega ? "1 →" : "← 1", () => Mover(1), null, Componentes.Variante.Fantasma, 26f); Estilo.Relleno(b1, 0f, 8f);
        var b10 = Componentes.Boton(aBodega ? "10 →" : "← 10", () => Mover(10), null, Componentes.Variante.Fantasma, 26f); Estilo.Relleno(b10, 0f, 8f); b10.style.marginLeft = 4f;
        var bt = Componentes.Boton(aBodega ? "Todo →" : "← Todo", () => Mover(int.MaxValue), null, Componentes.Variante.Fantasma, 26f); Estilo.Relleno(bt, 0f, 8f); bt.style.marginLeft = 4f;
        f.Add(b1); f.Add(b10); f.Add(bt);
        if (!ABordo) { b1.SetEnabled(false); b10.SetEnabled(false); bt.SetEnabled(false); }
        return f;
    }

    // ---------- Plano del barco visto desde arriba ----------
    class Plano : VisualElement
    {
        ControlBarco barco; Canones canones;
        float L = 10f, w = 3f;
        readonly List<(Vector2 p, int tipo)> puntos = new List<(Vector2, int)>();   // tipo: 0 vacía, 1 cañón, 2 barril, 3 barril vacío
        readonly List<float> mastiles = new List<float>();

        public Plano()
        {
            generateVisualContent += Dibujar;
            pickingMode = PickingMode.Ignore;
        }

        public void Fijar(ControlBarco b, Canones cn)
        {
            barco = b; canones = cn;
            BoxCollider bc = b.GetComponent<BoxCollider>();
            if (bc != null) Cubierta.Medidas(bc, out w, out _, out L);
            puntos.Clear();
            if (cn != null)
                foreach (LadoCanon l in new[] { LadoCanon.Babor, LadoCanon.Estribor, LadoCanon.Proa, LadoCanon.Popa })
                {
                    Transform[] r = cn.Ranuras(l);
                    if (r == null) continue;
                    int en = cn.EnLado(l);
                    for (int k = 0; k < r.Length; k++)
                    {
                        if (r[k] == null) continue;
                        Vector3 lp = b.transform.InverseTransformPoint(r[k].position);
                        int t = l == LadoCanon.Popa ? (k < en ? 2 : 3) : (k < en ? 1 : 0);
                        puntos.Add((new Vector2(lp.x, lp.z), t));
                    }
                }
            FormaBarco.Leer(b, out int mo, out _, out _, out _);
            mastiles.Clear();
            int n = FormaBarco.De(mo).mastiles;
            for (int k = 0; k < n; k++) mastiles.Add(n == 1 ? 0.05f : Mathf.Lerp(0.28f, -0.22f, k / (float)(n - 1)));
            MarkDirtyRepaint();
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            if (barco == null) return;
            var p = mgc.painter2D;
            Rect r = contentRect;
            float margen = 26f;
            // proa arriba: el largo va en vertical
            float esc = Mathf.Min((r.height - margen * 2f) / Mathf.Max(1f, L), (r.width - margen * 2f) / Mathf.Max(1f, w * 1.6f));
            Vector2 c = r.center;
            Vector2 A(float x, float z) => new Vector2(c.x + x * esc, c.y - z * esc);

            // Casco: contorno que se afina hacia la proa (como el casco del juego)
            p.fillColor = new Color(0.55f, 0.38f, 0.22f, 0.95f);
            p.strokeColor = Estilo.BronceClaro; p.lineWidth = 2f;
            p.BeginPath();
            int seg = 40;
            for (int k = 0; k <= seg; k++)
            {
                float t = k / (float)seg;                 // 0 popa → 1 proa
                float z = Mathf.Lerp(-L / 2f, L / 2f, t);
                float ancho = w / 2f * Forma(t);
                Vector2 v = A(ancho, z);
                if (k == 0) p.MoveTo(A(0f, -L / 2f + 0.001f));
                p.LineTo(v);
            }
            for (int k = seg; k >= 0; k--)
            {
                float t = k / (float)seg;
                float z = Mathf.Lerp(-L / 2f, L / 2f, t);
                p.LineTo(A(-w / 2f * Forma(t), z));
            }
            p.ClosePath(); p.Fill(); p.Stroke();

            // Tablones de cubierta
            p.strokeColor = new Color(0.3f, 0.2f, 0.1f, 0.35f); p.lineWidth = 1f;
            for (float x = -w / 2f + 0.5f; x < w / 2f; x += 0.5f)
            {
                p.BeginPath(); p.MoveTo(A(x, -L / 2f * 0.95f)); p.LineTo(A(x, L / 2f * 0.6f)); p.Stroke();
            }
            // Castillo de popa
            float lc = Cubierta.LargoCamarote(L);
            p.fillColor = new Color(0.4f, 0.27f, 0.15f, 0.95f);
            p.BeginPath(); p.MoveTo(A(-w / 2f * 0.85f, -L / 2f + 0.15f)); p.LineTo(A(w / 2f * 0.85f, -L / 2f + 0.15f));
            p.LineTo(A(w / 2f * 0.9f, -L / 2f + lc)); p.LineTo(A(-w / 2f * 0.9f, -L / 2f + lc)); p.ClosePath(); p.Fill();
            // Mástiles
            foreach (float m in mastiles)
            {
                p.fillColor = new Color(0.25f, 0.16f, 0.08f); p.BeginPath(); p.Arc(A(0f, m * L), Mathf.Max(3f, 0.18f * esc), 0f, 360f); p.ClosePath(); p.Fill();
                p.strokeColor = new Color(0.9f, 0.85f, 0.7f, 0.5f); p.lineWidth = 2f;
                p.BeginPath(); p.MoveTo(A(-w * 0.7f, m * L)); p.LineTo(A(w * 0.7f, m * L)); p.Stroke();
            }
            // Portas y cañones
            float rp = Mathf.Clamp(0.32f * esc, 3f, 7f);
            foreach (var pt in puntos)
            {
                Vector2 v = A(pt.p.x, pt.p.y);
                Color col = pt.tipo == 1 ? Estilo.Oro : pt.tipo == 2 ? Estilo.ColHambre : new Color(1f, 1f, 1f, 0.35f);
                p.fillColor = col; p.strokeColor = new Color(0f, 0f, 0f, 0.6f); p.lineWidth = 1f;
                p.BeginPath(); p.Arc(v, rp, 0f, 360f); p.ClosePath();
                if (pt.tipo == 1 || pt.tipo == 2) p.Fill(); else { p.strokeColor = col; p.lineWidth = 1.5f; }
                p.Stroke();
            }
            // Proa
            p.fillColor = Estilo.TextoTenue;
            Vector2 pr = A(0f, L / 2f) + new Vector2(0f, -12f);
            p.BeginPath(); p.MoveTo(pr + new Vector2(0f, -8f)); p.LineTo(pr + new Vector2(6f, 2f)); p.LineTo(pr + new Vector2(-6f, 2f)); p.ClosePath(); p.Fill();
        }

        // Media manga según la posición: llena en el centro, se cierra en la proa y un poco en la popa
        static float Forma(float t)
        {
            if (t > 0.6f) { float u = (t - 0.6f) / 0.4f; return Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)); }
            if (t < 0.12f) return Mathf.Lerp(0.82f, 1f, t / 0.12f);
            return 1f;
        }
    }
}
