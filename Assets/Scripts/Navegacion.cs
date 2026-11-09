using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Navegación: brújula arriba de la pantalla (como en Black Flag) y mapa con la tecla M.
// Las islas se descubren al acercarse; solo las descubiertas salen en el mapa y en la brújula.
// Lo dibuja Hud (para que nada se tape); aquí está el estado y el dibujo.
public class Navegacion : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("Navegación").AddComponent<Navegacion>();

    public static Navegacion Instancia { get; private set; }
    // La carta de navegación ahora es una ventana (VentanaMapa, tecla M)
    public static bool MapaAbierto => Ventanas.EstaAbierta<VentanaMapa>();

    // Marcas que el jugador pone en la carta (se guardan con la partida)
    [System.Serializable] public class Marca { public string nombre; public Vector3 pos; public int icono; }
    public readonly List<Marca> Marcas = new List<Marca>();

    const float DistanciaDescubrir = 160f;   // metros desde la orilla
    const float AnchoBrujula = 440f;
    const float GradosVisibles = 180f;       // la brújula muestra media vuelta

    // Lo que se sabe de cada isla descubierta (sigue en el mapa aunque la isla se descargue al alejarte)
    public struct Conocida { public string nombre; public Vector3 centro; public float orilla; public bool mercado; }
    readonly Dictionary<string, Conocida> descubiertas = new Dictionary<string, Conocida>();
    const float DistanciaBrujula = 2500f;   // la brújula muestra islas conocidas hasta esta distancia
    static Texture2D circulo, flecha;

    // Zoom y desplazamiento del mapa
    const float RadioInicial = 600f;   // al abrir, el mapa muestra ~600 m alrededor de ti
    const float RadioMin = 60f;
    const float RadioMax = 20000f;
    float radioVista = RadioInicial;   // metros del centro al borde del mapa
    Vector3 desplazamiento;            // cuánto se movió la vista respecto a ti (arrastrando)
    Rect marUltimo;                    // zona de agua del último dibujo (para saber dónde está el mouse)
    bool arrastrando;

    void Awake() => Instancia = this;

    void Update()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Vector3 pos = p.transform.position;
        foreach (Isla isla in Isla.Todas)
        {
            if (isla == null || descubiertas.ContainsKey(isla.Id)) continue;
            float d = Plano(isla.transform.position - pos).magnitude - isla.orilla;
            if (d < DistanciaDescubrir)
            {
                bool mercado = isla.GetComponentInChildren<Mercader>() != null;
                descubiertas[isla.Id] = new Conocida { nombre = isla.name, centro = isla.transform.position, orilla = isla.orilla, mercado = mercado };
                if (Time.timeSinceLevelLoad > 2f) Notificaciones.Mostrar("Descubriste " + isla.name + (mercado ? " · tiene mercader" : ""), Notificaciones.Tipo.Descubrimiento, 5f);   // la isla donde apareces no se anuncia
            }
        }
    }

    float EscalaMapa(Rect mar) => Mathf.Min(mar.width, mar.height) / 2f / radioVista;   // px por metro

    // Rueda o +/- : zoom hacia donde apunta el mouse. Arrastrar con clic izquierdo: mover. Clic derecho: volver a ti.
    void ControlMapa(Keyboard k)
    {
        if (marUltimo.width < 1f) return;
        Mouse m = Mouse.current;
        Vector2 cursor = marUltimo.center;
        bool dentro = false;
        if (m != null)
        {
            Vector2 sp = m.position.ReadValue();
            cursor = new Vector2(sp.x, Screen.height - sp.y) / Hud.Escala;   // a coordenadas del HUD
            dentro = marUltimo.Contains(cursor);
        }

        float pasos = 0f;
        if (m != null && dentro)
        {
            float rueda = m.scroll.ReadValue().y;
            if (Mathf.Abs(rueda) > 0.01f) pasos += Mathf.Abs(rueda) >= 20f ? rueda / 120f : rueda;   // 1 o 120 por paso según el sistema
        }
        if (k != null)
        {
            if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame) pasos += 1f;
            if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame) pasos -= 1f;
        }
        if (Mathf.Abs(pasos) > 0.001f)
        {
            // El punto bajo el mouse se queda quieto mientras se acerca o aleja
            Vector2 ancla = dentro ? cursor : marUltimo.center;
            Vector2 d = ancla - marUltimo.center;
            float escAntes = EscalaMapa(marUltimo);
            Vector3 mundo = desplazamiento + new Vector3(d.x, 0f, -d.y) / escAntes;
            radioVista = Mathf.Clamp(radioVista * Mathf.Pow(0.8f, pasos), RadioMin, RadioMax);
            desplazamiento = mundo - new Vector3(d.x, 0f, -d.y) / EscalaMapa(marUltimo);
        }

        if (m == null) return;
        if (m.leftButton.wasPressedThisFrame && dentro) arrastrando = true;
        if (!m.leftButton.isPressed) arrastrando = false;
        if (arrastrando)
        {
            Vector2 delta = m.delta.ReadValue() / Hud.Escala;   // en pantalla, y crece hacia arriba
            float esc = EscalaMapa(marUltimo);
            desplazamiento -= new Vector3(delta.x, 0f, delta.y) / esc;
        }
        if (m.rightButton.wasPressedThisFrame && dentro) desplazamiento = Vector3.zero;
    }

    public IEnumerable<KeyValuePair<string, Conocida>> Descubiertas => descubiertas;
    public void Recordar(string id, Conocida c) => descubiertas[id] = c;   // al cargar la partida
    public bool Descubierta(Isla i) => i != null && descubiertas.ContainsKey(i.Id);
    public int CantidadDescubiertas => descubiertas.Count;

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
    static float Rumbo(Vector3 v) => (Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg + 360f) % 360f;
    static float Diferencia(float a, float b) => Mathf.DeltaAngle(b, a);

    // ---------------- Brújula ----------------
    public void DibujarBrujula(Personaje p)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float rumbo = Rumbo(Plano(cam.transform.forward));
        float W = Hud.Ancho, cx = W / 2f, ancho = Mathf.Min(AnchoBrujula, W - 520f);
        if (ancho < 200f) ancho = Mathf.Min(AnchoBrujula, W - 40f);
        Rect r = new Rect(cx - ancho / 2f, 10f, ancho, 34f);
        Hud.Caja(r);
        float porGrado = ancho / GradosVisibles;

        // Marcas cada 15°, letras cada 45°
        string[] letras = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
        for (int g = 0; g < 360; g += 15)
        {
            float dx = Diferencia(g, rumbo);
            if (Mathf.Abs(dx) > GradosVisibles / 2f - 4f) continue;
            float x = cx + dx * porGrado;
            if (g % 45 == 0)
            {
                bool norte = g == 0;
                GUI.Label(new Rect(x - 16f, r.y + 2f, 32f, 18f), Hud.Col(letras[g / 45], norte ? Hud.Mal : Hud.Oro), Hud.Centrado);
            }
            else Hud.Relleno(new Rect(x - 0.5f, r.y + 6f, 1f, 7f), Hud.Tenue);
        }

        // Marcadores: islas descubiertas, tu barco (si no vas en él) y el enemigo cercano
        foreach (Conocida isla in descubiertas.Values)
        {
            Vector3 v = Plano(isla.centro - p.transform.position);
            if (v.magnitude > DistanciaBrujula) continue;
            // Las islas con mercader salen con una moneda dorada
            Marcador(cx, porGrado, r, rumbo, Rumbo(v), isla.mercado ? "◆" : "●", isla.mercado ? Hud.Oro : Hud.Bien, Mathf.RoundToInt(Mathf.Max(0f, v.magnitude - isla.orilla)) + " m" + (isla.mercado ? " · mercado" : ""));
        }
        if (p.barco != null && p.BarcoActual != p.barco)
        {
            Vector3 v = Plano(p.barco.transform.position - p.transform.position);
            Marcador(cx, porGrado, r, rumbo, Rumbo(v), "▲", Hud.Oro, Mathf.RoundToInt(v.magnitude) + " m");
        }
        foreach (IABarco e in IABarco.Todos)
        {
            Vector3 v = Plano(e.transform.position - p.transform.position);
            if (v.magnitude > 600f) continue;
            Marcador(cx, porGrado, r, rumbo, Rumbo(v), "▼", Hud.Mal, Mathf.RoundToInt(v.magnitude) + " m");
        }

        // Aguja central y rumbo en grados
        Hud.Relleno(new Rect(cx - 1f, r.y, 2f, r.height), Hud.Texto);
        GUI.Label(new Rect(cx - 30f, r.yMax + 1f, 60f, 16f), Hud.Col(Mathf.RoundToInt(rumbo).ToString("000") + "°", Hud.Texto), new GUIStyle(Hud.Pequeno) { alignment = TextAnchor.MiddleCenter });
    }

    static void Marcador(float cx, float porGrado, Rect r, float rumbo, float hacia, string signo, Color color, string distancia)
    {
        float dx = Diferencia(hacia, rumbo);
        bool fuera = Mathf.Abs(dx) > GradosVisibles / 2f - 6f;
        dx = Mathf.Clamp(dx, -(GradosVisibles / 2f - 6f), GradosVisibles / 2f - 6f);   // si queda atrás, se pega al borde
        float x = cx + dx * porGrado;
        GUI.Label(new Rect(x - 10f, r.y + 15f, 20f, 18f), Hud.Col(signo, fuera ? new Color(color.r, color.g, color.b, 0.45f) : color), Hud.Centrado);
        if (!fuera && Mathf.Abs(dx) < 12f)
            GUI.Label(new Rect(x - 60f, r.yMax + 15f, 120f, 14f), Hud.Col(distancia, color), new GUIStyle(Hud.Pequeno) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });
    }

    // ---------------- Mapa ----------------
    // Sin enemigos a propósito: el mapa es una carta, no un radar (los ves con tus ojos o el catalejo).
    readonly List<Conocida> ordenadas = new List<Conocida>();
    readonly List<Rect> etiquetas = new List<Rect>();
    static readonly float[] Pasos = { 25f, 50f, 100f, 250f, 500f, 1000f, 2500f, 5000f, 10000f };

    public void DibujarMapa(Personaje p)
    {
        Texturas();
        float W = Hud.Ancho, H = Hud.Alto;
        float lado = Mathf.Min(W - 40f, H - 40f);
        Rect marco = new Rect(W / 2f - lado / 2f, H / 2f - lado / 2f, lado, lado);
        Hud.Caja(marco);
        Rect mar = new Rect(marco.x + 12f, marco.y + 40f, marco.width - 24f, marco.height - 64f);
        marUltimo = mar;
        Hud.Relleno(mar, new Color(0.12f, 0.32f, 0.42f, 0.95f));
        GUI.Label(new Rect(marco.x + 14f, marco.y + 10f, 300f, 22f), "Carta de navegación", Hud.Titulo);
        GUI.Label(new Rect(marco.x, marco.y + 10f, marco.width - 14f, 22f), Hud.Col("M o Esc cerrar", Hud.Tenue), Hud.Derecha);

        Vector3 yo = p.transform.position;
        Vector3 vista = yo + desplazamiento;            // centro de lo que se ve
        float escala = EscalaMapa(mar);                 // px por metro
        Vector2 centro = mar.center;
        Vector2 APantalla(Vector3 mundo) { Vector3 v = mundo - vista; return centro + new Vector2(v.x, -v.z) * escala; }

        // Paso de cuadrícula y de la barra de escala: el que mida entre ~60 y ~150 px
        float paso = Pasos[Pasos.Length - 1];
        foreach (float s in Pasos) if (s * escala >= 60f) { paso = s; break; }

        // Todo lo del mar se recorta al borde del agua
        GUI.BeginGroup(mar);
        Vector2 o = mar.position;

        Color linea = new Color(1f, 1f, 1f, 0.06f);
        float mx = mar.width / 2f / escala, mz = mar.height / 2f / escala;
        for (float gx = Mathf.Floor((vista.x - mx) / paso) * paso; gx <= vista.x + mx; gx += paso)
            Hud.Relleno(new Rect(centro.x + (gx - vista.x) * escala - o.x, 0f, 1f, mar.height), linea);
        for (float gz = Mathf.Floor((vista.z - mz) / paso) * paso; gz <= vista.z + mz; gz += paso)
            Hud.Relleno(new Rect(0f, centro.y - (gz - vista.z) * escala - o.y, mar.width, 1f), linea);

        // Islas descubiertas: solo las que caen en pantalla
        ordenadas.Clear();
        foreach (Conocida i in descubiertas.Values)
        {
            Vector2 c = APantalla(i.centro);
            float rr = Mathf.Max(3f, i.orilla * escala);
            if (c.x + rr < mar.x || c.x - rr > mar.xMax || c.y + rr < mar.y || c.y - rr > mar.yMax) continue;
            ordenadas.Add(i);
            c -= o;
            Pintar(new Rect(c.x - rr, c.y - rr, rr * 2f, rr * 2f), circulo, new Color(0.86f, 0.78f, 0.55f));
            Pintar(new Rect(c.x - rr * 0.6f, c.y - rr * 0.6f, rr * 1.2f, rr * 1.2f), circulo, new Color(0.35f, 0.6f, 0.3f));
            if (i.mercado)   // moneda dorada: aquí hay mercader
            {
                float m = 10f;
                Pintar(new Rect(c.x + rr * 0.7f - m / 2f, c.y - rr * 0.7f - m / 2f, m, m), circulo, new Color(0.15f, 0.1f, 0.02f));
                Pintar(new Rect(c.x + rr * 0.7f - m / 2f + 1.5f, c.y - rr * 0.7f - m / 2f + 1.5f, m - 3f, m - 3f), circulo, Hud.Oro);
            }
        }

        // Nombres: primero los más cercanos al centro; si un nombre pisaría a otro, no se escribe (acerca el zoom para verlo)
        ordenadas.Sort((a, b) => Plano(a.centro - vista).sqrMagnitude.CompareTo(Plano(b.centro - vista).sqrMagnitude));
        etiquetas.Clear();
        GUIStyle estiloNombre = new GUIStyle(Hud.Pequeno) { alignment = TextAnchor.MiddleCenter };
        foreach (Conocida i in ordenadas)
        {
            Vector2 c = APantalla(i.centro) - o;
            float rr = Mathf.Max(3f, i.orilla * escala);
            float w = Mathf.Min(estiloNombre.CalcSize(new GUIContent(i.nombre)).x + 6f, 180f);
            Rect r = new Rect(c.x - w / 2f, c.y + rr + 2f, w, 15f);
            bool choca = false;
            foreach (Rect e in etiquetas) if (e.Overlaps(r)) { choca = true; break; }
            if (choca) continue;
            etiquetas.Add(r);
            GUI.Label(r, Hud.Col(i.nombre, Hud.Texto), estiloNombre);
        }
        GUI.EndGroup();

        // Tu barco (si no vas en él) y tú; si quedan fuera de la vista, se pegan al borde
        Vector2 Borde(Vector2 v) => new Vector2(Mathf.Clamp(v.x, mar.x + 10f, mar.xMax - 10f), Mathf.Clamp(v.y, mar.y + 10f, mar.yMax - 10f));
        if (p.barco != null && p.BarcoActual != p.barco)
        {
            Vector2 b = Borde(APantalla(p.barco.transform.position));
            Flecha(b, p.barco.transform.eulerAngles.y, 20f, Hud.Oro);
            GUI.Label(new Rect(b.x - 60f, b.y + 8f, 120f, 14f), Hud.Col("tu barco", Hud.Oro), new GUIStyle(Hud.Pequeno) { alignment = TextAnchor.MiddleCenter });
        }
        float rumboYo = p.BarcoActual != null && p.estado != Personaje.Estado.Libre ? p.BarcoActual.transform.eulerAngles.y : p.transform.eulerAngles.y;
        Flecha(Borde(APantalla(yo)), rumboYo, 26f, Color.white);

        // Norte y escala
        GUI.Label(new Rect(mar.xMax - 30f, mar.y + 6f, 24f, 18f), Hud.Col("N ↑", Hud.Mal), Hud.Centrado);
        float barra = paso * escala;
        Hud.Relleno(new Rect(mar.x + 10f, mar.yMax - 14f, barra, 2f), Hud.Texto);
        string textoPaso = paso >= 1000f ? (paso / 1000f) + " km" : paso + " m";
        GUI.Label(new Rect(mar.x + 10f, mar.yMax - 30f, 120f, 14f), Hud.Col(textoPaso, Hud.Texto), Hud.Pequeno);
        GUI.Label(new Rect(mar.x, mar.yMax - 30f, mar.width - 10f, 14f),
            Hud.Col("rueda o +/- zoom · arrastrar mover · clic der. centrar", Hud.Tenue), new GUIStyle(Hud.Pequeno) { alignment = TextAnchor.MiddleRight });

        // Leyenda
        GUI.Label(new Rect(marco.x + 14f, marco.yMax - 22f, marco.width - 28f, 16f),
            Hud.Col("▲", Color.white) + " tú   " + Hud.Col("▲", Hud.Oro) + " tu barco   " +
            Hud.Col("●", Hud.Bien) + " islas descubiertas: " + descubiertas.Count + "   " + Hud.Col("●", Hud.Oro) + " mercader", Hud.Pequeno);
    }

    static void Pintar(Rect r, Texture2D t, Color c)
    {
        Color antes = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, t);
        GUI.color = antes;
    }

    // Flecha que apunta según el rumbo (0 = norte, hacia arriba en el mapa)
    static void Flecha(Vector2 c, float rumbo, float tam, Color color)
    {
        Matrix4x4 antes = GUI.matrix;
        GUIUtility.RotateAroundPivot(rumbo, c * Hud.Escala);   // el pivote va en píxeles de pantalla
        Pintar(new Rect(c.x - tam / 2f, c.y - tam / 2f, tam, tam), flecha, color);
        GUI.matrix = antes;
    }

    // Texturas hechas por código: un círculo y una flecha
    static void Texturas()
    {
        if (circulo != null) return;
        const int n = 64;
        circulo = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        flecha = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
            float a = Mathf.Clamp01((1f - Mathf.Sqrt(u * u + v * v)) * n / 2f);
            circulo.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            // Flecha hacia arriba (y de la textura crece hacia arriba)
            float ancho = (1f - v) * 0.45f;                      // punta arriba, base ancha abajo
            bool dentro = v > -0.85f && Mathf.Abs(u) < ancho && !(v < -0.35f && Mathf.Abs(u) < (-0.35f - v) * 0.9f);   // muesca atrás
            flecha.SetPixel(x, y, new Color(1f, 1f, 1f, dentro ? 1f : 0f));
        }
        circulo.Apply();
        flecha.Apply();
    }
}
