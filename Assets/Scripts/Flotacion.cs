using UnityEngine;

// Hace flotar un Rigidbody sobre las olas.
// Los "puntos de flotación" (objetos vacíos hijos) van en las esquinas de abajo del casco.
[RequireComponent(typeof(Rigidbody))]
public class Flotacion : MonoBehaviour
{
    [Tooltip("Puntos donde se aplica el empuje. Si está vacío, usa el centro.")]
    public Transform[] puntosFlotacion;

    [Tooltip("Empuje por metro sumergido (aceleración). Más alto = el barco flota más alto.")]
    public float fuerzaEmpuje = 8f;

    [Tooltip("Frena el rebote vertical.")]
    public float amortiguacion = 1.5f;

    [Tooltip("Arrastre en el agua.")]
    public float arrastreAgua = 0.33f;
    public float arrastreAngularAgua = 1.6f;

    [Tooltip("Centro de masa bajo para que el barco no se voltee.")]
    public Vector3 centroDeMasa = new Vector3(0f, -1.2f, 0f);

    [Tooltip("Fuerza que endereza el barco, como la quilla. 0 = sin ayuda.")]
    public float estabilidad = 3f;

    [HideInInspector] public bool activa = true;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // En Start (no en Awake) para respetar valores puestos al crear el barco desde código
    void Start() => rb.centerOfMass = centroDeMasa;

    void FixedUpdate()
    {
        if (!activa) return;

        Transform[] puntos = (puntosFlotacion != null && puntosFlotacion.Length > 0)
            ? puntosFlotacion
            : new[] { transform };

        bool tocaAgua = false;
        foreach (Transform p in puntos)
        {
            float profundidad = Olas.Altura(p.position) - p.position.y;
            if (profundidad <= 0f) continue;

            tocaAgua = true;
            Vector3 velPunto = rb.GetPointVelocity(p.position);
            float empuje = profundidad * fuerzaEmpuje / puntos.Length;
            float freno = -velPunto.y * amortiguacion / puntos.Length;
            rb.AddForceAtPosition(Vector3.up * (empuje + freno) * rb.mass, p.position, ForceMode.Force);
        }

        // Quilla: empuja el mástil de vuelta hacia arriba
        if (tocaAgua && estabilidad > 0f)
            rb.AddTorque(Vector3.Cross(transform.up, Vector3.up) * estabilidad, ForceMode.Acceleration);

        rb.linearDamping = tocaAgua ? arrastreAgua : 0.05f;
        rb.angularDamping = tocaAgua ? arrastreAngularAgua : 0.05f;
    }
}
