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

    // Gira sola alrededor del barco (mientras el Taller está abierto)
    public static bool Orbitar;
    // Arrastre con el dedo (teléfono): lo escribe ControlesTactiles y se suma al movimiento del mouse
    public static Vector2 DeltaTactil;
    Vector2 TomarDelta(Mouse m)
    {
        Vector2 d = m != null && CursorCapturado ? m.delta.ReadValue() : Vector2.zero;
        d += DeltaTactil; DeltaTactil = Vector2.zero;
        return d;
    }

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
        // Con una ventana, el menú o el mapa abiertos el cursor queda libre; al cerrarse el último, vuelve al juego
        bool abierto = Fabricacion.Abierto || Navegacion.MapaAbierto;
        if (mapaAntes && !abierto) Capturar(true);
        else if (abierto) { if (CursorCapturado) Capturar(false); }
        // Esc sin nada abierto suelta el cursor
        else if (k != null && k.escapeKey.wasPressedThisFrame) { if (CursorCapturado) Capturar(false); }
        // Un clic en el juego (no sobre la interfaz) lo vuelve a capturar
        else if (!CursorCapturado && m != null && m.leftButton.wasPressedThisFrame && !Lienzo.PunteroSobreUI)
            Capturar(true);
        mapaAntes = abierto;
    }

    void OnApplicationFocus(bool foco) { if (foco && !Fabricacion.Abierto && !Navegacion.MapaAbierto) Capturar(true); }

    void OnDisable() => Capturar(false);

    // Recorre la línea del foco a la cámara y se detiene antes de lo sólido:
    // el casco (bajo la cubierta) y el castillo de popa del barco donde estás, y cualquier collider que no sea un barco.
    readonly RaycastHit[] golpes = new RaycastHit[48];
    readonly System.Collections.Generic.Dictionary<Collider, bool> deBarco = new System.Collections.Generic.Dictionary<Collider, bool>();

    // ¿El collider es parte de un barco o de una persona? (se recuerda: un barco tiene cientos de piezas)
    bool EsDeBarco(Collider c)
    {
        if (deBarco.TryGetValue(c, out bool si)) return si;
        if (deBarco.Count > 4000) deBarco.Clear();
        si = c.GetComponentInParent<ControlBarco>() != null || c.GetComponentInParent<Personaje>() != null;
        deBarco[c] = si;
        return si;
    }

    Vector3 Despejar(Vector3 foco, Vector3 pos, out bool tapado)
    {
        tapado = false;
        Vector3 d = pos - foco;
        float largo = d.magnitude;
        if (largo < 0.01f) return pos;
        Vector3 dir = d / largo;
        float libre = largo;

        // Islas y rocas (colliders que no son barcos ni personajes)
        int nGolpes = Physics.SphereCastNonAlloc(foco, 0.3f, dir, golpes, largo, ~0, QueryTriggerInteraction.Ignore);   // sin crear basura cada cuadro
        for (int g = 0; g < nGolpes; g++)
        {
            RaycastHit hit = golpes[g];
            if (hit.distance >= libre || hit.collider is CharacterController) continue;
            if (EsDeBarco(hit.collider)) continue;
            libre = hit.distance;
        }

        // Los barcos cercanos (el tuyo y los demás): el casco y el castillo son sólidos para la cámara
        foreach (ControlBarco b in ControlBarco.Todos)
        {
            if (b == null) continue;
            if ((b.transform.position - foco).sqrMagnitude > (largo + 120f) * (largo + 120f)) continue;   // lejos: ni mirarlo
            BoxCollider bc = b.GetComponent<BoxCollider>();
            if (bc == null) continue;
            Cubierta.Medidas(bc, out float w, out float h, out float L);
            // ¿pasa la línea cerca de este barco?
            Vector3 c = b.transform.position;
            float tc = Mathf.Clamp(Vector3.Dot(c - foco, dir), 0f, libre);
            if (Vector3.Distance(foco + dir * tc, c) > L * 0.6f + h + 3f) continue;
            float limite = DentroDeBarco(b, foco + dir * 0.05f, w, h, L) ? -1f : libre;   // si el foco ya está adentro (raro), no se corta
            if (limite < 0f) continue;
            int pasos = Mathf.Clamp(Mathf.CeilToInt(libre / 0.25f), 8, 160);
            for (int i = 1; i <= pasos; i++)
            {
                float t = libre * i / pasos;
                if (DentroDeBarco(b, foco + dir * t, w, h, L)) { libre = Mathf.Max(0.3f, libre * (i - 1) / pasos); break; }
            }
        }
        if (libre >= largo - 0.01f) return pos;
        tapado = true;
        return foco + dir * Mathf.Max(0.3f, libre - 0.2f);
    }

    static bool DentroDeBarco(ControlBarco b, Vector3 p, float w, float h, float L)
    {
        Vector3 l = b.transform.InverseTransformPoint(p);
        if (Cubierta.EsPlana(b)) return l.y < h / 2f + 0.15f && l.y > -h / 2f - 0.3f && Mathf.Abs(l.x) < w / 2f + 0.2f && Mathf.Abs(l.z) < L / 2f + 0.2f;
        if (l.y < h / 2f + CascoProcedural.AltoBorda(h) + 0.2f && l.y > -h / 2f - 0.3f && l.z > CascoProcedural.ZPopa(L) - 0.25f && l.z < CascoProcedural.ZProa(w, L) + 0.25f)
        {
            float borde = CascoProcedural.Costado(l.z, Mathf.Min(l.y, h / 2f), w, h, L);
            // bajo la cubierta: todo el casco; sobre la cubierta: solo la borda (la franja del borde)
            if (l.y < h / 2f + 0.15f) { if (Mathf.Abs(l.x) < borde + 0.25f) return true; }
            else if (Mathf.Abs(l.x) < borde + 0.25f && Mathf.Abs(l.x) > borde - CascoProcedural.GrosorBorda(w) - 0.25f) return true;
        }
        Transform castillo = b.transform.Find("Camarote");
        if (castillo != null)
        {
            float cx = castillo.localScale.x / 2f + 0.25f;
            float cz0 = Cubierta.CentroCamarote(L) - Cubierta.LargoCamarote(L) / 2f - 0.25f, cz1 = Cubierta.CentroCamarote(L) + Cubierta.LargoCamarote(L) / 2f + 0.25f;
            if (Mathf.Abs(l.x) < cx && l.z > cz0 && l.z < cz1 && l.y > h / 2f && l.y < h / 2f + Cubierta.AltoCamaroteDe(h) + 0.25f) return true;
        }
        return false;
    }

    // Primera persona dentro del barco (cubiertas de abajo, bodega y camarote)
    float yawLocal, pitchFP;
    bool primeraPersona;

    Camera cam;
    float lejosAntes = 1500f;
    bool PrimeraPersona()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Personaje p = Personaje.Instancia;
        bool dentro = p != null && p.Bajo;
        if (!dentro)
        {
            if (primeraPersona && p != null && p.BarcoActual != null) anguloH = p.BarcoActual.transform.eulerAngles.y + yawLocal;
            if (primeraPersona && cam != null) { cam.nearClipPlane = 0.3f; cam.farClipPlane = lejosAntes; }
            primeraPersona = false;
            return false;
        }
        Transform barco = p.BarcoActual.transform;
        if (!primeraPersona) { yawLocal = anguloH - barco.eulerAngles.y; pitchFP = 0f; primeraPersona = true; }
        Mouse m = Mouse.current;
        Vector2 d = TomarDelta(m);
        if (d.sqrMagnitude > 0f)
        {
            yawLocal += d.x * sensibilidad;
            pitchFP = Mathf.Clamp(pitchFP + (invertirVertical ? d.y : -d.y) * sensibilidad, -75f, 75f);
        }
        transform.position = p.Ojos;
        if (cam != null && cam.nearClipPlane > 0.06f)
        {
            // Pegado a la pared no se ve a través del casco; y como el plano cercano baja tanto, el lejano también
            // (si no, la precisión de profundidad no alcanza y las superficies titilan)
            lejosAntes = cam.farClipPlane;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = Mathf.Min(lejosAntes, 400f);
        }
        transform.rotation = barco.rotation * Quaternion.Euler(pitchFP, yawLocal, 0f);
        return true;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        if (PrimeraPersona()) return;

        Mouse m = Mouse.current;
        Vector2 delta = TomarDelta(m);
        {
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
            float rueda = m != null ? m.scroll.ReadValue().y : 0f;
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

        if (Orbitar) anguloH += (MenuPrincipal.Visible ? 6f : 14f) * Time.unscaledDeltaTime;   // en el menú principal gira más despacio (toma cinematográfica)

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

        // No atravesar paredes: el casco y el castillo de tu barco, y las islas
        bool tapado = false;
        if (mezcla <= 0.5f) posDeseada = Despejar(foco, posDeseada, out tapado);

        Vector3 siguiente = tapado ? posDeseada : Vector3.Lerp(transform.position, posDeseada, suavizado * Time.deltaTime);
        // En el camino (mientras se suaviza) tampoco puede quedar detrás de una pared
        if (mezcla <= 0.5f) siguiente = Despejar(foco, siguiente, out _);
        transform.position = siguiente;
        Quaternion objetivoRot = Quaternion.LookRotation(mirarDeseado - transform.position);
        transform.rotation = Quaternion.Angle(transform.rotation, objetivoRot) > 1f
            ? Quaternion.Slerp(transform.rotation, objetivoRot, suavizado * 1.5f * Time.deltaTime) : objetivoRot;
    }
}
