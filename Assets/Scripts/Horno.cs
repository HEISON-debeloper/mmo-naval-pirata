using System.Collections.Generic;
using UnityEngine;

// Horno de piedra que el jugador coloca en tierra. Cerca de él se funde mineral de hierro en lingotes.
public class Horno : MonoBehaviour
{
    public static readonly List<Horno> Todos = new List<Horno>();
    public const float Alcance = 4f;

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    public static bool HayCerca(Vector3 p)
    {
        foreach (Horno h in Todos)
        {
            Vector3 d = h.transform.position - p; d.y = 0f;
            if (d.magnitude <= Alcance) return true;
        }
        return false;
    }

    // Lo arma frente al personaje, apoyado en el suelo
    public static GameObject Construir(Vector3 pos, float rumbo, Material piedra, Material brasa)
    {
        GameObject h = new GameObject("Horno de piedra");
        h.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        BoxCollider col = h.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.75f, 0f);
        col.size = new Vector3(1.6f, 1.5f, 1.6f);
        Astillero.Pieza(PrimitiveType.Cube, "Cuerpo", h.transform, new Vector3(0f, 0.6f, 0f), Quaternion.identity, new Vector3(1.6f, 1.2f, 1.6f), piedra);
        Astillero.Pieza(PrimitiveType.Cube, "Chimenea", h.transform, new Vector3(0f, 1.6f, -0.2f), Quaternion.identity, new Vector3(0.6f, 0.9f, 0.6f), piedra);
        Astillero.Pieza(PrimitiveType.Cube, "Boca", h.transform, new Vector3(0f, 0.45f, 0.78f), Quaternion.identity, new Vector3(0.7f, 0.5f, 0.08f), brasa);
        h.AddComponent<Horno>();
        return h;
    }
}
