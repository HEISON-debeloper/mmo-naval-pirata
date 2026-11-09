using System.Collections.Generic;
using UnityEngine;

// Pintura de tu barco: color del casco, de las velas y de la bandera.
// Se elige en el menú Tab → Pintura (estando a bordo de tu barco) y se conserva al mejorar o agrandar el barco
// y al guardar la partida. Cada color es una copia del material original, así no se pintan los demás barcos.
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

    public enum Parte { Casco, Velas, Bandera }

    // -1 = color original
    public int casco = -1, velas = -1, bandera = -1;

    class Grupo { public Material original; public readonly List<Renderer> piezas = new List<Renderer>(); public Material pintado; }
    readonly Dictionary<Parte, Grupo> grupos = new Dictionary<Parte, Grupo>();
    bool listo;

    public int Indice(Parte p) => p == Parte.Casco ? casco : p == Parte.Velas ? velas : bandera;
    public static string NombreColor(int i) => i < 0 ? "original" : Paleta[i % Paleta.Length].nombre;
    public bool Tiene(Parte p) { Preparar(); return grupos.ContainsKey(p); }

    // Cambia al color siguiente (paso = 1) o anterior (paso = -1). Vuelve a "original" después del último.
    public void Cambiar(Parte p, int paso)
    {
        int n = Paleta.Length + 1;   // los colores y el original
        int i = (Indice(p) + 1 + paso + n) % n - 1;
        Poner(p, i);
    }

    public void Poner(Parte p, int i)
    {
        if (p == Parte.Casco) casco = i; else if (p == Parte.Velas) velas = i; else bandera = i;
        Aplicar();
    }

    public void Copiar(Pintura otra) { casco = otra.casco; velas = otra.velas; bandera = otra.bandera; Aplicar(); }

    void Start() => Aplicar();

    // Busca qué piezas son casco, velas y bandera (por el material que tenían al construirse)
    void Preparar()
    {
        if (listo) return;
        listo = true;
        Renderer cascoR = Buscar("Casco");
        if (cascoR == null)   // balsa: los troncos
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                if (r.transform.parent == transform && r.name == "Tronco") { cascoR = r; break; }
        Renderer velaR = Buscar("Vela/Lona");
        Renderer banderaR = Buscar("Bandera");
        Agrupar(Parte.Casco, cascoR);
        Agrupar(Parte.Velas, velaR);
        Agrupar(Parte.Bandera, banderaR);
    }

    Renderer Buscar(string ruta)
    {
        Transform t = transform.Find(ruta);
        return t != null ? t.GetComponent<Renderer>() : null;
    }

    void Agrupar(Parte p, Renderer muestra)
    {
        if (muestra == null || muestra.sharedMaterial == null) return;
        Grupo g = new Grupo { original = muestra.sharedMaterial };
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial == g.original && r.GetComponentInParent<Tripulante>() == null) g.piezas.Add(r);
        grupos[p] = g;
    }

    public void Aplicar()
    {
        Preparar();
        VelasSinEstorbo.Olvidar();   // que vuelva a leer los materiales de las velas
        foreach (var kv in grupos)
        {
            Grupo g = kv.Value;
            int i = Indice(kv.Key);
            Material m = g.original;
            if (i >= 0)
            {
                if (g.pintado == null) g.pintado = new Material(g.original) { name = g.original.name + " (pintado)" };
                Color c = Paleta[i % Paleta.Length].color;
                if (g.pintado.HasProperty("_BaseColor")) g.pintado.SetColor("_BaseColor", c);
                if (g.pintado.HasProperty("_Color")) g.pintado.SetColor("_Color", c);
                m = g.pintado;
            }
            foreach (Renderer r in g.piezas) if (r != null) r.sharedMaterial = m;
        }
    }

    void OnDestroy()
    {
        foreach (var g in grupos.Values) if (g.pintado != null) Destroy(g.pintado);
    }
}
