using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Toda la interfaz en pantalla, en un solo lugar, para que nada se tape:
//   arriba izq.  vida, hambre, sed          arriba centro  avisos          arriba der.  barco enemigo cercano
//   abajo izq.   mochila                    abajo centro   qué puedes hacer aquí       abajo der.   tu barco (a bordo)
// Con el menú de fabricación abierto solo quedan las barras y el menú. F1 muestra los controles.
// Se escala con el alto de la ventana (referencia 720 px).
[DefaultExecutionOrder(100)]
public class Hud : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("HUD").AddComponent<Hud>();

    public static float Escala { get; private set; } = 1f;
    public static float Ancho => Screen.width / Escala;
    public static float Alto => Screen.height / Escala;
    public static bool AyudaVisible { get; private set; }

    // ---------- Paleta: azul noche y latón ----------
    public static readonly Color Fondo = new Color(0.05f, 0.08f, 0.11f, 0.84f);
    public static readonly Color FondoClaro = new Color(1f, 1f, 1f, 0.07f);
    public static readonly Color Laton = new Color(0.86f, 0.68f, 0.36f, 0.6f);
    public static readonly Color Oro = new Color(0.96f, 0.81f, 0.50f);
    public static readonly Color Texto = new Color(0.93f, 0.92f, 0.88f);
    public static readonly Color Tenue = new Color(0.62f, 0.67f, 0.70f);
    public static readonly Color Bien = new Color(0.56f, 0.86f, 0.56f);
    public static readonly Color Mal = new Color(0.96f, 0.46f, 0.40f);
    public static readonly Color Ambar = new Color(0.98f, 0.76f, 0.32f);
    public static readonly Color ColVida = new Color(0.82f, 0.25f, 0.24f);
    public static readonly Color ColHambre = new Color(0.86f, 0.58f, 0.22f);
    public static readonly Color ColSed = new Color(0.27f, 0.58f, 0.88f);
    public static readonly Color ColCasco = new Color(0.62f, 0.45f, 0.26f);

    public static GUIStyle Normal, Pequeno, Titulo, Centrado, Derecha, Tecla;
    static int skinId;

    static void Estilos()
    {
        if (Normal != null && skinId == GUI.skin.GetInstanceID()) return;
        skinId = GUI.skin.GetInstanceID();
        Normal = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = false, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        Normal.normal.textColor = Texto;
        Pequeno = new GUIStyle(Normal) { fontSize = 11 };
        Pequeno.normal.textColor = Tenue;
        Titulo = new GUIStyle(Normal) { fontSize = 14, fontStyle = FontStyle.Bold };
        Titulo.normal.textColor = Oro;
        Centrado = new GUIStyle(Normal) { alignment = TextAnchor.MiddleCenter };
        Derecha = new GUIStyle(Normal) { alignment = TextAnchor.MiddleRight };
        Tecla = new GUIStyle(Normal) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Tecla.normal.textColor = new Color(0.08f, 0.09f, 0.10f);
    }

    // ---------- Piezas de dibujo (las usa también el menú de fabricación) ----------
    public static void Relleno(Rect r, Color c)
    {
        Color antes = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    public static void Caja(Rect r, bool borde = true)
    {
        Relleno(r, Fondo);
        if (!borde) return;
        Relleno(new Rect(r.x, r.y, r.width, 1f), Laton);
        Relleno(new Rect(r.x, r.yMax - 1f, r.width, 1f), new Color(Laton.r, Laton.g, Laton.b, 0.25f));
    }

    public static void Barra(Rect r, float frac, Color c, string etiqueta, string valor)
    {
        Relleno(r, new Color(0f, 0f, 0f, 0.45f));
        Relleno(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), c);
        GUI.Label(new Rect(r.x + 6f, r.y, r.width - 12f, r.height), etiqueta, Normal);
        GUI.Label(new Rect(r.x + 6f, r.y, r.width - 12f, r.height), valor, Derecha);
    }

    public static void BadgeTecla(Rect r, string tecla)
    {
        Relleno(r, Oro);
        GUI.Label(r, tecla, Tecla);
    }

    public static string Col(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

    // Una pastilla centrada con texto (avisos, indicaciones)
    static void Pastilla(float centroX, float y, string texto, GUIStyle st, float maxAncho)
    {
        float w = Mathf.Min(st.CalcSize(new GUIContent(texto)).x + 28f, maxAncho);
        Rect r = new Rect(centroX - w / 2f, y, w, 28f);
        Caja(r);
        GUI.Label(r, texto, Centrado);
    }

    // ---------- Estado ----------
    IABarco[] enemigos = new IABarco[0];
    float tBuscar;

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.f1Key.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
        if (Fabricacion.Abierto) AyudaVisible = false;
        tBuscar -= Time.deltaTime;
        if (tBuscar <= 0f) { enemigos = IABarco.Todos.ToArray(); tBuscar = 0.5f; }

        // Escucha la mochila del personaje para mostrar lo que va entrando
        Inventario inv = Personaje.Instancia != null ? Personaje.Instancia.GetComponent<Inventario>() : null;
        if (inv != invEscuchado)
        {
            if (invEscuchado != null) invEscuchado.alRecibir -= Recibido;
            invEscuchado = inv;
            if (inv != null) inv.alRecibir += Recibido;
        }
        for (int i = recibidos.Count - 1; i >= 0; i--)
        {
            Recibo rc = recibidos[i];
            rc.t -= Time.deltaTime;
            if (rc.t <= 0f) recibidos.RemoveAt(i); else recibidos[i] = rc;
        }
    }

    // ---------- Lo que acaba de entrar a la mochila ----------
    struct Recibo { public string tipo; public int n; public float t; }
    readonly List<Recibo> recibidos = new List<Recibo>();
    Inventario invEscuchado;
    const float DuracionRecibo = 2.5f;

    void Recibido(string tipo, int n)
    {
        for (int i = 0; i < recibidos.Count; i++)
            if (recibidos[i].tipo == tipo) { Recibo r = recibidos[i]; r.n += n; r.t = DuracionRecibo; recibidos[i] = r; return; }
        recibidos.Add(new Recibo { tipo = tipo, n = n, t = DuracionRecibo });
        if (recibidos.Count > 6) recibidos.RemoveAt(0);
    }

    bool RecienRecibido(string tipo)
    {
        foreach (Recibo r in recibidos) if (r.tipo == tipo) return true;
        return false;
    }

    void OnGUI()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Estilos();
        Escala = Mathf.Clamp(Screen.height / 720f, 0.8f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(Escala, Escala, 1f));
        GUI.depth = 0;

        Fabricacion fab = p.GetComponent<Fabricacion>();
        bool menu = Fabricacion.MenuAbierto && fab != null;

        Golpe(p);
        Navegacion nav = Navegacion.Instancia;
        if (nav != null && Navegacion.MapaAbierto)
        {
            nav.DibujarMapa(p);
            string avisoMapa = p.AvisoActual;
            if (avisoMapa != null) Pastilla(Ancho / 2f, 14f, Col(avisoMapa, Ambar), Normal, Ancho - 80f);
            GUI.matrix = Matrix4x4.identity;
            return;
        }
        EstadoJugador(p);
        if (menu) fab.DibujarMenu();
        else
        {
            if (nav != null) nav.DibujarBrujula(p);
            if (!AyudaVisible) Mochila(p);
            TuBarco(p);
            Enemigo(p);
            Indicacion(p);
            if (AyudaVisible) Ayuda();
        }
        string aviso = p.AvisoActual;
        if (aviso != null) Pastilla(Ancho / 2f, menu ? 14f : 78f, Col(aviso, Ambar), Normal, Ancho - 480f);   // debajo de la brújula
        GUI.matrix = Matrix4x4.identity;
    }

    // Arriba izquierda: vida, hambre y sed
    void EstadoJugador(Personaje p)
    {
        if (ModoCreativo.Activo)
        {
            Rect rc = new Rect(12f, 116f, 220f, 22f);
            Relleno(rc, new Color(Oro.r, Oro.g, Oro.b, 0.9f));
            GUI.Label(rc, Col("<b>MODO CREATIVO</b>   F2 quitar", new Color(0.1f, 0.08f, 0.05f)), Centrado);
        }
        Supervivencia s = p.GetComponent<Supervivencia>();
        Rect r = new Rect(12f, 12f, 220f, 98f);
        Caja(r);
        float y = r.y + 10f, w = r.width - 20f;
        Barra(new Rect(r.x + 10f, y, w, 18f), p.Vida / p.vidaMaxima, ColVida, "Vida", Mathf.CeilToInt(p.Vida).ToString());
        y += 22f;
        if (s != null)
        {
            Barra(new Rect(r.x + 10f, y, w, 18f), s.Hambre / 100f, ColHambre, "Hambre" + (s.Hambre <= 0f ? Col("  ¡comida!", Mal) : ""), Mathf.CeilToInt(s.Hambre).ToString());
            y += 22f;
            Barra(new Rect(r.x + 10f, y, w, 18f), s.Sed / 100f, ColSed, "Sed" + (s.Sed <= 0f ? Col("  ¡agua!", Mal) : ""), Mathf.CeilToInt(s.Sed).ToString());
            y += 22f;
        }
        GUI.Label(new Rect(r.x + 10f, y, w, 16f), p.TextoModo(), Pequeno);
    }

    // Abajo izquierda: lo que llevas encima (solo lo que tienes)
    static readonly string[] OrdenMochila =
    {
        Inventario.Oro,
        Inventario.Madera, Inventario.Palo, Inventario.Fibra, Inventario.Cuerda, Inventario.Piedra,
        Inventario.Mineral, Inventario.Lingote, Inventario.Canon, Inventario.Coco, Inventario.Fruta
    };
    readonly List<string> filas = new List<string>();

    void Mochila(Personaje p)
    {
        Inventario inv = p.GetComponent<Inventario>();
        if (inv == null) return;
        filas.Clear();
        foreach (string t in OrdenMochila) if (inv.Cantidad(t) > 0) filas.Add(t);
        string herramientas = ModoCreativo.Activo ? "todas (creativo)" : (inv.Tiene(Inventario.Hacha) ? "Hacha  " : "") + (inv.Tiene(Inventario.Pico) ? "Pico" : "");

        const float fila = 17f;
        float alto = 34f + Mathf.Max(1, filas.Count) * fila + 24f;
        Rect r = new Rect(12f, Alto - alto - 12f, 220f, alto);
        Caja(r);
        GUI.Label(new Rect(r.x + 10f, r.y + 8f, 200f, 18f), "Mochila", Titulo);
        float y = r.y + 30f;
        if (filas.Count == 0) { GUI.Label(new Rect(r.x + 10f, y, 200f, fila), "vacía", Pequeno); y += fila; }
        foreach (string t in filas)
        {
            bool comida = t == Inventario.Coco || t == Inventario.Fruta;
            GUI.Label(new Rect(r.x + 10f, y, 150f, fila), comida ? t + Col("  C comer", Tenue) : t, Normal);
            // Lo que acaba de subir se resalta en verde
            GUI.Label(new Rect(r.x + 10f, y, 200f, fila), Col(inv.Cantidad(t).ToString(), RecienRecibido(t) ? Bien : Oro), Derecha);
            y += fila;
        }
        // Encima de la mochila: "+4 madera" de lo que va entrando, y se desvanece
        float yr = r.y - 6f;
        for (int i = recibidos.Count - 1; i >= 0; i--)
        {
            Recibo rc = recibidos[i];
            yr -= 20f;
            float a = Mathf.Clamp01(rc.t / 0.6f);
            Rect pr = new Rect(12f, yr, 150f, 18f);
            Relleno(pr, new Color(Fondo.r, Fondo.g, Fondo.b, 0.75f * a));
            GUI.Label(new Rect(pr.x + 8f, pr.y, pr.width - 8f, pr.height), Col("+" + rc.n + " ", new Color(Bien.r, Bien.g, Bien.b, a)) + Col(rc.tipo.ToLower(), new Color(Texto.r, Texto.g, Texto.b, a)), Normal);
        }
        Relleno(new Rect(r.x + 10f, y + 3f, 200f, 1f), FondoClaro);
        GUI.Label(new Rect(r.x + 10f, y + 6f, 200f, 16f), herramientas == "" ? "Herramientas: tus manos" : "Herramientas: " + Col(herramientas, Texto), Pequeno);
    }

    // Abajo derecha: el barco en el que vas
    void TuBarco(Personaje p)
    {
        if (p.estado == Personaje.Estado.Libre || p.BarcoActual == null) return;
        ControlBarco b = p.BarcoActual;
        VidaBarco v = b.GetComponent<VidaBarco>();
        Canones cn = b.GetComponent<Canones>();
        Bodega bd = b.GetComponent<Bodega>();
        PiezasBarco pz = b.GetComponent<PiezasBarco>();
        Rigidbody rb = b.GetComponent<Rigidbody>();

        bool proaPopa = cn != null && cn.RanurasLado(LadoCanon.Proa) + cn.RanurasLado(LadoCanon.Popa) > 0;
        float alto = 38f + 22f + 17f * 2f + (cn != null ? 17f : 0f) + (proaPopa ? 17f : 0f) + (bd != null ? 17f : 0f) + 12f;
        Rect r = new Rect(Ancho - 292f, Alto - alto - 12f, 280f, alto);
        Caja(r);
        bool propio = b == p.barco;
        GUI.Label(new Rect(r.x + 10f, r.y + 8f, 240f, 18f), b.name + (propio ? "" : Col("  (no es tuyo)", Tenue)), Titulo);
        if (pz != null && pz.NivelTotal > 0)
            GUI.Label(new Rect(r.x + 10f, r.y + 8f, 240f, 18f), Col("casco +" + pz.nivelCasco + "  velas +" + pz.nivelVelas, Tenue), Derecha);

        float y = r.y + 32f, w = r.width - 20f;
        if (v != null)
            Barra(new Rect(r.x + 10f, y, w, 18f), v.vida / v.vidaMaxima, ColCasco, v.Hundido ? "Hundiéndose" : "Casco", Mathf.CeilToInt(v.vida) + " / " + Mathf.RoundToInt(v.vidaMaxima));
        y += 24f;

        string velas = "";
        for (int i = 1; i <= b.nivelesVela; i++) velas += i <= b.NivelVelaActual ? "■ " : "□ ";
        if (b.EnReversa) velas = "reversa ◄ ";
        GUI.Label(new Rect(r.x + 10f, y, w, 17f), "Velas  " + Col(velas, Oro) + (b.Anclado ? Col("  ANCLADO", Ambar) : ""), Normal);
        y += 17f;

        float nudos = rb != null ? Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude * 1.944f : 0f;
        float rumbo = (b.transform.eulerAngles.y + 360f) % 360f;
        GUI.Label(new Rect(r.x + 10f, y, w, 17f), Col(nudos.ToString("0.0"), Oro) + " nudos   rumbo " + Col(rumbo.ToString("000") + "° " + Rosa(rumbo), Oro), Normal);
        y += 17f;

        if (cn != null)
        {
            string Lado(LadoCanon l, string tecla)
            {
                if (cn.RanurasLado(l) == 0) return "";
                float t = cn.Recarga(l);
                string cuantos = l == LadoCanon.Popa ? (cn.EnLado(l) > 0 ? "x" + cn.BarrilesPorTanda : "0") : cn.EnLado(l) + "/" + cn.RanurasLado(l);
                return Col(tecla, Oro) + " " + cuantos + " "
                    + (cn.EnLado(l) == 0 ? Col("vacío", Tenue) : t <= 0f ? Col("listo", Bien) : Col(t.ToString("0.0") + "s", Ambar)) + "   ";
            }
            GUI.Label(new Rect(r.x + 10f, y, w, 17f), "Cañones  " + Lado(LadoCanon.Babor, "Babor") + Lado(LadoCanon.Estribor, "Estribor"), Normal);
            y += 17f;
            if (cn.RanurasLado(LadoCanon.Proa) + cn.RanurasLado(LadoCanon.Popa) > 0)
            {
                GUI.Label(new Rect(r.x + 10f, y, w, 17f), "              " + Lado(LadoCanon.Proa, "Proa") + Lado(LadoCanon.Popa, "Barriles"), Normal);
                y += 17f;
            }
        }
        if (bd != null)
            GUI.Label(new Rect(r.x + 10f, y, w, 17f), "Bodega " + bd.Total + "/" + bd.capacidad + Col("   " + bd.Cantidad(Inventario.Coco) + " cocos, " + bd.Cantidad(Inventario.Fruta) + " frutas", Tenue), Normal);
    }

    static string Rosa(float grados)
    {
        string[] p = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
        return p[Mathf.RoundToInt(grados / 45f) % 8];
    }

    // Arriba derecha: el barco enemigo más cercano (si está a la vista)
    void Enemigo(Personaje p)
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
        if (cerca == null) return;
        VidaBarco v = cerca.GetComponent<VidaBarco>();
        Rect r = new Rect(Ancho - 232f, 12f, 220f, 92f);
        Caja(r);
        GUI.Label(new Rect(r.x + 10f, r.y + 8f, 200f, 18f), Col(cerca.name, Mal), Titulo);
        float dist = Vector3.Distance(cerca.transform.position, p.transform.position);
        GUI.Label(new Rect(r.x + 10f, r.y + 8f, 200f, 18f), Col(Mathf.RoundToInt(dist) + " m", Oro), Derecha);
        if (v != null)
            Barra(new Rect(r.x + 10f, r.y + 32f, 200f, 18f), v.vida / v.vidaMaxima, ColCasco, "Casco", Mathf.CeilToInt(v.vida).ToString());
        string trip = "";
        for (int i = 0; i < cerca.tripulacion; i++) trip += i < cerca.TripulantesVivos() ? "● " : "○ ";
        GUI.Label(new Rect(r.x + 10f, r.y + 56f, 200f, 17f), "Tripulación  " + Col(trip, Mal), Normal);
        GUI.Label(new Rect(r.x + 10f, r.y + 56f, 200f, 17f), Col(cerca.EstadoTexto, Ambar), Derecha);
    }

    // Abajo centro: qué puedes hacer ahora mismo
    void Indicacion(Personaje p)
    {
        float maxAncho = Mathf.Max(260f, Ancho - 2f * 304f);
        Pastilla(Ancho / 2f, Alto - 62f, p.TextoAyuda(), Normal, maxAncho);
        Rect pista = new Rect(Ancho / 2f - 95f, Alto - 30f, 190f, 20f);
        Relleno(pista, new Color(Fondo.r, Fondo.g, Fondo.b, 0.7f));
        GUI.Label(pista, Col("Tab", Oro) + " fabricar     " + Col("F1", Oro) + " controles", new GUIStyle(Pequeno) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Texto } });
    }

    // Borde rojo cuando te golpean
    void Golpe(Personaje p)
    {
        float g = p.Golpe;
        if (g <= 0f) return;
        Color c = new Color(0.85f, 0.1f, 0.08f, 0.45f * g);
        float b = 18f;
        Relleno(new Rect(0, 0, Ancho, b), c);
        Relleno(new Rect(0, Alto - b, Ancho, b), c);
        Relleno(new Rect(0, b, b, Alto - 2f * b), c);
        Relleno(new Rect(Ancho - b, b, b, Alto - 2f * b), c);
    }

    // F1: controles
    static readonly string[][] Controles =
    {
        new[] { "A pie", "WASD caminar o nadar · Shift correr · Espacio saltar · F recoger a mano (fibra, ramas, piedras, frutas) · Clic golpear con hacha o pico · C comer · F subir al barco / tomar el timón" },
        new[] { "Al timón", "W/S subir o bajar velas · A/D girar · R ancla · F soltar" },
        new[] { "Cañones", "Mantén clic derecho y mira hacia un lado (o al frente, o atrás): ves por dónde irán las balas. La cámara sigue la zona de caída. Mouse hacia el cielo: arco alto y lejos; hacia el barco: al agua, cerca. Clic izquierdo dispara. Q y E disparan babor y estribor directo. Más cañones en un lado = recarga un poco más lenta (máximo el doble), y disparan en ola de popa a proa" },
        new[] { "Interiores", "En cubierta, F junto a la escotilla baja a las cubiertas de cañones y a la bodega (F al pie de la escalera sube). F frente a la puerta del castillo de popa entra al camarote del capitán. Adentro la cámara va en primera persona" },
        new[] { "Proa y popa", "Proa: 2 cañones en cubierta que solo disparan al frente, y el espolón: embiste a otro barco yendo rápido y le haces mucho daño (más si eres más grande). Popa: mira atrás y haz clic para soltar un barril en llamas; flota y explota cuando lo toca otro barco o una bala" },
        new[] { "Combate", "Clic espada (cerca de un enemigo)" },
        new[] { "Fabricar", "Tab abrir o cerrar · Q/E cambiar página (Fabricar, Tu barco, Cañones y bodega) · números hacer · Shift + número hace 5 · Esc cierra" },
        new[] { "Navegar", "Brújula arriba: islas descubiertas (verde), tu barco (dorado), enemigos (rojo). M abre la carta de navegación" },
        new[] { "Cámara", "Mover el mouse mirar · Rueda acercar o alejar (al timón: desde el timón hasta ver el barco pequeñito) · Esc soltar el cursor · Clic volver a capturarlo" },
        new[] { "Creativo", "F2 activar o quitar: todo gratis, todas las herramientas, sin daño ni hambre. En Tab aparece la página Creativo: llenar de cañones, reparar, saltar de molde, traer o reiniciar el enemigo, mar en calma, +100 de todo" },
        new[] { "Pintura", "A bordo de tu barco, tecla P: el Taller. Pinta cada parte (casco, cubierta, castillo, madera, velas, bandera, cañones, adornos) con cualquier color, dibujos en velas y bandera (franjas, cruz, calavera, sol...) y elige el mascarón. Se paga al guardar (5 de oro por parte). Se conserva al mejorar el barco y al guardar la partida" },
        new[] { "Piratas", "Algunos te cañonean de costado y otros van directo a abordarte: cuando te alcanzan, sus piratas saltan a tu cubierta. Suelta el timón (F) y pelea con la espada (clic). Sin tripulación, su barco queda para capturar" },
        new[] { "Mercado", "Algunas islas tienen un mercader en la playa (la inicial siempre). Junto a él, Tab abre Vender y Comprar. Cada isla paga distinto: lleva lo barato de una a donde es caro. Si vendes mucho de lo mismo, el precio baja un rato" },
        new[] { "Botín", "Hunde un pirata y suelta cajas que flotan: pasa cerca (a pie, nadando o con tu barco) y se recogen solas. Si lo capturas (sin tripulación), todo su botín va a tu mochila" },
        new[] { "Partida", "Se guarda sola cada 30 s y al salir · F5 guardar ya · F8 borrar la partida (al volver a entrar empiezas de cero)" },
        new[] { "Prueba", "1 dañar al pirata más cercano · 2/3 traerlo · 4 materiales · 5–0 y H ir a un recurso · B a tu barco · T puntería fija · J ir a una isla nueva · K tu barco 800 m más lejos · L ir a la caja de botín más cercana · N ir al mercader más cercano · V un pirata viene a abordarte" },
    };

    void Ayuda()
    {
        float w = Mathf.Min(600f, Ancho - 40f);
        GUIStyle texto = new GUIStyle(Normal) { wordWrap = true };
        float alto = 44f;
        foreach (var c in Controles) alto += texto.CalcHeight(new GUIContent(c[1]), w - 120f) + 8f;
        Rect r = new Rect(Ancho / 2f - w / 2f, Alto / 2f - alto / 2f - 20f, w, alto);
        Caja(r);
        GUI.Label(new Rect(r.x + 14f, r.y + 10f, w - 28f, 20f), "Controles  " + Col(Version.Texto, Tenue), Titulo);
        GUI.Label(new Rect(r.x + 14f, r.y + 10f, w - 28f, 20f), Col("F1 cerrar", Tenue), Derecha);
        float y = r.y + 38f;
        foreach (var c in Controles)
        {
            float h = texto.CalcHeight(new GUIContent(c[1]), w - 120f);
            GUI.Label(new Rect(r.x + 14f, y, 90f, h), Col(c[0], Oro), Normal);
            GUI.Label(new Rect(r.x + 106f, y, w - 120f, h), c[1], texto);
            y += h + 8f;
        }
    }
}
