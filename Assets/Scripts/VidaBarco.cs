using UnityEngine;

// Vida del casco. Al llegar a 0 el barco se hunde y reaparece donde empezó.
[RequireComponent(typeof(Rigidbody))]
public class VidaBarco : MonoBehaviour
{
    public float vidaMaxima = 100f;
    public float vida = 100f;
    [Tooltip("Segundos hundiéndose antes de reaparecer.")]
    public float tiempoReaparecer = 7f;
    [HideInInspector] public bool reaparece = true;   // los piratas del mundo infinito no reaparecen: se van al fondo
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
        OrigenFlotante.AlMover += Correr;
    }

    void Correr(Vector3 c) => posInicio -= c;   // el origen flotante corrió el mundo
    void OnDestroy() => OrigenFlotante.AlMover -= Correr;

    public void Dano(float cantidad)
    {
        if (Hundido) return;
        // En modo creativo tu barco no recibe daño
        if (ModoCreativo.Activo && Personaje.Instancia != null && Personaje.Instancia.barco != null && Personaje.Instancia.barco.gameObject == gameObject) return;
        cantidad *= Pactos.DanoRecibido(gameObject);   // Casco de Coral
        vida -= cantidad;
        if (vida <= 0f)
        {
            vida = 0f;
            Hundido = true;
            tiempoHundido = 0f;
            SonidosMundo.Uno("hundimiento", transform.position, 1f, 1f, 200f);
            BoxCollider bc = GetComponent<BoxCollider>();
            if (!Cubierta.EsPlana(GetComponent<ControlBarco>())) Pecios.Registrar(gameObject, bc != null ? bc.size.z : 20f);   // queda en el mar para siempre (las balsas no)
            if (flot != null) flot.activa = false;
            Debug.Log(name + " se hundió");
        }
    }

    void Update()
    {
        if (!Hundido) return;
        tiempoHundido += Time.deltaTime;
        if (tiempoHundido >= tiempoReaparecer) { if (reaparece) Reaparecer(); else Destroy(gameObject); }
    }

    // La fuerza que lo hunde va con la física (en Update dependía de los cuadros por segundo)
    void FixedUpdate()
    {
        if (Hundido) rb.AddForce(Vector3.down * 2f, ForceMode.Acceleration);
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
        // Tu barco vuelve a flote, pero la carga de la bodega se fue al fondo, y los piratas que te abordaron no vuelven con él
        Personaje p = Personaje.Instancia;
        if (p != null && p.barco != null && p.barco.gameObject == gameObject)
        {
            foreach (Tripulante t in GetComponentsInChildren<Tripulante>(true)) Destroy(t.gameObject);
            Bodega bd = GetComponent<Bodega>();
            if (bd != null && bd.Total > 0)
            {
                bd.Vaciar();
                Notificaciones.Mostrar("Tu barco volvió a flote, pero la carga de la bodega se perdió en el fondo", Notificaciones.Tipo.Alerta, 7f);
            }
        }
        alReaparecer?.Invoke();
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.GetComponentInParent<BalaCanon>() != null) return;
        if (c.collider.GetComponentInParent<BarrilLlamas>() != null) return;
        float impacto = c.relativeVelocity.magnitude;
        if (impacto <= 1.5f) return;
        float d = (impacto - 1.5f) * danoPorChoque;
        if (impacto > 3f) SonidosMundo.Uno("choque", c.contactCount > 0 ? c.GetContact(0).point : transform.position, Mathf.Clamp01(impacto / 10f), 1f, 150f, 0.8f);
        // Espolón: si embestí yo con la proa, mi barco casi no sufre; si me embistieron, sufro mucho más
        Espolon mio = GetComponent<Espolon>();
        if (mio != null && c.rigidbody != null && c.rigidbody.GetComponent<VidaBarco>() != null && mio.Golpea(c)) d *= 0.25f;   // el espolón protege contra barcos, no contra rocas
        Espolon suyo = c.rigidbody != null ? c.rigidbody.GetComponent<Espolon>() : null;
        if (suyo != null && suyo != mio && suyo.Golpea(c))
        {
            d *= suyo.Multiplicador(rb);
            Espolon.Avisar(suyo.gameObject, gameObject, d);
        }
        Dano(d);
    }
}
