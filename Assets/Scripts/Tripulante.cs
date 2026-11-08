using System.Collections.Generic;
using UnityEngine;

// Tripulante enemigo: camina por la cubierta de su barco y ataca con espada al jugador que aborda.
// Va como hijo del barco, así se mueve con él.
public class Tripulante : MonoBehaviour
{
    public static readonly List<Tripulante> Vivos = new List<Tripulante>();

    public float vida = 100f;
    public float velocidad = 2.6f;
    public float dano = 5f;
    public float alcance = 1.6f;
    public float recarga = 1.5f;
    [HideInInspector] public Transform espada;

    public bool Muerto { get; private set; }

    ControlBarco barco;
    float w, h, L;
    bool plana;   // balsa: sin castillo de popa (si no, chocan con un camarote que no existe)
    Vector3 destino;
    float yawLocal, tAtaque, tPausa, tMuerto, tGolpe, tAnim;
    Renderer[] cuerpos;
    MaterialPropertyBlock bloque;

    void OnEnable() { if (!Muerto) Vivos.Add(this); }
    void OnDisable() => Vivos.Remove(this);

    void Start()
    {
        barco = GetComponentInParent<ControlBarco>();
        Cubierta.Medidas(barco.GetComponent<BoxCollider>(), out w, out h, out L);
        plana = Cubierta.EsPlana(barco);
        destino = transform.localPosition;
        yawLocal = Random.Range(0f, 360f);
        cuerpos = GetComponentsInChildren<Renderer>();
        bloque = new MaterialPropertyBlock();
        tAtaque = recarga;
    }

    // Salto al abordar: de su barco a la cubierta de otro (el tuyo), en arco
    Vector3 saltoDesde, saltoHasta;
    float tSalto = -1f;
    public bool Saltando => tSalto >= 0f;
    public ControlBarco Barco => barco;

    public void Saltar(ControlBarco nuevo)
    {
        if (Muerto || nuevo == null || nuevo == barco) return;
        barco = nuevo;
        transform.SetParent(nuevo.transform, true);
        Cubierta.Medidas(nuevo.GetComponent<BoxCollider>(), out w, out h, out L);
        plana = Cubierta.EsPlana(nuevo);
        saltoDesde = transform.localPosition;
        saltoHasta = Cubierta.PuntoAleatorio(w, h, L, plana);
        destino = saltoHasta;
        tSalto = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (tSalto >= 0f && !Muerto)
        {
            tSalto += dt / 0.8f;
            float k = Mathf.Clamp01(tSalto);
            transform.localPosition = Vector3.Lerp(saltoDesde, saltoHasta, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 4f;
            Vector3 dir = saltoHasta - saltoDesde; dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) yawLocal = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (k >= 1f) tSalto = -1f;
            return;
        }
        if (Muerto)
        {
            tMuerto += dt;
            if (tMuerto > 4f) Destroy(gameObject);
            return;
        }

        Personaje p = Personaje.Instancia;
        Vector3 lp = transform.localPosition;
        bool persigue = p != null && p.Vida > 0f && p.SobreBarco(barco);
        Vector3 objetivo;
        if (persigue) objetivo = p.LocalEnBarco;
        else
        {
            Vector3 dd = destino - lp; dd.y = 0f;
            if (dd.magnitude < 0.5f)
            {
                tPausa -= dt;
                if (tPausa <= 0f) { destino = Cubierta.PuntoAleatorio(w, h, L, plana); tPausa = Random.Range(1f, 4f); }
            }
            objetivo = destino;
        }

        Vector3 d = objetivo - lp; d.y = 0f;
        float dist = d.magnitude;
        float sp = persigue ? velocidad : velocidad * 0.45f;
        if (dist > (persigue ? alcance * 0.8f : 0.3f))
        {
            Vector3 n = lp + d.normalized * Mathf.Min(sp * dt, dist);
            n = Cubierta.EvitarCamarote(n, w, L, plana);
            if (Cubierta.Dentro(n, w, L, plana)) lp = n;
        }
        if (dist > 0.05f) yawLocal = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        lp.y = Cubierta.Altura(h);
        transform.localPosition = lp;

        tAtaque -= dt;
        if (persigue && dist <= alcance && tAtaque <= 0f)
        {
            tAtaque = recarga;
            tAnim = 0.25f;
            p.RecibirDano(dano);
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        float yaw = barco != null ? barco.transform.eulerAngles.y + yawLocal : yawLocal;
        transform.rotation = Muerto
            ? Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-85f, 0f, 0f)
            : Quaternion.Euler(0f, yaw, 0f);

        if (espada != null)
        {
            tAnim = Mathf.Max(0f, tAnim - dt);
            float ang = tAnim > 0f ? Mathf.Lerp(80f, -60f, tAnim / 0.25f) : -20f;
            espada.localRotation = Quaternion.Euler(ang, 0f, 0f);
        }

        // Destello blanco al recibir un golpe
        if (tGolpe > 0f || bloque == null)
        {
            tGolpe = Mathf.Max(0f, tGolpe - dt);
            if (cuerpos != null && bloque != null)
                foreach (Renderer r in cuerpos)
                {
                    if (r == null || (espada != null && r.transform.IsChildOf(espada))) continue;
                    if (tGolpe > 0f) { bloque.SetColor("_BaseColor", Color.white); r.SetPropertyBlock(bloque); }
                    else r.SetPropertyBlock(null);
                }
        }
    }

    public void Dano(float cantidad)
    {
        if (Muerto) return;
        vida -= cantidad;
        tGolpe = 0.12f;
        if (vida <= 0f)
        {
            Muerto = true;
            Vivos.Remove(this);
        }
    }
}
