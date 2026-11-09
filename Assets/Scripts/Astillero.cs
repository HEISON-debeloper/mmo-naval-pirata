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
        // Casco con forma de barco (proa en punta, fondo curvo), cubierta y borda: hechos por código para cualquier tamaño
        Malla("Casco", t, CascoProcedural.Casco(w, h, L), m.casco);
        Malla("Borda", t, CascoProcedural.Borda(w, h, L), m.casco);
        Malla("Cubierta", t, CascoProcedural.CubiertaMalla(w, h, L), m.cubierta);
        // Castillo de popa: el timón va encima, atrás del todo, como en los barcos de verdad
        float zCam = Cubierta.CentroCamarote(L), largoCam = Cubierta.LargoCamarote(L);
        Pieza(PrimitiveType.Cube, "Camarote", t, new Vector3(0, h / 2 + Cubierta.AltoCamarote / 2f, zCam), Quaternion.identity, new Vector3(w * 0.8f, Cubierta.AltoCamarote, largoCam), m.casco);
        // Ventanas del castillo, mirando hacia atrás
        int ventanas = Mathf.Clamp(Mathf.RoundToInt(w * 0.8f / 1.4f), 1, 7);
        for (int i = 0; i < ventanas; i++)
        {
            float x = ventanas == 1 ? 0f : Mathf.Lerp(-w * 0.3f, w * 0.3f, (float)i / (ventanas - 1));
            Pieza(PrimitiveType.Cube, "Ventana", t, new Vector3(x, h / 2 + Cubierta.AltoCamarote * 0.55f, zCam - largoCam / 2f - 0.02f), Quaternion.identity, new Vector3(0.55f, 0.7f, 0.06f), m.canon);
        }
        Vector3 timon = Cubierta.Timon(h, L, false);
        if (Modelos.Existe("timon"))
            Modelos.Crear("timon", t, timon + new Vector3(0, 0.95f, 0.7f), Quaternion.identity, 1f, _ => m.mastil);
        else
            Pieza(PrimitiveType.Cylinder, "Rueda", t, timon + new Vector3(0, 0.9f, 0.7f), Quaternion.Euler(90, 0, 0), new Vector3(1.1f, 0.08f, 1.1f), m.mastil);
        // Farol de popa y anclas a los lados de la proa
        if (Modelos.Existe("farol"))
            Modelos.Crear("farol", t, new Vector3(0f, h / 2 + Cubierta.AltoCamarote, zCam - largoCam / 2f + 0.3f), Quaternion.identity, Mathf.Clamp(w / 4f, 1f, 2.5f),
                n => n == "Vidrio" ? Modelos.DeColor(m.vela, new Color(1f, 0.78f, 0.35f), true) : m.canon);

        // Aparejo real: mástiles con varias velas cuadradas una encima de otra (más pisos de vela si el mástil es alto),
        // bauprés con foque (vela triangular) en la proa y cangreja (vela de cuchillo) en el mástil de popa.
        // Todo sale del largo, el ancho y el alto del barco: más largo = más mástiles; más ancho = velas más anchas;
        // más alto (más pisos) = mástiles más altos con más velas.
        int nMast = Mathf.Max(mastiles, Mathf.Clamp(Mathf.RoundToInt(L / 20f), 1, 5));
        float zIni = -0.28f * L, zFin = 0.24f * L;
        if (nMast == 1) { zIni = zFin = 0.05f * L; }
        float espacio = nMast > 1 ? (zFin - zIni) / (nMast - 1) : L * 0.6f;
        float anchoVela = Mathf.Min(Mathf.Max(w * 1.8f, L * 0.25f / nMast), espacio * 1.1f);
        float hm = 0.55f * L / Mathf.Sqrt(nMast) + w * 1.1f + h * 0.6f;
        int principal = nMast / 2;   // el del medio es el más alto
        Transform velaPrincipal = null;
        var extras = new System.Collections.Generic.List<Transform>();
        float zProaMastil = zFin, hmProa = hm;
        for (int i = 0; i < nMast; i++)
        {
            float zm = nMast == 1 ? zIni : Mathf.Lerp(zIni, zFin, (float)i / (nMast - 1));
            bool esPrincipal = i == principal;
            float alto = esPrincipal ? hm : hm * (i == 0 ? 0.78f : 0.88f);   // el de popa (mesana) más bajo
            float ancho = esPrincipal ? anchoVela : anchoVela * (i == 0 ? 0.75f : 0.9f);
            var velas = Mastil(t, zm, alto, ancho, h, m, esPrincipal);
            foreach (Transform v in velas) { if (esPrincipal && velaPrincipal == null) velaPrincipal = v; else extras.Add(v); }
            if (i == nMast - 1) { zProaMastil = zm; hmProa = alto; }
            // Cangreja: vela de cuchillo detrás del mástil de popa (solo con 2 o más mástiles)
            if (i == 0 && nMast >= 2)
            {
                float hc = alto * 0.42f, lc = Mathf.Min(espacio * 0.55f, zm - (-0.5f * L) + 1f);
                Pieza(PrimitiveType.Cylinder, "Botavara", t, new Vector3(0, h / 2 + Cubierta.AltoCamarote + 1.4f, zm - lc / 2f), Quaternion.Euler(90, 0, 0), new Vector3(0.18f, lc / 2f, 0.18f), m.mastil);
                GameObject pc = new GameObject(VelaCuchillo);
                pc.transform.SetParent(t, false);
                pc.transform.localPosition = new Vector3(0, h / 2 + Cubierta.AltoCamarote + 1.5f + hc, zm - 0.3f);
                pc.transform.localScale = new Vector3(hc * 0.12f, hc, lc);
                Malla("Lona", pc.transform, CascoProcedural.VelaCangreja(), m.vela);
                extras.Add(pc.transform);
            }
        }
        // Bauprés (palo que sale de la proa) y foque: vela triangular del bauprés al mástil de proa
        float zRoda = CascoProcedural.ZProa(w, L);
        float largoBaupres = Mathf.Clamp(0.16f * L, 2.5f, 22f);
        Vector3 baseBaupres = new Vector3(0, h / 2 + 0.3f, zRoda - 0.6f);
        Quaternion giroBaupres = Quaternion.Euler(70f, 0, 0);   // apunta adelante y un poco arriba
        Vector3 puntaBaupres = baseBaupres + giroBaupres * Vector3.up * largoBaupres;
        Pieza(PrimitiveType.Cylinder, "Bauprés", t, baseBaupres + giroBaupres * Vector3.up * (largoBaupres / 2f), giroBaupres, new Vector3(0.22f + L * 0.003f, largoBaupres / 2f, 0.22f + L * 0.003f), m.mastil);
        {
            float topeY = h / 2 + hmProa * 0.72f;
            float altoF = topeY - (puntaBaupres.y + 0.3f);
            GameObject pf = new GameObject(VelaCuchillo);
            pf.transform.SetParent(t, false);
            pf.transform.localPosition = new Vector3(0, topeY, zProaMastil + 0.4f);
            float largoF = Mathf.Max(1f, puntaBaupres.z - (zProaMastil + 0.4f));
            pf.transform.localScale = new Vector3(altoF * 0.1f, altoF, largoF);
            Malla("Lona", pf.transform, CascoProcedural.VelaFoque(), m.vela);
            extras.Add(pf.transform);
        }
        // Espolón de bronce en la punta de la proa, a ras del agua: parte barcos al embestir
        float eEsp = Mathf.Clamp(w / 4f, 0.7f, 3.5f);
        if (Modelos.Existe("espolon"))
            Modelos.Crear("espolon", t, new Vector3(0f, -h / 2f + 0.62f * h, zRoda - 0.25f * eEsp), Quaternion.identity, eEsp, _ => m.canon);
        else
            Pieza(PrimitiveType.Cube, "Espolon", t, new Vector3(0f, 0.1f * h, zRoda + 0.6f * eEsp), Quaternion.Euler(0, 45, 0), new Vector3(eEsp, eEsp * 0.6f, eEsp), m.canon);
        raiz.AddComponent<Espolon>();

        // Ranuras de cañón por piso: en cada piso caben los que entran en el casco a esa altura
        // (abajo el casco se angosta y la quilla sube en la proa, así que los pisos de abajo pueden llevar menos).
        // Con casco alto van por dentro, asomando por portas (huecos con tapa); con casco bajo van en cubierta.
        bool enCubierta = h < AltoMinPortas;
        var puestos = PuestosCanones(L, w, h, Mathf.Max(1, porFila), Mathf.Max(1, filas));
        Transform[] babor = new Transform[puestos.Count], estribor = new Transform[puestos.Count];
        for (int k = 0; k < puestos.Count; k++)
        {
            float y = puestos[k].x, z = puestos[k].y;
            if (enCubierta)
            {
                float x = CascoProcedural.Costado(z, h / 2f, w, h, L) - 0.75f;
                babor[k] = Canon("Cañón babor " + (k + 1), t, new Vector3(-x, h / 2f + 0.1f, z), -90f, 1f, m.canon, true, m.mastil);
                estribor[k] = Canon("Cañón estribor " + (k + 1), t, new Vector3(x, h / 2f + 0.1f, z), 90f, 1f, m.canon, true, m.mastil);
            }
            else
            {
                float x = Mathf.Max(CascoProcedural.Costado(z, y - 0.42f, w, h, L), CascoProcedural.Costado(z, y + 0.42f, w, h, L)) - 0.45f;
                babor[k] = Canon("Cañón babor " + (k + 1), t, new Vector3(-x, y, z), -90f, 1f, m.canon);
                estribor[k] = Canon("Cañón estribor " + (k + 1), t, new Vector3(x, y, z), 90f, 1f, m.canon);
            }
        }
        if (!enCubierta && puestos.Count > 0)
        {
            CascoProcedural.Portas(puestos, w, h, L, out Mesh huecos, out Mesh marcos, out Mesh tapas);
            Rendimiento.Capa(Malla("Portas", t, huecos, Modelos.DeColor(m.canon, new Color(0.04f, 0.03f, 0.025f))), Rendimiento.CapaBarcoDetalle);
            Rendimiento.Capa(Malla("Marcos", t, marcos, m.mastil), Rendimiento.CapaBarcoDetalle);
            Rendimiento.Capa(Malla("Tapas", t, tapas, m.casco), Rendimiento.CapaBarcoDetalle);
        }
        // Anclas a los lados de la proa, más adelante que las portas
        if (Modelos.Existe("ancla"))
        {
            float ea = Mathf.Clamp(h / 3.5f, 0.7f, 2.5f);
            float zMax = 0.3f * L - 0.75f * w;
            foreach (Vector2 pu in puestos) zMax = Mathf.Max(zMax, pu.y + 0.9f + 0.35f * ea);
            float za = Mathf.Min(zMax, CascoProcedural.ZProa(w, L) - 1.2f * ea);
            float ya = h / 2f - 0.85f * ea;
            float xa = CascoProcedural.Costado(za, ya + 0.4f * ea, w, h, L) + 0.12f * ea;
            foreach (float lado in new[] { -1f, 1f })
                Modelos.Crear("ancla", t, new Vector3(lado * xa, ya, za), Quaternion.Euler(0f, lado * 90f, 0f), ea,
                    n => n == "Cepo" ? m.mastil : m.canon);
        }
        // Dos cañones de proa en cubierta, uno a cada lado, mirando al frente (no giran hacia los lados)
        float zCp = 0.3f * L - 0.6f * w - 0.6f, xCp = Mathf.Max(0.6f, w / 2f - 0.7f);
        Transform proaB = Canon("Cañón proa babor", t, new Vector3(-xCp, h / 2 + 0.1f, zCp), 0f, 1f, m.canon, true, m.mastil);
        Transform proaE = Canon("Cañón proa estribor", t, new Vector3(xCp, h / 2 + 0.1f, zCp), 0f, 1f, m.canon, true, m.mastil);
        // Popa: repisa con barriles en llamas que se sueltan al agua
        Transform popa = Barriles("Barriles popa", t, new Vector3(0, h / 2 - 0.2f, -0.5f * L - 0.5f), Mathf.Clamp(w / 5f, 0.8f, 2f), m);

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
        c.proa = new[] { proaB, proaE };
        c.popa = new[] { popa };
        c.controlJugador = esJugador;
        if (!esJugador) c.recarga = 3.6f;
        c.Repartir(canonesInstalados);

        ControlBarco cb = raiz.AddComponent<ControlBarco>();
        cb.vela = velaPrincipal;
        cb.velasExtra = extras.ToArray();
        cb.controlActivo = esJugador;
        cb.mostrarPanel = esJugador;
        return raiz;
    }

    public const string VelaCuchillo = "Vela de cuchillo";   // foque y cangreja: al recogerlas se esconden
    public const float AltoMinPortas = 2.6f;   // con casco más bajo los cañones van en cubierta
    public const float SeparacionMin = 1.9f;   // lo más juntos que pueden quedar dos cañones
    public const float PrimeraFila = 1.2f;     // la fila de arriba va 1,2 m bajo la cubierta (deja altura para caminar adentro)

    // Dónde va cada cañón de un costado: (y, z) en el barco, piso por piso (de arriba hacia abajo).
    // porFila es el tope por piso (lo que da el largo); cada piso lleva los que le quepan.
    public static System.Collections.Generic.List<Vector2> PuestosCanones(float L, float w, float h, int porFila, int filas)
    {
        var res = new System.Collections.Generic.List<Vector2>();
        bool enCubierta = h < AltoMinPortas;
        int nf = enCubierta ? 1 : Mathf.Max(1, filas);
        float sepFilas = Mathf.Min(FormaBarco.AltoPiso, (h - 1.2f) / nf);
        float b = CascoProcedural.AltoPorta / 2f;
        float sep = FormaBarco.SeparacionCanones;
        for (int f = 0; f < nf; f++)
        {
            float y = enCubierta ? h / 2f : Mathf.Max(h / 2 - PrimeraFila - f * sepFilas, -h / 2 + 0.6f);
            // Cabe un cañón en z si el casco a esa altura es ancho de sobra (arriba y abajo de la porta)
            bool Cabe(float z) => enCubierta
                ? CascoProcedural.Costado(z, y, w, h, L) >= Mathf.Max(0.9f, 0.3f * w)
                : Mathf.Min(CascoProcedural.Costado(z, y - b, w, h, L), CascoProcedural.Costado(z, y + b, w, h, L)) >= 0.33f * w;
            float zA = -0.5f * L + Mathf.Max(1.4f, 0.07f * L);   // atrás queda el espejo de popa
            float zB = 0.3f * L - 0.6f * w - 0.9f;                 // adelante, donde empieza a cerrarse la proa
            while (zB < CascoProcedural.ZProa(w, L) - 1f && Cabe(zB + 0.25f)) zB += 0.25f;   // pisos altos: un poco más adelante
            if (enCubierta) zB = Mathf.Min(zB, 0.3f * L - 0.6f * w - 2.1f);   // que no choquen con los cañones de proa
            while (zB > zA && !Cabe(zB)) zB -= 0.25f;
            while (zA < zB && !Cabe(zA)) zA += 0.25f;
            if (zB < zA) continue;
            int cabe = Mathf.FloorToInt((zB - zA) / SeparacionMin) + 1;
            int n = Mathf.Min(porFila, cabe);
            float paso = n > 1 ? Mathf.Min(sep, (zB - zA) / (n - 1)) : 0f;
            float zc = (zA + zB) / 2f;
            for (int i = 0; i < n; i++) res.Add(new Vector2(y, zc + (i - (n - 1) / 2f) * paso));
        }
        return res;
    }

    // Un mástil con sus velas cuadradas, una encima de otra (1 a 4 según lo alto), cada una con su verga,
    // y la bandera arriba en el principal. Cada vela cuelga de un pivote que se estira para subirla y bajarla.
    // hm = alto del mástil sobre la cubierta; anchoVela = ancho de la vela de abajo (las de arriba son más angostas).
    static System.Collections.Generic.List<Transform> Mastil(Transform t, float zm, float hm, float anchoVela, float h, Materiales m, bool principal)
    {
        var velas = new System.Collections.Generic.List<Transform>();
        float grosor = 0.25f + hm * 0.012f;
        Pieza(PrimitiveType.Cylinder, "Mástil", t, new Vector3(0, h / 2 + hm / 2, zm), Quaternion.identity, new Vector3(grosor, hm / 2, grosor), m.mastil);
        int pisos = hm < 9f ? 1 : hm < 18f ? 2 : hm < 30f ? 3 : 4;
        float abajo = h / 2 + Mathf.Max(2.2f, hm * 0.12f), arriba = h / 2 + hm * 0.95f;
        float tramo = (arriba - abajo) / pisos;
        for (int k = 0; k < pisos; k++)
        {
            float yv = abajo + tramo * (k + 1);                        // verga arriba de cada vela
            float ancho = anchoVela * (1f - 0.17f * k);                 // más angostas hacia arriba
            float gv = grosor * (0.75f - 0.08f * k);
            Pieza(PrimitiveType.Cylinder, "Verga", t, new Vector3(0, yv, zm + grosor), Quaternion.Euler(0, 0, 90), new Vector3(gv, ancho * 0.56f, gv), m.mastil);
            GameObject pivote = new GameObject("Vela");
            pivote.transform.SetParent(t, false);
            pivote.transform.localPosition = new Vector3(0, yv, zm + grosor * 1.5f);
            pivote.transform.localScale = new Vector3(ancho, tramo * 0.93f, ancho * 0.16f);
            Lona(pivote.transform, m.vela);
            velas.Add(pivote.transform);
        }
        if (principal)
        {
            float b = 0.6f + hm * 0.05f;
            Pieza(PrimitiveType.Cube, "Bandera", t, new Vector3(0, h / 2 + hm + b * 0.6f, zm - b), Quaternion.identity, new Vector3(0.08f, b, b * 1.8f), m.bandera);
        }
        return velas;
    }

    // Repisa de popa con barriles en llamas (se ven dos; al soltar uno sale por detrás)
    static Transform Barriles(string nombre, Transform padre, Vector3 pos, float e, Materiales m)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = pos;
        pivote.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // su eje Z mira hacia atrás
        Pieza(PrimitiveType.Cube, "Repisa", pivote.transform, new Vector3(0, -0.1f * e, 0.2f * e), Quaternion.identity, new Vector3(2.2f * e, 0.15f * e, 1.1f * e), m.mastil);
        foreach (float x in new[] { -0.5f, 0.5f })
        {
            if (Modelos.Existe("barril"))
                Modelos.Crear("barril", pivote.transform, new Vector3(x * e, -0.02f * e, 0.2f * e), Quaternion.identity, e, n => n == "Aros" ? m.canon : m.mastil);
            else
                Pieza(PrimitiveType.Cylinder, "Barril", pivote.transform, new Vector3(x * e, 0.45f * e, 0.2f * e), Quaternion.identity, new Vector3(0.65f * e, 0.45f * e, 0.65f * e), m.mastil);
        }
        Rendimiento.Capa(pivote, Rendimiento.CapaBarcoDetalle);
        return pivote.transform;
    }

    // cureña: con su base de madera y ruedas (en cubierta, en la balsa) o solo el tubo (asomando por las portas del casco)
    // Vela curva (inflada por el viento) dentro del pivote que sube y baja
    public static GameObject Lona(Transform pivote, Material m)
    {
        GameObject g = new GameObject("Lona");
        g.transform.SetParent(pivote, false);
        g.AddComponent<MeshFilter>().sharedMesh = CascoProcedural.Vela();
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    // Una pieza con malla hecha por código
    public static GameObject Malla(string nombre, Transform padre, Mesh malla, Material m)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    public static Transform Canon(string nombre, Transform padre, Vector3 pos, float giro, float s, Material m, bool cureña = false, Material madera = null)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = pos;
        pivote.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
        if (Modelos.Existe("canon"))
        {
            Material mad = madera != null ? madera : m;
            if (cureña) Modelos.Crear("canon", pivote.transform, new Vector3(0f, 0.42f * s, 0.25f * s), Quaternion.identity, s, n => n == "Tubo" ? m : mad);
            else Modelos.Crear("canon", pivote.transform, new Vector3(0f, 0f, 0.25f * s), Quaternion.identity, s, _ => m, "Tubo");
        }
        else
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
