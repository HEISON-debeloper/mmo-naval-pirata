using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Junta las piezas quietas de un barco en pocas mallas. Un navío grande tenía unas 275 piezas con su propio
// dibujado (cada cañón, ventana, verga, tablón…): en un teléfono cada dibujado cuesta tiempo del procesador, y con
// dos barcos y una isla a la vista se iba la mitad del cuadro solo en eso. Juntas por material quedan unas 20-40.
//
// Qué NO se junta (porque algo lo mueve, lo cambia o lo prende y apaga por su cuenta):
//   las velas (suben y bajan, se vuelven transparentes), la tripulación, el mascarón (se cambia en el Taller),
//   la bandera, el timón, las portas (se esconden bajo cubierta), lo que tenga un script propio (barriles en llamas…),
//   el interior y todo lo apagado.
// Las piezas originales siguen ahí (con sus colisiones y nombres), solo que sin dibujarse.
// Al instalar o quitar cañones (o al pintar) se vuelve a juntar.
public class MallaCombinada : MonoBehaviour
{
    readonly List<Renderer> ocultos = new List<Renderer>();
    readonly List<GameObject> juntas = new List<GameObject>();
    bool pedido;

    void Start() => Pedir();

    // Rehacer en el próximo cuadro (si se pide varias veces, se hace una sola)
    public void Pedir()
    {
        if (pedido || !isActiveAndEnabled) { pedido = true; return; }
        pedido = true;
        StartCoroutine(Luego());
    }

    void OnEnable() { if (pedido) { pedido = false; Pedir(); } }

    IEnumerator Luego()
    {
        yield return null;
        yield return null;   // que terminen de armarse cañones, pintura, tripulación y detalles
        pedido = false;
        Juntar();
    }

    static readonly string[] NoJuntar = { "Portas", "Bandera", "Rueda", "Lona" };

    public void Deshacer()
    {
        foreach (Renderer r in ocultos) if (r != null) r.enabled = true;
        ocultos.Clear();
        foreach (GameObject g in juntas)
        {
            if (g == null) continue;
            MeshFilter f = g.GetComponent<MeshFilter>();
            if (f != null && f.sharedMesh != null) { CascoProcedural.Propias.Remove(f.sharedMesh); Destroy(f.sharedMesh); }
            Destroy(g);
        }
        juntas.Clear();
    }

    void Juntar()
    {
        Deshacer();
        Transform raiz = transform;
        var excluir = new HashSet<Transform>();
        ControlBarco cb = GetComponent<ControlBarco>();
        if (cb != null)
        {
            if (cb.vela != null) excluir.Add(cb.vela);
            if (cb.velasExtra != null) foreach (Transform v in cb.velasExtra) if (v != null) excluir.Add(v);
        }
        double t = Cronos.Ahora;
        Combinar(raiz, excluir, GetComponent<Pintura>(), ocultos, juntas);
        Cronos.Anotar("juntar mallas de un barco", t);
    }

    // Junta los hijos quietos de una pieza que se mueve entera (un recurso de isla: palma, arbusto, veta…).
    // La malla junta se borra con la isla (MallasPropias en la raíz de la isla).
    public static void JuntarHijos(Transform raiz) => Combinar(raiz, null, null, null, null);

    static void Combinar(Transform raiz, HashSet<Transform> excluir, Pintura pintura, List<Renderer> ocultos, List<GameObject> juntas)
    {
        GameObject yo = raiz.gameObject;
        // Grupos: mismo material, capa, sombras y parte de la pintura
        var grupos = new Dictionary<(Material, int, ShadowCastingMode, bool, int), List<(Renderer r, Mesh m)>>();
        Matrix4x4 aLocal = raiz.worldToLocalMatrix;
        foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
        {
            if (!r.enabled || r.gameObject == yo) continue;
            MeshFilter f = r.GetComponent<MeshFilter>();
            Mesh m = f != null ? f.sharedMesh : null;
            if (m == null || !m.isReadable || m.subMeshCount != 1 || r.sharedMaterials.Length != 1 || r.sharedMaterial == null) continue;
            if (System.Array.IndexOf(NoJuntar, r.name) >= 0) continue;
            if (r.transform.localToWorldMatrix.determinant < 0f) continue;   // espejadas: quedarían al revés
            if (!Quieta(r.transform, raiz, excluir)) continue;
            Material orig = pintura != null ? pintura.Original(r) : r.sharedMaterial;
            var k = (orig, r.gameObject.layer, r.shadowCastingMode, r.receiveShadows, Pintura.ParteDe(r));
            if (!grupos.TryGetValue(k, out var l)) grupos[k] = l = new List<(Renderer, Mesh)>();
            l.Add((r, m));
        }

        foreach (var kv in grupos)
        {
            var l = kv.Value;
            if (l.Count < 2) continue;   // una sola pieza: no gana nada
            // En trozos de hasta ~60.000 vértices (y con índices de 32 bits si hiciera falta)
            int desde = 0;
            while (desde < l.Count)
            {
                var ci = new List<CombineInstance>();
                int verts = 0, i = desde;
                for (; i < l.Count; i++)
                {
                    int n = l[i].m.vertexCount;
                    if (ci.Count > 0 && verts + n > 60000) break;
                    ci.Add(new CombineInstance { mesh = l[i].m, subMeshIndex = 0, transform = aLocal * l[i].r.localToWorldMatrix });
                    verts += n;
                }
                Renderer modelo = l[desde].r;
                var malla = new Mesh { name = "Junta " + modelo.name, indexFormat = verts > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                malla.CombineMeshes(ci.ToArray(), true, true, false);
                malla.RecalculateBounds();
                CascoProcedural.Propias.Add(malla);   // MallasPropias la borra con el barco

                var g = new GameObject(modelo.name);   // mismo nombre: la pintura la reconoce como esa parte
                g.layer = kv.Key.Item2;
                g.transform.SetParent(raiz, false);
                g.AddComponent<MeshFilter>().sharedMesh = malla;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = modelo.sharedMaterial;
                mr.shadowCastingMode = kv.Key.Item3;
                mr.receiveShadows = kv.Key.Item4;
                if (pintura != null) pintura.Registrar(mr, modelo);
                juntas?.Add(g);
                for (int j = desde; j < i; j++) { l[j].r.enabled = false; ocultos?.Add(l[j].r); }
                desde = i;
            }
        }
    }

    // Una pieza es "quieta" si ni ella ni nada entre ella y el barco tiene un script, una pieza excluida
    // o algo que la gente use (tripulantes)
    static bool Quieta(Transform t, Transform raiz, HashSet<Transform> excluir)
    {
        for (Transform x = t; x != null && x != raiz; x = x.parent)
        {
            if (excluir != null && excluir.Contains(x)) return false;
            if (x.name.StartsWith("mascaron") || x.name.StartsWith("Mascar")) return false;
            if (x.GetComponent<MonoBehaviour>() != null) return false;
        }
        return true;
    }

    void OnDestroy() => Deshacer();
}
