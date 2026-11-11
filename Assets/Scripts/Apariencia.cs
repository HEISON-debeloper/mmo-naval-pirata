using System.Collections.Generic;
using UnityEngine;

// Aspecto de los piratas: color de camisa y piel, maquillaje, falda, barba, banderita de orgullo en la espalda
// y los adornos de Luwyn Antena Caída. Lo usan el jugador (ventana "Tu pirata", tecla T) y los tripulantes
// de los barcos piratas (cada uno sale distinto: con falda, maquillaje, barba o banderita trans, de orgullo…).
//
// Luwyn Antena Caída es un personaje especial: barba loca, la falda más linda (de volantes), maquillaje completo
// y una antena caída sobre el sombrero. Su barco lleva de insignia una crisopa (mascarón y bandera).
// Se desbloquea besando a 5 capitanes y 3 cañoneros (F junto a ellos, en su cubierta).
public static class Apariencia
{
    [System.Serializable]
    public class Datos
    {
        public int camisa, piel, maquillaje, falda, barba, banderita;
        public bool antena;
        public string personaje = "";
        public Datos Copia() => (Datos)MemberwiseClone();
    }

    public static readonly (string nombre, Color color)[] Camisas =
    {
        ("Azul", new Color(0.18f, 0.44f, 0.69f)), ("Rojo", new Color(0.7f, 0.16f, 0.14f)), ("Blanco", new Color(0.9f, 0.88f, 0.82f)),
        ("Negro", new Color(0.12f, 0.12f, 0.13f)), ("Verde", new Color(0.16f, 0.45f, 0.25f)), ("Morado", new Color(0.45f, 0.22f, 0.58f)),
        ("Rosado", new Color(0.92f, 0.5f, 0.68f)), ("Amarillo", new Color(0.92f, 0.75f, 0.2f)),
    };
    public static readonly (string nombre, Color color)[] Pieles =
    {
        ("Clara", new Color(0.88f, 0.71f, 0.54f)), ("Trigueña", new Color(0.74f, 0.53f, 0.36f)), ("Morena", new Color(0.55f, 0.36f, 0.23f)),
        ("Oscura", new Color(0.33f, 0.21f, 0.14f)), ("Muy clara", new Color(0.96f, 0.83f, 0.72f)),
    };
    public static readonly string[] Maquillajes = { "Ninguno", "Rubor", "Labios y rubor", "Completo" };
    public static readonly string[] Faldas = { "Ninguna", "Falda", "Falda de volantes" };
    public static readonly string[] Barbas = { "Ninguna", "Barba", "Barba loca" };
    // Banderita: 0 ninguna; si no, el dibujo de bandera de Pintura (orgullo, trans, bisexual, lesbiana, no binaria)
    public static readonly string[] Banderitas = { "Ninguna", "Orgullo", "Trans", "Bisexual", "Lesbiana", "No binaria" };
    static readonly Pintura.Patron[] PatronBanderita = { Pintura.Patron.Liso, Pintura.Patron.Arcoiris, Pintura.Patron.Trans, Pintura.Patron.Bisexual, Pintura.Patron.Lesbiana, Pintura.Patron.NoBinaria };

    public const string Luwyn = "luwyn", NombreLuwyn = "Luwyn Antena Caída";
    public const int CapitanesParaLuwyn = 5, CanonerosParaLuwyn = 3;

    // ---------- Lo del jugador (se guarda con la partida) ----------
    public static Datos Jugador = new Datos();
    public static int BesosCapitanes, BesosCanoneros;
    public static bool LuwynDesbloqueado;
    public static bool EsLuwyn => Jugador.personaje == Luwyn;

    // La falda de volantes, la barba loca y la antena son de Luwyn
    public static bool Bloqueado(string que, int valor) =>
        !LuwynDesbloqueado && ((que == "falda" && valor == 2) || (que == "barba" && valor == 2) || (que == "antena" && valor == 1));

    public static Datos DeLuwyn() => new Datos { camisa = 5, piel = Jugador.piel, maquillaje = 3, falda = 2, barba = 2, banderita = Jugador.banderita, antena = true, personaje = Luwyn };

    public static void Fijar(Datos d, int capitanes, int canoneros, bool luwyn)
    {
        Jugador = d ?? new Datos();
        BesosCapitanes = capitanes; BesosCanoneros = canoneros;
        LuwynDesbloqueado = luwyn || (capitanes >= CapitanesParaLuwyn && canoneros >= CanonerosParaLuwyn);
        AplicarJugador();
    }

    public static string Progreso => "Capitanes " + Mathf.Min(BesosCapitanes, CapitanesParaLuwyn) + "/" + CapitanesParaLuwyn +
                                     " · cañoneros " + Mathf.Min(BesosCanoneros, CanonerosParaLuwyn) + "/" + CanonerosParaLuwyn;

    // Un beso a un capitán o a un cañonero: cuenta para Luwyn
    public static void ContarBeso(Tripulante.Rol rol)
    {
        if (rol == Tripulante.Rol.Capitan) BesosCapitanes++;
        else if (rol == Tripulante.Rol.Canonero) BesosCanoneros++;
        if (!LuwynDesbloqueado && BesosCapitanes >= CapitanesParaLuwyn && BesosCanoneros >= CanonerosParaLuwyn)
        {
            LuwynDesbloqueado = true;
            Sonidos.Reproducir(Sonidos.Tipo.Descubrir);
            Notificaciones.Mostrar("¡Desbloqueaste a " + NombreLuwyn + "! Elígelo en Tu pirata (" + Teclas.Nombre(Accion.Pirata) +
                                   "). Su barco lleva una crisopa de insignia.", Notificaciones.Tipo.Descubrimiento, 8f);
        }
    }

    public static void AplicarJugador()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Construir(p.transform, Jugador, true, Tripulante.Rol.Marinero);
        OcultarDeCerca o = p.GetComponent<OcultarDeCerca>();
        if (o == null) o = p.gameObject.AddComponent<OcultarDeCerca>();
        o.Refrescar();
    }

    // Luwyn: su barco lleva la crisopa de mascarón y en la bandera (gratis)
    public static void InsigniaCrisopa()
    {
        Personaje p = Personaje.Instancia;
        if (p == null || p.barco == null) return;
        Pintura pin = p.barco.GetComponent<Pintura>();
        if (pin == null) pin = p.barco.gameObject.AddComponent<Pintura>();
        pin.datos.mascaron = "mascaron_crisopa";
        Pintura.Estilo b = pin.De(Pintura.Parte.Bandera);
        b.pintado = true; b.color = new Color(0.09f, 0.09f, 0.1f); b.patron = (int)Pintura.Patron.Crisopa;
        pin.Aplicar();
    }

    // Para un tripulante pirata: cada uno distinto (misma semilla, mismo aspecto)
    public static Datos Aleatoria(System.Random r, Tripulante.Rol rol)
    {
        var d = new Datos();
        double x = r.NextDouble();
        d.maquillaje = x < 0.12 ? 3 : x < 0.22 ? 2 : x < 0.3 ? 1 : 0;
        d.falda = r.NextDouble() < 0.22 ? 1 : 0;
        d.barba = r.NextDouble() < (rol == Tripulante.Rol.Capitan ? 0.6 : 0.35) ? 1 : 0;
        d.banderita = r.NextDouble() < 0.18 ? 1 + r.Next(5) : 0;
        return d;
    }

    // ---------- Armar las piezas ----------
    // raiz: el pirata (cápsula de 1,8 m que mira hacia +z). jugador: también cambia el color de la camisa y la piel.
    public static void Construir(Transform raiz, Datos d, bool jugador, Tripulante.Rol rol)
    {
        if (raiz == null || d == null) return;
        Transform viejo = raiz.Find("Apariencia");
        if (viejo != null) Object.Destroy(viejo.gameObject);
        Material baseMat = null;
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial != null) { baseMat = r.sharedMaterial; break; }
        if (baseMat == null) return;
        Color piel = Pieles[Mathf.Clamp(d.piel, 0, Pieles.Length - 1)].color;
        if (jugador)
        {
            Color camisa = Camisas[Mathf.Clamp(d.camisa, 0, Camisas.Length - 1)].color;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Cuerpo") r.sharedMaterial = Modelos.DeColor(baseMat, camisa);
                else if (r.name.StartsWith("Nariz")) r.sharedMaterial = Modelos.DeColor(baseMat, piel);
            }
        }
        var ap = new GameObject("Apariencia").transform;
        ap.SetParent(raiz, false);
        M m = new M(baseMat);

        // Ojos (todos tienen; con el maquillaje completo, sombra encima)
        foreach (float s in new[] { -1f, 1f })
            Esfera(ap, new Vector3(s * 0.1f, 1.62f, 0.262f), new Vector3(0.06f, 0.07f, 0.04f), m.De(new Color(0.06f, 0.05f, 0.05f)));

        // Maquillaje
        if (d.maquillaje >= 1)
            foreach (float s in new[] { -1f, 1f })
                Esfera(ap, new Vector3(s * 0.17f, 1.5f, 0.243f), new Vector3(0.1f, 0.055f, 0.03f), m.De(new Color(0.95f, 0.45f, 0.55f)), s * 35f);
        if (d.maquillaje >= 2)
            Esfera(ap, new Vector3(0f, 1.43f, 0.296f), new Vector3(0.13f, 0.04f, 0.03f), m.De(new Color(0.8f, 0.05f, 0.18f)));
        if (d.maquillaje >= 3)
            foreach (float s in new[] { -1f, 1f })
                Esfera(ap, new Vector3(s * 0.1f, 1.675f, 0.255f), new Vector3(0.1f, 0.035f, 0.03f), m.De(new Color(0.55f, 0.25f, 0.75f)));

        // Barba
        Color pelo = d.barba == 2 ? new Color(0.62f, 0.27f, 0.08f) : new Color(0.24f, 0.15f, 0.08f);
        if (d.barba == 1)
            Esfera(ap, new Vector3(0f, 1.37f, 0.2f), new Vector3(0.38f, 0.24f, 0.2f), m.De(pelo));
        else if (d.barba == 2)
        {
            // Barba loca: mechones desordenados que bajan hasta el pecho, y dos trenzas
            var rb = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                float a = (float)rb.NextDouble() * 2f - 1f, b = (float)rb.NextDouble();
                Vector3 pos = new Vector3(a * 0.2f, 1.42f - b * 0.42f, 0.2f + (1f - b) * 0.06f + (float)rb.NextDouble() * 0.05f);
                Color c = Color.Lerp(pelo, new Color(0.85f, 0.45f, 0.12f), (float)rb.NextDouble() * 0.5f);
                Esfera(ap, pos, new Vector3(0.14f + (float)rb.NextDouble() * 0.1f, 0.18f + (float)rb.NextDouble() * 0.12f, 0.12f), m.De(c), a * 40f, ((float)rb.NextDouble() - 0.5f) * 50f);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Palo(ap, new Vector3(s * 0.12f, 1.25f, 0.24f), new Vector3(s * 0.2f, 0.85f, 0.28f), 0.035f, m.De(pelo));
                Esfera(ap, new Vector3(s * 0.2f, 0.84f, 0.28f), Vector3.one * 0.07f, m.De(new Color(0.3f, 0.8f, 0.45f)));   // cintas verdes (de crisopa)
            }
        }

        // Falda
        if (d.falda == 1)
            Falda(ap, 0.31f, 0.52f, 1.02f, 0.38f, 0f, m.De(Color.Lerp(new Color(0.35f, 0.12f, 0.15f), new Color(0.15f, 0.2f, 0.45f), jugador ? 0f : 0.5f)));
        else if (d.falda == 2)
        {
            // La falda más linda: rosada, de dos pisos con volantes, borde blanco y moño verde en la cintura
            Falda(ap, 0.31f, 0.46f, 1.05f, 0.62f, 0.025f, m.De(new Color(0.96f, 0.55f, 0.75f)));
            Falda(ap, 0.42f, 0.62f, 0.66f, 0.3f, 0.04f, m.De(new Color(0.98f, 0.66f, 0.82f)));
            Falda(ap, 0.6f, 0.65f, 0.34f, 0.27f, 0.04f, m.De(new Color(1f, 0.97f, 0.95f)));
            Esfera(ap, new Vector3(0f, 1.0f, 0.31f), new Vector3(0.22f, 0.1f, 0.06f), m.De(new Color(0.3f, 0.75f, 0.4f)));
            Esfera(ap, new Vector3(0f, 1.0f, 0.33f), new Vector3(0.06f, 0.07f, 0.05f), m.De(new Color(0.2f, 0.55f, 0.3f)));
        }

        // Antena caída: sale del sombrero, sube y se dobla hacia un lado, con una bolita en la punta
        if (d.antena)
        {
            Material ant = m.De(new Color(0.2f, 0.62f, 0.3f));
            Vector3 a0 = new Vector3(0f, 1.82f, 0f), a1 = new Vector3(0.02f, 2.22f, 0f), a2 = new Vector3(0.24f, 2.16f, 0.04f), a3 = new Vector3(0.33f, 1.94f, 0.06f);
            Palo(ap, a0, a1, 0.018f, ant); Palo(ap, a1, a2, 0.016f, ant); Palo(ap, a2, a3, 0.014f, ant);
            Esfera(ap, a3, Vector3.one * 0.08f, m.De(new Color(1f, 0.82f, 0.2f), true));
        }

        // Sombrero del capitán y pañuelo del cañonero (los tripulantes no llevan el sombrero del jugador)
        if (!jugador && rol == Tripulante.Rol.Capitan)
        {
            Cilindro(ap, new Vector3(0f, 1.8f, 0f), new Vector3(0.78f, 0.03f, 0.78f), m.De(new Color(0.08f, 0.08f, 0.09f)));
            Cilindro(ap, new Vector3(0f, 1.9f, 0f), new Vector3(0.44f, 0.1f, 0.44f), m.De(new Color(0.08f, 0.08f, 0.09f)));
            Cilindro(ap, new Vector3(0f, 1.86f, 0f), new Vector3(0.45f, 0.025f, 0.45f), m.De(new Color(0.85f, 0.68f, 0.25f)));
            Esfera(ap, new Vector3(-0.18f, 2.05f, -0.05f), new Vector3(0.06f, 0.3f, 0.06f), m.De(new Color(0.95f, 0.95f, 0.92f)), 0f, 25f);   // pluma
        }
        else if (!jugador && rol == Tripulante.Rol.Canonero)
            Esfera(ap, new Vector3(0f, 1.74f, -0.02f), new Vector3(0.62f, 0.2f, 0.62f), m.De(new Color(0.75f, 0.12f, 0.1f)));

        // Banderita en la espalda
        if (d.banderita > 0 && d.banderita < PatronBanderita.Length)
        {
            Vector3 b0 = new Vector3(0.16f, 0.95f, -0.3f), b1 = new Vector3(0.18f, 2.4f, -0.36f);
            Palo(ap, b0, b1, 0.02f, m.De(new Color(0.35f, 0.24f, 0.14f)));
            Material tela = m.Bandera(PatronBanderita[d.banderita]);
            foreach (float giro in new[] { 90f, -90f })   // dos caras, para que se vea de los dos lados
            {
                GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Banderita";
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(ap, false);
                q.transform.localPosition = new Vector3(0.18f, 2.22f, -0.36f - 0.24f);
                q.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
                q.transform.localScale = new Vector3(0.48f, 0.3f, 1f);
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = tela;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        if (!jugador) Rendimiento.Capa(ap.gameObject, Rendimiento.CapaBarcoDetalle);
    }

    // Materiales de colores a partir del material del pirata (se reutilizan)
    class M
    {
        readonly Material b;
        public M(Material b) { this.b = b; }
        public Material De(Color c, bool brilla = false) => Modelos.DeColor(b, c, brilla);
        public Material Bandera(Pintura.Patron p)
        {
            if (banderas.TryGetValue((b, p), out Material mt) && mt != null) return mt;
            mt = new Material(b) { name = "Banderita " + p };
            if (mt.HasProperty("_BaseColor")) mt.SetColor("_BaseColor", Color.white);
            Texture2D t = Pintura.Textura(p, Color.white, Color.black);
            if (mt.HasProperty("_BaseMap")) mt.SetTexture("_BaseMap", t);
            if (mt.HasProperty("_MainTex")) mt.SetTexture("_MainTex", t);
            banderas[(b, p)] = mt;
            return mt;
        }
    }
    static readonly Dictionary<(Material, Pintura.Patron), Material> banderas = new Dictionary<(Material, Pintura.Patron), Material>();

    static Transform Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material m, string nombre)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    static void Esfera(Transform p, Vector3 pos, Vector3 escala, Material m, float yaw = 0f, float roll = 0f)
        => Pieza(PrimitiveType.Sphere, p, pos, escala, m, "Adorno").localRotation = Quaternion.Euler(0f, yaw, roll);

    static void Cilindro(Transform p, Vector3 pos, Vector3 escala, Material m) => Pieza(PrimitiveType.Cylinder, p, pos, escala, m, "Sombrero");

    // Un palito (cilindro) de a a b
    static void Palo(Transform p, Vector3 a, Vector3 b, float radio, Material m)
    {
        Vector3 d = b - a;
        Transform t = Pieza(PrimitiveType.Cylinder, p, (a + b) / 2f, new Vector3(radio * 2f, d.magnitude / 2f, radio * 2f), m, "Palo");
        t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
    }

    // Falda: un cono truncado abierto (con caras por dentro y por fuera) y el borde ondulado si lleva volantes
    static readonly Dictionary<(float, float, float, float, float), Mesh> mallas = new Dictionary<(float, float, float, float, float), Mesh>();
    static void Falda(Transform p, float rArriba, float rAbajo, float yArriba, float yAbajo, float volantes, Material mat)
    {
        var clave = (rArriba, rAbajo, yArriba, yAbajo, volantes);
        if (!mallas.TryGetValue(clave, out Mesh malla) || malla == null)
        {
            const int N = 28;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int lado = 0; lado < 2; lado++)   // dos juegos de vértices: cara de afuera y cara de adentro, cada una con su normal
            {
                int o = v.Count;
                for (int i = 0; i <= N; i++)
                {
                    float a = i * Mathf.PI * 2f / N;
                    float ola = volantes > 0f ? Mathf.Sin(a * 9f) * volantes : 0f;
                    v.Add(new Vector3(Mathf.Sin(a) * rArriba, yArriba, Mathf.Cos(a) * rArriba));
                    v.Add(new Vector3(Mathf.Sin(a) * (rAbajo + ola), yAbajo + ola * 0.8f, Mathf.Cos(a) * (rAbajo + ola)));
                }
                for (int i = 0; i < N; i++)
                {
                    int a = o + i * 2, b = a + 1, c = a + 2, d = a + 3;
                    if (lado == 0) t.AddRange(new[] { a, b, c, c, b, d });
                    else t.AddRange(new[] { a, c, b, c, d, b });
                }
            }
            malla = new Mesh { name = "Falda" };
            malla.SetVertices(v); malla.SetTriangles(t, 0);
            malla.RecalculateNormals(); malla.RecalculateBounds();
            mallas[clave] = malla;
        }
        var g = new GameObject("Falda");
        g.transform.SetParent(p, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
