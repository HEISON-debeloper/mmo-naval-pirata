using UnityEngine;

// Espolón: la punta de bronce en la proa que parte barcos.
// Si embistes con la proa yendo hacia adelante, el otro barco recibe mucho más daño (más si eres más grande
// y vas más rápido) y tu barco casi no se daña. VidaBarco lo consulta en cada choque.
public class Espolon : MonoBehaviour
{
    public const float VelocidadMinima = 1.5f;   // m/s hacia adelante para que cuente como embestida

    Rigidbody rb;
    float largo;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        BoxCollider bc = GetComponent<BoxCollider>();
        largo = bc != null ? bc.size.z : 10f;
    }

    float Avance => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;

    // ¿Este choque lo dio mi proa yendo hacia adelante?
    public bool Golpea(Collision c)
    {
        if (Avance < VelocidadMinima) return false;
        for (int i = 0; i < c.contactCount; i++)
            if (transform.InverseTransformPoint(c.GetContact(i).point).z > 0.28f * largo) return true;
        return false;
    }

    // Cuánto se multiplica el daño que recibe el barco embestido
    public float Multiplicador(Rigidbody victima)
    {
        float masa = victima != null ? Mathf.Clamp(rb.mass / Mathf.Max(1f, victima.mass), 0.5f, 3f) : 1f;
        return 2.5f * masa;
    }

    // Aviso para el jugador cuando embiste él o lo embisten
    public static void Avisar(GameObject atacante, GameObject victima, float dano)
    {
        Personaje p = Personaje.Instancia;
        if (p == null || p.barco == null) return;
        if (p.barco.gameObject == atacante) p.Avisar("¡Embestida! " + Mathf.RoundToInt(dano) + " de daño con el espolón");
        else if (p.barco.gameObject == victima) p.Avisar("¡Te embistieron! " + Mathf.RoundToInt(dano) + " de daño");
    }
}
