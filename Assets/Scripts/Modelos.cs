using System.Collections.Generic;
using UnityEngine;

// Piezas modeladas en Blender (cañón, timón, ancla, farol, barril, mascarones).
// Cada modelo está en Assets/Resources/Modelos/<nombre>.json: una lista de objetos con sus vértices y triángulos,
// ya en coordenadas de Unity (los genera blender/modelos.py). Cada objeto es una pieza con su propio material,
// así se puede pintar por separado.
public static class Modelos
{
    [System.Serializable] class Objeto { public string nombre; public float[] v; public int[] t; }
    [System.Serializable] class Archivo { public Objeto[] objetos; }

    static readonly Dictionary<string, List<(string nombre, Mesh malla)>> cache = new Dictionary<string, List<(string, Mesh)>>();

    static List<(string nombre, Mesh malla)> Cargar(string modelo)
    {
        if (cache.TryGetValue(modelo, out var lista)) return lista;
        lista = new List<(string, Mesh)>();
        TextAsset txt = Resources.Load<TextAsset>("Modelos/" + modelo);
        if (txt == null) { Debug.LogWarning("Falta el modelo " + modelo); cache[modelo] = lista; return lista; }
        Archivo a = JsonUtility.FromJson<Archivo>(txt.text);
        foreach (Objeto o in a.objetos)
        {
            var verts = new Vector3[o.v.Length / 3];
            for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(o.v[i * 3], o.v[i * 3 + 1], o.v[i * 3 + 2]);
            Mesh m = new Mesh { name = modelo + "_" + o.nombre };
            if (verts.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = verts;
            m.triangles = HaciaAfuera(verts, o.t);
            m.RecalculateNormals();   // cada triángulo con sus vértices: caras planas (low poly)
            m.RecalculateBounds();
            lista.Add((o.nombre, m));
        }
        cache[modelo] = lista;
        return lista;
    }

    // Los modelos exportados traían los triángulos al revés (la cara visible miraba hacia adentro): de afuera se veía
    // el interior y los barriles parecían huecos. Con el volumen con signo se sabe hacia dónde miran y se dan vuelta si hace falta.
    static int[] HaciaAfuera(Vector3[] v, int[] t)
    {
        double vol = 0;
        for (int i = 0; i + 2 < t.Length; i += 3)
            vol += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]]));
        // Unity dibuja el lado hacia donde apunta Cross(b-a, c-a): mirando afuera, el volumen sale positivo
        if (vol >= 0) return t;
        int[] r = (int[])t.Clone();
        for (int i = 0; i + 2 < r.Length; i += 3) { int x = r[i + 1]; r[i + 1] = r[i + 2]; r[i + 2] = x; }
        return r;
    }

    public static bool Existe(string modelo) => Cargar(modelo).Count > 0;

    // Crea el modelo como hijo de "padre". material(nombrePieza) da el material de cada pieza (null = sin cambiar).
    // solo: si no es null, solo se crean esas piezas (por ejemplo, el tubo del cañón sin la cureña).
    public static GameObject Crear(string modelo, Transform padre, Vector3 pos, Quaternion rot, float escala,
                                   System.Func<string, Material> material, params string[] solo)
    {
        GameObject raiz = new GameObject(modelo);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = pos;
        raiz.transform.localRotation = rot;
        raiz.transform.localScale = Vector3.one * escala;
        foreach (var (nombre, malla) in Cargar(modelo))
        {
            if (solo != null && solo.Length > 0 && System.Array.IndexOf(solo, nombre) < 0) continue;
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(raiz.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = material?.Invoke(nombre);
        }
        return raiz;
    }

    // Material de un color, copiado de otro (para el vidrio del farol, por ejemplo). Se guarda para no repetir.
    static readonly Dictionary<(Material, Color), Material> colores = new Dictionary<(Material, Color), Material>();
    public static Material DeColor(Material base_, Color c, bool brilla = false)
    {
        if (base_ == null) return null;
        if (colores.TryGetValue((base_, c), out Material m) && m != null) return m;
        m = new Material(base_) { name = base_.name + " " + ColorUtility.ToHtmlStringRGB(c) };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (brilla && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.5f);
        }
        colores[(base_, c)] = m;
        if (colores.Count % 64 == 0) Podar();
        return m;
    }

    // Los materiales pintados de un barco se borran con él: sus copias de color ya no las usa nadie
    static readonly List<(Material, Color)> sueltas = new List<(Material, Color)>();
    static void Podar()
    {
        sueltas.Clear();
        foreach (var kv in colores) if (kv.Key.Item1 == null) sueltas.Add(kv.Key);
        foreach (var k in sueltas) { if (colores[k] != null) Object.Destroy(colores[k]); colores.Remove(k); }
    }
}
