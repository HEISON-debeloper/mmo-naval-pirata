using UnityEngine;

// Corazoncitos que suben y se achican (al dar un beso). Cada corazón: dos bolitas y un rombo, mirando a la cámara.
public class Corazones : MonoBehaviour
{
    static Material rosa;
    Transform[] corazones;
    Vector3[] vel;
    float t;
    const float Duracion = 1.8f;

    public static void Lanzar(Vector3 pos)
    {
        if (rosa == null)
        {
            Personaje p = Personaje.Instancia;
            Renderer r = p != null ? p.GetComponentInChildren<Renderer>() : null;
            if (r == null || r.sharedMaterial == null) return;
            rosa = Modelos.DeColor(r.sharedMaterial, new Color(1f, 0.35f, 0.6f), true);
        }
        var g = new GameObject("Corazones");
        g.transform.position = pos;
        g.AddComponent<Corazones>();
    }

    void Start()
    {
        corazones = new Transform[6];
        vel = new Vector3[corazones.Length];
        for (int i = 0; i < corazones.Length; i++)
        {
            var c = new GameObject("Corazón").transform;
            c.SetParent(transform, false);
            c.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.2f), Random.Range(-0.3f, 0.3f));
            Pieza(PrimitiveType.Sphere, c, new Vector3(-0.055f, 0.03f, 0f), 0.12f);
            Pieza(PrimitiveType.Sphere, c, new Vector3(0.055f, 0.03f, 0f), 0.12f);
            Transform rombo = Pieza(PrimitiveType.Cube, c, new Vector3(0f, -0.04f, 0f), 0.12f);
            rombo.localRotation = Quaternion.Euler(0f, 0f, 45f);
            corazones[i] = c;
            vel[i] = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.6f, 1.1f), Random.Range(-0.25f, 0.25f));
        }
    }

    static Transform Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, float tam)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = Vector3.one * tam;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = rosa;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t > Duracion) { Destroy(gameObject); return; }
        Camera cam = Camera.main;
        float k = 1f - t / Duracion;
        for (int i = 0; i < corazones.Length; i++)
        {
            Transform c = corazones[i];
            c.localPosition += vel[i] * Time.deltaTime;
            c.localScale = Vector3.one * (0.6f + 0.6f * Mathf.Sin(Mathf.Min(1f, t * 3f) * Mathf.PI * 0.5f)) * k;
            if (cam != null) c.rotation = Quaternion.LookRotation(c.position - cam.transform.position);
        }
    }
}
