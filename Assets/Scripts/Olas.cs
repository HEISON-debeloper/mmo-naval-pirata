using System.Collections.Generic;
using UnityEngine;

// Altura de las olas en cualquier punto del mar (en metros).
// La usan el agua (para dibujarse) y la flotación (para que los barcos sigan las olas).
public static class Olas
{
    // 1 = olas normales, 0 = mar en calma. Se puede subir en tormentas.
    public static float intensidad = 1f;

    // Cerca de las islas el agua es poco profunda y las olas pierden fuerza
    public const float CalmaEnOrilla = 0.12f;     // fracción de ola que queda pegado a la playa
    public const float DistanciaCalma = 70f;      // metros desde la orilla hasta recuperar la ola completa

    public static float Altura(float x, float z, float t) => OlaBase(x, z, t) * intensidad * Calma(x, z);

    // La ola sin la calma de las islas (el agua la combina con una calma guardada)
    public static float OlaBase(float x, float z, float t)
    {
        return 0.6f * Mathf.Sin(0.08f * x + t * 0.8f)
             + 0.4f * Mathf.Sin(0.11f * z + t * 1.1f)
             + 0.25f * Mathf.Sin(0.06f * (x + z) + t * 0.6f);
    }

    // Se pone en true cuando aparece o desaparece una isla, para que el agua recalcule la calma
    public static bool IslasCambiaron;

    public static float Altura(Vector3 p) => Altura(p.x, p.z, Time.time);

    // 1 en mar abierto, baja hasta CalmaEnOrilla junto a la playa
    // Islas cerca del mar visible: con el mundo infinito hay muchas, y revisar todas en cada ola sería muy lento
    static readonly List<Isla> cercanas = new List<Isla>();
    static int frameCercanas = -1;

    public static void ActualizarCercanas(float x, float z, float radio)
    {
        cercanas.Clear();
        foreach (Isla s in Isla.Todas)
        {
            float dx = s.cx - x, dz = s.cz - z, lejos = radio + s.orilla + DistanciaCalma;
            if (dx * dx + dz * dz < lejos * lejos) cercanas.Add(s);
        }
        frameCercanas = Time.frameCount;
    }

    public static float Calma(float x, float z)
    {
        float f = 1f;
        var islas = frameCercanas >= 0 ? cercanas : Isla.Todas;
        for (int i = 0; i < islas.Count; i++)
        {
            Isla s = islas[i];
            float dx = x - s.cx, dz = z - s.cz;
            float lejos = s.orilla + DistanciaCalma;
            float d2 = dx * dx + dz * dz;
            if (d2 >= lejos * lejos) continue;
            float k = Mathf.Clamp01((Mathf.Sqrt(d2) - s.orilla) / DistanciaCalma);
            k = k * k * (3f - 2f * k);   // suave
            f = Mathf.Min(f, Mathf.Lerp(CalmaEnOrilla, 1f, k));
        }
        return f;
    }
}
