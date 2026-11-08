using UnityEngine;
using UnityEngine.InputSystem;

// Cámara en tercera persona: se gira con solo mover el mouse (sin mantener ningún clic).
// El cursor queda oculto mientras juegas. Esc o Tab (menú) lo liberan; un clic en el juego lo vuelve a capturar.
// Rueda del mouse: acercar o alejar.
public class CamaraBarco : MonoBehaviour
{
    public Transform objetivo;
    [Tooltip("Si está asignado (el personaje al timón), al acercar del todo la cámara mira a este punto en vez del centro del barco.")]
    public Transform focoCerca;
    public float alturaCerca = 1.7f;
    [Tooltip("Largo del barco al timón (0 a pie). Sirve para encuadrar la puntería.")]
    public float largoBarco;
    bool bloqueado;
    float mezcla;

    // Hacia dónde gira la cámara en el plano (con el mouse a los lados): elige el costado al apuntar
    public Vector3 DireccionHorizontal => Quaternion.Euler(0f, anguloH, 0f) * Vector3.forward;
    public float distancia = 48f;
    public float distanciaMin = 20f;
    public float distanciaMax = 140f;
    public float altura = 6f;
    [Tooltip("Grados por píxel de movimiento del mouse.")]
    public float sensibilidad = 0.12f;
    public bool invertirVertical = false;
    public float suavizado = 8f;
    [Tooltip("Si está activo, la cámara vuelve sola detrás del objetivo cuando no mueves el mouse (útil al timón).")]
    public bool seguirRumbo = true;
    [Tooltip("Segundos sin mover el mouse antes de que la cámara vuelva detrás del barco.")]
    public float esperaRecentrar = 2f;

    float anguloH;
    float anguloV = 16f;
    float tQuieto;

    static bool mapaAntes;

    public static bool CursorCapturado => Cursor.lockState == CursorLockMode.Locked;

    void Start()
    {
        if (objetivo != null) anguloH = objetivo.eulerAngles.y;
        Capturar(true);
    }

    static void Capturar(bool si)
    {
        Cursor.lockState = si ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !si;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        // Al cerrar el mapa se vuelve a capturar el cursor
        if (mapaAntes && !Navegacion.MapaAbierto && !Fabricacion.Abierto) Capturar(true);
        mapaAntes = Navegacion.MapaAbierto;
        // Liberar el cursor con Esc, al abrir el menú de fabricación o el mapa (para arrastrarlo con el mouse)
        if ((k != null && k.escapeKey.wasPressedThisFrame) || Fabricacion.Abierto || Navegacion.MapaAbierto)
        {
            if (CursorCapturado) Capturar(false);
        }
        // Un clic en el juego (con el menú cerrado) lo vuelve a capturar
        else if (!CursorCapturado && m != null && m.leftButton.wasPressedThisFrame)
            Capturar(true);
    }

    void OnApplicationFocus(bool foco) { if (foco && !Fabricacion.Abierto && !Navegacion.MapaAbierto) Capturar(true); }

    void OnDisable() => Capturar(false);

    void LateUpdate()
    {
        if (objetivo == null) return;

        Mouse m = Mouse.current;
        if (m != null)
        {
            Vector2 delta = CursorCapturado ? m.delta.ReadValue() : Vector2.zero;
            if (delta.sqrMagnitude > 0.01f)
            {
                anguloH += delta.x * sensibilidad;
                // Apuntando, el mouse vertical sube o baja el tiro (lo maneja Punteria), no la cámara
                if (!Punteria.Apuntando)
                    anguloV = Mathf.Clamp(anguloV + (invertirVertical ? delta.y : -delta.y) * sensibilidad, -5f, 80f);
                tQuieto = 0f;
            }
            else tQuieto += Time.deltaTime;

            // Al timón, si no mueves el mouse un rato, la cámara vuelve sola detrás del barco
            if (seguirRumbo && tQuieto > esperaRecentrar && !Punteria.Apuntando)   // apuntando, la cámara se queda donde miras
                anguloH = Mathf.LerpAngle(anguloH, objetivo.eulerAngles.y, Time.deltaTime);

            // Zoom proporcional: cada paso de la rueda acerca o aleja un 12 %, así sirve igual de cerca que de lejos
            float rueda = m.scroll.ReadValue().y;
            if (Mathf.Abs(rueda) > 0.01f && !Navegacion.MapaAbierto)   // con el mapa abierto la rueda es del mapa
            {
                // Según el sistema, un paso de rueda llega como 1 o como 120
                float pasos = Mathf.Abs(rueda) >= 20f ? rueda / 120f : rueda;
                distancia = Mathf.Clamp(distancia * Mathf.Pow(0.85f, pasos), distanciaMin, distanciaMax);
            }
        }

        // Celular: pellizcar con dos dedos acerca o aleja
        var pantalla = UnityEngine.InputSystem.Touchscreen.current;
        if (pantalla != null && pantalla.touches.Count > 1 && pantalla.touches[0].isInProgress && pantalla.touches[1].isInProgress)
        {
            Vector2 a = pantalla.touches[0].position.ReadValue(), b = pantalla.touches[1].position.ReadValue();
            Vector2 da = pantalla.touches[0].delta.ReadValue(), db = pantalla.touches[1].delta.ReadValue();
            float antes = Vector2.Distance(a - da, b - db), ahora = Vector2.Distance(a, b);
            if (antes > 1f) distancia = Mathf.Clamp(distancia * antes / ahora, distanciaMin, distanciaMax);
        }

        // Pose normal: girando alrededor del timón
        Quaternion rot = Quaternion.Euler(anguloV, anguloH, 0f);
        Vector3 foco = objetivo.position + Vector3.up * altura;
        if (focoCerca != null)
        {
            // Muy cerca se mira al personaje en el timón; al alejarse, al centro del barco
            float t = Mathf.InverseLerp(distanciaMin, distanciaMin + 15f, distancia);
            foco = Vector3.Lerp(focoCerca.position + Vector3.up * alturaCerca, foco, t);
        }
        Vector3 posDeseada = foco - rot * Vector3.forward * distancia;
        Vector3 mirarDeseado = foco;

        // Puntería: la cámara solo se engancha al fantasma si, con el botón de apuntar oprimido,
        // estás mirando hacia donde caen las balas y ese lado tiene cañones. Entra y sale suave.
        if (Punteria.Apuntando && Punteria.HayCanonesEnLado)
        {
            // Se engancha cuando miras hacia ese costado (no importa lo largo que sea el barco)
            if (!bloqueado && Vector3.Angle(DireccionHorizontal, Punteria.DireccionLado) < 45f) bloqueado = true;
        }
        else bloqueado = false;
        mezcla = Mathf.MoveTowards(mezcla, bloqueado ? 1f : 0f, Time.deltaTime / 0.5f);

        if (mezcla > 0f)
        {
            // Como en Black Flag: la cámara se queda junto a ti en el timón (por encima del hombro),
            // de frente a la zona donde caen las balas. No se va para arriba: solo se inclina con el alcance
            // (cerca mira al agua junto al casco, lejos casi al horizonte). En barcos grandes queda un poco
            // más alta para ver por encima de la borda.
            Vector3 hombro = (focoCerca != null ? focoCerca.position : objetivo.position) + Vector3.up * 2f;
            Vector3 caida = Punteria.PuntoCaida;
            // De frente al costado, girada un poco hacia el centro de la zona de caída
            // (en un barco largo la cinta queda adelante del timón)
            Vector3 dir = caida - hombro; dir.y = 0f;
            dir = dir.sqrMagnitude > 1f ? dir.normalized : Punteria.DireccionLado;
            dir = Vector3.Slerp(Punteria.DireccionLado, dir, 0.5f); dir.y = 0f; dir.Normalize();
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            float atras = 6f + largoBarco * 0.05f;
            float arriba = 2.8f + largoBarco * 0.1f;
            Vector3 posApunte = hombro - dir * atras + Vector3.up * arriba + lado * 2.5f;   // corrida a un lado: tu personaje no tapa el centro
            Vector3 hacia = caida - posApunte;
            float plano = new Vector2(hacia.x, hacia.z).magnitude;
            float pitch = Mathf.Clamp(Mathf.Atan2(hacia.y, Mathf.Max(plano, 0.1f)) * Mathf.Rad2Deg + 8f, -50f, 6f);
            Vector3 mirar = posApunte + Quaternion.AngleAxis(-pitch, lado) * dir * 50f;
            float k = mezcla * mezcla * (3f - 2f * mezcla);
            posDeseada = Vector3.Lerp(posDeseada, posApunte, k);
            mirarDeseado = Vector3.Lerp(mirarDeseado, mirar, k);
        }
        posDeseada.y = Mathf.Max(posDeseada.y, Olas.Altura(posDeseada) + 1f);

        transform.position = Vector3.Lerp(transform.position, posDeseada, suavizado * Time.deltaTime);
        Quaternion objetivoRot = Quaternion.LookRotation(mirarDeseado - transform.position);
        transform.rotation = Quaternion.Angle(transform.rotation, objetivoRot) > 1f
            ? Quaternion.Slerp(transform.rotation, objetivoRot, suavizado * 1.5f * Time.deltaTime) : objetivoRot;
    }
}
