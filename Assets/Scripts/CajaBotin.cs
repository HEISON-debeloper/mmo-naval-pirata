using System.Collections.Generic;
using UnityEngine;

// Una caja de botín que flota en el mar. Se recoge sola al pasar cerca (a pie, nadando o con tu barco).
public class CajaBotin : MonoBehaviour
{
    public static readonly List<CajaBotin> Todas = new List<CajaBotin>();
    const float Duracion = 240f;        // segundos antes de hundirse
    const float AlcancePersona = 4f;

    Dictionary<string, int> contenido;
    float vida, fase;
    bool enTierra;
    float edad;   // unos segundos sin recogerse (la que suelta el jugador al morir no vuelve sola a su mochila)

    void OnEnable() => Todas.Add(this);
    void OnDisable() => Todas.Remove(this);

    public static void Crear(Vector3 pos, Dictionary<string, int> contenido, Material madera, Material banda, bool enTierra = false)
    {
        GameObject g = new GameObject("Caja de botín");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Random.value * 360f, 0f));
        Astillero.Pieza(PrimitiveType.Cube, "Caja", g.transform, Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.8f, 0.8f), madera);
        Astillero.Pieza(PrimitiveType.Cube, "Banda", g.transform, Vector3.zero, Quaternion.identity, new Vector3(1.14f, 0.18f, 0.84f), banda);
        Astillero.Pieza(PrimitiveType.Cube, "Tapa", g.transform, new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.0f, 0.06f, 0.7f), banda);
        CajaBotin c = g.AddComponent<CajaBotin>();
        c.contenido = contenido;
        c.enTierra = enTierra;
        if (enTierra) c.vida = -Duracion * 4f;   // en tierra no se hunde: dura unos 20 minutos
        c.fase = Random.value * 10f;
    }

    void Update()
    {
        vida += Time.deltaTime;
        Vector3 p = transform.position;
        // Flota con las olas, mecida
        if (!enTierra)
        {
            float hundir = vida > Duracion - 5f ? (vida - (Duracion - 5f)) * 0.4f : 0f;
            p.y = Olas.Altura(p) + 0.15f - hundir;
            transform.position = p;
            transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f + fase) * 8f, transform.eulerAngles.y + Time.deltaTime * 6f, Mathf.Cos(Time.time * 1.1f + fase) * 8f);
        }
        if (vida > Duracion) { Destroy(gameObject); return; }

        edad += Time.deltaTime;
        if (edad < 4f) return;
        Personaje pj = Personaje.Instancia;
        if (pj == null) return;
        bool cerca = Plano(pj.transform.position - p) < AlcancePersona;
        // Con tu barco basta con pasarle por al lado
        if (!cerca && pj.barco != null && pj.estado != Personaje.Estado.Libre && pj.BarcoActual == pj.barco)
        {
            BoxCollider col = pj.barco.GetComponent<BoxCollider>();
            float alcance = col != null ? Mathf.Max(col.size.x, col.size.z) * 0.5f + 3f : 8f;
            cerca = Plano(pj.barco.transform.position - p) < alcance;
        }
        if (cerca) Recoger(pj);
    }

    static float Plano(Vector3 d) { d.y = 0f; return d.magnitude; }

    void Recoger(Personaje pj)
    {
        Inventario inv = pj.GetComponent<Inventario>();
        if (inv == null) return;
        foreach (var kv in contenido) inv.Agregar(kv.Key, kv.Value);
        pj.Avisar("Recogiste botín: " + Botin.Resumen(contenido));
        Destroy(gameObject);
    }
}
