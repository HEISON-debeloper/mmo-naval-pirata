using UnityEngine;

// Bala de cañón: daña al barco que toca y desaparece al caer al agua.
// También hiere a la gente: de lleno a quien atraviesa (cubierta enemiga barrida) y con astillas a quien está cerca del impacto.
// No hiere a la tripulación ni al jugador del barco que disparó.
[RequireComponent(typeof(Rigidbody))]
public class BalaCanon : MonoBehaviour
{
    [HideInInspector] public VidaBarco dueno;
    [HideInInspector] public float dano = 12f;
    float edad;
    Rigidbody rb;
    Collider col;

    void Awake() { rb = GetComponent<Rigidbody>(); col = GetComponent<Collider>(); }

    // ---------- Reciclaje: una andanada eran decenas de objetos creados y destruidos de golpe ----------
    static readonly System.Collections.Generic.Stack<BalaCanon> libres = new System.Collections.Generic.Stack<BalaCanon>();
    readonly System.Collections.Generic.List<Collider> ignorados = new System.Collections.Generic.List<Collider>();

    public static BalaCanon Tomar()
    {
        while (libres.Count > 0)
        {
            BalaCanon b = libres.Pop();
            if (b != null) return b;   // las de una escena anterior ya no existen
        }
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "Bala";
        g.transform.localScale = Vector3.one * 0.4f;
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Rigidbody r = g.AddComponent<Rigidbody>();
        r.mass = 10f;
        r.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        g.SetActive(false);
        return g.AddComponent<BalaCanon>();
    }

    public void Lanzar(Vector3 pos, Vector3 vel, Material m, Collider[] propios, VidaBarco duenoNuevo, float danoNuevo)
    {
        // Las colisiones ignoradas del disparo anterior vuelven a contar
        foreach (Collider c in ignorados) if (c != null && col != null) Physics.IgnoreCollision(col, c, false);
        ignorados.Clear();
        transform.position = pos;
        if (m != null) GetComponent<Renderer>().sharedMaterial = m;
        gameObject.SetActive(true);
        rb.position = pos;
        rb.linearVelocity = vel;
        rb.angularVelocity = Vector3.zero;
        if (propios != null)
            foreach (Collider c in propios) if (c != null) { Physics.IgnoreCollision(col, c); ignorados.Add(c); }
        dueno = duenoNuevo; dano = danoNuevo; edad = 0f;
    }

    void Devolver()
    {
        if (!gameObject.activeSelf) return;
        rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
        libres.Push(this);
    }

    void Update()
    {
        edad += Time.deltaTime;
        // Solo se pierde en el agua cuando ya va cayendo (si no, una ola alta la borraría al salir del cañón)
        bool enAgua = edad > 0.2f && rb.linearVelocity.y < 0f && transform.position.y < Olas.Altura(transform.position) - 0.3f;
        if (enAgua) SonidosMundo.Uno("canon_agua", transform.position, 0.9f, 1f, 120f, 0.05f);
        if (edad > 10f || enAgua) { Devolver(); return; }
        if (edad > 0.15f) Atravesar();
    }

    const float DanoDirecto = 45f, DanoAstillas = 20f, RadioDirecto = 0.9f, RadioAstillas = 2.4f;

    // ¿Es gente del barco que disparó? (el jugador va en su barco, los tripulantes son hijos de él)
    bool EsAmigo(Transform t)
    {
        if (dueno == null) return false;
        if (t.IsChildOf(dueno.transform)) return true;
        Personaje p = Personaje.Instancia;
        if (p != null && t == p.transform)
        {
            ControlBarco suyo = p.BarcoActual != null ? p.BarcoActual : p.barco;
            return suyo != null && suyo.transform == dueno.transform;
        }
        return false;
    }

    // Golpe de lleno a quien esté en el camino de la bala
    void Atravesar()
    {
        Vector3 b = transform.position;
        Personaje p = Personaje.Instancia;
        if (p != null && !p.Bajo && !EsAmigo(p.transform) && Cerca(p.transform.position, b, RadioDirecto))
        {
            p.RecibirDano(DanoDirecto);
            SonidosMundo.Uno("espada_golpe", b, 0.8f, 0.7f, 30f, 0.1f);
            Devolver();
            return;
        }
        for (int i = Tripulante.Vivos.Count - 1; i >= 0; i--)
        {
            Tripulante t = Tripulante.Vivos[i];
            if (t == null || EsAmigo(t.transform) || !Cerca(t.transform.position, b, RadioDirecto)) continue;
            t.Dano(DanoDirecto * 2f);   // a un marinero lo tumba
            SonidosMundo.Uno("espada_golpe", b, 0.8f, 0.7f, 30f, 0.1f);
            Devolver();
            return;
        }
    }

    // El cuerpo es un cilindro de ~1,8 m: se mide al tronco, no a los pies
    static bool Cerca(Vector3 pies, Vector3 bala, float r)
    {
        Vector3 d = bala - (pies + Vector3.up * 0.9f);
        d.y = Mathf.Max(0f, Mathf.Abs(d.y) - 0.6f);
        return d.sqrMagnitude < r * r;
    }

    // Astillas: hieren a quien está cerca del impacto, menos cuanto más lejos
    void Astillas(Vector3 punto)
    {
        Personaje p = Personaje.Instancia;
        if (p != null && !p.Bajo && !EsAmigo(p.transform))
        {
            float d = Vector3.Distance(p.transform.position + Vector3.up * 0.9f, punto);
            if (d < RadioAstillas) p.RecibirDano(DanoAstillas * (1f - d / RadioAstillas));
        }
        for (int i = Tripulante.Vivos.Count - 1; i >= 0; i--)
        {
            Tripulante t = Tripulante.Vivos[i];
            if (t == null || EsAmigo(t.transform)) continue;
            float d = Vector3.Distance(t.transform.position + Vector3.up * 0.9f, punto);
            if (d < RadioAstillas) t.Dano(DanoAstillas * 1.5f * (1f - d / RadioAstillas));
        }
    }

    void OnCollisionEnter(Collision c)
    {
        BarrilLlamas barril = c.collider.GetComponentInParent<BarrilLlamas>();
        if (barril != null) { barril.Explotar(); Devolver(); return; }
        VidaBarco objetivo = c.collider.GetComponentInParent<VidaBarco>();
        SonidosMundo.Uno("canon_impacto", transform.position, objetivo != null ? 1f : 0.6f, 1f, 180f, 0.05f);
        if (objetivo != null && objetivo != dueno) objetivo.Dano(dano);
        if (objetivo != dueno) Astillas(transform.position);
        Devolver();
    }
}
