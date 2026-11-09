using UnityEngine;
using UnityEngine.InputSystem;

// Taller (tecla P, a bordo de tu barco): pinta cada parte con el color que quieras, ponle dibujos a las velas
// y a la bandera, y elige el mascarón de proa. Los cambios se ven al momento; se pagan al guardar
// (5 de oro por parte cambiada, 20 el mascarón; gratis en modo creativo). Cancelar deja todo como estaba.
public class Taller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("Taller").AddComponent<Taller>();

    public static bool Abierto { get; private set; }
    static Taller instancia;
    public static void Abrir() { if (instancia != null) instancia.IntentarAbrir(); }

    const int CostoParte = 5, CostoMascaron = 20;

    Pintura pintura;
    Pintura.Datos antes;
    int sel;                 // parte elegida (o NombresParte.Length = mascarón)
    bool segundo;            // editando el segundo color del dibujo
    Vector2 scroll;
    Texture2D blanco;

    void Awake() => instancia = this;

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (!Abierto)
        {
            if (k.pKey.wasPressedThisFrame && !Fabricacion.MenuAbierto && !Navegacion.MapaAbierto) IntentarAbrir();
            return;
        }
        Personaje p = Personaje.Instancia;
        if (p == null || p.barco == null || pintura == null || p.BarcoActual != p.barco) { Cancelar(); return; }
        if (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame) Cancelar();
    }

    void IntentarAbrir()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        if (p.barco == null || p.estado == Personaje.Estado.Libre || p.BarcoActual != p.barco) { p.Avisar("El taller se abre a bordo de tu barco (tecla P)"); return; }
        pintura = p.barco.GetComponent<Pintura>();
        if (pintura == null) pintura = p.barco.gameObject.AddComponent<Pintura>();
        antes = pintura.datos.Copia();
        Abierto = true;
        CamaraBarco.Orbitar = true;
    }

    void Cerrar() { Abierto = false; CamaraBarco.Orbitar = false; }

    void Cancelar()
    {
        if (pintura != null && antes != null) { pintura.datos = antes.Copia(); pintura.Aplicar(); }
        Cerrar();
    }

    int Costo()
    {
        if (antes == null || pintura == null) return 0;
        int c = 0;
        for (int i = 0; i < Pintura.NombresParte.Length; i++)
            if (!pintura.De((Pintura.Parte)i).Igual(antes.estilos[i])) c += CostoParte;
        if ((pintura.datos.mascaron ?? "") != (antes.mascaron ?? "")) c += CostoMascaron;
        return ModoCreativo.Activo ? 0 : c;
    }

    void Guardar()
    {
        Personaje p = Personaje.Instancia;
        Inventario inv = p != null ? p.GetComponent<Inventario>() : null;
        int c = Costo();
        if (inv != null && c > 0)
        {
            if (inv.Cantidad(Inventario.Oro) < c) { p.Avisar("Te faltan " + (c - inv.Cantidad(Inventario.Oro)) + " de oro"); return; }
            inv.Quitar(Inventario.Oro, c);
        }
        if (p != null) p.Avisar(c > 0 ? "Barco pintado (" + c + " de oro)" : "Barco pintado");
        Cerrar();
    }

    // ---------- Ventana ----------
    void OnGUI()
    {
        if (!Abierto || pintura == null) return;
        if (blanco == null) { blanco = new Texture2D(1, 1); blanco.SetPixel(0, 0, Color.white); blanco.Apply(); }
        GUI.matrix = Matrix4x4.Scale(new Vector3(Hud.Escala, Hud.Escala, 1f));
        GUI.depth = -10;
        float ancho = 430f, alto = 470f;
        Rect r = new Rect(16f, 60f, ancho, alto);
        GUI.color = new Color(0.08f, 0.1f, 0.13f, 0.93f);
        GUI.DrawTexture(r, blanco);
        GUI.color = Color.white;

        GUIStyle titulo = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.8f, 0.45f) } };
        GUIStyle chico = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.8f, 0.82f, 0.85f) } };
        GUI.Label(new Rect(r.x + 12, r.y + 8, ancho, 24), "Taller — " + pintura.name, titulo);
        GUI.Label(new Rect(r.x + 12, r.y + 30, ancho, 18), "Elige una parte y su color. P o Esc cancela.", chico);

        // Partes
        float y = r.y + 54;
        for (int i = 0; i <= Pintura.NombresParte.Length; i++)
        {
            string n = i < Pintura.NombresParte.Length ? Pintura.NombresParte[i] : "Mascarón de proa";
            bool cambiado = i < Pintura.NombresParte.Length ? !pintura.De((Pintura.Parte)i).Igual(antes.estilos[i]) : (pintura.datos.mascaron ?? "") != (antes.mascaron ?? "");
            GUI.color = i == sel ? new Color(1f, 0.85f, 0.45f) : Color.white;
            if (GUI.Button(new Rect(r.x + 12, y, 150, 26), n + (cambiado ? " •" : ""))) { sel = i; segundo = false; }
            GUI.color = Color.white;
            if (i < Pintura.NombresParte.Length)
            {
                Pintura.Estilo e = pintura.De((Pintura.Parte)i);
                GUI.color = e.pintado ? e.color : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                GUI.DrawTexture(new Rect(r.x + 166, y + 5, 16, 16), blanco);
                GUI.color = Color.white;
            }
            y += 30;
        }

        // Panel de la parte elegida
        float px = r.x + 196, py = r.y + 54, pw = ancho - 208;
        if (sel < Pintura.NombresParte.Length) PanelParte((Pintura.Parte)sel, px, py, pw, chico);
        else PanelMascaron(px, py, pw);

        // Abajo: costo, guardar, cancelar
        int c = Costo();
        GUI.Label(new Rect(r.x + 12, r.y + alto - 70, ancho - 24, 20), ModoCreativo.Activo ? "Gratis (modo creativo)" : "Costo: " + c + " de oro  (5 por parte, 20 el mascarón)", chico);
        if (GUI.Button(new Rect(r.x + 12, r.y + alto - 44, 130, 30), "Guardar")) Guardar();
        if (GUI.Button(new Rect(r.x + 150, r.y + alto - 44, 130, 30), "Cancelar")) Cancelar();
        if (GUI.Button(new Rect(r.x + 288, r.y + alto - 44, 130, 30), "Todo original"))
        {
            pintura.datos = new Pintura.Datos { mascaron = antes.mascaron };   // colores originales (el mascarón queda como estaba)
            pintura.Aplicar();
        }
    }

    void PanelParte(Pintura.Parte parte, float x, float y, float w, GUIStyle chico)
    {
        Pintura.Estilo e = pintura.De(parte);
        bool dibujo = Pintura.TieneDibujo(parte);
        bool cambio = false, original = false;

        if (GUI.Button(new Rect(x, y, w, 24), e.pintado ? "Volver al color original" : "(color original)")) { e.pintado = false; original = true; }
        y += 30;
        if (dibujo)
        {
            if (GUI.Toggle(new Rect(x, y, w / 2f, 20), !segundo, " Color 1")) segundo = false;
            if (GUI.Toggle(new Rect(x + w / 2f, y, w / 2f, 20), segundo, " Color 2 (dibujo)")) segundo = true;
            y += 22;
        }
        Color actual = segundo && dibujo ? e.color2 : e.color;

        // Paleta: 12 colores en 3 filas
        for (int i = 0; i < Pintura.Paleta.Length; i++)
        {
            Rect c = new Rect(x + (i % 4) * (w / 4f), y + (i / 4) * 26, w / 4f - 4, 22);
            GUI.color = Pintura.Paleta[i].color;
            GUI.DrawTexture(c, blanco);
            GUI.color = Color.white;
            if (GUI.Button(c, "", GUIStyle.none)) { actual = Pintura.Paleta[i].color; cambio = true; }
        }
        y += 82;

        // Color libre: tono, saturación y brillo
        Color.RGBToHSV(actual, out float hh, out float ss, out float vv);
        GUI.Label(new Rect(x, y, 70, 18), "Tono", chico);
        float h2 = GUI.HorizontalSlider(new Rect(x + 70, y + 5, w - 70, 16), hh, 0f, 1f); y += 22;
        GUI.Label(new Rect(x, y, 70, 18), "Color vivo", chico);
        float s2 = GUI.HorizontalSlider(new Rect(x + 70, y + 5, w - 70, 16), ss, 0f, 1f); y += 22;
        GUI.Label(new Rect(x, y, 70, 18), "Brillo", chico);
        float v2 = GUI.HorizontalSlider(new Rect(x + 70, y + 5, w - 70, 16), vv, 0f, 1f); y += 26;
        if (Mathf.Abs(h2 - hh) > 0.001f || Mathf.Abs(s2 - ss) > 0.001f || Mathf.Abs(v2 - vv) > 0.001f) { actual = Color.HSVToRGB(h2, s2, v2); cambio = true; }
        GUI.color = actual;
        GUI.DrawTexture(new Rect(x, y, w, 14), blanco);
        GUI.color = Color.white;
        y += 22;

        if (dibujo)
        {
            GUI.Label(new Rect(x, y, w, 18), "Dibujo", chico); y += 20;
            for (int i = 0; i < Pintura.NombresPatron.Length; i++)
            {
                Rect b = new Rect(x + (i % 4) * (w / 4f), y + (i / 4) * 26, w / 4f - 4, 22);
                GUI.color = e.patron == i ? new Color(1f, 0.85f, 0.45f) : Color.white;
                if (GUI.Button(b, Pintura.NombresPatron[i])) { e.patron = i; cambio = true; }
                GUI.color = Color.white;
            }
        }

        if (cambio)
        {
            if (!e.pintado && !(segundo && dibujo)) e.color = actual;
            if (segundo && dibujo) e.color2 = actual; else e.color = actual;
            e.pintado = true;
        }
        if (cambio || original) pintura.Aplicar();
    }

    void PanelMascaron(float x, float y, float w)
    {
        for (int i = 0; i < Pintura.Mascarones.Length; i++)
        {
            bool elegido = pintura.datos.mascaron == Pintura.Mascarones[i];
            GUI.color = elegido ? new Color(1f, 0.85f, 0.45f) : Color.white;
            if (GUI.Button(new Rect(x, y, w, 28), Pintura.NombresMascaron[i]))
            {
                pintura.datos.mascaron = Pintura.Mascarones[i];
                pintura.Aplicar();
            }
            GUI.color = Color.white;
            y += 34;
        }
    }
}
