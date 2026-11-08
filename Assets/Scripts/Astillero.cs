using UnityEngine;

// Construye barcos low poly a escala real. Lo usan el editor (para armar la escena) y el juego
// (cuando el barco del jugador crece, ver FormaBarco).
// Medidas normales: eslora L, manga 0.25 L, puntal 0.17 L. Un balandro mide 18 m; un bergantín, 27 m.
// Se puede cambiar la proporción (barcos alargados o rechonchos), las ranuras de cañón por costado y los mástiles.
public static class Astillero
{
    [System.Serializable]
    public struct Materiales
    {
        public Material casco, vela, bandera, cubierta, mastil, canon;
    }

    // canonesInstalados: cuántos cañones de los costados vienen puestos (0 = ranuras vacías)
    // w, h: manga y puntal en metros (por defecto 0.25 L y 0.17 L). porFila: cañones por fila y costado
    // (por defecto según el largo). filas: filas de cañones (pisos). largoMastil: largo con que se miden los mástiles.
    public static GameObject CrearBarco(string nombre, float L, Vector3 pos, float rumbo, Materiales m, bool esJugador, int canonesInstalados,
                                        float w = -1f, float h = -1f, int porFila = -1, int filas = 1, int mastiles = 1, float largoMastil = -1f)
    {
        if (w <= 0f) w = L * 0.25f;
        if (h <= 0f) h = L * 0.17f;
        if (porFila <= 0) porFila = Mathf.Clamp(Mathf.FloorToInt(L * 0.62f / FormaBarco.SeparacionCanones), 1, 40);
        if (largoMastil <= 0f) largoMastil = L;
        float s = largoMastil / 18f;

        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        BoxCollider col = raiz.AddComponent<BoxCollider>();
        col.size = new Vector3(w, h, L);

        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 20000f * (L * w * h) / (18f * 4.5f * 3.06f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Transform t = raiz.transform;
        Pieza(PrimitiveType.Cube, "Casco", t, new Vector3(0, 0, -0.1f * L), Quaternion.identity, new Vector3(w, h, L * 0.8f), m.casco);
        Pieza(PrimitiveType.Cube, "Proa", t, new Vector3(0, 0, 0.3f * L), Quaternion.Euler(0, 45, 0), new Vector3(w * 0.7071f, h, w * 0.7071f), m.casco);
        Pieza(PrimitiveType.Cube, "Cubierta", t, new Vector3(0, h / 2 + 0.05f, -0.1f * L), Quaternion.identity, new Vector3(w * 0.92f, 0.1f, L * 0.78f), m.cubierta);
        // Castillo de popa: el timón va encima, atrás del todo, como en los barcos de verdad
        float zCam = Cubierta.CentroCamarote(L), largoCam = Cubierta.LargoCamarote(L);
        Pieza(PrimitiveType.Cube, "Camarote", t, new Vector3(0, h / 2 + Cubierta.AltoCamarote / 2f, zCam), Quaternion.identity, new Vector3(w * 0.8f, Cubierta.AltoCamarote, largoCam), m.casco);
        Vector3 timon = Cubierta.Timon(h, L, false);
        Pieza(PrimitiveType.Cylinder, "Rueda del timón", t, timon + new Vector3(0, 0.9f, 0.7f), Quaternion.Euler(90, 0, 0), new Vector3(1.1f, 0.08f, 1.1f), m.mastil);

        // Mástiles y velas proporcionales al barco real (largo, ancho y alto), sin importar el molde:
        // más largo = más mástiles; más ancho = velas más anchas; todo crece junto.
        int nMast = Mathf.Max(mastiles, Mathf.Clamp(Mathf.RoundToInt(L / 20f), 1, 5));
        float zIni = -0.28f * L, zFin = 0.24f * L;
        float espacio = nMast > 1 ? (zFin - zIni) / (nMast - 1) : L * 0.6f;
        float anchoVela = Mathf.Min(Mathf.Max(w * 1.8f, L * 0.25f / nMast), espacio * 0.95f);
        float hm = 0.6f * L / Mathf.Sqrt(nMast) + w * 1.2f;
        int principal = (nMast - 1) / 2;   // el del medio es el más alto
        Transform velaPrincipal = null;
        Transform[] extras = new Transform[nMast - 1];
        for (int i = 0, e = 0; i < nMast; i++)
        {
            float zm = nMast == 1 ? 0.05f * L : Mathf.Lerp(zIni, zFin, (float)i / (nMast - 1));
            bool esPrincipal = i == principal;
            Transform v = Mastil(t, zm, esPrincipal ? hm : hm * 0.85f, esPrincipal ? anchoVela : anchoVela * 0.9f, h, m, esPrincipal);
            if (esPrincipal) velaPrincipal = v; else extras[e++] = v;
        }

        // Ranuras de cañón: una o varias filas (pisos) por costado. Los cañones son siempre del mismo tamaño.
        int n = Mathf.Max(1, porFila), nf = Mathf.Max(1, filas);
        float sepFilas = Mathf.Min(FormaBarco.AltoPiso, (h - 1.2f) / nf);
        Transform[] babor = new Transform[n * nf], estribor = new Transform[n * nf];
        for (int f = 0; f < nf; f++)
        for (int i = 0; i < n; i++)
        {
            float z = n == 1 ? 0f : Mathf.Lerp(-0.3f * L, 0.24f * L, (float)i / (n - 1));
            float y = Mathf.Max(h / 2 - 0.8f - f * sepFilas, -h / 2 + 0.6f);
            int k = f * n + i;
            babor[k] = Canon("Cañón babor " + (k + 1), t, new Vector3(-(w / 2 + 0.1f), y, z), -90f, 1f, m.canon);
            estribor[k] = Canon("Cañón estribor " + (k + 1), t, new Vector3(w / 2 + 0.1f, y, z), 90f, 1f, m.canon);
        }
        // Un cañón de proa (adelante) y uno de popa (atrás)
        Transform proa = Canon("Cañón proa", t, new Vector3(0, h / 2 - 0.8f, 0.3f * L + w * 0.5f), 0f, 1f, m.canon);
        Transform popa = Canon("Cañón popa", t, new Vector3(0, h / 2 - 0.8f, -0.5f * L - 0.1f), 180f, 1f, m.canon);

        // Puntos de flotación en las esquinas de abajo
        Transform[] puntos = new Transform[4];
        int pk = 0;
        foreach (float x in new[] { -0.45f * w, 0.45f * w })
        foreach (float z in new[] { -0.4f * L, 0.4f * L })
        {
            GameObject p = new GameObject("Flotación " + (pk + 1));
            p.transform.SetParent(t, false);
            p.transform.localPosition = new Vector3(x, -h / 2, z);
            puntos[pk++] = p.transform;
        }

        Flotacion fl = raiz.AddComponent<Flotacion>();
        fl.puntosFlotacion = puntos;
        fl.centroDeMasa = new Vector3(0f, -h * 0.4f, 0f);

        raiz.AddComponent<VidaBarco>();
        Canones c = raiz.AddComponent<Canones>();
        c.babor = babor;
        c.estribor = estribor;
        c.proa = new[] { proa };
        c.popa = new[] { popa };
        c.controlJugador = esJugador;
        if (!esJugador) c.recarga = 3.6f;
        c.Repartir(canonesInstalados);

        ControlBarco cb = raiz.AddComponent<ControlBarco>();
        cb.vela = velaPrincipal;
        cb.velasExtra = extras;
        cb.controlActivo = esJugador;
        cb.mostrarPanel = esJugador;
        return raiz;
    }

    // Un mástil con su verga, su vela (que cuelga de un pivote para subirla y bajarla) y, el principal, la bandera
    // Un mástil con su verga, su vela (que cuelga de un pivote para subirla y bajarla) y, el principal, la bandera.
    // hm = alto del mástil sobre la cubierta; anchoVela = ancho de la vela.
    static Transform Mastil(Transform t, float zm, float hm, float anchoVela, float h, Materiales m, bool principal)
    {
        float grosor = 0.25f + hm * 0.012f;
        Pieza(PrimitiveType.Cylinder, "Mástil", t, new Vector3(0, h / 2 + hm / 2, zm), Quaternion.identity, new Vector3(grosor, hm / 2, grosor), m.mastil);
        float yv = h / 2 + hm * 0.92f;
        Pieza(PrimitiveType.Cube, "Verga", t, new Vector3(0, yv, zm + grosor), Quaternion.identity, new Vector3(anchoVela * 1.08f, grosor * 0.7f, grosor * 0.7f), m.mastil);
        GameObject pivote = new GameObject("Vela");
        pivote.transform.SetParent(t, false);
        pivote.transform.localPosition = new Vector3(0, yv, zm + grosor * 1.5f);
        pivote.transform.localScale = new Vector3(anchoVela, hm * 0.72f, 0.15f);
        Pieza(PrimitiveType.Cube, "Lona", pivote.transform, new Vector3(0, -0.5f, 0), Quaternion.identity, Vector3.one, m.vela);
        if (principal)
        {
            float b = 0.6f + hm * 0.05f;
            Pieza(PrimitiveType.Cube, "Bandera", t, new Vector3(0, h / 2 + hm + b * 0.6f, zm - b), Quaternion.identity, new Vector3(0.08f, b, b * 1.8f), m.bandera);
        }
        return pivote.transform;
    }

    public static Transform Canon(string nombre, Transform padre, Vector3 pos, float giro, float s, Material m)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = pos;
        pivote.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
        Pieza(PrimitiveType.Cylinder, "Tubo", pivote.transform, new Vector3(0, 0, 0.4f * s), Quaternion.Euler(90, 0, 0), new Vector3(0.45f * s, 0.8f * s, 0.45f * s), m);
        Rendimiento.Capa(pivote, Rendimiento.CapaBarcoDetalle);   // de lejos no se dibujan
        return pivote.transform;
    }

    public static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = escala;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        Collider c = g.GetComponent<Collider>();
        Object.DestroyImmediate(c);   // inmediato: si no, Canones guarda colliders que van a desaparecer
        return g;
    }
}
