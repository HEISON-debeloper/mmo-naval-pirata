using UnityEngine;

// Vida del casco. Al llegar a 0 el barco se hunde y reaparece donde empezó.
[RequireComponent(typeof(Rigidbody))]
public class VidaBarco : MonoBehaviour
{
    public float vidaMaxima = 100f;
    public float vida = 100f;
    [Tooltip("Segundos hundiéndose antes de reaparecer.")]
    public float tiempoReaparecer = 7f;
    [Tooltip("Daño por choque contra islas u otros barcos, por cada m/s de impacto.")]
    public float danoPorChoque = 6f;

    public bool Hundido { get; private set; }
    public event System.Action alReaparecer;

    Rigidbody rb;
    Flotacion flot;
    Vector3 posInicio;
    Quaternion rotInicio;
    float tiempoHundido;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        flot = GetComponent<Flotacion>();
        posInicio = transform.position;
        rotInicio = transform.rotation;
        vida = vidaMaxima;
    }

    public void Dano(float cantidad)
    {
        if (Hundido) return;
        // En modo creativo tu barco no recibe daño
        if (ModoCreativo.Activo && Personaje.Instancia != null && Personaje.Instancia.barco != null && Personaje.Instancia.barco.gameObject == gameObject) return;
        vida -= cantidad;
        if (vida <= 0f)
        {
            vida = 0f;
            Hundido = true;
            tiempoHundido = 0f;
            SonidosMundo.Uno("hundimiento", transform.position, 1f, 1f, 70f);
            if (flot != null) flot.activa = false;
            Debug.Log(name + " se hundió");
        }
    }

    void Update()
    {
        if (!Hundido) return;
        tiempoHundido += Time.deltaTime;
        rb.AddForce(Vector3.down * 2f, ForceMode.Acceleration);
        if (tiempoHundido >= tiempoReaparecer) Reaparecer();
    }

    public void Reaparecer()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = posInicio;
        rb.rotation = rotInicio;
        transform.SetPositionAndRotation(posInicio, rotInicio);
        vida = vidaMaxima;
        Hundido = false;
        if (flot != null) flot.activa = true;
        alReaparecer?.Invoke();
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.GetComponentInParent<BalaCanon>() != null) return;
        if (c.collider.GetComponentInParent<BarrilLlamas>() != null) return;
        float impacto = c.relativeVelocity.magnitude;
        if (impacto <= 1.5f) return;
        float d = (impacto - 1.5f) * danoPorChoque;
        if (impacto > 3f) SonidosMundo.Uno("choque", c.contactCount > 0 ? c.GetContact(0).point : transform.position, Mathf.Clamp01(impacto / 10f), 1f, 50f, 0.8f);
        // Espolón: si embestí yo con la proa, mi barco casi no sufre; si me embistieron, sufro mucho más
        Espolon mio = GetComponent<Espolon>();
        if (mio != null && mio.Golpea(c)) d *= 0.25f;
        Espolon suyo = c.rigidbody != null ? c.rigidbody.GetComponent<Espolon>() : null;
        if (suyo != null && suyo != mio && suyo.Golpea(c))
        {
            d *= suyo.Multiplicador(rb);
            Espolon.Avisar(suyo.gameObject, gameObject, d);
        }
        Dano(d);
    }
}
