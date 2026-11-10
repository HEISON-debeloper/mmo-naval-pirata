using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Carta de navegación (M): pergamino con el mar, las islas descubiertas (con mercader en dorado), tu barco, tú, la rosa de los
// vientos, la cuadrícula y la escala. Rueda o +/- zoom (se recuerda), arrastrar mueve, doble clic pone una marca con nombre,
// clic derecho sobre una marca la quita. "Centrar" vuelve a ti. Sin enemigos: es una carta, no un radar.
public class VentanaMapa : Ventana
{
    static VentanaMapa i;
    public static VentanaMapa I => i ??= new VentanaMapa();

    readonly Carta carta;
    readonly Label info, escala, islaCerca;
    readonly Componentes.CampoBusqueda nombreIsla;
    readonly Button botonNombrar;
    readonly VisualElement etiquetas;
    readonly List<Label> poolNombres = new List<Label>();

    VentanaMapa() : base("mapa", "Carta de navegación", "mapa", 900f, 700f)
    {
        var barra = Estilo.Fila(8f); barra.style.marginBottom = 8f;
        info = Estilo.Pequena(""); info.style.flexGrow = 1f;
        barra.Add(info);
        barra.Add(Componentes.Boton("Centrar", () => carta.Centrar(), "objetivo", Componentes.Variante.Fantasma, 30f));
        var bz1 = Componentes.BotonIcono("mas", () => carta.Zoom(1f, null), "Acercar (rueda, +)", 16f, Estilo.Texto); bz1.style.marginLeft = 6f;
        var bz2 = Componentes.BotonIcono("menos", () => carta.Zoom(-1f, null), "Alejar (rueda, −)", 16f, Estilo.Texto);
        barra.Add(bz1); barra.Add(bz2);
        Contenido.Add(barra);

        // Nombrar la isla descubierta más cercana
        var fn = Estilo.Fila(8f); fn.style.marginBottom = 8f;
        nombreIsla = new Componentes.CampoBusqueda("Nombre nuevo…", 240f);
        fn.Add(nombreIsla);
        botonNombrar = Componentes.Boton("Nombrar isla", () =>
        {
            Navegacion nav = Navegacion.Instancia; Personaje p = Personaje.Instancia;
            if (nav == null || p == null) return;
            string error = nav.Renombrar(p.transform.position, nombreIsla.Texto);
            if (error != null) { Notificaciones.Alerta(error); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            Sonidos.Reproducir(Sonidos.Tipo.Descubrir, 0.7f);
            nombreIsla.Limpiar();
            Actualizar();
        }, "pergamino", Componentes.Variante.Fantasma, 30f);
        fn.Add(botonNombrar);
        islaCerca = Estilo.Pequena(""); islaCerca.style.flexGrow = 1f;
        fn.Add(islaCerca);
        Contenido.Add(fn);

        carta = new Carta(this);
        carta.style.flexGrow = 1f;
        Estilo.PanelPergamino(carta);
        carta.style.overflow = Overflow.Hidden;
        Contenido.Add(carta);
        etiquetas = new VisualElement { pickingMode = PickingMode.Ignore };
        etiquetas.style.position = Position.Absolute; etiquetas.style.left = etiquetas.style.right = etiquetas.style.top = etiquetas.style.bottom = 0f;
        carta.Add(etiquetas);
        escala = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Tinta, Estilo.FuenteNegrita);
        escala.style.position = Position.Absolute; escala.style.left = 14f; escala.style.bottom = 22f;
        escala.pickingMode = PickingMode.Ignore;
        carta.Add(escala);

        var ley = Estilo.Fila(14f); ley.style.marginTop = 8f; ley.style.flexWrap = Wrap.Wrap;
        ley.Add(Leyenda("flecha_arriba", Estilo.Tinta, "tú"));
        ley.Add(Leyenda("barco", new Color(0.55f, 0.32f, 0.08f), "tu barco"));
        ley.Add(Leyenda("isla", new Color(0.3f, 0.5f, 0.25f), "isla descubierta"));
        ley.Add(Leyenda("moneda", new Color(0.7f, 0.5f, 0.1f), "mercader"));
        ley.Add(Leyenda("marcador", new Color(0.65f, 0.15f, 0.12f), "tu marca"));
        ley.Add(Leyenda("cerrar", new Color(0.32f, 0.24f, 0.18f), "pecio"));
        ley.Add(Estilo.Hueco(0f, 0f, true));
        ley.Add(Estilo.Pequena("Rueda: zoom · Arrastrar: mover · Doble clic: marcar · Clic der. en una marca: quitarla"));
        Contenido.Add(ley);
    }

    static VisualElement Leyenda(string icono, Color c, string t)
    {
        var f = Estilo.Fila(4f);
        var fondo = new VisualElement(); fondo.style.backgroundColor = Estilo.Pergamino; Estilo.Redondear(fondo, 4f); Estilo.Relleno(fondo, 2f);
        fondo.Add(Iconos.Crear(icono, 14f, c));
        f.Add(fondo);
        var l = Estilo.Pequena(t); l.style.marginLeft = 4f; f.Add(l);
        return f;
    }

    protected override float Refrescos => 10f;

    protected override void AlAbrir() { carta.Centrar(); Actualizar(); }

    public override void Actualizar()
    {
        Navegacion nav = Navegacion.Instancia;
        info.text = nav != null ? "Islas descubiertas: " + nav.CantidadDescubiertas + "   ·   Marcas: " + nav.Marcas.Count : "";
        Personaje pj = Personaje.Instancia;
        string idCerca = nav != null && pj != null ? nav.IslaParaNombrar(pj.transform.position, out Navegacion.Conocida cc) : null;
        bool yaTiene = idCerca != null && nav.YaNombrada(idCerca);
        islaCerca.text = idCerca == null ? "Acércate a una isla descubierta para ponerle nombre (es para siempre)"
            : yaTiene ? "Estás junto a " + Estilo.Col(nav.Descubierta(idCerca), Estilo.Oro) + " (ya le pusiste nombre)"
            : "Estás junto a " + Estilo.Col(nav.Descubierta(idCerca), Estilo.Oro) + ": ponle nombre (es para siempre)";
        botonNombrar.SetEnabled(idCerca != null && !yaTiene);
        carta.MarkDirtyRepaint();
        Nombres();
    }

    public override void CadaCuadro()
    {
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k == null || Teclas.Capturando || Ventanas.Superior != this) return;
        if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame) carta.Zoom(1f, null);
        if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame) carta.Zoom(-1f, null);
    }

    // Nombres de islas y marcas (sin encimarse: primero los más cercanos al centro)
    readonly List<Rect> ocupado = new List<Rect>();
    void Nombres()
    {
        Navegacion nav = Navegacion.Instancia;
        if (nav == null) return;
        int n = 0; ocupado.Clear();
        var lista = new List<(Vector2 p, string t, Color c)>();
        foreach (var kv in nav.Descubiertas)
        {
            Vector2 p = carta.APantalla(kv.Value.centro);
            float rr = Mathf.Max(4f, kv.Value.orilla * carta.Esc);
            lista.Add((p + new Vector2(0f, rr + 4f), kv.Value.nombre, Estilo.Tinta));
        }
        foreach (var m in nav.Marcas) lista.Add((carta.APantalla(m.pos) + new Vector2(0f, 8f), m.nombre, new Color(0.55f, 0.12f, 0.1f)));
        Vector2 c = carta.contentRect.center;
        lista.Sort((a, b) => (a.p - c).sqrMagnitude.CompareTo((b.p - c).sqrMagnitude));
        Rect area = carta.contentRect;
        foreach (var e in lista)
        {
            if (!area.Contains(e.p)) continue;
            float w = Mathf.Min(180f, e.t.Length * 7.5f + 8f);
            Rect r = new Rect(e.p.x - w / 2f, e.p.y, w, 16f);
            bool choca = false;
            foreach (var o in ocupado) if (o.Overlaps(r)) { choca = true; break; }
            if (choca) continue;
            ocupado.Add(r);
            if (n >= poolNombres.Count)
            {
                var l = Estilo.Etiqueta("", Estilo.TamTextoChico * 0.9f, Estilo.Tinta, Estilo.FuenteNegrita);
                l.style.position = Position.Absolute; l.style.unityTextAlign = TextAnchor.UpperCenter; l.style.whiteSpace = WhiteSpace.NoWrap;
                l.pickingMode = PickingMode.Ignore;
                etiquetas.Add(l); poolNombres.Add(l);
            }
            var lb = poolNombres[n++];
            lb.style.display = DisplayStyle.Flex;
            lb.text = e.t; lb.style.color = e.c;
            lb.style.left = r.x; lb.style.top = r.y; lb.style.width = w;
        }
        for (int k = n; k < poolNombres.Count; k++) poolNombres[k].style.display = DisplayStyle.None;
        escala.text = carta.TextoEscala;
    }

    internal void PedirNombreMarca(Vector3 mundo)
    {
        Navegacion nav = Navegacion.Instancia;
        if (nav == null) return;
        string nombre = "Marca " + (nav.Marcas.Count + 1);
        nav.Marcas.Add(new Navegacion.Marca { nombre = nombre, pos = mundo });
        Sonidos.Reproducir(Sonidos.Tipo.Descubrir, 0.6f);
        Notificaciones.Mostrar("Marcaste un punto en la carta: " + nombre, Notificaciones.Tipo.Info, 3f, "marcador");
        Actualizar();
    }

    // ---------- La carta (dibujo) ----------
    class Carta : VisualElement
    {
        readonly VentanaMapa ventana;
        Vector3 desplazamiento;
        bool arrastrando; Vector2 ultimo; int puntero = -1; float bajada; Vector2 dondeBajada;
        static readonly float[] Pasos = { 25f, 50f, 100f, 250f, 500f, 1000f, 2500f, 5000f, 10000f };
        float paso;
        // Dedos en la pantalla (teléfono): con dos se pellizca para acercar o alejar
        readonly System.Collections.Generic.Dictionary<int, Vector2> dedos = new System.Collections.Generic.Dictionary<int, Vector2>();
        float distanciaPellizco;

        public Carta(VentanaMapa v)
        {
            ventana = v;
            generateVisualContent += Dibujar;
            RegisterCallback<WheelEvent>(e => { float d = e.delta.y; Zoom(-Mathf.Clamp(Mathf.Abs(d) >= 20f ? d / 120f : d / 3f, -4f, 4f), e.localMousePosition); e.StopPropagation(); });
            RegisterCallback<PointerDownEvent>(AlBajar);
            RegisterCallback<PointerMoveEvent>(AlMover);
            RegisterCallback<PointerUpEvent>(AlSubir);
            RegisterCallback<PointerCaptureOutEvent>(_ => arrastrando = false);
            RegisterCallback<GeometryChangedEvent>(_ => ventana.Actualizar());
        }

        static float Radio { get => Mathf.Clamp(Preferencias.ZoomMapa, 60f, 20000f); set { Preferencias.ZoomMapa = Mathf.Clamp(value, 60f, 20000f); } }
        public float Esc => Mathf.Min(contentRect.width, contentRect.height) / 2f / Radio;   // px por metro
        Vector3 Yo => Personaje.Instancia != null ? Personaje.Instancia.transform.position : Vector3.zero;
        Vector3 Vista => Yo + desplazamiento;

        public Vector2 APantalla(Vector3 mundo) { Vector3 v = mundo - Vista; return contentRect.center + new Vector2(v.x, -v.z) * Esc; }
        public Vector3 AMundo(Vector2 p) { Vector2 d = (p - contentRect.center) / Esc; return Vista + new Vector3(d.x, 0f, -d.y); }

        public void Centrar() { desplazamiento = Vector3.zero; ventana.Actualizar(); }

        public void Zoom(float pasos, Vector2? ancla)
        {
            Vector2 a = ancla ?? contentRect.center;
            Vector3 antes = AMundo(a);
            Radio *= Mathf.Pow(0.8f, pasos);
            Vector3 despues = AMundo(a);
            desplazamiento += antes - despues;
            Preferencias.Guardar();
            ventana.Actualizar();
        }

        void AlBajar(PointerDownEvent e)
        {
            if (e.button == 1)
            {
                // Clic derecho: quitar la marca bajo el puntero
                Navegacion nav = Navegacion.Instancia;
                if (nav == null) return;
                for (int k = nav.Marcas.Count - 1; k >= 0; k--)
                    if ((APantalla(nav.Marcas[k].pos) - (Vector2)e.localPosition).magnitude < 12f)
                    {
                        Notificaciones.Info("Quitaste " + nav.Marcas[k].nombre);
                        nav.Marcas.RemoveAt(k); ventana.Actualizar(); return;
                    }
                return;
            }
            if (e.button != 0) return;
            if (e.pointerType == UnityEngine.UIElements.PointerType.touch)
            {
                dedos[e.pointerId] = e.localPosition;
                this.CapturePointer(e.pointerId);
                if (dedos.Count == 2)
                {
                    arrastrando = false;
                    distanciaPellizco = Separacion();
                    return;
                }
            }
            // Doble clic (medido aquí: el arrastre captura el puntero y el contador de clics del sistema no siempre llega)
            if (e.clickCount >= 2 || (Time.unscaledTime - bajada < 0.35f && ((Vector2)e.localPosition - dondeBajada).magnitude < 8f))
            { bajada = -1f; ventana.PedirNombreMarca(AMundo(e.localPosition)); return; }
            arrastrando = true; puntero = e.pointerId; ultimo = e.localPosition; bajada = Time.unscaledTime; dondeBajada = e.localPosition;
            this.CapturePointer(e.pointerId);
        }

        float Separacion()
        {
            Vector2 a = Vector2.zero, b = Vector2.zero; int i = 0;
            foreach (var kv in dedos) { if (i == 0) a = kv.Value; else if (i == 1) b = kv.Value; i++; }
            return (a - b).magnitude;
        }

        Vector2 Medio()
        {
            Vector2 s = Vector2.zero; int n = 0;
            foreach (var kv in dedos) { s += kv.Value; n++; if (n == 2) break; }
            return n > 0 ? s / n : contentRect.center;
        }

        void AlMover(PointerMoveEvent e)
        {
            if (dedos.ContainsKey(e.pointerId))
            {
                dedos[e.pointerId] = e.localPosition;
                if (dedos.Count >= 2)
                {
                    float sep = Separacion();
                    if (distanciaPellizco > 10f && sep > 10f)
                    {
                        float f = sep / distanciaPellizco;   // >1 = abrir los dedos = acercar
                        if (Mathf.Abs(f - 1f) > 0.01f) { Zoom(Mathf.Log(1f / f) / Mathf.Log(0.8f), Medio()); distanciaPellizco = sep; }
                    }
                    return;
                }
            }
            if (!arrastrando || e.pointerId != puntero) return;
            Vector2 d = (Vector2)e.localPosition - ultimo;
            ultimo = e.localPosition;
            desplazamiento -= new Vector3(d.x, 0f, -d.y) / Esc;
            ventana.Actualizar();
        }

        void AlSubir(PointerUpEvent e)
        {
            if (dedos.Remove(e.pointerId)) this.ReleasePointer(e.pointerId);
            if (e.pointerId != puntero) return;
            arrastrando = false;
            this.ReleasePointer(e.pointerId);
        }

        public string TextoEscala => paso >= 1000f ? (paso / 1000f) + " km" : paso + " m";

        void Dibujar(MeshGenerationContext mgc)
        {
            var p = mgc.painter2D;
            Rect r = contentRect;
            if (r.width < 10f) return;
            float esc = Esc;
            Color tinta = new Color(0.25f, 0.17f, 0.1f, 1f);

            // Mar: tinta azul lavada sobre el pergamino
            p.fillColor = new Color(0.3f, 0.52f, 0.6f, 0.42f);
            p.BeginPath(); p.MoveTo(r.min); p.LineTo(new Vector2(r.xMax, r.yMin)); p.LineTo(r.max); p.LineTo(new Vector2(r.xMin, r.yMax)); p.ClosePath(); p.Fill();

            // Cuadrícula
            paso = Pasos[Pasos.Length - 1];
            foreach (float s in Pasos) if (s * esc >= 70f) { paso = s; break; }
            Vector3 vista = Vista;
            float mx = r.width / 2f / esc, mz = r.height / 2f / esc;
            p.strokeColor = new Color(tinta.r, tinta.g, tinta.b, 0.12f); p.lineWidth = 1f;
            for (float gx = Mathf.Floor((vista.x - mx) / paso) * paso; gx <= vista.x + mx; gx += paso)
            { float x = r.center.x + (gx - vista.x) * esc; p.BeginPath(); p.MoveTo(new Vector2(x, r.yMin)); p.LineTo(new Vector2(x, r.yMax)); p.Stroke(); }
            for (float gz = Mathf.Floor((vista.z - mz) / paso) * paso; gz <= vista.z + mz; gz += paso)
            { float y = r.center.y - (gz - vista.z) * esc; p.BeginPath(); p.MoveTo(new Vector2(r.xMin, y)); p.LineTo(new Vector2(r.xMax, y)); p.Stroke(); }

            // Islas
            Navegacion nav = Navegacion.Instancia;
            if (nav != null)
                foreach (var kv in nav.Descubiertas)
                {
                    var isla = kv.Value;
                    Vector2 c = APantalla(isla.centro);
                    float rr = Mathf.Max(4f, isla.orilla * esc);
                    if (c.x + rr < r.xMin || c.x - rr > r.xMax || c.y + rr < r.yMin || c.y - rr > r.yMax) continue;
                    // Bajío, arena y verde, con contorno a tinta
                    p.fillColor = new Color(0.45f, 0.65f, 0.65f, 0.35f); Circulo(p, c, rr * 1.25f); p.Fill();
                    p.fillColor = new Color(0.86f, 0.76f, 0.52f, 1f); Circulo(p, c, rr); p.Fill();
                    p.strokeColor = tinta; p.lineWidth = 1.5f; p.Stroke();
                    p.fillColor = new Color(0.42f, 0.58f, 0.32f, 1f); Circulo(p, c, rr * 0.62f); p.Fill();
                    if (isla.mercado)
                    {
                        Vector2 m = c + new Vector2(rr * 0.7f, -rr * 0.7f);
                        p.fillColor = new Color(0.85f, 0.65f, 0.15f); Circulo(p, m, 6f); p.Fill();
                        p.strokeColor = tinta; p.lineWidth = 1f; p.Stroke();
                    }
                }

            // Pecios: una cruz de palos con el mástil roto
            foreach (Pecios.Pecio pe in Pecios.Todos)
            {
                Vector2 c = APantalla(pe.pos);
                if (!r.Contains(c)) continue;
                p.strokeColor = new Color(0.32f, 0.24f, 0.18f); p.lineWidth = 2f;
                p.BeginPath(); p.MoveTo(c + new Vector2(-5f, -5f)); p.LineTo(c + new Vector2(5f, 5f)); p.MoveTo(c + new Vector2(5f, -5f)); p.LineTo(c + new Vector2(-5f, 5f)); p.Stroke();
            }

            // Marcas
            if (nav != null)
                foreach (var m in nav.Marcas)
                {
                    Vector2 c = APantalla(m.pos);
                    p.fillColor = new Color(0.65f, 0.15f, 0.12f);
                    p.BeginPath(); p.MoveTo(c + new Vector2(0f, 4f)); p.LineTo(c + new Vector2(-6f, -8f)); p.Arc(c + new Vector2(0f, -10f), 6f, 160f, 380f); p.ClosePath(); p.Fill();
                    p.fillColor = Estilo.Pergamino; Circulo(p, c + new Vector2(0f, -10f), 2.2f); p.Fill();
                }

            Personaje yo = Personaje.Instancia;
            if (yo != null)
            {
                Vector2 Borde(Vector2 v) => new Vector2(Mathf.Clamp(v.x, r.xMin + 12f, r.xMax - 12f), Mathf.Clamp(v.y, r.yMin + 12f, r.yMax - 12f));
                if (yo.barco != null && yo.BarcoActual != yo.barco)
                    Flecha(p, Borde(APantalla(yo.barco.transform.position)), yo.barco.transform.eulerAngles.y, 11f, new Color(0.55f, 0.32f, 0.08f));
                float rumbo = yo.BarcoActual != null && yo.estado != Personaje.Estado.Libre ? yo.BarcoActual.transform.eulerAngles.y : yo.transform.eulerAngles.y;
                Flecha(p, Borde(APantalla(yo.transform.position)), rumbo, 13f, tinta);
            }

            // Rosa de los vientos
            Vector2 rc = new Vector2(r.xMax - 56f, r.yMin + 56f);
            p.strokeColor = new Color(tinta.r, tinta.g, tinta.b, 0.6f); p.lineWidth = 1f;
            Circulo(p, rc, 38f); p.Stroke(); Circulo(p, rc, 30f); p.Stroke();
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad; float largo = k % 2 == 0 ? 36f : 22f;
                Vector2 d = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
                Vector2 lado = new Vector2(-d.y, d.x) * (k % 2 == 0 ? 6f : 4f);
                p.fillColor = k == 0 ? new Color(0.65f, 0.15f, 0.12f) : new Color(tinta.r, tinta.g, tinta.b, k % 2 == 0 ? 0.85f : 0.5f);
                p.BeginPath(); p.MoveTo(rc + d * largo); p.LineTo(rc + lado); p.LineTo(rc - lado); p.ClosePath(); p.Fill();
            }

            // Barra de escala
            float barra = paso * esc;
            Vector2 b0 = new Vector2(r.xMin + 14f, r.yMax - 14f);
            p.strokeColor = tinta; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(b0); p.LineTo(b0 + new Vector2(barra, 0f)); p.Stroke();
            p.BeginPath(); p.MoveTo(b0 + new Vector2(0f, -5f)); p.LineTo(b0 + new Vector2(0f, 2f)); p.Stroke();
            p.BeginPath(); p.MoveTo(b0 + new Vector2(barra, -5f)); p.LineTo(b0 + new Vector2(barra, 2f)); p.Stroke();
        }

        static void Circulo(Painter2D p, Vector2 c, float r) { p.BeginPath(); p.Arc(c, r, 0f, 360f); p.ClosePath(); }

        static void Flecha(Painter2D p, Vector2 c, float rumbo, float tam, Color col)
        {
            float a = rumbo * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)), l = new Vector2(-d.y, d.x);
            p.fillColor = col;
            p.BeginPath(); p.MoveTo(c + d * tam); p.LineTo(c - d * tam * 0.7f + l * tam * 0.65f); p.LineTo(c - d * tam * 0.35f); p.LineTo(c - d * tam * 0.7f - l * tam * 0.65f); p.ClosePath(); p.Fill();
            p.strokeColor = Estilo.Pergamino; p.lineWidth = 1f; p.Stroke();
        }
    }
}
