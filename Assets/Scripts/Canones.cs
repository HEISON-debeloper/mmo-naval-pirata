using UnityEngine;
using UnityEngine.InputSystem;

public enum LadoCanon { Babor, Estribor, Proa, Popa }

// Cañones del barco, en cuatro lados: babor (izquierda), estribor (derecha), proa (adelante) y popa (atrás).
// Son piezas: el barco tiene ranuras por lado y solo disparan los instalados. Se instalan uno por uno
// en el lado que quieras. Se apunta con clic derecho (Punteria) y se dispara con clic izquierdo;
// Q y E disparan babor y estribor directo.
[RequireComponent(typeof(Rigidbody))]
public class Canones : MonoBehaviour
{
    [Tooltip("Ranuras de cada lado. Su eje Z debe apuntar hacia afuera.")]
    public Transform[] babor, estribor, proa, popa;
    [Tooltip("Cañones instalados en cada lado.")]
    public int enBabor, enEstribor, enProa, enPopa;
    // Escenas viejas guardaban un total; se reparte al cargar
    [SerializeField, HideInInspector] int instalados = -1;

    public float velocidadBala = 44f;
    public float elevacion = 5f;
    public float dispersion = 0.04f;
    [Tooltip("Recarga con un solo cañón. Cada cañón más en ese lado la alarga (más cañones que cargar).")]
    public float recarga = 2.5f;
    public const float ExtraPorCanon = 0.012f;   // +1,2 % por cada cañón más (10 cañones ≈ 1,1×), con tope de 2×
    public const float RecargaBarriles = 7f;
    public const int MaxNivelBarriles = 4;
    [Tooltip("Mejoras de los barriles en llamas: cada nivel suelta un barril más por tanda (1 a 5).")]
    public int nivelBarriles;
    public int BarrilesPorTanda => 1 + Mathf.Clamp(nivelBarriles, 0, MaxNivelBarriles);
    public float dano = 12f;
    [Tooltip("Si está activo, el jugador lo controla (Q, E y la puntería).")]
    public bool controlJugador = true;
    // Ángulo del tiro elegido con la puntería (grados: negativo = hacia el agua, positivo = arco al cielo).
    // NaN = tiro normal (Q/E sin apuntar, y la IA).
    [System.NonSerialized] public float anguloApunte = float.NaN;

    readonly float[] recargas = new float[4];
    Rigidbody rb;
    Collider[] propios;
    VidaBarco vida;

    // ---------- Consultas ----------
    public Transform[] Ranuras(LadoCanon l) => l == LadoCanon.Babor ? babor : l == LadoCanon.Estribor ? estribor : l == LadoCanon.Proa ? proa : popa;
    public int RanurasLado(LadoCanon l) { Transform[] r = Ranuras(l); return r != null ? r.Length : 0; }
    public int EnLado(LadoCanon l) => l == LadoCanon.Babor ? enBabor : l == LadoCanon.Estribor ? enEstribor : l == LadoCanon.Proa ? enProa : enPopa;
    public bool HayRanura(LadoCanon l) => EnLado(l) < RanurasLado(l);
    public float Recarga(LadoCanon l) => recargas[(int)l];
    public int Ranuras() => RanurasLado(LadoCanon.Babor) + RanurasLado(LadoCanon.Estribor) + RanurasLado(LadoCanon.Proa) + RanurasLado(LadoCanon.Popa);
    public int Instalados => enBabor + enEstribor + enProa + enPopa;

    // Cuánto tarda en recargar un lado con n cañones instalados
    public float TiempoRecarga(LadoCanon l)
    {
        if (l == LadoCanon.Popa) return RecargaBarriles;
        return recarga * Mathf.Min(2f, 1f + ExtraPorCanon * Mathf.Max(0, EnLado(l) - 1));
    }

    public static string Nombre(LadoCanon l) => l == LadoCanon.Babor ? "babor" : l == LadoCanon.Estribor ? "estribor" : l == LadoCanon.Proa ? "proa" : "popa";

    // Compatibilidad: true = babor, false = estribor
    static LadoCanon L(bool porBabor) => porBabor ? LadoCanon.Babor : LadoCanon.Estribor;
    public int RanurasLado(bool porBabor) => RanurasLado(L(porBabor));
    public int EnLado(bool porBabor) => EnLado(L(porBabor));
    public float RecargaBabor => recargas[0];
    public float RecargaEstribor => recargas[1];
    public bool Disparar(bool porBabor) => Disparar(L(porBabor));

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        propios = GetComponentsInChildren<Collider>();
        vida = GetComponent<VidaBarco>();
        if (instalados >= 0) { Repartir(instalados); instalados = -1; }
        else ActualizarVisibles();
    }

    // Reparte n cañones entre babor y estribor (para barcos que salen ya armados)
    public void Repartir(int n)
    {
        n = Mathf.Clamp(n, 0, RanurasLado(LadoCanon.Babor) + RanurasLado(LadoCanon.Estribor));
        enBabor = Mathf.Min((n + 1) / 2, RanurasLado(LadoCanon.Babor));
        enEstribor = Mathf.Min(n - enBabor, RanurasLado(LadoCanon.Estribor));
        ActualizarVisibles();
    }

    public void Fijar(int nBabor, int nEstribor, int nProa = 0, int nPopa = 0)
    {
        enBabor = nBabor; enEstribor = nEstribor; enProa = nProa; enPopa = nPopa;
        ActualizarVisibles();
    }

    public void ActualizarVisibles()
    {
        enBabor = Mathf.Clamp(enBabor, 0, RanurasLado(LadoCanon.Babor));
        enEstribor = Mathf.Clamp(enEstribor, 0, RanurasLado(LadoCanon.Estribor));
        enProa = Mathf.Clamp(enProa, 0, RanurasLado(LadoCanon.Proa));
        enPopa = Mathf.Clamp(enPopa, 0, RanurasLado(LadoCanon.Popa));
        for (int l = 0; l < 4; l++)
        {
            Transform[] r = Ranuras((LadoCanon)l);
            int n = EnLado((LadoCanon)l);
            for (int i = 0; r != null && i < r.Length; i++) if (r[i]) r[i].gameObject.SetActive(i < n);
        }
    }

    public bool Instalar(LadoCanon l)
    {
        if (!HayRanura(l)) return false;
        Sumar(l, 1);
        return true;
    }

    // Saca un cañón de ese lado (vuelve al inventario)
    public bool Quitar(LadoCanon l)
    {
        if (EnLado(l) <= 0) return false;
        Sumar(l, -1);
        return true;
    }

    void Sumar(LadoCanon l, int d)
    {
        if (l == LadoCanon.Babor) enBabor += d;
        else if (l == LadoCanon.Estribor) enEstribor += d;
        else if (l == LadoCanon.Proa) enProa += d;
        else enPopa += d;
        ActualizarVisibles();
    }

    void Update()
    {
        for (int i = 0; i < 4; i++) recargas[i] = Mathf.Max(0f, recargas[i] - Time.deltaTime);
        if (!controlJugador || Fabricacion.Abierto) return;   // con el menú abierto, Q/E cambian de página
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (Teclas.Pulsada(Accion.DispararBabor)) Disparar(LadoCanon.Babor);
        if (Teclas.Pulsada(Accion.DispararEstribor)) Disparar(LadoCanon.Estribor);
    }

    // ---------- Tiro ----------
    // Dónde sale la bala y con qué velocidad (la puntería usa lo mismo para dibujar el fantasma)
    public Vector3 SalidaBarril(Transform c) => c.position + c.forward * 1.6f + Vector3.up * 0.4f;
    public Vector3 Salida(Transform c) => c.position + c.forward * 1.2f + Vector3.up * 0.6f;
    public Vector3 VelocidadInicial(Transform c)
    {
        if (float.IsNaN(anguloApunte))
            return rb.linearVelocity + c.forward * velocidadBala + Vector3.up * elevacion;
        Vector3 plano = Vector3.ProjectOnPlane(c.forward, Vector3.up).normalized;
        float a = anguloApunte * Mathf.Deg2Rad;
        return rb.linearVelocity + (plano * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * velocidadBala;
    }
    public bool EsPropio(Collider c) { foreach (Collider p in propios) if (p == c) return true; return false; }

    // Dispara los cañones uno tras otro; la ola entera dura más mientras más cañones (hasta 1,2 s)
    System.Collections.IEnumerator Ola(System.Collections.Generic.List<Transform> orden, float angulo)
    {
        int n = orden.Count;
        float total = Mathf.Clamp(0.04f * (n - 1), 0f, 1.2f);
        float paso = n > 1 ? total / (n - 1) : 0f, espera = 0f;
        for (int i = 0; i < n; i++)
        {
            Transform c = orden[i];
            if (c != null && (vida == null || !vida.Hundido))
            {
                float antes = anguloApunte;
                anguloApunte = angulo;   // todos con el ángulo que había al dar la orden
                Bala(c);
                anguloApunte = antes;
            }
            espera += paso;
            while (espera >= Time.deltaTime && i < n - 1) { espera -= Time.deltaTime; yield return null; }
        }
    }

    void Bala(Transform c)
    {
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = "Bala";
        SonidosMundo.Uno("canon_disparo", c.position, 1f, Random.Range(0.9f, 1.05f), 60f, 0.07f);
        b.transform.position = Salida(c);
        b.transform.localScale = Vector3.one * 0.4f;
        Renderer rc = c.GetComponentInChildren<Renderer>();
        if (rc != null) b.GetComponent<Renderer>().sharedMaterial = rc.sharedMaterial;
        Rigidbody brb = b.AddComponent<Rigidbody>();
        brb.mass = 10f;
        brb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        brb.linearVelocity = VelocidadInicial(c) + Random.insideUnitSphere * dispersion * velocidadBala;
        Collider bc = b.GetComponent<Collider>();
        foreach (Collider p in propios) if (p != null) Physics.IgnoreCollision(bc, p);   // las piezas recién creadas pueden haber perdido su collider
        BalaCanon bala = b.AddComponent<BalaCanon>();
        bala.dueno = vida;
        bala.dano = dano;
    }

    public bool Disparar(LadoCanon lado)
    {
        if (vida != null && vida.Hundido) return false;
        int n = EnLado(lado);
        if (n == 0) return false;
        if (recargas[(int)lado] > 0f) return false;
        recargas[(int)lado] = TiempoRecarga(lado);
        propios = GetComponentsInChildren<Collider>();

        Transform[] r = Ranuras(lado);
        if (lado == LadoCanon.Popa)
        {
            // Popa: barriles en llamas que caen al agua detrás del barco
            for (int i = 0; i < n && i < r.Length; i++)
            {
                if (r[i] == null) continue;
                Material madera = null, hierro = null;
                foreach (Renderer x in r[i].GetComponentsInChildren<Renderer>())
                {
                    if (x.name == "Aros") hierro = x.sharedMaterial;
                    else if (madera == null && x.name != "Repisa") madera = x.sharedMaterial;
                }
                // Varios barriles por tanda (según la mejora): se abren en abanico detrás del barco
                int nb = BarrilesPorTanda;
                for (int j = 0; j < nb; j++)
                {
                    float abre = nb == 1 ? 0f : Mathf.Lerp(-1f, 1f, (float)j / (nb - 1));
                    Vector3 lateral = r[i].right * abre;
                    Vector3 vel = rb.linearVelocity * 0.5f + r[i].forward * (3f + 0.6f * Mathf.Abs(abre)) + lateral * 2.2f + Vector3.up * 1f;
                    BarrilLlamas.Soltar(SalidaBarril(r[i]) + lateral * 0.9f, r[i].rotation, vel, vida, madera, hierro != null ? hierro : madera);
                }
            }
            return true;
        }
        // Andanada en ola: primero los de popa y luego hacia la proa, como una ola de disparos
        var orden = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < n && i < r.Length; i++) if (r[i] != null) orden.Add(r[i]);
        orden.Sort((x, y) => transform.InverseTransformPoint(x.position).z.CompareTo(transform.InverseTransformPoint(y.position).z));
        StartCoroutine(Ola(orden, anguloApunte));
        return true;
    }
}
