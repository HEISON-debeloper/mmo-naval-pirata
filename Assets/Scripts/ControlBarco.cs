using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Control del barco: W/S velas, A/D timón, R ancla.
// Lo maneja el jugador cuando está al timón (controlActivo) o la IA por medio de timonExterno y FijarVelas.
[RequireComponent(typeof(Rigidbody))]
public class ControlBarco : MonoBehaviour
{
    public static readonly List<ControlBarco> Todos = new List<ControlBarco>();

    [Header("Velas")]
    [Tooltip("Niveles de vela: 0 = arriadas, el máximo = todas desplegadas.")]
    public int nivelesVela = 3;
    [Tooltip("Aceleración por nivel de vela (m/s²).")]
    public float fuerzaPorNivel = 0.7f;
    [Tooltip("La vela (objeto hijo) que se estira según el nivel.")]
    public Transform vela;
    [Tooltip("Velas de los otros mástiles (barcos grandes); suben y bajan junto con la principal.")]
    public Transform[] velasExtra;

    [Header("Timón")]
    [Tooltip("Aceleración de giro (rad/s²).")]
    public float fuerzaGiro = 0.35f;
    [Tooltip("El timón casi no gira el barco si va muy lento.")]
    public float velocidadParaGiroCompleto = 3f;

    [Header("Agua")]
    [Tooltip("Evita que el barco se deslice de lado como en hielo.")]
    public float agarreLateral = 2f;

    [Header("Control")]
    [Tooltip("Se maneja con el teclado solo cuando el jugador está al timón.")]
    public bool controlActivo = true;
    [Tooltip("Muestra el panel de este barco en pantalla.")]
    public bool mostrarPanel = true;
    [HideInInspector] public float timonExterno;

    public int NivelVelaActual { get; private set; }
    public bool Anclado { get; private set; }

    Rigidbody rb;
    VidaBarco vida;
    float entradaTimon;
    float altoVela = -1f;
    float[] altoExtras;

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vida = GetComponent<VidaBarco>();
        if (vela != null) altoVela = vela.localScale.y;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        bool hundido = vida != null && vida.Hundido;

        entradaTimon = 0f;
        if (controlActivo && k != null)
        {
            if (!hundido && k.wKey.wasPressedThisFrame) NivelVelaActual = Mathf.Min(NivelVelaActual + 1, nivelesVela);
            if (k.sKey.wasPressedThisFrame) NivelVelaActual = Mathf.Max(NivelVelaActual - 1, 0);
            if (k.rKey.wasPressedThisFrame) AlternarAncla();
            if (k.aKey.isPressed) entradaTimon -= 1f;
            if (k.dKey.isPressed) entradaTimon += 1f;
        }
        else entradaTimon = Mathf.Clamp(timonExterno, -1f, 1f);
        if (hundido) NivelVelaActual = 0;

        if (vela != null)
        {
            if (altoVela < 0f) altoVela = vela.localScale.y; // por si la vela se asignó después de Awake
            float objetivo = (NivelVelaActual == 0 || Anclado) ? 0.08f : (float)NivelVelaActual / nivelesVela;
            Vector3 s = vela.localScale;
            s.y = Mathf.Lerp(s.y, altoVela * objetivo, Time.deltaTime * 3f);
            vela.localScale = s;

            if (velasExtra != null && velasExtra.Length > 0)
            {
                if (altoExtras == null || altoExtras.Length != velasExtra.Length)
                {
                    altoExtras = new float[velasExtra.Length];
                    for (int i = 0; i < velasExtra.Length; i++) altoExtras[i] = velasExtra[i] != null ? velasExtra[i].localScale.y : 0f;
                }
                for (int i = 0; i < velasExtra.Length; i++)
                {
                    if (velasExtra[i] == null) continue;
                    Vector3 se = velasExtra[i].localScale;
                    se.y = Mathf.Lerp(se.y, altoExtras[i] * objetivo, Time.deltaTime * 3f);
                    velasExtra[i].localScale = se;
                    // El foque y la cangreja recogidos no se ven como una tabla: se esconden
                    if (velasExtra[i].name == Astillero.VelaCuchillo && velasExtra[i].childCount > 0)
                    {
                        bool ver = se.y > altoExtras[i] * 0.15f;
                        GameObject lona = velasExtra[i].GetChild(0).gameObject;
                        if (lona.activeSelf != ver) lona.SetActive(ver);
                    }
                }
            }
        }
    }

    public void AlternarAncla() => Anclado = !Anclado;
    public void FijarVelas(int nivel) => NivelVelaActual = Mathf.Clamp(nivel, 0, nivelesVela);

    void FixedUpdate()
    {
        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);

        if (!Anclado)
            rb.AddForce(adelante * NivelVelaActual * fuerzaPorNivel, ForceMode.Acceleration);
        else
        {
            // El ancla frena el barco casi por completo
            rb.AddForce(-Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up) * 2.5f, ForceMode.Acceleration);
            rb.AddTorque(-Vector3.up * rb.angularVelocity.y * 3f, ForceMode.Acceleration);
        }

        float velAdelante = Vector3.Dot(rb.linearVelocity, adelante);
        float factor = Mathf.Clamp01(Mathf.Abs(velAdelante) / velocidadParaGiroCompleto);
        rb.AddTorque(Vector3.up * entradaTimon * fuerzaGiro * factor, ForceMode.Acceleration);

        float velLateral = Vector3.Dot(rb.linearVelocity, derecha);
        rb.AddForce(-derecha * velLateral * agarreLateral, ForceMode.Acceleration);
    }

}
