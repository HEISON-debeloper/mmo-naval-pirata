using UnityEngine;

// Geometría de la cubierta de los barcos low poly, en coordenadas locales del barco (metros).
// w = manga (ancho), h = puntal (alto del casco), L = eslora (largo). Salen del BoxCollider del barco.
// "plana" = barco sin camarote ni proa en punta (la balsa).
// El timón va siempre atrás: en la balsa, al fondo de los troncos; en los barcos, encima del castillo de popa.
public static class Cubierta
{
    public const float AltoCamarote = 2.4f;
    public static float LargoCamarote(float L) => Mathf.Max(2.5f, 0.14f * L);
    public static float CentroCamarote(float L) => -0.5f * L + LargoCamarote(L) / 2f;

    public static float Altura(float h) => h / 2f + 0.1f;

    public static Vector3 Timon(float h, float L, bool plana = false) => plana
        ? new Vector3(0f, Altura(h), -0.42f * L)
        : new Vector3(0f, Altura(h) + AltoCamarote, CentroCamarote(L) - LargoCamarote(L) * 0.2f);

    // Dónde queda el personaje al soltar el timón (en la cubierta, delante del castillo de popa)
    public static Vector3 SalidaTimon(float h, float L, bool plana = false, float w = 4f) => plana
        ? Timon(h, L, true) + new Vector3(0.9f, 0f, 0f)
        : new Vector3(Mathf.Min(0.4f * w, 1.6f), Altura(h), CentroCamarote(L) + LargoCamarote(L) / 2f + 0.9f);   // a un lado: al frente está la puerta del camarote

    // ¿Puede tomar el timón desde aquí? (en los barcos, basta con estar al pie del castillo de popa)
    public static bool CercaDelTimon(Vector3 v, float w, float h, float L, bool plana = false)
    {
        if (plana) { Vector3 d = v - Timon(h, L, true); d.y = 0f; return d.magnitude < 2.5f; }
        return Mathf.Abs(v.x) < 0.5f * w + 1f && v.z < CentroCamarote(L) + LargoCamarote(L) / 2f + 2f;
    }

    public static bool Dentro(Vector3 v, float w, float L, bool plana = false)
    {
        if (plana) return Mathf.Abs(v.x) <= 0.46f * w && Mathf.Abs(v.z) <= 0.48f * L;
        if (v.z < -0.5f * L || v.z > 0.425f * L) return false;
        float mitad = 0.46f * w;
        if (v.z > 0.3f * L) mitad *= (0.425f * L - v.z) / (0.125f * L); // la proa se estrecha
        return Mathf.Abs(v.x) <= mitad;
    }

    // El castillo de popa es sólido: empuja hacia su borde más cercano
    public static Vector3 EvitarCamarote(Vector3 v, float w, float L, bool plana = false)
    {
        if (plana) return v;
        float bx = 0.4f * w + 0.3f;
        float z0 = CentroCamarote(L) - LargoCamarote(L) / 2f - 0.3f, z1 = CentroCamarote(L) + LargoCamarote(L) / 2f + 0.3f;
        if (Mathf.Abs(v.x) >= bx || v.z <= z0 || v.z >= z1) return v;
        float dFrente = z1 - v.z, dAtras = v.z - z0, dLado = bx - Mathf.Abs(v.x);
        if (dFrente <= dAtras && dFrente <= dLado) v.z = z1;
        else if (dAtras <= dLado) v.z = z0;
        else v.x = Mathf.Sign(v.x == 0f ? 1f : v.x) * bx;
        return v;
    }

    public static Vector3 PuntoAleatorio(float w, float h, float L, bool plana = false)
    {
        Vector3 p = new Vector3(Random.Range(-0.38f, 0.38f) * w, Altura(h), Random.Range(-0.25f, 0.25f) * L);
        return EvitarCamarote(p, w, L, plana);
    }

    public static void Medidas(BoxCollider col, out float w, out float h, out float L)
    {
        w = col.size.x; h = col.size.y; L = col.size.z;
    }

    public static bool EsPlana(Component barco) => barco.GetComponent<CubiertaPlana>() != null;
}
