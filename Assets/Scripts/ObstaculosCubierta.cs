using System.Collections.Generic;
using UnityEngine;

// Cosas sólidas sobre la cubierta (mástiles, cañones, cabrestante): círculos en coordenadas del barco.
// El personaje y la tripulación caminan en coordenadas del barco, así que aquí se les empuja fuera de ellas.
public class ObstaculosCubierta : MonoBehaviour
{
    readonly List<Vector3> circulos = new List<Vector3>();   // (x, z, radio)

    public void Agregar(float x, float z, float radio) => circulos.Add(new Vector3(x, z, radio));

    // Saca el punto (local) de cualquier obstáculo que pise
    public Vector3 Empujar(Vector3 p)
    {
        foreach (Vector3 c in circulos)
        {
            Vector2 d = new Vector2(p.x - c.x, p.z - c.y);
            float r = c.z + 0.3f;   // radio del personaje
            if (d.sqrMagnitude >= r * r) continue;
            if (d.sqrMagnitude < 1e-6f) d = Vector2.right;
            d = d.normalized * r;
            p.x = c.x + d.x; p.z = c.y + d.y;
        }
        return p;
    }
}
