using UnityEngine;

// Bala de cañón: daña al barco que toca y desaparece al caer al agua.
[RequireComponent(typeof(Rigidbody))]
public class BalaCanon : MonoBehaviour
{
    [HideInInspector] public VidaBarco dueno;
    [HideInInspector] public float dano = 12f;
    float edad;
    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Update()
    {
        edad += Time.deltaTime;
        // Solo se pierde en el agua cuando ya va cayendo (si no, una ola alta la borraría al salir del cañón)
        bool enAgua = edad > 0.2f && rb.linearVelocity.y < 0f && transform.position.y < Olas.Altura(transform.position) - 0.3f;
        if (enAgua) SonidosMundo.Uno("canon_agua", transform.position, 0.9f, 1f, 35f, 0.05f);
        if (edad > 10f || enAgua) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision c)
    {
        BarrilLlamas barril = c.collider.GetComponentInParent<BarrilLlamas>();
        if (barril != null) { barril.Explotar(); Destroy(gameObject); return; }
        VidaBarco objetivo = c.collider.GetComponentInParent<VidaBarco>();
        SonidosMundo.Uno("canon_impacto", transform.position, objetivo != null ? 1f : 0.6f, 1f, 45f, 0.05f);
        if (objetivo != null && objetivo != dueno) objetivo.Dano(dano);
        Destroy(gameObject);
    }
}
