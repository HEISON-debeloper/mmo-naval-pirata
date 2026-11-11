using UnityEngine;

// Si la cámara queda pegada a la cabeza de tu pirata (primera persona bajo cubierta, o la cámara empujada contra
// un mástil), no se dibuja tu propio cuerpo: así los ojos, la barba o la banderita no tapan la vista.
[DefaultExecutionOrder(1000)]   // después de la cámara
public class OcultarDeCerca : MonoBehaviour
{
    Renderer[] partes;
    bool oculto;

    public void Refrescar() { partes = GetComponentsInChildren<Renderer>(true); if (oculto) Poner(true); }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (partes == null) Refrescar();
        bool cerca = (cam.transform.position - (transform.position + Vector3.up * 1.5f)).sqrMagnitude < 0.9f * 0.9f;
        if (cerca != oculto) Poner(cerca);
    }

    void Poner(bool o)
    {
        oculto = o;
        Personaje p = GetComponent<Personaje>();
        foreach (Renderer r in partes)
            if (r != null && (p == null || p.espada == null || !r.transform.IsChildOf(p.espada))) r.forceRenderingOff = o;   // la espada sí se ve
    }
}
