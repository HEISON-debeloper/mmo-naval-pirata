using UnityEngine;

// Construye en tiempo de juego una balsa de troncos amarrados con cuerda (6 x 3 m la primera), con una vela.
// Es el primer barco del jugador: tiene una ranura de cañón por lado y después crece (ver FormaBarco).
public static class Balsa
{
    // Troncos amarrados con cuerda: sin cuerda no hay balsa
    public const int CostoMadera = 24;
    public const int CostoCuerda = 8;
    public const float TiempoConstruir = 8f;

    // pisos: plataformas de troncos una encima de otra (una balsa de 10 pisos sigue siendo balsa).
    // Cada piso lleva una ranura de cañón por costado.
    public static GameObject Crear(Vector3 pos, float rumbo, Material madera, Material lona, Material hierro = null, float L = 6f, float w = 3f, int pisos = 0)
    {
        const float altoPiso = 1.2f;
        float h = 0.8f + pisos * altoPiso;
        float e = Mathf.Sqrt(L * w / 18f);   // escala de la vela y el mástil
        float abajo = -h / 2f;
        GameObject raiz = new GameObject("Balsa");
        raiz.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        BoxCollider col = raiz.AddComponent<BoxCollider>();
        col.size = new Vector3(w, h, L);
        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 900f * L * w / 18f * (1f + 0.5f * pisos);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        raiz.AddComponent<CubiertaPlana>();

        // Troncos a lo largo (más troncos si es más ancha)
        int troncos = Mathf.Max(5, Mathf.RoundToInt(w / 0.6f));
        for (int i = 0; i < troncos; i++)
        {
            float x = -w / 2f + 0.3f + i * (w - 0.6f) / (troncos - 1);
            Pieza(PrimitiveType.Cylinder, raiz.transform, new Vector3(x, abajo + 0.5f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.6f, L / 2f, 0.6f), madera);
        }
        // Pisos: postes en las esquinas y una plataforma de tablas encima
        for (int p = 1; p <= pisos; p++)
        {
            float y = abajo + 0.8f + p * altoPiso;
            Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(0f, y - 0.06f, 0f), Quaternion.identity, new Vector3(w, 0.12f, L), madera);
            foreach (float px in new[] { -w / 2f + 0.2f, w / 2f - 0.2f })
            foreach (float pz in new[] { -L / 2f + 0.2f, 0f, L / 2f - 0.2f })
                Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(px, y - altoPiso / 2f, pz), Quaternion.identity, new Vector3(0.25f, altoPiso, 0.25f), madera);
        }

        // Mástil y vela, desde la cubierta de arriba
        float arriba = h / 2f;
        Pieza(PrimitiveType.Cylinder, raiz.transform, new Vector3(0f, arriba + 2f * e, 0.8f * e), Quaternion.identity, new Vector3(0.15f * e, 2f * e, 0.15f * e), madera);
        GameObject pivote = new GameObject("Vela");
        pivote.transform.SetParent(raiz.transform, false);
        pivote.transform.localPosition = new Vector3(0f, arriba + 3.8f * e, 0.8f * e + 0.15f);
        pivote.transform.localScale = new Vector3(Mathf.Min(2f * e, w * 0.9f), 2.6f * e, 0.08f);
        Pieza(PrimitiveType.Cube, pivote.transform, new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one, lona);

        // Una ranura de cañón por costado en cada piso (vacías al principio). Cañones del tamaño de siempre.
        int filas = 1 + pisos;
        Transform[] babor = new Transform[filas], estribor = new Transform[filas];
        for (int f = 0; f < filas; f++)
        {
            float y = arriba - 0.25f - f * altoPiso;
            babor[f] = Astillero.Canon("Cañón babor " + (f + 1), raiz.transform, new Vector3(-(w / 2f - 0.3f), y, -0.13f * L), -90f, 1f, hierro);
            estribor[f] = Astillero.Canon("Cañón estribor " + (f + 1), raiz.transform, new Vector3(w / 2f - 0.3f, y, -0.13f * L), 90f, 1f, hierro);
        }

        // Flotación: troncos ligeros, flotan alto
        Transform[] puntos = new Transform[4];
        int k = 0;
        foreach (float x in new[] { -0.45f * w, 0.45f * w })
        foreach (float z in new[] { -0.4f * L, 0.4f * L })
        {
            GameObject pt = new GameObject("Flotación");
            pt.transform.SetParent(raiz.transform, false);
            pt.transform.localPosition = new Vector3(x, abajo, z);
            puntos[k++] = pt.transform;
        }
        Flotacion fl = raiz.AddComponent<Flotacion>();
        fl.puntosFlotacion = puntos;
        fl.fuerzaEmpuje = 30f;
        fl.centroDeMasa = new Vector3(0f, abajo + 0.2f, 0f);

        VidaBarco v = raiz.AddComponent<VidaBarco>();
        v.vidaMaxima = 40f;
        v.vida = 40f;

        Canones cn = raiz.AddComponent<Canones>();
        cn.babor = babor;
        cn.estribor = estribor;
        cn.controlJugador = false;
        cn.velocidadBala = 38f;
        cn.Fijar(0, 0);

        ControlBarco cb = raiz.AddComponent<ControlBarco>();
        cb.vela = pivote.transform;
        cb.nivelesVela = 1;
        cb.fuerzaPorNivel = 0.6f;
        cb.fuerzaGiro = 0.7f;
        cb.velocidadParaGiroCompleto = 1.5f;
        cb.controlActivo = false;
        cb.mostrarPanel = false;
        return raiz;
    }

    static void Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = escala;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
    }

    // Busca agua cerca de la orilla, saliendo desde el centro de la isla hacia el personaje.
    // profundidad: cuánta agua necesita el barco bajo el casco (una balsa casi nada, un balandro bastante)
    // largo: eslora del barco; se revisa todo su contorno (popa, proa y costados) para que no nazca encallado
    public static bool BuscarAgua(Vector3 desde, out Vector3 punto, out float rumbo, float profundidad = 1.2f, float alturaSobreAgua = 0.6f, float largo = 6f)
    {
        punto = desde; rumbo = 0f;
        Vector3 afuera = Vector3.forward;
        if (Physics.Raycast(desde + Vector3.up * 2f, Vector3.down, out RaycastHit suelo, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 centro = suelo.collider.bounds.center;
            afuera = desde - centro; afuera.y = 0f;
            if (afuera.sqrMagnitude < 0.01f) afuera = Vector3.forward;
            afuera.Normalize();
        }
        for (float d = 4f; d <= 160f; d += 2f)
        {
            Vector3 c = desde + afuera * d;
            if (HayAguaPara(c, afuera, largo, profundidad))
            {
                punto = new Vector3(c.x, Olas.Altura(c) + alturaSobreAgua, c.z);
                rumbo = Mathf.Atan2(afuera.x, afuera.z) * Mathf.Rad2Deg;
                return true;
            }
        }
        return false;
    }

    // ¿Cabe un barco de este largo aquí sin tocar fondo? Revisa centro, proa, popa y costados.
    public static bool HayAguaPara(Vector3 centro, Vector3 adelante, float largo, float profundidad)
    {
        adelante.y = 0f;
        adelante = adelante.sqrMagnitude > 0.001f ? adelante.normalized : Vector3.forward;
        Vector3 lado = Vector3.Cross(Vector3.up, adelante);
        float medio = largo / 2f + 1.5f, ancho = Mathf.Max(3f, largo * 0.25f) / 2f + 1f;
        Vector3 popa = centro - adelante * medio, proa = centro + adelante * medio;
        return Hondo(centro, profundidad) && Hondo(popa, profundidad) && Hondo(proa, profundidad)
            && Hondo(popa + lado * ancho, profundidad) && Hondo(popa - lado * ancho, profundidad)
            && Hondo(centro + lado * ancho, profundidad) && Hondo(centro - lado * ancho, profundidad)
            && Hondo(proa + lado * ancho, profundidad) && Hondo(proa - lado * ancho, profundidad);
    }

    // Busca el fondo solo contra el terreno (las islas), no contra barcos ni personajes
    static bool Hondo(Vector3 c, float profundidad)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(c.x, 30f, c.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.rigidbody != null || h.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (h.point.y >= -profundidad) return false;
        }
        return true;
    }
}
