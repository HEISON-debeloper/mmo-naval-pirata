using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Ninguna vela tapa la vista (como en Black Flag): si una vela de tu barco queda entre la cámara
// y el timón (o la zona donde apuntas), se vuelve casi transparente mientras estorba.
public class VelasSinEstorbo : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() => new GameObject("Velas sin estorbo").AddComponent<VelasSinEstorbo>();

    const float Transparencia = 0.18f;

    readonly Dictionary<Material, Material> transparentes = new Dictionary<Material, Material>();
    readonly Dictionary<Renderer, Material> originales = new Dictionary<Renderer, Material>();
    readonly List<Renderer> velas = new List<Renderer>();
    ControlBarco barcoVelas;
    static VelasSinEstorbo instancia;
    void Awake() => instancia = this;

    // Al pintar las velas: devuelve todo a como estaba y vuelve a leer los materiales en el siguiente frame
    public static void Olvidar()
    {
        if (instancia == null) return;
        instancia.Restaurar();
        instancia.barcoVelas = null;
        foreach (var kv in instancia.transparentes) if (kv.Value != null) Destroy(kv.Value);   // copias con el color viejo
        instancia.transparentes.Clear();
    }

    void LateUpdate()
    {
        Personaje p = Personaje.Instancia;
        Camera cam = Camera.main;
        ControlBarco b = p != null && p.estado != Personaje.Estado.Libre ? p.BarcoActual : null;
        if (b != barcoVelas) { Restaurar(); barcoVelas = b; Recolectar(b); }
        if (b == null || cam == null) return;

        Vector3 c = cam.transform.position;
        Vector3 timon = p.transform.position + Vector3.up * 1.5f;
        bool apunta = Punteria.Apuntando;
        for (int i = 0; i < velas.Count; i++)
        {
            Renderer r = velas[i];
            if (r == null) continue;
            bool estorba = Tapa(r.bounds, c, timon) || (apunta && Tapa(r.bounds, c, Punteria.PuntoCaida));
            Poner(r, estorba);
        }
    }

    static bool Tapa(Bounds caja, Vector3 desde, Vector3 hasta)
    {
        Vector3 d = hasta - desde;
        float largo = d.magnitude;
        if (largo < 0.01f) return false;
        caja.Expand(0.3f);
        return caja.IntersectRay(new Ray(desde, d / largo), out float dist) && dist < largo;
    }

    void Recolectar(ControlBarco b)
    {
        velas.Clear();
        if (b == null) return;
        Agregar(b.vela);
        if (b.velasExtra != null) foreach (Transform v in b.velasExtra) Agregar(v);
    }

    void Agregar(Transform pivote)
    {
        if (pivote == null) return;
        foreach (Renderer r in pivote.GetComponentsInChildren<Renderer>())
        {
            velas.Add(r);
            if (!originales.ContainsKey(r)) originales[r] = r.sharedMaterial;
        }
    }

    void Poner(Renderer r, bool transparente)
    {
        if (!originales.TryGetValue(r, out Material orig) || orig == null) return;
        Material t = transparente ? Transparente(orig) : null;
        if (transparente && t == null) { r.enabled = false; return; }   // si el material no deja ser transparente, se oculta
        r.enabled = true;
        Material quiero = transparente ? t : orig;
        if (r.sharedMaterial != quiero) r.sharedMaterial = quiero;
    }

    void Restaurar()
    {
        foreach (var kv in originales)
            if (kv.Key != null) { kv.Key.sharedMaterial = kv.Value; kv.Key.enabled = true; }
        originales.Clear();
        velas.Clear();
    }

    // Copia del material de la vela en modo transparente (Universal Render Pipeline / Lit)
    Material Transparente(Material m)
    {
        if (transparentes.TryGetValue(m, out Material t)) return t;
        t = null;
        if (m.HasProperty("_Surface") && m.HasProperty("_BaseColor"))
        {
            t = new Material(m) { name = m.name + " (transparente)" };
            t.SetFloat("_Surface", 1f);
            t.SetFloat("_Blend", 0f);
            t.SetOverrideTag("RenderType", "Transparent");
            t.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            t.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (t.HasProperty("_SrcBlendAlpha")) t.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (t.HasProperty("_DstBlendAlpha")) t.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            t.SetFloat("_ZWrite", 0f);
            t.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            t.DisableKeyword("_ALPHATEST_ON");
            t.renderQueue = (int)RenderQueue.Transparent;
            Color c = t.GetColor("_BaseColor");
            c.a = Transparencia;
            t.SetColor("_BaseColor", c);
        }
        transparentes[m] = t;
        return t;
    }
}
