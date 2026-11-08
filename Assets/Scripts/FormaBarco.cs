using System.Collections.Generic;
using UnityEngine;

// El barco del jugador tiene dos cosas que se mejoran por separado:
//
// 1) El MOLDE: qué tipo de barco es (balsa, bote, chalupa... hasta el navío de cuatro puentes).
//    Mejorar el barco = pasar al siguiente molde. Una balsa sigue siendo balsa aunque la estires,
//    y una goleta estirada o con más pisos sigue siendo goleta.
//
// 2) El TAMAÑO, en tres direcciones, sin cambiar el molde:
//    · Alargar: más largo → más rápido y más cañones por fila, pero gira peor
//    · Ensanchar: más ancho → más vida y bodega, gira mejor, más lento
//    · Subir piso: más alto → una fila más de cañones por costado y más vida, pero más lento
//
// Al pasar al siguiente molde se conserva lo estirado, lo ancho y los pisos; por eso
// el molde siguiente cuesta más mientras más grande lleves el barco.
// Moldes basados en los barcos de la época de la vela (siglos XVI a XIX), de menor a mayor.
public static class FormaBarco
{
    public struct Molde
    {
        public string nombre;
        public bool femenino;
        public float largo, ancho, alto;   // medidas base en metros
        public int filas;                   // filas (cubiertas) de cañones de base
        public int mastiles;
        public float vida;
        public float velocidad, giro;       // carácter del molde (1 = normal)
        public bool balsa;
    }

    public static readonly Molde[] Moldes =
    {
        M("Balsa",                    true,   6f,  3f,   0.8f, 1, 1,  40f, 1.00f, 1.00f, true),
        M("Bote",                     false,  7f,  2.4f, 1.3f, 1, 1,  55f, 1.05f, 1.10f),
        M("Chalupa",                  true,   9f,  3f,   1.6f, 1, 1,  70f, 1.05f, 1.10f),
        M("Pinaza",                   true,  12f,  3.4f, 2.0f, 1, 1,  85f, 1.05f, 1.05f),
        M("Cúter",                    false, 14f,  4f,   2.4f, 1, 1, 100f, 1.10f, 1.10f),
        M("Balandro",                 false, 18f,  4.5f, 3.0f, 1, 1, 120f, 1.05f, 1.00f),
        M("Goleta",                   true,  22f,  5.4f, 3.2f, 1, 2, 140f, 1.15f, 1.00f),
        M("Bergantín",                false, 27f,  7f,   3.8f, 1, 2, 170f, 1.05f, 0.95f),
        M("Corbeta",                  true,  33f,  8.5f, 4.4f, 1, 3, 210f, 1.05f, 0.90f),
        M("Fragata",                  true,  42f, 11f,   5.4f, 1, 3, 270f, 1.05f, 0.85f),
        M("Galeón",                   false, 46f, 12.5f, 7.0f, 2, 3, 330f, 0.85f, 0.80f),
        M("Navío de cuarta",          false, 48f, 13f,   7.0f, 2, 3, 380f, 0.90f, 0.80f),
        M("Navío de línea",           false, 54f, 14.5f, 8.0f, 2, 3, 450f, 0.90f, 0.75f),
        M("Navío de primera",         false, 62f, 16.5f, 10.5f, 3, 3, 560f, 0.85f, 0.70f),
        M("Navío de cuatro puentes",  false, 64f, 17f,  12.5f, 4, 3, 650f, 0.80f, 0.65f),
    };

    static Molde M(string n, bool f, float L, float w, float h, int filas, int mast, float vida, float vel, float giro, bool balsa = false) =>
        new Molde { nombre = n, femenino = f, largo = L, ancho = w, alto = h, filas = filas, mastiles = mast, vida = vida, velocidad = vel, giro = giro, balsa = balsa };

    public static int UltimoMolde => Moldes.Length - 1;
    public const int MaxPasos = 10;            // tope de alargar, ensanchar y pisos
    public const float AltoPiso = 2.2f;        // cada piso sube el casco esto (balsa: 1.2)
    public const float SeparacionCanones = 2.4f;   // los cañones son siempre del mismo tamaño

    public static Molde De(int molde) => Moldes[Mathf.Clamp(molde, 0, UltimoMolde)];

    public static void Medidas(int molde, int a, int b, int c, out float L, out float w, out float h)
    {
        Molde m = De(molde);
        L = m.largo * Mathf.Pow(1.1f, a);
        w = m.ancho * Mathf.Pow(1.1f, b);
        h = m.alto + c * (m.balsa ? 1.2f : AltoPiso);
    }

    public static int Filas(int molde, int c) => De(molde).filas + c;

    // Cañones por fila y por costado: según el largo, siempre con la misma separación
    public static int PorFila(int molde, int a)
    {
        Medidas(molde, a, 0, 0, out float L, out _, out _);
        if (De(molde).balsa) return 1;
        return Mathf.Clamp(Mathf.FloorToInt(L * 0.62f / SeparacionCanones), 1, 40);
    }

    public static int CanonesTotales(int molde, int a, int c) =>
        PorFila(molde, a) * Filas(molde, c) * 2 + (De(molde).balsa ? 0 : 2);

    public static string Nombre(int molde, int a, int b, int c)
    {
        Molde m = De(molde);
        string s = m.nombre;
        int mayor = Mathf.Max(a, Mathf.Max(b, c));
        if (mayor >= 2)
        {
            if (a == mayor) s += m.femenino ? " alargada" : " alargado";
            else if (b == mayor) s += m.femenino ? " ancha" : " ancho";
            else s += " de " + (Filas(molde, c)) + " pisos";
        }
        return s;
    }

    public static float Vida(int molde, int a, int b, int c) =>
        Mathf.Round(De(molde).vida * (1f + 0.03f * a + 0.06f * b + 0.08f * c));

    public static float Hondo(int molde, int a, int b, int c)
    {
        Medidas(molde, a, b, c, out _, out _, out float h);
        return De(molde).balsa ? 1.2f : 2f + h * 0.15f;
    }

    // ---------- Costos ----------
    // Agrandar (alargar, ensanchar o subir piso): más caro mientras más grande el molde y el barco
    public static Dictionary<string, int> CostoAgrandar(int molde, int a, int b, int c)
    {
        float f = (1f + molde * 0.6f) * Mathf.Pow(1.15f, a + b + c);
        var d = new Dictionary<string, int>
        {
            [Inventario.Madera] = Mathf.RoundToInt(10f * f),
            [Inventario.Cuerda] = Mathf.RoundToInt(3f * f),
        };
        if (molde >= 4) d[Inventario.Lingote] = Mathf.Max(1, Mathf.RoundToInt(0.5f * f));
        return d;
    }

    // Pasar al siguiente molde: el precio crece con el molde y con todo lo que ya agrandaste
    public static Dictionary<string, int> CostoMolde(int moldeNuevo, int a, int b, int c)
    {
        float f = Mathf.Pow(1.1f, a) * Mathf.Pow(1.1f, b) * (1f + 0.15f * c);
        var d = new Dictionary<string, int>
        {
            [Inventario.Madera] = Mathf.RoundToInt(30f * Mathf.Pow(1.35f, moldeNuevo) * f),
            [Inventario.Cuerda] = Mathf.RoundToInt(10f * Mathf.Pow(1.3f, moldeNuevo) * f),
            [Inventario.Fibra] = Mathf.RoundToInt(10f * Mathf.Pow(1.25f, moldeNuevo) * f),
        };
        if (moldeNuevo >= 2) d[Inventario.Lingote] = Mathf.RoundToInt(3f * Mathf.Pow(1.35f, moldeNuevo - 2) * f);
        return d;
    }

    // ---------- Construcción ----------
    public static GameObject Construir(int molde, int a, int b, int c, Vector3 pos, float rumbo, Astillero.Materiales mats, Material madera, Material lona)
    {
        Molde m = De(molde);
        Medidas(molde, a, b, c, out float L, out float w, out float h);
        string nombre = Nombre(molde, a, b, c);
        int filas = Filas(molde, c);
        GameObject g;
        if (m.balsa)
        {
            g = Balsa.Crear(pos, rumbo, madera, lona, mats.canon, L, w, c);
            g.GetComponent<ControlBarco>().nivelesVela = 1 + Mathf.Min(2, (a + b + c + 1) / 2);
        }
        else
        {
            // Los mástiles se miden con el largo del molde (no se vuelven gigantes al estirar el barco)
            g = Astillero.CrearBarco(nombre, L, pos, rumbo, mats, false, 0, w, h, PorFila(molde, a), filas, m.mastiles, m.largo);
        }
        g.name = nombre;

        VidaBarco v = g.GetComponent<VidaBarco>();
        v.vidaMaxima = Vida(molde, a, b, c);
        v.vida = v.vidaMaxima;

        // Largo: más rápido, gira peor. Ancho: gira mejor, más lento. Pisos: más lento.
        ControlBarco cb = g.GetComponent<ControlBarco>();
        cb.fuerzaPorNivel *= m.velocidad * (1f + 0.04f * a) * (1f - 0.03f * b) * (1f - 0.02f * c);
        cb.fuerzaGiro *= m.giro * Mathf.Clamp((1f - 0.04f * a) * (1f + 0.03f * b), 0.4f, 1.8f);

        FormaActual f = g.AddComponent<FormaActual>();
        f.molde = molde; f.largo = a; f.ancho = b; f.alto = c;
        return g;
    }

    // Forma guardada en un barco (si no la tiene, se busca el molde más parecido por su largo)
    public static void Leer(ControlBarco barco, out int molde, out int a, out int b, out int c)
    {
        molde = 0; a = 0; b = 0; c = 0;
        if (barco == null) return;
        FormaActual f = barco.GetComponent<FormaActual>();
        if (f != null) { molde = f.molde; a = f.largo; b = f.ancho; c = f.alto; return; }
        BoxCollider col = barco.GetComponent<BoxCollider>();
        float L = col != null ? col.size.z : 6f;
        float mejor = float.MaxValue;
        for (int i = 0; i < Moldes.Length; i++)
        {
            float d = Mathf.Abs(Moldes[i].largo - L);
            if (d < mejor) { mejor = d; molde = i; }
        }
    }
}

// Guarda en el barco su molde y cuánto se alargó, ensanchó y subió de pisos
public class FormaActual : MonoBehaviour
{
    public int molde;
    public int largo;
    public int ancho;
    public int alto;
}
