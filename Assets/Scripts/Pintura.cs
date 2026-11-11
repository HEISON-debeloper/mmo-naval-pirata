using System.Collections.Generic;
using UnityEngine;

// Pintura y adornos de tu barco, pieza por pieza (se elige en el Taller, tecla P).
// Cada parte (casco, cubierta, castillo, madera, velas, bandera, cañones, adornos dorados) tiene su color;
// las velas y la bandera pueden llevar un dibujo con un segundo color. También el mascarón de proa.
// Se conserva al mejorar o agrandar el barco y al guardar la partida. Cada color es una copia del material
// original, así no se pintan los demás barcos.
public class Pintura : MonoBehaviour
{
    public static readonly (string nombre, Color color)[] Paleta =
    {
        ("Negro", new Color(0.09f, 0.09f, 0.1f)),
        ("Blanco hueso", new Color(0.9f, 0.87f, 0.78f)),
        ("Rojo", new Color(0.72f, 0.14f, 0.12f)),
        ("Rojo vino", new Color(0.4f, 0.08f, 0.1f)),
        ("Azul marino", new Color(0.1f, 0.16f, 0.35f)),
        ("Azul cielo", new Color(0.4f, 0.65f, 0.85f)),
        ("Verde botella", new Color(0.08f, 0.3f, 0.18f)),
        ("Amarillo ocre", new Color(0.8f, 0.6f, 0.18f)),
        ("Café oscuro", new Color(0.28f, 0.17f, 0.09f)),
        ("Gris", new Color(0.45f, 0.46f, 0.48f)),
        ("Dorado", new Color(0.85f, 0.68f, 0.25f)),
        ("Morado", new Color(0.35f, 0.15f, 0.42f)),
    };

    public enum Parte { Casco, Cubierta, Castillo, Madera, Velas, Bandera, Canones, Adornos }
    public static readonly string[] NombresParte = { "Casco", "Cubierta", "Castillo de popa", "Mástiles y madera", "Velas", "Bandera", "Cañones y hierro", "Adornos dorados" };
    // Qué piezas (por nombre) son de cada parte
    static readonly string[][] Piezas =
    {
        new[] { "Casco", "Borda", "Tapas", "Tronco", "Poste" },
        new[] { "Cubierta", "Piso" },
        new[] { "Camarote" },
        new[] { "Mástil", "Verga", "Bauprés", "Botavara", "Detalles", "Marcos", "Rueda", "Pedestal", "Cepo", "Repisa", "Cureña", "Ruedas", "Barras", "Marco", "Tambor", "Escotilla", "Vigas", "Mástil interior" },
        new[] { "Lona" },
        new[] { "Bandera" },
        new[] { "Tubo", "Ancla", "Aros", "Farol" },
        new[] { "Molduras", "Mascaron", "Melena", "Campana", "Espolon", "Collar" },
    };
    public static bool TieneDibujo(Parte p) => p == Parte.Velas || p == Parte.Bandera;

    // Los últimos son banderas de orgullo (con sus colores propios, no usan los dos colores elegidos) y la crisopa
    // (insignia de Luwyn Antena Caída: se desbloquea con ese personaje; el fondo es el primer color).
    public enum Patron { Liso, Franjas, Rayas, Cruz, Diagonal, Borde, Calavera, Sol, Arcoiris, Trans, Bisexual, Lesbiana, NoBinaria, Crisopa }
    public static readonly string[] NombresPatron = { "Liso", "Franjas", "Rayas", "Cruz", "Diagonal", "Borde", "Calavera", "Sol", "Orgullo", "Trans", "Bisexual", "Lesbiana", "No binaria", "Crisopa" };
    public static bool PatronBloqueado(int p) => p == (int)Patron.Crisopa && !Apariencia.LuwynDesbloqueado;

    public static readonly string[] Mascarones = { "mascaron_leon", "mascaron_sirena", "mascaron_dragon", "mascaron_calavera", "mascaron_crisopa", "ninguno" };
    public static readonly string[] NombresMascaron = { "León", "Sirena", "Dragón", "Calavera", "Crisopa", "Sin mascarón" };
    public static bool MascaronBloqueado(string m) => m == "mascaron_crisopa" && !Apariencia.LuwynDesbloqueado;

    [System.Serializable]
    public class Estilo
    {
        public bool pintado;
        public Color color = Color.white;
        public int patron;
        public Color color2 = new Color(0.09f, 0.09f, 0.1f);
        public Estilo Copia() => new Estilo { pintado = pintado, color = color, patron = patron, color2 = color2 };
        public bool Igual(Estilo o) => o != null && pintado == o.pintado && (!pintado || (color == o.color && patron == o.patron && color2 == o.color2));
    }

    [System.Serializable]
    public class Datos
    {
        public Estilo[] estilos = NuevosEstilos();
        public string mascaron = "";
        public Datos Copia()
        {
            var d = new Datos { mascaron = mascaron };
            for (int i = 0; estilos != null && i < estilos.Length && i < d.estilos.Length; i++) d.estilos[i] = estilos[i] != null ? estilos[i].Copia() : new Estilo();
            return d;
        }
    }
    static Estilo[] NuevosEstilos()
    {
        var e = new Estilo[NombresParte.Length];
        for (int i = 0; i < e.Length; i++) e[i] = new Estilo();
        return e;
    }

    public Datos datos = new Datos();

    public Estilo De(Parte p)
    {
        if (datos.estilos == null || datos.estilos.Length < NombresParte.Length)
        {
            var viejos = datos.estilos;
            datos.estilos = NuevosEstilos();
            if (viejos != null) for (int i = 0; i < viejos.Length && i < datos.estilos.Length; i++) datos.estilos[i] = viejos[i] ?? new Estilo();
        }
        if (datos.estilos[(int)p] == null) datos.estilos[(int)p] = new Estilo();
        return datos.estilos[(int)p];
    }

    // Compatibilidad con partidas viejas (índices de la paleta: -1 = original)
    public void DesdePaleta(int casco, int velas, int bandera)
    {
        void Poner(Parte p, int i) { if (i >= 0) { Estilo e = De(p); e.pintado = true; e.color = Paleta[i % Paleta.Length].color; } }
        Poner(Parte.Casco, casco); Poner(Parte.Velas, velas); Poner(Parte.Bandera, bandera);
    }

    public void Copiar(Pintura otra) { datos = otra.datos.Copia(); Aplicar(); }
    public string AJson() => JsonUtility.ToJson(datos);
    public void DeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        datos = JsonUtility.FromJson<Datos>(json) ?? new Datos();
        Aplicar();
    }
    public bool AlgoPintado()
    {
        if (!string.IsNullOrEmpty(datos.mascaron)) return true;
        foreach (Estilo e in datos.estilos) if (e != null && e.pintado) return true;
        return false;
    }

    void Start() => Aplicar();

    // ---------- Aplicar ----------
    readonly Dictionary<Renderer, Material> originales = new Dictionary<Renderer, Material>();
    readonly Dictionary<(int, Material), Material> pintados = new Dictionary<(int, Material), Material>();

    public void Aplicar()
    {
        if (!string.IsNullOrEmpty(datos.mascaron)) Detalles.CambiarMascaron(transform, datos.mascaron);
        VelasSinEstorbo.Olvidar();   // que vuelva a leer los materiales de las velas
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<Tripulante>() != null) continue;
            int parte = ParteDe(r);
            if (parte < 0) continue;
            if (!originales.TryGetValue(r, out Material orig)) { orig = r.sharedMaterial; if (orig == null) continue; originales[r] = orig; }
            Estilo e = De((Parte)parte);
            r.sharedMaterial = e.pintado ? Pintado(parte, orig, e) : orig;
        }
    }

    static int ParteDe(Renderer r)
    {
        string n = r.name;
        for (int i = 0; i < Piezas.Length; i++) if (System.Array.IndexOf(Piezas[i], n) >= 0) return i;
        return -1;
    }

    Material Pintado(int parte, Material orig, Estilo e)
    {
        if (!pintados.TryGetValue((parte, orig), out Material m) || m == null)
        {
            m = new Material(orig) { name = orig.name + " (pintado)" };
            pintados[(parte, orig)] = m;
        }
        bool dibujo = TieneDibujo((Parte)parte) && e.patron > 0;
        Color c = dibujo ? Color.white : e.color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        Texture2D tex = dibujo ? Textura((Patron)e.patron, e.color, e.color2) : null;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        return m;
    }

    void OnDestroy()
    {
        foreach (var m in pintados.Values) if (m != null) Destroy(m);
    }

    // ---------- Dibujos de las velas y la bandera (texturas hechas por código) ----------
    static readonly Dictionary<(int, Color, Color), Texture2D> texturas = new Dictionary<(int, Color, Color), Texture2D>();
    public static Texture2D Textura(Patron p, Color a, Color b)
    {
        if (texturas.TryGetValue(((int)p, a, b), out Texture2D t) && t != null) return t;
        const int N = 128;
        t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "Dibujo " + p, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[N * N];
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float u = (i + 0.5f) / N, v = (j + 0.5f) / N;
                px[j * N + i] = ColorDe(p, u, v, a, b);
            }
        t.SetPixels(px);
        t.Apply(true);
        texturas[((int)p, a, b)] = t;
        return t;
    }

    // Franjas de las banderas de orgullo (de arriba hacia abajo)
    static readonly Color[] FranjasArcoiris = { C(0xE40303), C(0xFF8C00), C(0xFFED00), C(0x008026), C(0x24408E), C(0x732982) };
    static readonly Color[] FranjasTrans = { C(0x5BCEFA), C(0xF5A9B8), C(0xFFFFFF), C(0xF5A9B8), C(0x5BCEFA) };
    static readonly Color[] FranjasLesbiana = { C(0xD52D00), C(0xFF9A56), C(0xFFFFFF), C(0xD362A4), C(0xA30262) };
    static readonly Color[] FranjasNoBinaria = { C(0xFCF434), C(0xFFFFFF), C(0x9C59D1), C(0x2C2C2C) };
    static Color C(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    static Color Franja(Color[] f, float v) => f[Mathf.Clamp(Mathf.FloorToInt((1f - v) * f.Length), 0, f.Length - 1)];

    static Color ColorDe(Patron p, float u, float v, Color a, Color b)
    {
        switch (p)
        {
            case Patron.Arcoiris: return Franja(FranjasArcoiris, v);
            case Patron.Trans: return Franja(FranjasTrans, v);
            case Patron.Lesbiana: return Franja(FranjasLesbiana, v);
            case Patron.NoBinaria: return Franja(FranjasNoBinaria, v);
            case Patron.Bisexual: return v > 0.6f ? C(0xD60270) : v > 0.4f ? C(0x9B4F96) : C(0x0038A8);   // 2 / 1 / 2
            case Patron.Crisopa: return Crisopa(u, v, a);
            default: return Dibujo(p, u, v) ? b : a;
        }
    }

    // Crisopa (Chrysoperla): cuerpo verde y largo, ojos dorados, cuatro alas de encaje con nervaduras y dos antenas
    static Color Crisopa(float u, float v, Color fondo)
    {
        Color verde = C(0x4CAF50), verdeClaro = C(0xC8F2C2), nervio = C(0x2E7D32), oro = C(0xFFC928);
        float x = u - 0.5f, y = v - 0.5f;
        // Ojos y cabeza (arriba)
        if (Sq(x - 0.045f) + Sq(y - 0.25f) < 0.0012f || Sq(x + 0.045f) + Sq(y - 0.25f) < 0.0012f) return oro;
        if (Sq(x) / 0.004f + Sq(y - 0.235f) / 0.003f < 1f) return verde;
        // Antenas largas que salen de la cabeza hacia arriba y se abren
        for (int s = -1; s <= 1; s += 2)
        {
            float t = (y - 0.27f) / 0.2f;
            if (t > 0f && t < 1f && Mathf.Abs(x - s * (0.02f + 0.18f * t * t)) < 0.006f) return verde;
        }
        // Cuerpo: tórax y abdomen largo
        if (Sq(x) / 0.0018f + Sq(y + 0.08f) / 0.07f < 1f) return verde;
        // Alas: dos pares de elipses inclinadas, de encaje (nervaduras en rejilla)
        for (int s = -1; s <= 1; s += 2)
            for (int par = 0; par < 2; par++)
            {
                float cx = s * 0.17f, cy = par == 0 ? 0.08f : -0.1f, ang = s * (par == 0 ? 0.35f : -0.25f);
                float dx = x - cx, dy = y - cy;
                float rx = dx * Mathf.Cos(ang) + dy * Mathf.Sin(ang), ry = -dx * Mathf.Sin(ang) + dy * Mathf.Cos(ang);
                float e = Sq(rx) / 0.028f + Sq(ry) / 0.0045f;
                if (e < 1f)
                {
                    bool borde = e > 0.86f;
                    bool red = Mathf.Abs(Mathf.Repeat(rx * 38f, 1f) - 0.5f) < 0.09f || Mathf.Abs(Mathf.Repeat(ry * 26f + rx * 6f, 1f) - 0.5f) < 0.09f;
                    return borde || red ? nervio : Color.Lerp(fondo, verdeClaro, 0.75f);
                }
            }
        return fondo;
    }

    // ¿Este punto (u, v de 0 a 1) va con el segundo color?
    static bool Dibujo(Patron p, float u, float v)
    {
        switch (p)
        {
            case Patron.Franjas: return Mathf.FloorToInt(v * 5f) % 2 == 1;
            case Patron.Rayas: return Mathf.FloorToInt(u * 6f) % 2 == 1;
            case Patron.Cruz: return Mathf.Abs(u - 0.5f) < 0.09f || Mathf.Abs(v - 0.55f) < 0.09f;
            case Patron.Diagonal: return Mathf.Abs(u - v) < 0.14f;
            case Patron.Borde: return u < 0.1f || u > 0.9f || v < 0.1f || v > 0.9f;
            case Patron.Sol:
            {
                float dx = u - 0.5f, dy = v - 0.55f, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 0.15f) return true;
                return r < 0.3f && Mathf.Cos(Mathf.Atan2(dy, dx) * 12f) > 0.55f;
            }
            case Patron.Calavera:
            {
                // Cráneo con ojos, nariz y dientes, y dos huesos cruzados debajo
                float dx = u - 0.5f, dy = v - 0.6f;
                bool craneo = dx * dx / 0.035f + dy * dy / 0.03f < 1f;
                bool mandibula = Mathf.Abs(dx) < 0.11f && v > 0.38f && v < 0.47f;
                bool ojo = Sq(u - 0.42f) + Sq(v - 0.6f) < 0.0022f || Sq(u - 0.58f) + Sq(v - 0.6f) < 0.0022f;
                bool nariz = Mathf.Abs(dx) < 0.02f && v > 0.5f && v < 0.55f;
                bool dientes = mandibula && Mathf.FloorToInt(u * 40f) % 2 == 0 && v < 0.42f;
                bool huesos = v < 0.36f && v > 0.12f && (Mathf.Abs((u - 0.5f) - (v - 0.24f) * 1.6f) < 0.04f || Mathf.Abs((u - 0.5f) + (v - 0.24f) * 1.6f) < 0.04f);
                return ((craneo || mandibula) && !ojo && !nariz && !dientes) || huesos;
            }
            default: return false;
        }
    }
    static float Sq(float x) => x * x;
}
