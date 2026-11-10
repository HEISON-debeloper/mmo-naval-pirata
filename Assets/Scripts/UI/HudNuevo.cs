using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// El HUD nuevo (UI Toolkit), limpio y por zonas para que nada se tape:
//   arriba izq.  vida, hambre, sed            arriba centro  brújula                 arriba der.  barco enemigo cercano
//   abajo izq.   mochila rápida y oro          abajo centro   qué puedes hacer aquí   abajo der.   tu barco (a bordo)
// Lo que no cambia se construye una vez; cada cuadro solo se actualizan valores. Cada zona se puede ocultar en Opciones.
[DefaultExecutionOrder(100)]
public class HudNuevo : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() => new GameObject("HUD").AddComponent<HudNuevo>();

    public static HudNuevo I { get; private set; }
    public static bool Visible = true;
    string firmaHerramientas;

    VisualElement raiz;
    // Estado del jugador
    VisualElement tarjetaEstado, creativo;
    Componentes.BarraEstado vida, hambre, sed;
    Label modo;
    // Brújula
    Brujula brujula;
    // Enemigo
    VisualElement tarjetaEnemigo; Label enemigoNombre, enemigoDistancia, enemigoEstado; Componentes.BarraEstado enemigoCasco; VisualElement enemigoTrip;
    // Mochila
    VisualElement tarjetaMochila, casillas, herramientas, flotantes; Label oro, mochilaVacia;
    readonly Dictionary<string, Componentes.CasillaObjeto> casillaPor = new Dictionary<string, Componentes.CasillaObjeto>();
    readonly Dictionary<string, float> recibidoEn = new Dictionary<string, float>();
    Inventario invEscuchado;
    // Barco
    VisualElement tarjetaBarco, velasPips, canonesFilas, filaBodega; Label barcoNombre, barcoNiveles, velasTexto, velocidad, bodegaTexto; Componentes.BarraEstado casco;
    readonly Dictionary<LadoCanon, (VisualElement fila, Label cuenta, VisualElement relleno, Label estado)> canonFila = new Dictionary<LadoCanon, (VisualElement, Label, VisualElement, Label)>();
    // Indicación
    VisualElement indicacion, atajos; Label indicacionTexto;
    // Golpe
    VisualElement vineta;

    IABarco[] enemigos = new IABarco[0];
    float tBuscar, tTexto;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        if (Lienzo.Listo) Construir();
    }

    static readonly string[] OrdenMochila =
    {
        Inventario.Madera, Inventario.Palo, Inventario.Fibra, Inventario.Cuerda, Inventario.Piedra,
        Inventario.Mineral, Inventario.Lingote, Inventario.Canon, Inventario.Coco, Inventario.Fruta
    };

    void Construir()
    {
        if (raiz != null) return;
        raiz = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore };
        raiz.style.position = Position.Absolute;
        raiz.style.left = raiz.style.right = raiz.style.top = raiz.style.bottom = 0f;
        Lienzo.CapaHud.Add(raiz);

        // Viñeta roja al recibir daño (borde grueso que se desvanece)
        vineta = new VisualElement { pickingMode = PickingMode.Ignore };
        vineta.style.position = Position.Absolute;
        vineta.style.left = vineta.style.right = vineta.style.top = vineta.style.bottom = 0f;
        vineta.style.borderTopWidth = vineta.style.borderBottomWidth = vineta.style.borderLeftWidth = vineta.style.borderRightWidth = 46f;
        Color rojo = new Color(0.75f, 0.08f, 0.05f, 0.55f);
        vineta.style.borderTopColor = vineta.style.borderBottomColor = vineta.style.borderLeftColor = vineta.style.borderRightColor = rojo;
        Estilo.Redondear(vineta, 60f);
        vineta.style.opacity = 0f;
        raiz.Add(vineta);

        // ---------- Arriba izquierda: estado ----------
        tarjetaEstado = Componentes.Tarjeta(null, null, 0.88f);
        tarjetaEstado.style.position = Position.Absolute; tarjetaEstado.style.left = 16f; tarjetaEstado.style.top = 16f;
        tarjetaEstado.style.width = 250f;
        tarjetaEstado.pickingMode = PickingMode.Ignore;
        vida = new Componentes.BarraEstado("Vida", "vida", Estilo.ColVida, 226f, 16f);
        hambre = new Componentes.BarraEstado("Hambre: come cocos o frutas (C)", "hambre", Estilo.ColHambre, 226f, 14f);
        sed = new Componentes.BarraEstado("Sed: los cocos quitan mucha sed", "sed", Estilo.ColSed, 226f, 14f);
        vida.style.marginBottom = hambre.style.marginBottom = 5f;
        tarjetaEstado.Add(vida); tarjetaEstado.Add(hambre); tarjetaEstado.Add(sed);
        modo = Estilo.Pequena("");
        modo.style.marginTop = 4f; modo.pickingMode = PickingMode.Ignore;
        tarjetaEstado.Add(modo);
        raiz.Add(tarjetaEstado);

        creativo = Estilo.Fila(6f);
        creativo.style.position = Position.Absolute; creativo.style.left = 16f; creativo.style.top = 128f;
        creativo.style.backgroundColor = Estilo.Oro; Estilo.Redondear(creativo, 4f); Estilo.Relleno(creativo, 3f, 10f);
        creativo.pickingMode = PickingMode.Ignore;
        creativo.Add(Iconos.Crear("rayo", 14f, new Color(0.1f, 0.08f, 0.05f)));
        var ct = Estilo.Etiqueta("MODO CREATIVO", Estilo.TamTextoChico, new Color(0.1f, 0.08f, 0.05f), Estilo.FuenteNegrita);
        ct.style.marginLeft = 6f; ct.style.letterSpacing = 1f;
        creativo.Add(ct);
        var ct2 = Estilo.Etiqueta("", Estilo.TamTextoChico * 0.9f, new Color(0.25f, 0.18f, 0.08f), Estilo.FuenteMedia);
        ct2.name = "quitar"; ct2.style.marginLeft = 8f;
        creativo.Add(ct2);
        creativo.style.display = DisplayStyle.None;
        raiz.Add(creativo);

        // ---------- Arriba centro: brújula ----------
        brujula = new Brujula();
        brujula.style.position = Position.Absolute; brujula.style.top = 12f;
        brujula.style.left = Length.Percent(50f); brujula.style.translate = new Translate(Length.Percent(-50f), 0f);
        raiz.Add(brujula);

        // ---------- Arriba derecha: enemigo ----------
        tarjetaEnemigo = Componentes.Tarjeta(null, null, 0.88f);
        tarjetaEnemigo.style.position = Position.Absolute; tarjetaEnemigo.style.right = 16f; tarjetaEnemigo.style.top = 16f;
        tarjetaEnemigo.style.width = 250f; tarjetaEnemigo.pickingMode = PickingMode.Ignore;
        tarjetaEnemigo.style.borderTopColor = tarjetaEnemigo.style.borderBottomColor = tarjetaEnemigo.style.borderLeftColor = tarjetaEnemigo.style.borderRightColor = new Color(0.6f, 0.2f, 0.15f);
        var fe = Estilo.Fila(6f);
        fe.Add(Iconos.Crear("calavera", 16f, Estilo.Mal));
        enemigoNombre = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Mal, Estilo.FuenteTitulo);
        enemigoNombre.style.marginLeft = 6f; enemigoNombre.style.flexGrow = 1f; enemigoNombre.style.whiteSpace = WhiteSpace.NoWrap; enemigoNombre.style.textOverflow = TextOverflow.Ellipsis; enemigoNombre.style.overflow = Overflow.Hidden;
        fe.Add(enemigoNombre);
        enemigoDistancia = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Oro, Estilo.FuenteNegrita);
        fe.Add(enemigoDistancia);
        tarjetaEnemigo.Add(fe);
        enemigoCasco = new Componentes.BarraEstado("Casco del enemigo", null, Estilo.ColCasco, 226f, 12f);
        enemigoCasco.style.marginTop = 6f;
        tarjetaEnemigo.Add(enemigoCasco);
        var fe2 = Estilo.Fila(4f); fe2.style.marginTop = 6f; fe2.style.justifyContent = Justify.SpaceBetween;
        enemigoTrip = Estilo.Fila(3f);
        fe2.Add(enemigoTrip);
        enemigoEstado = Estilo.Pequena("", Estilo.Ambar);
        fe2.Add(enemigoEstado);
        tarjetaEnemigo.Add(fe2);
        tarjetaEnemigo.style.display = DisplayStyle.None;
        raiz.Add(tarjetaEnemigo);

        // ---------- Abajo izquierda: mochila rápida ----------
        tarjetaMochila = Componentes.Tarjeta(null, null, 0.88f);
        tarjetaMochila.style.position = Position.Absolute; tarjetaMochila.style.left = 16f; tarjetaMochila.style.bottom = 16f;
        tarjetaMochila.style.minWidth = 250f; tarjetaMochila.style.maxWidth = 560f;
        var fm = Estilo.Fila(6f);
        fm.Add(Iconos.Crear("moneda", 18f, Estilo.Oro));
        oro = Estilo.Etiqueta("0", Estilo.TamTexto, Estilo.Oro, Estilo.FuenteNegrita);
        oro.style.marginLeft = 6f; oro.style.flexGrow = 1f;
        fm.Add(oro);
        var abrir = Componentes.Indicacion(Teclas.Nombre(Accion.Inventario), "mochila", 0.85f);
        fm.Add(abrir);
        tarjetaMochila.Add(fm);
        casillas = Estilo.Fila(0f); casillas.style.flexWrap = Wrap.Wrap; casillas.style.marginTop = 4f;
        casillas.style.maxWidth = 5 * 56f;
        tarjetaMochila.Add(casillas);
        mochilaVacia = Estilo.Pequena("Mochila vacía: recoge cosas en la isla con F");
        mochilaVacia.style.marginTop = 6f;
        tarjetaMochila.Add(mochilaVacia);
        herramientas = Estilo.Fila(4f); herramientas.style.marginTop = 6f;
        tarjetaMochila.Add(herramientas);
        raiz.Add(tarjetaMochila);
        flotantes = new VisualElement { pickingMode = PickingMode.Ignore };
        flotantes.style.position = Position.Absolute; flotantes.style.left = 24f; flotantes.style.bottom = 150f;
        raiz.Add(flotantes);

        // ---------- Abajo derecha: tu barco ----------
        tarjetaBarco = Componentes.Tarjeta(null, null, 0.88f);
        tarjetaBarco.style.position = Position.Absolute; tarjetaBarco.style.right = 16f; tarjetaBarco.style.bottom = 16f;
        tarjetaBarco.style.width = 300f; tarjetaBarco.pickingMode = PickingMode.Ignore;
        var fb = Estilo.Fila(6f);
        fb.Add(Iconos.Crear("barco", 18f, Estilo.Oro));
        barcoNombre = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Oro, Estilo.FuenteTitulo);
        barcoNombre.style.marginLeft = 6f; barcoNombre.style.flexGrow = 1f; barcoNombre.style.whiteSpace = WhiteSpace.NoWrap; barcoNombre.style.textOverflow = TextOverflow.Ellipsis; barcoNombre.style.overflow = Overflow.Hidden;
        fb.Add(barcoNombre);
        barcoNiveles = Estilo.Pequena("");
        fb.Add(barcoNiveles);
        tarjetaBarco.Add(fb);
        casco = new Componentes.BarraEstado("Casco del barco", "escudo", Estilo.ColCasco, 276f, 14f);
        casco.style.marginTop = 6f;
        tarjetaBarco.Add(casco);
        var fv = Estilo.Fila(6f); fv.style.marginTop = 6f;
        fv.Add(Iconos.Crear("vela", 16f, Estilo.TextoTenue));
        velasPips = Estilo.Fila(3f); velasPips.style.marginLeft = 6f;
        fv.Add(velasPips);
        velasTexto = Estilo.Pequena(""); velasTexto.style.marginLeft = 8f; velasTexto.style.flexGrow = 1f;
        fv.Add(velasTexto);
        velocidad = Estilo.Etiqueta("", Estilo.TamTextoChico, Estilo.Texto, Estilo.FuenteMedia);
        fv.Add(velocidad);
        tarjetaBarco.Add(fv);
        canonesFilas = Estilo.Columna(); canonesFilas.style.marginTop = 6f;
        tarjetaBarco.Add(canonesFilas);
        filaBodega = Estilo.Fila(6f); filaBodega.style.marginTop = 4f;
        filaBodega.Add(Iconos.Crear("caja", 14f, Estilo.TextoTenue));
        bodegaTexto = Estilo.Pequena(""); bodegaTexto.style.marginLeft = 6f;
        filaBodega.Add(bodegaTexto);
        tarjetaBarco.Add(filaBodega);
        tarjetaBarco.style.display = DisplayStyle.None;
        raiz.Add(tarjetaBarco);

        // ---------- Abajo centro: indicación y atajos ----------
        indicacion = new VisualElement { pickingMode = PickingMode.Ignore };
        indicacion.style.position = Position.Absolute; indicacion.style.bottom = 54f;
        indicacion.style.left = Length.Percent(50f); indicacion.style.translate = new Translate(Length.Percent(-50f), 0f);
        indicacion.style.maxWidth = 620f;
        Estilo.PanelMadera(indicacion, 0.82f);
        Estilo.Relleno(indicacion, 6f, 16f);
        indicacionTexto = Estilo.Etiqueta("", Estilo.TamTexto, Estilo.Texto, Estilo.FuenteMedia);
        indicacionTexto.style.unityTextAlign = TextAnchor.MiddleCenter; indicacionTexto.pickingMode = PickingMode.Ignore;
        indicacion.Add(indicacionTexto);
        raiz.Add(indicacion);

        atajos = Estilo.Fila(14f);
        atajos.style.position = Position.Absolute; atajos.style.bottom = 18f;
        atajos.style.left = Length.Percent(50f); atajos.style.translate = new Translate(Length.Percent(-50f), 0f);
        atajos.pickingMode = PickingMode.Ignore;
        raiz.Add(atajos);
        ReconstruirAtajos();

        Preferencias.alCambiar += AlCambiarPreferencias;
        AlCambiarPreferencias();
    }

    void ReconstruirAtajos()
    {
        atajos.Clear();
        void A(Accion a, string texto) { var i = Componentes.Indicacion(Teclas.Nombre(a), texto, 0.85f); i.style.marginRight = 14f; atajos.Add(i); }
        A(Accion.Fabricar, "fabricar"); A(Accion.Inventario, "mochila"); A(Accion.Barco, "barco"); A(Accion.Mapa, "mapa"); A(Accion.Ayuda, "controles");
    }

    void AlCambiarPreferencias()
    {
        if (raiz == null) return;
        ReconstruirAtajos();
        brujula.style.display = Preferencias.HudBrujula ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OnDestroy()
    {
        Preferencias.alCambiar -= AlCambiarPreferencias;
        if (invEscuchado != null) invEscuchado.alRecibir -= Recibido;
        if (I == this) I = null;
    }

    void Recibido(string tipo, int n)
    {
        recibidoEn[tipo] = Time.unscaledTime;
        Notificaciones.Objeto(tipo, n, false);
        // "+3 Madera" que sube y se desvanece junto a la mochila
        var l = Componentes.TextoHud("+" + n + " " + Objetos.Nombre(tipo), Estilo.TamTexto, Estilo.Bien, Estilo.FuenteNegrita);
        flotantes.Add(l);
        float y0 = 0f;
        Lienzo.Animar(l, Estilo.Ms(1600) > 0 ? Estilo.Ms(1600) : 1600, v => { l.style.translate = new Translate(0f, y0 - v * 60f); l.style.opacity = v < 0.6f ? 1f : 1f - (v - 0.6f) / 0.4f; }, () => l.RemoveFromHierarchy());
        if (flotantes.childCount > 5) flotantes.RemoveAt(0);
    }

    void Update()
    {
        if (raiz == null) { if (Lienzo.Listo) Construir(); else return; }
        Personaje p = Personaje.Instancia;
        bool mostrar = Visible && p != null && !MenuPrincipal.Visible && !Navegacion.MapaAbierto;
        raiz.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        if (!mostrar) return;

        Inventario inv = p.GetComponent<Inventario>();
        if (inv != invEscuchado)
        {
            if (invEscuchado != null) invEscuchado.alRecibir -= Recibido;
            invEscuchado = inv;
            if (inv != null) inv.alRecibir += Recibido;
        }

        bool ventana = Ventanas.AlgunaAbierta || Fabricacion.Abierto || Navegacion.MapaAbierto;
        float dt = Time.unscaledDeltaTime;

        // Barras: cada cuadro (animación suave)
        Supervivencia s = p.GetComponent<Supervivencia>();
        vida.Fijar(p.Vida, p.vidaMaxima);
        if (s != null) { hambre.Fijar(s.Hambre, 100f, s.Hambre <= 0f ? "¡Come algo!" : null); sed.Fijar(s.Sed, 100f, s.Sed <= 0f ? "¡Bebe agua de coco!" : null); }
        vida.Animar(); hambre.Animar(); sed.Animar();
        vineta.style.opacity = p.Golpe;

        // Brújula
        if (Preferencias.HudBrujula) brujula.Actualizar(p, enemigos);

        tBuscar -= dt;
        if (tBuscar <= 0f) { enemigos = IABarco.Todos.ToArray(); tBuscar = 0.5f; }

        tTexto -= dt;
        bool textos = tTexto <= 0f;
        if (textos) tTexto = 0.12f;

        if (textos)
        {
            modo.text = p.TextoModo();
            creativo.style.display = ModoCreativo.Activo ? DisplayStyle.Flex : DisplayStyle.None;
            if (ModoCreativo.Activo) creativo.Q<Label>("quitar").text = Teclas.Nombre(Accion.Creativo) + " quitar";
            Mochila(p, inv);
            Indicacion(p, ventana);
        }
        TuBarco(p, textos);
        Enemigo(p, textos);

        tarjetaMochila.style.display = Preferencias.HudMochila ? DisplayStyle.Flex : DisplayStyle.None;
        atajos.style.display = Preferencias.HudAtajos && !ventana ? DisplayStyle.Flex : DisplayStyle.None;
        indicacion.style.display = ventana ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // ---------- Mochila ----------
    void Mochila(Personaje p, Inventario inv)
    {
        if (inv == null) return;
        oro.text = inv.Cantidad(Inventario.Oro).ToString("N0");
        int visibles = 0;
        foreach (string t in OrdenMochila)
        {
            int n = inv.Cantidad(t);
            casillaPor.TryGetValue(t, out var c);
            if (n <= 0) { if (c != null) c.style.display = DisplayStyle.None; continue; }
            if (c == null)
            {
                c = new Componentes.CasillaObjeto(48f);
                c.alPulsar = _ => { if (t == Inventario.Coco || t == Inventario.Fruta) p.Comer(); else Ventanas.Abrir(VentanaInventario.I); };
                casillaPor[t] = c;
            }
            c.style.display = DisplayStyle.Flex;
            c.Fijar(t, n);
            c.Nueva(recibidoEn.TryGetValue(t, out float cuando) && Time.unscaledTime - cuando < 2.5f);
            // Orden estable: según OrdenMochila
            casillas.Add(c);   // Add lo mueve al final si ya estaba: el orden queda el de OrdenMochila
            visibles++;
        }
        mochilaVacia.style.display = visibles == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        casillas.style.display = visibles == 0 ? DisplayStyle.None : DisplayStyle.Flex;

        // Solo se rehace si cambió algo (antes se rehacía ~8 veces por segundo)
        string firma = (ModoCreativo.Activo ? "C" : "") + (inv.Tiene(Inventario.Hacha) ? "H" : "") + (inv.Tiene(Inventario.Pico) ? "P" : "")
                       + (inv.Tiene(Inventario.Coco) || inv.Tiene(Inventario.Fruta) ? "K" : "") + Teclas.Nombre(Accion.Comer);
        if (firma == firmaHerramientas) return;
        firmaHerramientas = firma;
        herramientas.Clear();
        var et = Estilo.Pequena("Herramientas:"); et.style.marginRight = 4f;
        herramientas.Add(et);
        if (ModoCreativo.Activo) herramientas.Add(Estilo.Pequena("todas (creativo)", Estilo.Oro));
        else
        {
            bool alguna = false;
            if (inv.Tiene(Inventario.Hacha)) { herramientas.Add(Herramienta("hacha", "Hacha")); alguna = true; }
            if (inv.Tiene(Inventario.Pico)) { herramientas.Add(Herramienta("pico", "Pico")); alguna = true; }
            if (!alguna) herramientas.Add(Estilo.Pequena("tus manos"));
        }
        if (inv.Tiene(Inventario.Coco) || inv.Tiene(Inventario.Fruta))
        {
            var h = Estilo.Hueco(0f, 0f, true); herramientas.Add(h);
            herramientas.Add(Componentes.Indicacion(Teclas.Nombre(Accion.Comer), "comer", 0.8f));
        }
    }

    static VisualElement Herramienta(string icono, string nombre)
    {
        var f = Estilo.Fila(3f); f.style.marginRight = 8f; f.pickingMode = PickingMode.Ignore;
        f.Add(Iconos.Crear(icono, 14f, Estilo.Texto));
        var l = Estilo.Pequena(nombre, Estilo.Texto); l.style.marginLeft = 3f;
        f.Add(l);
        return f;
    }

    // ---------- Tu barco ----------
    void TuBarco(Personaje p, bool textos)
    {
        bool aBordo = p.estado != Personaje.Estado.Libre && p.BarcoActual != null && Preferencias.HudBarco;
        tarjetaBarco.style.display = aBordo ? DisplayStyle.Flex : DisplayStyle.None;
        if (!aBordo) return;
        ControlBarco b = p.BarcoActual;
        VidaBarco v = b.GetComponent<VidaBarco>();
        Canones cn = b.GetComponent<Canones>();
        if (v != null) { casco.Fijar(v.vida, v.vidaMaxima, v.Hundido ? "¡Hundiéndose!" : null); casco.Animar(); }

        // Recarga de cada lado: cada cuadro para que la barra corra suave
        if (cn != null)
        {
            foreach (LadoCanon l in new[] { LadoCanon.Babor, LadoCanon.Estribor, LadoCanon.Proa, LadoCanon.Popa })
            {
                bool hay = cn.RanurasLado(l) > 0;
                if (!canonFila.TryGetValue(l, out var f))
                {
                    if (!hay) continue;
                    f = CrearFilaCanon(l);
                    canonFila[l] = f;
                }
                f.fila.style.display = hay ? DisplayStyle.Flex : DisplayStyle.None;
                if (!hay) continue;
                float t = cn.Recarga(l), total = Mathf.Max(0.01f, cn.TiempoRecarga(l));
                int en = cn.EnLado(l);
                float frac = en == 0 ? 0f : 1f - Mathf.Clamp01(t / total);
                f.relleno.style.width = Length.Percent(frac * 100f);
                f.relleno.style.backgroundColor = en == 0 ? Estilo.TextoApagado : t <= 0f ? Estilo.Bien : Estilo.Ambar;
                if (textos)
                {
                    f.cuenta.text = l == LadoCanon.Popa ? (en > 0 ? "×" + cn.BarrilesPorTanda : "0") : en + "/" + cn.RanurasLado(l);
                    f.estado.text = en == 0 ? "vacío" : t <= 0f ? "listo" : t.ToString("0.0") + " s";
                    f.estado.style.color = en == 0 ? Estilo.TextoApagado : t <= 0f ? Estilo.Bien : Estilo.Ambar;
                }
            }
        }

        if (!textos) return;
        PiezasBarco pz = b.GetComponent<PiezasBarco>();
        Bodega bd = b.GetComponent<Bodega>();
        Rigidbody rb = b.GetComponent<Rigidbody>();
        bool propio = b == p.barco;
        barcoNombre.text = b.name;
        barcoNombre.style.color = propio ? Estilo.Oro : Estilo.TextoTenue;
        barcoNiveles.text = !propio ? "no es tuyo" : pz != null && pz.NivelTotal > 0 ? "casco +" + pz.nivelCasco + "  velas +" + pz.nivelVelas : "";

        // Velas: un rectángulo por nivel
        if (velasPips.childCount != b.nivelesVela)
        {
            velasPips.Clear();
            for (int i = 0; i < b.nivelesVela; i++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.style.width = 14f; pip.style.height = 10f; Estilo.Redondear(pip, 2f);
                pip.style.borderTopWidth = pip.style.borderBottomWidth = pip.style.borderLeftWidth = pip.style.borderRightWidth = 1f;
                pip.style.borderTopColor = pip.style.borderBottomColor = pip.style.borderLeftColor = pip.style.borderRightColor = Estilo.Bronce;
                velasPips.Add(pip);
            }
        }
        for (int i = 0; i < velasPips.childCount; i++)
            velasPips[i].style.backgroundColor = i < b.NivelVelaActual ? Estilo.ColVelas : new Color(0f, 0f, 0f, 0.35f);
        velasTexto.text = b.EnReversa ? Estilo.Col("reversa", Estilo.Ambar) : b.Anclado ? Estilo.Col("anclado", Estilo.Ambar) : b.NivelVelaActual == 0 ? "velas recogidas" : "";
        float nudos = rb != null ? Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude * 1.944f : 0f;
        float rumbo = (b.transform.eulerAngles.y + 360f) % 360f;
        velocidad.text = Estilo.Col(nudos.ToString("0.0"), Estilo.Oro) + " nudos  " + Estilo.Col(rumbo.ToString("000") + "° " + Rosa(rumbo), Estilo.Oro);

        filaBodega.style.display = bd != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (bd != null) bodegaTexto.text = "Bodega " + Estilo.Col(bd.Total + "/" + bd.capacidad, Estilo.Texto) + "   " + bd.Cantidad(Inventario.Coco) + " cocos, " + bd.Cantidad(Inventario.Fruta) + " frutas";
    }

    (VisualElement, Label, VisualElement, Label) CrearFilaCanon(LadoCanon l)
    {
        var fila = Estilo.Fila(6f); fila.style.marginTop = 3f; fila.pickingMode = PickingMode.Ignore;
        string nombre = l == LadoCanon.Babor ? "Babor" : l == LadoCanon.Estribor ? "Estribor" : l == LadoCanon.Proa ? "Proa" : "Barriles";
        Accion? tecla = l == LadoCanon.Babor ? Accion.DispararBabor : l == LadoCanon.Estribor ? Accion.DispararEstribor : (Accion?)null;
        var et = Estilo.Pequena(nombre, Estilo.Texto); et.style.width = 58f;
        fila.Add(et);
        if (tecla != null) { var t = Componentes.TeclaDe(tecla.Value, 0.75f); t.style.marginRight = 4f; fila.Add(t); }
        else fila.Add(Estilo.Hueco(24f));
        var cuenta = Estilo.Pequena("", Estilo.Texto); cuenta.style.width = 34f;
        fila.Add(cuenta);
        var pista = new VisualElement { pickingMode = PickingMode.Ignore };
        pista.style.flexGrow = 1f; pista.style.height = 8f; pista.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        Estilo.Borde(pista, new Color(Estilo.Bronce.r, Estilo.Bronce.g, Estilo.Bronce.b, 0.6f), 1f, 4f); pista.style.overflow = Overflow.Hidden;
        var relleno = new VisualElement { pickingMode = PickingMode.Ignore };
        relleno.style.position = Position.Absolute; relleno.style.left = 0f; relleno.style.top = 0f; relleno.style.bottom = 0f;
        relleno.style.backgroundColor = Estilo.Bien; Estilo.Redondear(relleno, 3f);
        pista.Add(relleno);
        fila.Add(pista);
        var estado = Estilo.Pequena(""); estado.style.width = 46f; estado.style.unityTextAlign = TextAnchor.MiddleRight;
        fila.Add(estado);
        canonesFilas.Add(fila);
        // Orden: babor, estribor, proa, popa
        return (fila, cuenta, relleno, estado);
    }

    static string Rosa(float grados)
    {
        string[] p = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
        return p[Mathf.RoundToInt(grados / 45f) % 8];
    }

    // ---------- Enemigo cercano ----------
    void Enemigo(Personaje p, bool textos)
    {
        IABarco cerca = null;
        float dMin = 350f;
        foreach (IABarco e in enemigos)
        {
            if (e == null) continue;
            float d = Vector3.Distance(e.transform.position, p.transform.position);
            bool abordo = p.estado != Personaje.Estado.Libre && p.BarcoActual == e.GetComponent<ControlBarco>();
            if (abordo) d = 0f;
            if (d < dMin) { dMin = d; cerca = e; }
        }
        bool mostrar = cerca != null && Preferencias.HudEnemigo;
        tarjetaEnemigo.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        if (!mostrar) return;
        VidaBarco v = cerca.GetComponent<VidaBarco>();
        if (v != null) { enemigoCasco.Fijar(v.vida, v.vidaMaxima); enemigoCasco.Animar(); }
        if (!textos) return;
        enemigoNombre.text = cerca.name;
        enemigoDistancia.text = Mathf.RoundToInt(Vector3.Distance(cerca.transform.position, p.transform.position)) + " m";
        enemigoEstado.text = cerca.EstadoTexto;
        int vivos = cerca.TripulantesVivos();
        if (enemigoTrip.childCount != cerca.tripulacion)
        {
            enemigoTrip.Clear();
            for (int i = 0; i < cerca.tripulacion; i++) enemigoTrip.Add(Iconos.Crear("persona", 12f, Estilo.Mal));
        }
        for (int i = 0; i < enemigoTrip.childCount; i++) ((Icono)enemigoTrip[i]).Color = i < vivos ? Estilo.Mal : new Color(Estilo.Mal.r, Estilo.Mal.g, Estilo.Mal.b, 0.25f);
    }

    // ---------- Indicación de contexto ----------
    void Indicacion(Personaje p, bool ventana)
    {
        if (ventana) return;
        string t = p.TextoAyuda();
        if (t != indicacionTexto.text) indicacionTexto.text = t;
    }
}

// La brújula: una franja con letras y marcas que se desplaza con el rumbo de la cámara, y marcadores de islas, tu barco y enemigos.
public class Brujula : VisualElement
{
    const float Ancho = 440f, Alto = 36f, GradosVisibles = 180f, DistanciaIslas = 2500f;
    readonly VisualElement franja, aguja;
    readonly Label[] letras = new Label[8];
    readonly List<VisualElement> marcas = new List<VisualElement>();
    readonly List<(Icono icono, Label dist)> marcadores = new List<(Icono, Label)>();
    readonly Label grados;
    static readonly string[] Nombres = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };

    public Brujula()
    {
        pickingMode = PickingMode.Ignore;
        style.width = Ancho; style.height = Alto + 30f;
        franja = new VisualElement { pickingMode = PickingMode.Ignore };
        franja.style.position = Position.Absolute; franja.style.left = 0f; franja.style.top = 0f; franja.style.width = Ancho; franja.style.height = Alto;
        Estilo.PanelMadera(franja, 0.88f);
        franja.style.overflow = Overflow.Hidden;
        Add(franja);
        for (int g = 0; g < 360; g += 15)
        {
            if (g % 45 == 0) continue;
            var m = new VisualElement { pickingMode = PickingMode.Ignore };
            m.style.position = Position.Absolute; m.style.top = 6f; m.style.width = 1f; m.style.height = 7f;
            m.style.backgroundColor = Estilo.TextoApagado;
            franja.Add(m); marcas.Add(m);
        }
        for (int i = 0; i < 8; i++)
        {
            var l = Estilo.Etiqueta(Nombres[i], Estilo.TamTexto, i == 0 ? Estilo.Mal : Estilo.Oro, Estilo.FuenteTitulo);
            l.style.position = Position.Absolute; l.style.top = 2f; l.style.width = 40f; l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.pickingMode = PickingMode.Ignore;
            franja.Add(l); letras[i] = l;
        }
        aguja = new VisualElement { pickingMode = PickingMode.Ignore };
        aguja.style.position = Position.Absolute; aguja.style.left = Ancho / 2f - 1f; aguja.style.top = 0f; aguja.style.width = 2f; aguja.style.height = Alto;
        aguja.style.backgroundColor = Estilo.Texto;
        franja.Add(aguja);
        grados = Estilo.Pequena("", Estilo.Texto);
        grados.style.position = Position.Absolute; grados.style.top = Alto + 1f; grados.style.left = Ancho / 2f - 30f; grados.style.width = 60f;
        grados.style.unityTextAlign = TextAnchor.MiddleCenter; grados.pickingMode = PickingMode.Ignore;
        grados.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.85f), offset = new Vector2(0f, 1f), blurRadius = 2f };
        Add(grados);
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
    static float RumboDe(Vector3 v) => (Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg + 360f) % 360f;

    public void Actualizar(Personaje p, IABarco[] enemigos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float rumbo = RumboDe(Plano(cam.transform.forward));
        float porGrado = Ancho / GradosVisibles, cx = Ancho / 2f;
        int im = 0;
        for (int g = 0; g < 360; g += 15)
        {
            float dx = Mathf.DeltaAngle(rumbo, g);
            if (g % 45 == 0)
            {
                var l = letras[g / 45];
                bool ver = Mathf.Abs(dx) <= GradosVisibles / 2f - 4f;
                l.style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
                if (ver) l.style.left = cx + dx * porGrado - 20f;
            }
            else
            {
                var m = marcas[im++];
                bool ver = Mathf.Abs(dx) <= GradosVisibles / 2f - 4f;
                m.style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
                if (ver) m.style.left = cx + dx * porGrado;
            }
        }
        grados.text = Mathf.RoundToInt(rumbo).ToString("000") + "°";

        // Marcadores
        int n = 0;
        Navegacion nav = Navegacion.Instancia;
        if (nav != null)
            foreach (var kv in nav.Descubiertas)
            {
                Vector3 v = Plano(kv.Value.centro - p.transform.position);
                if (v.magnitude > DistanciaIslas) continue;
                Marcador(n++, rumbo, RumboDe(v), kv.Value.mercado ? "moneda" : "isla", kv.Value.mercado ? Estilo.Oro : Estilo.Bien,
                    Mathf.RoundToInt(Mathf.Max(0f, v.magnitude - kv.Value.orilla)) + " m" + (kv.Value.mercado ? " · mercado" : ""));
            }
        if (p.barco != null && p.BarcoActual != p.barco)
        {
            Vector3 v = Plano(p.barco.transform.position - p.transform.position);
            Marcador(n++, rumbo, RumboDe(v), "barco", Estilo.Oro, Mathf.RoundToInt(v.magnitude) + " m · tu barco");
        }
        foreach (IABarco e in enemigos)
        {
            if (e == null) continue;
            Vector3 v = Plano(e.transform.position - p.transform.position);
            if (v.magnitude > 600f) continue;
            Marcador(n++, rumbo, RumboDe(v), "calavera", Estilo.Mal, Mathf.RoundToInt(v.magnitude) + " m");
        }
        for (int i = n; i < marcadores.Count; i++) { marcadores[i].icono.style.display = DisplayStyle.None; marcadores[i].dist.style.display = DisplayStyle.None; }
    }

    void Marcador(int i, float rumbo, float hacia, string icono, Color color, string distancia)
    {
        while (marcadores.Count <= i)
        {
            var ic = Iconos.Crear("punto", 14f, color);
            ic.style.position = Position.Absolute; ic.style.top = Alto - 17f;
            franja.Add(ic);
            var d = Estilo.Etiqueta("", Estilo.TamTextoChico * 0.8f, color);
            d.style.position = Position.Absolute; d.style.top = Alto + 14f; d.style.width = 160f; d.style.unityTextAlign = TextAnchor.MiddleCenter;
            d.pickingMode = PickingMode.Ignore; d.style.whiteSpace = WhiteSpace.NoWrap;
            d.style.textShadow = new TextShadow { color = new Color(0f, 0f, 0f, 0.85f), offset = new Vector2(0f, 1f), blurRadius = 2f };
            Add(d);
            marcadores.Add((ic, d));
        }
        var m = marcadores[i];
        float dx = Mathf.DeltaAngle(rumbo, hacia);
        float lim = GradosVisibles / 2f - 6f;
        bool fuera = Mathf.Abs(dx) > lim;
        dx = Mathf.Clamp(dx, -lim, lim);
        float x = Ancho / 2f + dx * (Ancho / GradosVisibles);
        m.icono.style.display = DisplayStyle.Flex;
        m.icono.Nombre = icono;
        m.icono.Color = fuera ? new Color(color.r, color.g, color.b, 0.4f) : color;
        m.icono.style.left = x - 7f;
        bool cerca = !fuera && Mathf.Abs(dx) < 12f;
        m.dist.style.display = cerca ? DisplayStyle.Flex : DisplayStyle.None;
        if (cerca) { m.dist.text = distancia; m.dist.style.color = color; m.dist.style.left = x - 80f; }
    }
}
