using UnityEngine;
using UnityEngine.InputSystem;

// Pirata del jugador: toma el timón, camina por la cubierta de cualquier barco mientras navega,
// salta por la borda, nada, camina en las islas, aborda barcos enemigos y pelea con espada.
// F = timón / subir al barco · WASD = caminar · Shift = correr · Espacio = saltar
// R = ancla (en cubierta) · Clic izquierdo o K = espadazo
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CharacterController))]
public class Personaje : MonoBehaviour
{
    public enum Estado { Timon, Cubierta, Libre }

    public static Personaje Instancia { get; private set; }

    [Header("Referencias")]
    [Tooltip("Tu barco: reapareces en su timón. Vacío = empiezas a pie en la playa, sin nada.")]
    public ControlBarco barco;
    [Tooltip("Isla donde apareces (en una playa al azar) cuando no tienes barco.")]
    public Transform islaInicio;
    public float radioIslaInicio = 40f;
    public CamaraBarco camara;
    public Transform espada;

    [Header("Movimiento (metros y segundos)")]
    public float velocidadCaminar = 3.2f;
    public float velocidadCorrer = 6.5f;
    public float velocidadNadar = 2.2f;
    public float fuerzaSalto = 5.5f;
    public float gravedad = 9.8f;
    [Tooltip("Qué tan hundido flota el personaje al nadar (desde los pies).")]
    public float profundidadFlotar = 1.3f;

    [Header("Combate")]
    public float vidaMaxima = 100f;
    public float danoEspada = 50f;
    public float alcanceEspada = 2.2f;
    public float recargaEspada = 0.55f;

    [Header("Construcción")]
    public Material materialMadera;
    public Material materialLona;
    [Tooltip("Distancia máxima para recolectar de una palma o roca.")]
    public float alcanceRecolectar = 2.5f;

    [Header("Cámara")]
    public float distanciaCamaraBarco = 48f;
    public float distanciaCamaraPie = 7f;

    public Estado estado = Estado.Timon;
    public bool Nadando { get; private set; }
    public bool EnTierra { get; private set; }
    public float Vida { get; private set; }
    public ControlBarco BarcoActual { get; private set; }
    public Vector3 LocalEnBarco => local;
    // Interiores: 0 = cubierta de arriba, 1.. = cubiertas de cañones y bodega, -1 = camarote del capitán
    int nivel;
    public int Nivel => nivel;
    public bool Bajo => estado == Estado.Cubierta && nivel != 0 && BarcoActual != null;
    Interior interior;
    // Ojos del personaje (la cámara en primera persona adentro del barco)
    public Vector3 Ojos => barcoT.TransformPoint(local + Vector3.up * (interior != null ? interior.AlturaOjos(nivel) : 1.6f));
    public bool SobreBarco(ControlBarco b) => estado != Estado.Libre && BarcoActual == b;

    CharacterController cc;
    Transform barcoT;
    Rigidbody barcoRb;
    Canones canones;
    VidaBarco vidaBarco;
    float w, h, L;
    bool plana;
    Inventario inv;
    Supervivencia sup;
    Vector3 local;   // posición sobre la cubierta, en coordenadas del barco actual
    Vector3 vel;     // velocidad cuando está libre (tierra, agua o aire)
    float yaw, tEspada, tAnim, tAviso, tGolpe;
    string aviso = "";

    float AlturaCubierta => Cubierta.Altura(h);
    Vector3 PuntoTimon => Cubierta.Timon(h, L, plana);

    void Awake()
    {
        Instancia = this;
        cc = GetComponent<CharacterController>();
        inv = GetComponent<Inventario>();
        sup = GetComponent<Supervivencia>();
        Vida = vidaMaxima;
        if (barco != null) Asignar(barco);
    }

    void Start()
    {
        if (barco == null) { estado = Estado.Libre; AparecerEnPlaya(); }
        else CambiarEstado(estado);
    }

    // Una playa al azar de la isla inicial, con las manos vacías
    void AparecerEnPlaya()
    {
        if (islaInicio == null) { CambiarEstado(Estado.Libre); return; }
        float a = Random.Range(0f, Mathf.PI * 2f);
        Vector3 p = islaInicio.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radioIslaInicio * 0.72f;
        // Solo cuenta el suelo de la isla (no la copa de una palma ni una roca)
        float mejorY = float.MinValue;
        foreach (RaycastHit h in Physics.RaycastAll(p + Vector3.up * 60f, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore))
            if ((h.collider.name == "Playa" || h.collider.name == "Colina") && h.point.y > mejorY) mejorY = h.point.y;
        if (mejorY > float.MinValue) p.y = mejorY;
        Teletransportar(p + Vector3.up * 0.3f);
        // Mirar hacia el mar
        yaw = Mathf.Atan2(p.x - islaInicio.position.x, p.z - islaInicio.position.z) * Mathf.Rad2Deg;
    }

    // El último barco que construyes pasa a ser el tuyo (donde reapareces)
    public void AsignarBarcoPropio(ControlBarco b) => barco = b;
    public void FijarVida(float v) => Vida = Mathf.Clamp(v, 1f, vidaMaxima);

    void Asignar(ControlBarco b)
    {
        BarcoActual = b;
        nivel = 0;
        interior = null;
        barcoT = b.transform;
        barcoRb = b.GetComponent<Rigidbody>();
        canones = b.GetComponent<Canones>();
        vidaBarco = b.GetComponent<VidaBarco>();
        Cubierta.Medidas(b.GetComponent<BoxCollider>(), out w, out h, out L);
        plana = Cubierta.EsPlana(b);
    }

    // Aviso en pantalla: va al sistema de notificaciones (globo a la derecha + bitácora)
    public void Avisar(string texto) { aviso = texto; tAviso = 3.5f; Notificaciones.Mostrar(texto, Notificaciones.Adivinar(texto)); }

    // Para pruebas: pone al personaje a pie en un punto
    public void Teletransportar(Vector3 p)
    {
        CambiarEstado(Estado.Libre);
        cc.enabled = false;
        transform.position = p;
        cc.enabled = true;
        vel = Vector3.zero;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        float dt = Time.deltaTime;
        tAviso -= dt;
        tGolpe -= dt;
        bool usar = Teclas.Pulsada(Accion.Usar);
        bool saltar = Teclas.Pulsada(Accion.Saltar);
        bool correr = Teclas.Presionada(Accion.Correr);
        bool atacar = Teclas.Pulsada(Accion.Espada) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !Ventanas.AlgunaAbierta && !Lienzo.PunteroSobreUI);

        Vector2 inp = Vector2.zero;
        if (estado != Estado.Timon)
        {
            if (Teclas.Presionada(Accion.Adelante)) inp.y += 1f;
            if (Teclas.Presionada(Accion.Atras)) inp.y -= 1f;
            if (Teclas.Presionada(Accion.Derecha)) inp.x += 1f;
            if (Teclas.Presionada(Accion.Izquierda)) inp.x -= 1f;
            inp = Vector2.ClampMagnitude(inp, 1f);
        }
        float camYaw = camara != null ? camara.transform.eulerAngles.y : 0f;
        Vector3 dir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(inp.x, 0f, inp.y);
        if (dir.sqrMagnitude > 0.01f) yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // Clic izquierdo: espada si hay un enemigo cerca; si no, golpear con hacha o pico (palmas, rocas, hierro)
        tEspada -= dt;
        // (el clic que recaptura el cursor no cuenta como golpe)
        if (atacar && estado != Estado.Timon && !Nadando && tEspada <= 0f && !Fabricacion.Abierto && CamaraBarco.CursorCapturado)
        {
            if (EnemigoCerca()) Atacar();
            else Golpear();
        }
        // F: recoger con la mano lo que esté cerca (fibra, ramas, piedras, frutas, cocos)
        bool recogio = false;
        if (usar && estado == Estado.Libre && !Nadando && !Fabricacion.Abierto)
        {
            Recurso r = RecursoCercano(false);
            if (r != null) { Recoger(r); recogio = true; }
        }
        if (Teclas.Pulsada(Accion.Comer)) Comer();

        bool hundido = vidaBarco != null && vidaBarco.Hundido;

        switch (estado)
        {
            case Estado.Timon:
                if (hundido) { SalirDelBarco(0f); break; }
                if (usar) { local = Cubierta.SalidaTimon(h, L, plana, w); CambiarEstado(Estado.Cubierta); }
                break;

            case Estado.Cubierta:
                if (hundido) { SalirDelBarco(0f); break; }
                if (interior == null) interior = Interior.De(BarcoActual);   // se arma la primera vez que pisas este barco
                if (Teclas.Pulsada(Accion.Ancla) && nivel == 0) BarcoActual.AlternarAncla();
                // La puerta del camarote gana si estás justo frente a ella; si no, F cerca del castillo toma el timón
                if (usar && nivel == 0 && CercaDelTimon() && (interior == null || !interior.FrenteAPuerta(local))) { IntentarTomarTimon(); break; }
                // Escaleras, escotillas y la puerta del camarote
                if (usar)
                {
                    if (interior != null && interior.Usar(ref nivel, ref local))
                    {
                        Avisar(nivel == 0 ? "En cubierta" : "Estás en " + interior.Nombre(nivel));
                        break;
                    }
                }
                float sp = correr ? velocidadCorrer : velocidadCaminar;
                Vector3 dl = barcoT.InverseTransformDirection(dir) * sp * dt;
                dl.y = 0f;
                if (nivel != 0 && interior != null) { local = interior.Limitar(nivel, local + dl); break; }
                Vector3 nuevo = Cubierta.EvitarCamarote(local + dl, w, L, plana);
                ObstaculosCubierta obs = BarcoActual.GetComponent<ObstaculosCubierta>();
                if (obs != null) nuevo = obs.Empujar(nuevo);
                local = nuevo;
                if (!Cubierta.Dentro(nuevo, w, L, plana)) { SalirDelBarco(1.5f); break; }
                if (saltar) SalirDelBarco(fuerzaSalto);
                break;

            case Estado.Libre:
                MoverLibre(dir, correr, saltar);
                if (usar && !recogio && PuedeSubirAUnBarco(out ControlBarco cb, out Vector3 l))
                {
                    Asignar(cb);
                    local = l;
                    CambiarEstado(Estado.Cubierta);
                }
                break;
        }
    }

    void LateUpdate()
    {
        if (estado == Estado.Libre) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        else
        {
            // Sobre el barco: se mueve con él (después de la interpolación del Rigidbody)
            transform.position = barcoT.TransformPoint(estado == Estado.Timon ? PuntoTimon : local);
            float ry = estado == Estado.Timon ? barcoT.eulerAngles.y : yaw;
            transform.rotation = Quaternion.Euler(0f, ry, 0f);
        }

        if (espada != null)
        {
            tAnim = Mathf.Max(0f, tAnim - Time.deltaTime);
            float ang = tAnim > 0f ? Mathf.Lerp(80f, -60f, tAnim / 0.22f) : -20f;
            espada.localRotation = Quaternion.Euler(ang, 0f, 0f);
            espada.gameObject.SetActive(estado != Estado.Timon && !Nadando && (!Bajo || tAnim > 0f));   // adentro (primera persona) solo al golpear
        }
    }

    // ---------- Combate ----------
    void Atacar()
    {
        tEspada = recargaEspada;
        tAnim = 0.22f;

        // Apunta solo hacia el enemigo más cercano a su alcance
        Tripulante cercano = null;
        float mejor = alcanceEspada + 1f;
        foreach (Tripulante t in Tripulante.Vivos)
        {
            Vector3 d = t.transform.position - transform.position; d.y = 0f;
            if (d.magnitude < mejor) { mejor = d.magnitude; cercano = t; }
        }
        if (cercano != null)
        {
            Vector3 d = cercano.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        Vector3 frente = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        foreach (Tripulante t in Tripulante.Vivos.ToArray())
        {
            Vector3 d = t.transform.position - transform.position;
            if (Mathf.Abs(d.y) > 1.5f) continue;
            d.y = 0f;
            if (d.magnitude <= alcanceEspada && Vector3.Dot(frente, d.normalized) > 0.2f) t.Dano(danoEspada);
        }
    }

    // Pone al personaje al timón de su barco propio (usado por las teclas de prueba)
    public bool IrAlTimonPropio()
    {
        if (barco == null) return false;
        Asignar(barco);
        CambiarEstado(Estado.Timon);
        return true;
    }

    public void RecibirDano(float cantidad)
    {
        if (Vida <= 0f || ModoCreativo.Activo) return;
        Vida -= cantidad;
        tGolpe = 0.3f;
        if (Vida <= 0f)
        {
            Vida = vidaMaxima;
            if (sup != null) sup.Reiniciar();
            VidaBarco vb = barco != null ? barco.GetComponent<VidaBarco>() : null;
            if (barco != null && (vb == null || !vb.Hundido))
            {
                Asignar(barco);
                CambiarEstado(Estado.Timon);
                Notificaciones.Anuncio("Te derrotaron", "Reapareces al timón de tu barco", 3.5f, Notificaciones.Tipo.Peligro);
            }
            else
            {
                AparecerEnPlaya();
                Notificaciones.Anuncio("Te derrotaron", "Despiertas en una playa", 3.5f, Notificaciones.Tipo.Peligro);
            }
        }
    }

    // ---------- Supervivencia ----------
    // conHerramienta: null = cualquiera, true = solo los que se golpean, false = solo los que se recogen a mano
    Recurso RecursoCercano(bool? conHerramienta = null)
    {
        if (estado == Estado.Timon || Nadando) return null;
        Recurso mejor = null;
        float dMejor = alcanceRecolectar;
        Vector3 yo = transform.position;
        float filtro = (alcanceRecolectar + 12f) * (alcanceRecolectar + 12f);   // descarte rápido de lo lejano
        foreach (Recurso r in Recurso.Todos)
        {
            Vector3 dr = r.transform.position - yo;
            if (dr.x * dr.x + dr.z * dr.z > filtro) continue;
            if (conHerramienta.HasValue && r.ConHerramienta != conHerramienta.Value) continue;
            float d = r.Distancia(yo);
            if (d < dMejor) { dMejor = d; mejor = r; }
        }
        return mejor;
    }

    bool EnemigoCerca()
    {
        foreach (Tripulante t in Tripulante.Vivos)
        {
            Vector3 d = t.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 1.5f && new Vector2(d.x, d.z).magnitude <= alcanceEspada + 1f) return true;
        }
        return false;
    }

    // Clic: golpe con la herramienta al recurso grande más cercano
    void Golpear()
    {
        tEspada = 0.5f;   // ritmo de golpes
        tAnim = 0.22f;
        if (inv == null) return;
        Recurso r = RecursoCercano(true);
        if (r == null)
        {
            if (RecursoCercano(false) != null) Avisar("Eso se recoge con la mano: presiona F");
            return;   // golpe al aire
        }
        MirarA(r);
        Avisar(r.Golpear(inv));
    }

    // F: agarrar con la mano
    void Recoger(Recurso r)
    {
        if (inv == null) return;
        tAnim = 0.15f;
        MirarA(r);
        Avisar(r.Golpear(inv));
    }

    void MirarA(Recurso r)
    {
        Vector3 hacia = r.transform.position - transform.position; hacia.y = 0f;
        if (hacia.sqrMagnitude > 0.01f) yaw = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
    }

    // Come lo que más falta: coco si tienes más sed, fruta si tienes más hambre
    public void Comer()
    {
        if (inv == null || sup == null) return;
        bool prefiereCoco = sup.Sed <= sup.Hambre;
        string primero = prefiereCoco ? Inventario.Coco : Inventario.Fruta;
        string segundo = prefiereCoco ? Inventario.Fruta : Inventario.Coco;
        string comido = inv.Quitar(primero, 1) ? primero : inv.Quitar(segundo, 1) ? segundo : null;
        string desde = "";
        // Si no llevas nada encima pero estás a bordo, se come de la bodega del barco
        Bodega bodega = estado != Estado.Libre && BarcoActual != null ? BarcoActual.GetComponent<Bodega>() : null;
        if (comido == null && bodega != null)
        {
            comido = bodega.Consumir(primero) ? primero : bodega.Consumir(segundo) ? segundo : null;
            if (comido != null) desde = " (de la bodega)";
        }
        if (comido == null) { Avisar("No llevas comida. Agarra frutas y cocos en la isla"); return; }
        if (comido == Inventario.Coco) { sup.Comer(15f, 30f); Avisar("Coco" + desde + ": +15 hambre, +30 sed"); }
        else { sup.Comer(25f, 8f); Avisar("Fruta" + desde + ": +25 hambre, +8 sed"); }
    }

    static bool EsEnemigo(ControlBarco b)
    {
        IABarco ia = b != null ? b.GetComponent<IABarco>() : null;
        return ia != null && !ia.Capturado;
    }

    void IntentarTomarTimon()
    {
        IABarco ia = BarcoActual.GetComponent<IABarco>();
        if (ia != null && !ia.Capturado) { Avisar("Primero derrota a la tripulación de este barco"); return; }
        CambiarEstado(Estado.Timon);
    }

    // ---------- Movimiento libre ----------
    void MoverLibre(Vector3 dir, bool correr, bool saltar)
    {
        float dt = Time.deltaTime;
        float agua = Olas.Altura(transform.position);
        Nadando = !cc.isGrounded && transform.position.y <= agua - profundidadFlotar + 0.1f;
        float sp = Nadando ? velocidadNadar : (correr ? velocidadCorrer : velocidadCaminar);

        // La velocidad heredada del barco se pierde al tocar suelo o agua
        Vector3 hor = new Vector3(vel.x, 0f, vel.z);
        hor = Vector3.Lerp(hor, Vector3.zero, dt * (cc.isGrounded || Nadando ? 6f : 0.3f));
        vel.x = hor.x; vel.z = hor.z;

        if (cc.isGrounded && vel.y < 0f) vel.y = -2f;
        vel.y -= gravedad * dt;
        if (Nadando) vel.y = (agua - profundidadFlotar - transform.position.y) * 3f;
        if (saltar && cc.isGrounded) vel.y = fuerzaSalto;

        cc.Move((dir * sp + hor) * dt + Vector3.up * vel.y * dt);
        EnTierra = cc.isGrounded;
    }

    // Aterrizar sobre la cubierta de un barco desde un salto (también para abordar)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (estado != Estado.Libre || hit.normal.y < 0.5f) return;
        ControlBarco cb = hit.collider.GetComponentInParent<ControlBarco>();
        if (cb == null || hit.collider.isTrigger || hit.collider.attachedRigidbody != cb.GetComponent<Rigidbody>()) return;
        Cubierta.Medidas(cb.GetComponent<BoxCollider>(), out float bw, out float bh, out float bl);
        Vector3 l = cb.transform.InverseTransformPoint(transform.position);
        l.y = Cubierta.Altura(bh);
        if (!Cubierta.Dentro(l, bw, bl, Cubierta.EsPlana(cb))) return;
        Asignar(cb);
        local = Cubierta.EvitarCamarote(l, w, L, plana);
        CambiarEstado(Estado.Cubierta);
        if (EsEnemigo(cb)) Notificaciones.Anuncio("¡Abordaje!", "Derrota a su tripulación con la espada", 3f, Notificaciones.Tipo.Peligro);
    }

    void SalirDelBarco(float impulsoVertical)
    {
        Vector3 p = barcoT.TransformPoint(local);
        CambiarEstado(Estado.Libre);
        cc.enabled = false;
        transform.position = p;
        cc.enabled = true;
        vel = barcoRb.linearVelocity;
        vel.y = impulsoVertical;
    }

    void CambiarEstado(Estado e)
    {
        estado = e;
        if (e != Estado.Cubierta) nivel = 0;
        bool timon = e == Estado.Timon;
        foreach (ControlBarco b in ControlBarco.Todos)
        {
            bool manda = timon && b == BarcoActual;
            if (b.GetComponent<IABarco>() == null || manda || b.GetComponent<IABarco>().Capturado)
            {
                b.controlActivo = manda;
                Canones c = b.GetComponent<Canones>();
                if (c != null) c.controlJugador = manda;
            }
            if (timon) b.mostrarPanel = b == BarcoActual;
        }
        cc.enabled = e == Estado.Libre;
        if (e != Estado.Libre) { vel = Vector3.zero; Nadando = false; EnTierra = false; }
        if (timon) local = PuntoTimon;

        if (camara != null)
        {
            // La raíz de la cámara es siempre el timón (el personaje). La rueda acerca hasta quedar
            // justo detrás del timón y aleja hasta que el barco se ve pequeñito.
            camara.objetivo = transform;
            camara.focoCerca = null;
            camara.distanciaMin = timon ? 2.5f : 3f;
            camara.distanciaMax = timon ? L * 8f + 40f : 20f;
            camara.distancia = timon ? Mathf.Clamp(L * 1.1f + 8f, camara.distanciaMin, camara.distanciaMax) : distanciaCamaraPie;
            camara.altura = timon ? 2f : 1.6f;
            camara.largoBarco = timon ? L : 0f;
            camara.seguirRumbo = timon;
        }
    }

    bool CercaDelTimon()
    {
        return estado == Estado.Cubierta && Cubierta.CercaDelTimon(local, w, h, L, plana);
    }

    bool PuedeSubirAUnBarco(out ControlBarco elegido, out Vector3 lElegido)
    {
        elegido = null; lElegido = Vector3.zero;
        float mejor = float.MaxValue;
        foreach (ControlBarco cb in ControlBarco.Todos)
        {
            VidaBarco v = cb.GetComponent<VidaBarco>();
            if (v != null && v.Hundido) continue;
            Cubierta.Medidas(cb.GetComponent<BoxCollider>(), out float bw, out float bh, out float bl);
            Vector3 l = cb.transform.InverseTransformPoint(transform.position);
            bool cerca = Mathf.Abs(l.x) < bw / 2f + 2.5f && l.z > -bl / 2f - 1f && l.z < bl / 2f + 1f && l.y < Cubierta.Altura(bh) + 0.5f;
            if (!cerca || Mathf.Abs(l.x) >= mejor) continue;
            mejor = Mathf.Abs(l.x);
            l.x = Mathf.Clamp(l.x, -0.4f * bw, 0.4f * bw);
            l.z = Mathf.Clamp(l.z, -0.27f * bl, 0.25f * bl);
            l.y = Cubierta.Altura(bh);
            elegido = cb; lElegido = l;
        }
        return elegido != null;
    }

    // ---------- Textos para la interfaz (los dibuja Hud) ----------
    public string AvisoActual => tAviso > 0f ? aviso : null;
    public float Golpe => Mathf.Clamp01(tGolpe / 0.3f);

    public string TextoModo() => estado == Estado.Timon ? "Al timón de " + BarcoActual.name
        : estado == Estado.Cubierta && nivel != 0 && interior != null ? "En " + interior.Nombre(nivel) + " de " + BarcoActual.name
        : estado == Estado.Cubierta ? (EsEnemigo(BarcoActual) ? "A bordo del enemigo" : "En cubierta de " + BarcoActual.name)
        : Nadando ? "Nadando" : EnTierra ? "En tierra" : "En el aire";

    // Lo más útil que puedes hacer aquí, en una línea
    public string TextoAyuda()
    {
        string T(string tecla) => Hud.Col(tecla, Hud.Oro);
        Recurso cerca = RecursoCercano();
        if (cerca != null && inv != null) return cerca.Accion(inv).Replace("Clic:", T("Clic")).Replace("F:", T("F"));
        if (estado == Estado.Timon)
        {
            if (Punteria.Apuntando)
                return "Apuntando a " + Hud.Col(Canones.Nombre(Punteria.LadoActual), Hud.Oro) + "   " + T("Clic") + " disparar   mouse arriba/abajo: alcance";
            return T("W/S") + " velas" + (BarcoActual != null && BarcoActual.EnReversa ? " (reversa)" : "") + "   " + T("A/D") + " timón   " + T("Clic der.") + " apuntar   " + T("F") + " soltar";
        }
        if (estado == Estado.Cubierta)
        {
            Interior it = interior != null ? interior : (BarcoActual != null ? BarcoActual.GetComponent<Interior>() : null);
            if (nivel == 0 && CercaDelTimon() && (it == null || !it.FrenteAPuerta(local))) return T("F") + " tomar el timón";
            string accion = it != null ? it.Ayuda(nivel, local) : null;
            if (accion != null) return T("F") + " " + accion;
            if (nivel != 0) return EnemigoCerca() ? T("Clic") + " atacar con la espada" : T("WASD") + " caminar   " + T("Shift") + " correr";
            return EnemigoCerca() ? T("Clic") + " atacar con la espada" : T("WASD") + " caminar   " + T("R") + " ancla   " + T("Clic") + " espada";
        }
        if (PuedeSubirAUnBarco(out _, out _)) return T("F") + " subir al barco";
        return Nadando ? T("WASD") + " nadar" : T("WASD") + " caminar   " + T("Shift") + " correr   " + T("Espacio") + " saltar";
    }
}
