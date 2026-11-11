using System.Collections.Generic;
using UnityEngine;

// Botín de un barco pirata: lo que lleva en su bodega. Depende del molde, del tamaño y de los cañones.
// - Si lo HUNDES: suelta cajas que flotan (60 % del botín). Pasa cerca, a pie, nadando o con tu barco, y se recogen solas.
// - Si lo CAPTURAS (sin tripulación viva): todo el botín va directo a tu mochila.
// Cuando el pirata reaparece, trae botín nuevo. IABarco agrega este componente solo.
public class Botin : MonoBehaviour
{
    const float FraccionHundido = 0.6f;

    readonly Dictionary<string, int> carga = new Dictionary<string, int>();
    VidaBarco vida;
    IABarco ia;
    bool soltado, entregado;

    void Start()
    {
        vida = GetComponent<VidaBarco>();
        ia = GetComponent<IABarco>();
        if (vida != null) vida.alReaparecer += Llenar;
        Llenar();
    }

    void OnDestroy() { if (vida != null) vida.alReaparecer -= Llenar; }

    // Lo que lleva en la bodega
    void Llenar()
    {
        carga.Clear();
        soltado = false; entregado = false;
        ControlBarco cb = GetComponent<ControlBarco>();
        int molde = 0, a = 0, b = 0, c = 0;
        if (cb != null) FormaBarco.Leer(cb, out molde, out a, out b, out c);
        float v = (molde + 1) * (1f + 0.15f * (a + b + c));   // más grande el barco, más botín
        Poner(Inventario.Oro, Mathf.RoundToInt(Random.Range(5f, 15f) * v));   // su cofre
        Poner(Inventario.Madera, Mathf.RoundToInt(Random.Range(6f, 12f) * v));
        Poner(Inventario.Cuerda, Mathf.RoundToInt(Random.Range(2f, 5f) * v));
        if (molde >= 2) Poner(Inventario.Lingote, Mathf.RoundToInt(Random.Range(1f, 3f) * v * 0.5f));
        Poner(Inventario.Mineral, Random.Range(0, 4));
        Poner(Inventario.Coco, Random.Range(1, 5));
        Poner(Inventario.Fruta, Random.Range(1, 5));
        // Algunos de sus cañones se pueden rescatar
        Canones cn = GetComponent<Canones>();
        int instalados = cn != null ? cn.Instalados : 0;
        if (instalados > 0) Poner(Inventario.Canon, Mathf.Max(Random.value < 0.5f ? 1 : 0, Mathf.RoundToInt(instalados * 0.25f)));
    }

    void Poner(string tipo, int n) { if (n > 0) carga[tipo] = n; }

    void Update()
    {
        if (vida != null && vida.Hundido && !soltado && !entregado) { soltado = true; Soltar(); }
        if (ia != null && ia.Capturado && !entregado && !soltado) { entregado = true; Entregar(); }
    }

    // Capturado: todo a tu mochila
    void Entregar()
    {
        Personaje p = Personaje.Instancia;
        Inventario inv = p != null ? p.GetComponent<Inventario>() : null;
        if (inv == null || carga.Count == 0) return;
        var tomado = new Dictionary<string, int>();
        var resto = new Dictionary<string, int>();
        foreach (var kv in carga)
        {
            int n = inv.Recibir(kv.Key, kv.Value);
            if (n > 0) tomado[kv.Key] = n;
            if (kv.Value - n > 0) resto[kv.Key] = kv.Value - n;
        }
        if (tomado.Count > 0) p.Avisar("¡Botín capturado! " + Resumen(tomado));
        if (resto.Count > 0)
        {
            // Lo que no cabe en la mochila queda en una caja flotando junto al barco
            BoxCollider bc = GetComponent<BoxCollider>();
            Vector3 pos = transform.position + transform.right * ((bc != null ? bc.size.x : 8f) * 0.5f + 4f);
            CajaBotin.Crear(pos, resto, MaterialDe("Mástil"), MaterialDe("Tubo"));
            p.Avisar("Mochila llena: el resto quedó en una caja junto al barco");
        }
        carga.Clear();
    }

    // Hundido: cajas flotando alrededor de donde se hundió
    void Soltar()
    {
        if (carga.Count == 0) return;
        // Se reparte entre 1 y 4 cajas
        BoxCollider col = GetComponent<BoxCollider>();
        float L = col != null ? col.size.z : 10f;
        int cajas = Mathf.Clamp(1 + Mathf.RoundToInt(L / 20f), 1, 4);
        var reparto = new List<Dictionary<string, int>>();
        for (int i = 0; i < cajas; i++) reparto.Add(new Dictionary<string, int>());
        int k = 0;
        foreach (var kv in carga)
        {
            int total = Mathf.Max(1, Mathf.RoundToInt(kv.Value * FraccionHundido * Pactos.Botin));   // Oro Maldito: el doble
            int porCaja = Mathf.CeilToInt(total / (float)cajas);
            for (int i = 0; i < cajas && total > 0; i++)
            {
                int n = Mathf.Min(porCaja, total);
                Dictionary<string, int> d = reparto[(i + k) % cajas];
                d[kv.Key] = (d.TryGetValue(kv.Key, out int ya) ? ya : 0) + n;
                total -= n;
            }
            k++;
        }
        carga.Clear();

        Material madera = MaterialDe("Mástil"), banda = MaterialDe("Tubo");   // madera café con bandas de hierro
        Vector3 centro = transform.position;
        for (int i = 0; i < cajas; i++)
        {
            if (reparto[i].Count == 0) continue;
            float ang = (i + Random.value * 0.5f) / cajas * Mathf.PI * 2f;
            Vector3 pos = centro + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(L * 0.2f, L * 0.4f + 4f);
            CajaBotin.Crear(pos, reparto[i], madera, banda);
        }
    }

    // Un material del barco para pintar las cajas (si no hay, el de cualquier pieza)
    Material MaterialDe(string pieza)
    {
        Renderer r = null;
        foreach (Renderer x in GetComponentsInChildren<Renderer>(true)) if (x.name == pieza) { r = x; break; }
        if (r == null) r = GetComponentInChildren<Renderer>();
        return r != null ? r.sharedMaterial : null;
    }

    public static string Resumen(Dictionary<string, int> d)
    {
        var partes = new List<string>();
        foreach (var kv in d) partes.Add("+" + kv.Value + " " + kv.Key.ToLower());
        return string.Join(", ", partes);
    }
}
