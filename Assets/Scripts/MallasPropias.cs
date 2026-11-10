using UnityEngine;

// Al destruirse un barco (el mundo infinito los quita al alejarte), destruye también las mallas que se hicieron solo
// para él: jarcia, detalles, tablones, portas, interior… Unity no las libera solas y en un viaje largo llenaban la memoria.
public class MallasPropias : MonoBehaviour
{
    void OnDestroy()
    {
        foreach (MeshFilter f in GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh m = f.sharedMesh;
            if (m != null && CascoProcedural.Propias.Remove(m)) Destroy(m);
        }
    }
}
