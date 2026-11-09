using System.Collections.Generic;
using UnityEngine;

// Interiores del barco: se puede bajar por escotillas y escaleras.
//  · Nivel 0: la cubierta de arriba (al aire libre).
//  · Niveles 1..n: una cubierta de cañones por cada fila de portas, con los cañones en su cureña.
//  · Último nivel: la bodega (barriles, cajas y sacos), si el casco es lo bastante hondo.
//  · Nivel -1: el camarote del capitán, dentro del castillo de popa (mesa con mapa, cama, cofre).
// Todo se arma la primera vez que alguien baja (así los barcos que nadie visita no cuestan nada)
// y solo se muestra mientras estás a bordo. Adentro la cámara pasa a primera persona.
// El personaje camina en coordenadas del barco (Personaje.local); aquí se limita por dónde puede andar.
public class Interior : MonoBehaviour
{
    public const int Camarote = -1;
    const float Grosor = 0.06f;          // grosor de los pisos
    const float AnchoHueco = 1.2f, LargoHueco = 2.2f;
    const float AlcanceUsar = 1.5f;

    public class Nivel
    {
        public string nombre;
        public float piso, techo;            // y del piso y del techo (local)
        public float zA, zB;                 // hasta dónde se camina, de popa a proa
        public float zBajada = float.NaN;    // hueco con escalera hacia el nivel de abajo
        public float zSubida = float.NaN;    // donde llega la escalera que viene de arriba
        public GameObject luces;
        public readonly List<Rect> solidos = new List<Rect>();   // muebles que no se atraviesan (x, z)
    }

    readonly List<Nivel> niveles = new List<Nivel>();
    float w, h, L;
    bool tieneCamarote;
    float camX, camZ0, camZ1;            // límites caminables del camarote
    float puertaZ;                       // frente del castillo (donde está la puerta)
    GameObject raiz, bajoCubierta, camarote, lucesCamarote;
    Astillero.Materiales m;
    Material oscuro;

    public int Niveles => niveles.Count;
    public string Nombre(int n) => n == Camarote ? "el camarote del capitán" : n >= 0 && n < niveles.Count ? niveles[n].nombre : "";

    // Interior de un barco (se arma la primera vez). null si el barco no tiene (la balsa).
    public static Interior De(ControlBarco cb)
    {
        if (cb == null || Cubierta.EsPlana(cb)) return null;
        Interior it = cb.GetComponent<Interior>();
        if (it == null) { it = cb.gameObject.AddComponent<Interior>(); it.Armar(); }
        return it;
    }

    // ---------- Altura de los ojos y límites ----------
    public float Piso(int n) => n == Camarote ? Cubierta.Altura(h) : niveles[Mathf.Clamp(n, 0, niveles.Count - 1)].piso;
    public float AlturaOjos(int n)
    {
        if (n == Camarote) return 1.6f;
        Nivel nv = niveles[Mathf.Clamp(n, 0, niveles.Count - 1)];
        return Mathf.Clamp(nv.techo - nv.piso - 0.18f, 0.9f, 1.6f);
    }

    float Borde(float z, float piso) => Mathf.Max(0f, CascoProcedural.Costado(z, piso + 0.1f, w, h, L) - 0.08f);

    // Deja el punto dentro de lo caminable del nivel (no cruza paredes, huecos ni escaleras)
    public Vector3 Limitar(int n, Vector3 p)
    {
        if (n == Camarote)
        {
            p.x = Mathf.Clamp(p.x, -camX, camX);
            p.z = Mathf.Clamp(p.z, camZ0, camZ1);
            p.y = Cubierta.Altura(h);
            return p;
        }
        Nivel nv = niveles[n];
        p.z = Mathf.Clamp(p.z, nv.zA + 0.35f, nv.zB - 0.35f);
        float bx = Mathf.Max(0.2f, Borde(p.z, nv.piso) - 0.35f);
        p.x = Mathf.Clamp(p.x, -bx, bx);
        if (n > 0 && !float.IsNaN(nv.zBajada)) p = Fuera(p, nv.zBajada);   // no caerse por el hueco
        if (!float.IsNaN(nv.zSubida)) p = Fuera(p, nv.zSubida);            // la escalera ocupa su sitio
        foreach (Rect r in nv.solidos) p = FueraDe(p, r);                  // fogón, mesa
        p.y = nv.piso;
        return p;
    }

    // Saca el punto de un rectángulo (x, z) por el lado más cercano, dejando el ancho del cuerpo
    static Vector3 FueraDe(Vector3 p, Rect r)
    {
        const float cuerpo = 0.3f;
        float x0 = r.xMin - cuerpo, x1 = r.xMax + cuerpo, z0 = r.yMin - cuerpo, z1 = r.yMax + cuerpo;
        if (p.x <= x0 || p.x >= x1 || p.z <= z0 || p.z >= z1) return p;
        float dI = p.x - x0, dD = x1 - p.x, dA = p.z - z0, dB = z1 - p.z;
        float m = Mathf.Min(Mathf.Min(dI, dD), Mathf.Min(dA, dB));
        if (m == dI) p.x = x0; else if (m == dD) p.x = x1; else if (m == dA) p.z = z0; else p.z = z1;
        return p;
    }

    static Vector3 Fuera(Vector3 p, float zc)
    {
        float ax = AnchoHueco / 2f + 0.25f, az = LargoHueco / 2f + 0.25f;
        float dx = ax - Mathf.Abs(p.x), dz = az - Mathf.Abs(p.z - zc);
        if (dx <= 0f || dz <= 0f) return p;
        if (dx < dz) p.x = Mathf.Sign(p.x == 0f ? 1f : p.x) * ax;
        else p.z = zc + Mathf.Sign(p.z - zc) * az;
        return p;
    }

    // ---------- Usar (F): escaleras y puerta del camarote ----------
    Vector3 PuntoBajar(int n) => new Vector3(0f, niveles[n].piso, niveles[n].zBajada + LargoHueco / 2f + 0.5f);
    Vector3 PuntoSubir(int n) => new Vector3(0f, niveles[n].piso, niveles[n].zSubida - LargoHueco / 2f - 0.5f);
    Vector3 PuertaFuera => new Vector3(0f, Cubierta.Altura(h), puertaZ + 0.6f);
    Vector3 PuertaDentro => new Vector3(0f, Cubierta.Altura(h), puertaZ - 0.6f);

    public bool FrenteAPuerta(Vector3 p) { if (!tieneCamarote) return false; p.y = 0f; Vector3 q = PuertaFuera; q.y = 0f; return Vector3.Distance(p, q) < 0.9f; }

    // Junto al hueco (o encima, en la cubierta de arriba): hasta 1 m alrededor
    bool PuedeBajar(int n, Vector3 p) => n >= 0 && n < niveles.Count - 1 && !float.IsNaN(niveles[n].zBajada)
        && Mathf.Abs(p.x) < AnchoHueco / 2f + 1.1f && Mathf.Abs(p.z - niveles[n].zBajada) < LargoHueco / 2f + 1.1f;
    // Al pie de la escalera que sube (o a sus lados)
    bool PuedeSubir(int n, Vector3 p) => n > 0 && !float.IsNaN(niveles[n].zSubida)
        && Mathf.Abs(p.x) < AnchoHueco / 2f + 1.1f && Mathf.Abs(p.z - niveles[n].zSubida) < LargoHueco / 2f + 1.3f;

    static bool Cerca(Vector3 a, Vector3 b) { a.y = b.y = 0f; return Vector3.Distance(a, b) < AlcanceUsar; }

    // Qué haría F aquí (para la ayuda en pantalla); null = nada
    public string Ayuda(int n, Vector3 p)
    {
        if (n == Camarote) return Cerca(p, PuertaDentro) ? "salir a cubierta" : null;
        if (n == 0 && FrenteAPuerta(p)) return "entrar al camarote del capitán";
        if (PuedeBajar(n, p)) return "bajar a " + niveles[n + 1].nombre;
        if (PuedeSubir(n, p)) return "subir a " + niveles[n - 1].nombre;
        return null;
    }

    // Hace lo que diga Ayuda. Devuelve true si cambió de sitio.
    public bool Usar(ref int n, ref Vector3 p)
    {
        if (n == Camarote) { if (Cerca(p, PuertaDentro)) { n = 0; p = PuertaFuera; return true; } return false; }
        if (n == 0 && FrenteAPuerta(p)) { n = Camarote; p = PuertaDentro; return true; }
        if (PuedeBajar(n, p)) { n++; p = PuntoSubir(n); return true; }
        if (PuedeSubir(n, p)) { n--; p = PuntoBajar(n); return true; }
        return false;
    }

    // ---------- Mostrar solo mientras estás a bordo; luces solo donde estás ----------
    void Update()
    {
        Personaje pj = Personaje.Instancia;
        bool aBordo = pj != null && pj.estado == Personaje.Estado.Cubierta && pj.BarcoActual != null && pj.BarcoActual.gameObject == gameObject;
        int n = aBordo ? pj.Nivel : 0;
        // En cubierta no se muestra nada de adentro (la cámara en tercera persona puede quedar dentro del castillo)
        bool abajo = aBordo && n > 0, enCamarote = aBordo && n == Camarote;
        if (raiz != null && raiz.activeSelf != (abajo || enCamarote)) raiz.SetActive(abajo || enCamarote);
        if (bajoCubierta != null && bajoCubierta.activeSelf != abajo) bajoCubierta.SetActive(abajo);
        if (camarote != null && camarote.activeSelf != enCamarote) camarote.SetActive(enCamarote);
        for (int i = 1; i < niveles.Count; i++)
            if (niveles[i].luces != null) niveles[i].luces.SetActive(aBordo && Mathf.Abs(n - i) <= 1 && n != 0 && n != Camarote);
        if (lucesCamarote != null) lucesCamarote.SetActive(aBordo && n == Camarote);
    }

    // ---------- Construcción ----------
    void Armar()
    {
        Cubierta.Medidas(GetComponent<BoxCollider>(), out w, out h, out L);
        m.casco = Mat("Casco"); m.cubierta = Mat("Cubierta"); m.mastil = Mat("Mástil");
        m.canon = Mat("Tubo"); m.vela = Mat("Lona"); m.bandera = Mat("Bandera");
        if (m.cubierta == null) m.cubierta = m.casco;
        if (m.mastil == null) m.mastil = m.cubierta;
        if (m.canon == null) m.canon = m.mastil;
        if (m.vela == null) m.vela = m.cubierta;
        oscuro = Modelos.DeColor(m.canon, new Color(0.05f, 0.04f, 0.03f));

        raiz = new GameObject("Interior");
        raiz.transform.SetParent(transform, false);
        bajoCubierta = new GameObject("Bajo cubierta");
        bajoCubierta.transform.SetParent(raiz.transform, false);

        CalcularNiveles();
        ElegirEscaleras();
        foreach (Transform hijo in transform) if (hijo.name.StartsWith("Cañón babor") || hijo.name.StartsWith("Cañón estribor")) Cureña(hijo);
        for (int i = 1; i < niveles.Count; i++) ArmarNivel(i);
        Escotilla();
        for (int i = 0; i < niveles.Count - 1; i++) Escalera(i);
        Mastiles();
        ArmarCamarote();
        Rendimiento.Capa(raiz, Rendimiento.CapaBarcoDetalle);
        raiz.SetActive(false);
    }

    Material Mat(string pieza)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.name == pieza && r.GetComponentInParent<Tripulante>() == null) return r.sharedMaterial;
        return null;
    }

    void CalcularNiveles()
    {
        niveles.Add(new Nivel { nombre = "la cubierta", piso = Cubierta.Altura(h), techo = 99f, zA = -0.5f * L, zB = 0.3f * L - 0.6f * w });
        // Una cubierta de cañones por fila de portas (los cañones del casco cuelgan a la altura de la fila)
        var filas = new List<float>();
        foreach (Transform hijo in transform)
        {
            if (!hijo.name.StartsWith("Cañón babor")) continue;
            float y = hijo.localPosition.y;
            if (y >= h / 2f) continue;   // los de cubierta no cuentan
            bool nueva = true;
            foreach (float f in filas) if (Mathf.Abs(f - y) < 0.05f) nueva = false;
            if (nueva) filas.Add(y);
        }
        filas.Sort((a, b) => b.CompareTo(a));
        float techo = h / 2f + 0.04f;
        int k = 1;
        foreach (float y in filas)
        {
            float piso = y - 0.53f;
            Nivel nv = new Nivel { nombre = filas.Count == 1 ? "la cubierta de cañones" : "la cubierta de cañones " + k, piso = piso, techo = techo };
            if (!Rango(nv, 0.9f)) break;
            niveles.Add(nv);
            techo = piso - Grosor;
            k++;
        }
        // Bodega: abajo de todo, si el casco es hondo
        float abajo = niveles.Count > 1 ? niveles[niveles.Count - 1].piso - Grosor : h / 2f + 0.04f;
        float pisoB = Mathf.Max(abajo - 2.2f, -h / 2f + 0.35f);
        if (abajo - pisoB >= 1.6f)
        {
            Nivel b = new Nivel { nombre = "la bodega", piso = pisoB, techo = abajo };
            if (Rango(b, 0.7f)) niveles.Add(b);
        }
    }

    // Hasta dónde se camina en un nivel: donde el casco es lo bastante ancho a esa altura
    bool Rango(Nivel nv, float minimo)
    {
        float zA = float.NaN, zB = float.NaN;
        for (float z = -0.5f * L; z <= CascoProcedural.ZProa(w, L); z += 0.25f)
            if (Borde(z, nv.piso) >= minimo) { if (float.IsNaN(zA)) zA = z; zB = z; }
        if (float.IsNaN(zA) || zB - zA < 4f) return false;
        nv.zA = zA + 0.1f; nv.zB = zB;
        return true;
    }

    // Una escalera entre cada par de niveles, sin chocar con los mástiles ni con la escalera de arriba
    void ElegirEscaleras()
    {
        var mastiles = new List<float>();
        foreach (Transform hijo in transform) if (hijo.name == "Mástil" || hijo.name == "cabrestante") mastiles.Add(hijo.localPosition.z);
        float frenteCastillo = Cubierta.CentroCamarote(L) + Cubierta.LargoCamarote(L) / 2f;
        for (int i = 0; i < niveles.Count - 1; i++)
        {
            Nivel a = niveles[i], b = niveles[i + 1];
            float z0 = Mathf.Max(a.zA, b.zA) + 2.2f, z1 = Mathf.Min(a.zB, b.zB) - 2.2f;
            if (i == 0) { z0 = Mathf.Max(z0, frenteCastillo + 4f); z1 = Mathf.Min(z1, 0.3f * L - 0.6f * w); }
            float elegido = float.NaN;
            float centro = (z0 + z1) / 2f;
            for (float d = 0f; d <= (z1 - z0) / 2f + 0.01f && float.IsNaN(elegido); d += 0.5f)
                foreach (float z in new[] { centro - d, centro + d })
                {
                    if (z < z0 || z > z1) continue;
                    bool libre = true;
                    foreach (float mz in mastiles) if (Mathf.Abs(mz - z) < 1.9f) libre = false;
                    if (!float.IsNaN(a.zSubida) && Mathf.Abs(a.zSubida - z) < LargoHueco + 1.6f) libre = false;
                    if (libre) { elegido = z; break; }
                }
            if (float.IsNaN(elegido)) { niveles.RemoveRange(i + 1, niveles.Count - i - 1); break; }
            a.zBajada = elegido;
            b.zSubida = elegido;
        }
    }

    // ---------- Piezas ----------
    GameObject Malla(string nombre, List<Vector3> v, List<Vector2> uv, Material mat, Transform padre = null)
    {
        if (v.Count == 0) return null;
        GameObject g = Astillero.Malla(nombre, padre != null ? padre : raiz.transform, CascoProcedural.MallaDe(nombre, v, uv), mat);
        g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    // Cureña con ruedas debajo de cada cañón del casco (se ve desde adentro; se esconde si no hay cañón)
    void Cureña(Transform pivote)
    {
        if (pivote.localPosition.y >= h / 2f || !Modelos.Existe("canon")) return;
        GameObject c = Modelos.Crear("canon", pivote, new Vector3(0f, 0f, 0.25f), Quaternion.identity, 1f, _ => m.mastil, "Cureña", "Ruedas");
        c.name = "Cureña interior";
        Rendimiento.Capa(c, Rendimiento.CapaBarcoDetalle);
    }

    // Piso, paredes con las portas abiertas, mamparos de proa y popa, luces y carga de un nivel
    void ArmarNivel(int i)
    {
        Nivel nv = niveles[i];
        Transform padre = new GameObject(nv.nombre).transform;
        padre.SetParent(bajoCubierta.transform, false);

        // Puntos a lo largo del barco (incluye los bordes de los huecos y de las portas)
        var zs = new List<float>();
        for (float z = nv.zA; z < nv.zB; z += 1f) zs.Add(z);
        zs.Add(nv.zB);
        var huecos = new List<float>();
        if (!float.IsNaN(nv.zBajada)) huecos.Add(nv.zBajada);
        foreach (float zh in huecos) { zs.Add(zh - LargoHueco / 2f); zs.Add(zh + LargoHueco / 2f); }
        // Portas de esta fila
        var portas = new List<float>();
        float yFila = nv.piso + 0.53f;
        foreach (Transform hijo in transform)
            if (hijo.name.StartsWith("Cañón babor") && Mathf.Abs(hijo.localPosition.y - yFila) < 0.05f) portas.Add(hijo.localPosition.z);
        float ap = CascoProcedural.AnchoPorta / 2f, alp = CascoProcedural.AltoPorta / 2f;
        foreach (float zp in portas) { zs.Add(zp - ap); zs.Add(zp + ap); }
        zs.Sort();
        for (int k = zs.Count - 1; k > 0; k--) if (zs[k] - zs[k - 1] < 0.02f || zs[k] < nv.zA || zs[k] > nv.zB) zs.RemoveAt(k);

        var vp = new List<Vector3>(); var up = new List<Vector2>();   // piso (arriba) y techo del nivel de abajo
        var vw = new List<Vector3>(); var uw = new List<Vector2>();   // paredes
        float yT = nv.techo, yP = nv.piso;
        for (int k = 0; k < zs.Count - 1; k++)
        {
            float z0 = zs[k], z1 = zs[k + 1], zm = (z0 + z1) / 2f;
            float x0 = Mathf.Max(0.3f, Borde(z0, yP)), x1 = Mathf.Max(0.3f, Borde(z1, yP));
            bool enHueco = false;
            foreach (float zh in huecos) if (zm > zh - LargoHueco / 2f && zm < zh + LargoHueco / 2f) enHueco = true;
            // Piso: dos caras (arriba se camina; abajo es el techo del nivel siguiente)
            foreach (float y in new[] { yP, yP - Grosor })
            {
                Vector3 n = y == yP ? Vector3.up : Vector3.down;
                if (enHueco)
                {
                    float a = AnchoHueco / 2f;
                    CascoProcedural.Cara(vp, up, new Vector3(-x0, y, z0), new Vector3(-a, y, z0), new Vector3(-a, y, z1), new Vector3(-x1, y, z1), n);
                    CascoProcedural.Cara(vp, up, new Vector3(a, y, z0), new Vector3(x0, y, z0), new Vector3(x1, y, z1), new Vector3(a, y, z1), n);
                }
                else CascoProcedural.Cara(vp, up, new Vector3(-x0, y, z0), new Vector3(x0, y, z0), new Vector3(x1, y, z1), new Vector3(-x1, y, z1), n);
            }
            // Paredes de los costados, con el hueco de cada porta
            bool porta = false;
            foreach (float zp in portas) if (zm > zp - ap && zm < zp + ap) porta = true;
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 adentro = new Vector3(-s, 0f, 0f);
                void Panel(float ya, float yb)
                {
                    if (yb - ya < 0.01f) return;
                    CascoProcedural.Cara(vw, uw, new Vector3(s * x0, ya, z0), new Vector3(s * x1, ya, z1), new Vector3(s * x1, yb, z1), new Vector3(s * x0, yb, z0), adentro);
                }
                if (porta) { Panel(yP, yFila - alp); Panel(yFila + alp, yT); }
                else Panel(yP, yT);
            }
        }
        // Mamparos de popa y proa
        foreach (var (z, n) in new[] { (nv.zA, Vector3.forward), (nv.zB, Vector3.back) })
        {
            float x = Mathf.Max(0.3f, Borde(z, yP));
            CascoProcedural.Cara(vw, uw, new Vector3(-x, yP, z), new Vector3(x, yP, z), new Vector3(x, yT, z), new Vector3(-x, yT, z), n);
        }
        // Techo del primer nivel: el revés de la cubierta, con el hueco de la escotilla
        if (i == 1) TechoCubierta(nv, padre);

        Malla("Piso", vp, up, m.cubierta, padre);
        Malla("Forro", vw, uw, m.cubierta, padre);
        Vigas(nv, padre);
        Luces(nv, padre);
        if (nv.nombre == "la bodega") Carga(nv, padre);
        else Balas(nv, portas, padre);
        if (i == 1) Tripulacion(nv, padre);   // la primera cubierta bajo la de arriba: cocina y hamacas en la proa
    }

    // El revés de la cubierta de arriba (si no, desde abajo se vería el cielo), con el hueco de la escotilla
    void TechoCubierta(Nivel nv, Transform padre)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float y = h / 2f + 0.04f, zh = niveles[0].zBajada;
        var zs = new List<float>();
        for (float z = -0.5f * L; z < CascoProcedural.ZProa(w, L); z += 1f) zs.Add(z);
        zs.Add(CascoProcedural.ZProa(w, L));
        if (!float.IsNaN(zh)) { zs.Add(zh - LargoHueco / 2f); zs.Add(zh + LargoHueco / 2f); }
        zs.Sort();
        for (int k = 0; k < zs.Count - 1; k++)
        {
            float z0 = zs[k], z1 = zs[k + 1], zm = (z0 + z1) / 2f;
            if (z1 - z0 < 0.02f) continue;
            float x0 = CascoProcedural.Costado(z0, h / 2f, w, h, L), x1 = CascoProcedural.Costado(z1, h / 2f, w, h, L);
            if (!float.IsNaN(zh) && zm > zh - LargoHueco / 2f && zm < zh + LargoHueco / 2f)
            {
                float a = AnchoHueco / 2f;
                CascoProcedural.Cara(v, uv, new Vector3(-x0, y, z0), new Vector3(-a, y, z0), new Vector3(-a, y, z1), new Vector3(-x1, y, z1), Vector3.down);
                CascoProcedural.Cara(v, uv, new Vector3(a, y, z0), new Vector3(x0, y, z0), new Vector3(x1, y, z1), new Vector3(a, y, z1), Vector3.down);
            }
            else CascoProcedural.Cara(v, uv, new Vector3(-x0, y, z0), new Vector3(x0, y, z0), new Vector3(x1, y, z1), new Vector3(-x1, y, z1), Vector3.down);
        }
        Malla("Techo", v, uv, m.cubierta, padre);
    }

    // Vigas de lado a lado bajo el techo, cada 1,5 m
    void Vigas(Nivel nv, Transform padre)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        for (float z = nv.zA + 0.75f; z < nv.zB - 0.5f; z += 1.5f)
        {
            if (!float.IsNaN(nv.zSubida) && Mathf.Abs(z - nv.zSubida) < LargoHueco / 2f + 0.2f) continue;
            float x = Mathf.Max(0.3f, Borde(z, nv.piso));
            CascoProcedural.Caja(v, uv, new Vector3(0f, nv.techo - 0.09f, z), new Vector3(2f * x, 0.18f, 0.16f), Quaternion.identity);
        }
        Malla("Vigas", v, uv, m.mastil, padre);
    }

    // Faroles colgados y luces cálidas (solo se encienden en el nivel donde estás y los de al lado)
    void Luces(Nivel nv, Transform padre)
    {
        nv.luces = new GameObject("Luces");
        nv.luces.transform.SetParent(padre, false);
        int n = Mathf.Clamp(Mathf.RoundToInt((nv.zB - nv.zA) / 10f), 1, 4);
        for (int k = 0; k < n; k++)
        {
            float z = Mathf.Lerp(nv.zA, nv.zB, (k + 0.5f) / n);
            Vector3 p = new Vector3(0f, nv.techo - 0.45f, z);
            if (Modelos.Existe("farol"))
                Modelos.Crear("farol", padre, p + Vector3.down * 0.25f, Quaternion.identity, 0.8f,
                    nm => nm == "Vidrio" ? Modelos.DeColor(m.vela, new Color(1f, 0.78f, 0.35f), true) : m.canon);
            GameObject gl = new GameObject("Luz");
            gl.transform.SetParent(nv.luces.transform, false);
            gl.transform.localPosition = p;
            Light l = gl.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.72f, 0.42f); l.range = 9f; l.intensity = 2.2f;
            l.shadows = LightShadows.None;
        }
        nv.luces.SetActive(false);
    }

    // Pilas de balas junto a algunos cañones
    void Balas(Nivel nv, List<float> portas, Transform padre)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Mesh esfera = Primitiva(PrimitiveType.Sphere);
        for (int k = 0; k < portas.Count; k += 2)
            foreach (float s in new[] { -1f, 1f })
            {
                float zp = portas[k] + (k + 1 < portas.Count ? (portas[k + 1] - portas[k]) / 2f : 1f);
                if (zp < nv.zA + 0.5f || zp > nv.zB - 0.5f) continue;
                float x = s * (Borde(zp, nv.piso) - 0.45f);
                Vector3 b = new Vector3(x, nv.piso, zp);
                foreach (Vector3 o in new[] { new Vector3(-0.11f, 0.1f, -0.11f), new Vector3(0.11f, 0.1f, -0.11f), new Vector3(-0.11f, 0.1f, 0.11f), new Vector3(0.11f, 0.1f, 0.11f), new Vector3(0f, 0.27f, 0f) })
                    Agregar(v, uv, esfera, b + o, Vector3.one * 0.2f, Quaternion.identity);
            }
        Malla("Balas", v, uv, m.canon, padre);
    }

    // Cocina (fogón de ladrillo con olla y chimenea que sube a la cubierta) y camarote de la tripulación (hamacas colgadas
    // de las vigas a los costados), en la parte de proa del primer nivel bajo cubierta. Solo si el barco tiene sitio.
    void Tripulacion(Nivel nv, Transform padre)
    {
        float largo = nv.zB - nv.zA;
        if (largo < 7f) return;
        bool Libre(float z, float margen) =>
            (float.IsNaN(nv.zBajada) || Mathf.Abs(z - nv.zBajada) > LargoHueco / 2f + margen) &&
            (float.IsNaN(nv.zSubida) || Mathf.Abs(z - nv.zSubida) > LargoHueco / 2f + margen);

        var vl = new List<Vector3>(); var ul = new List<Vector2>();   // ladrillo
        var vh = new List<Vector3>(); var uh = new List<Vector2>();   // hierro (olla, chimenea)
        var vt = new List<Vector3>(); var ut = new List<Vector2>();   // tela (hamacas)
        var vc = new List<Vector3>(); var uc = new List<Vector2>();   // cuerda y madera

        // Fogón: lo más a proa posible donde quepa de lado a lado
        float zf = float.NaN;
        for (float z = nv.zB - 1.1f; z > nv.zA + largo * 0.45f; z -= 0.25f)
            if (Borde(z, nv.piso) > 1.4f && Libre(z, 0.9f)) { zf = z; break; }
        if (!float.IsNaN(zf))
        {
            float alto = Mathf.Min(0.95f, nv.techo - nv.piso - 0.6f);
            CascoProcedural.Caja(vl, ul, new Vector3(0f, nv.piso + alto / 2f, zf), new Vector3(1.2f, alto, 0.95f), Quaternion.identity);
            nv.solidos.Add(new Rect(-0.6f, zf - 0.48f, 1.2f, 1.0f));
            // Boca del fuego (oscura) y brasas
            CascoProcedural.Caja(vh, uh, new Vector3(0f, nv.piso + alto * 0.35f, zf - 0.48f), new Vector3(0.55f, alto * 0.4f, 0.02f), Quaternion.identity);
            // Olla de hierro encima y un cucharón
            Mesh esfera = Primitiva(PrimitiveType.Sphere);
            Agregar(vh, uh, esfera, new Vector3(-0.22f, nv.piso + alto + 0.17f, zf), new Vector3(0.45f, 0.36f, 0.45f), Quaternion.identity);
            CascoProcedural.Caja(vh, uh, new Vector3(0.3f, nv.piso + alto + 0.05f, zf + 0.1f), new Vector3(0.32f, 0.1f, 0.32f), Quaternion.identity);
            CascoProcedural.Caja(vc, uc, new Vector3(-0.05f, nv.piso + alto + 0.32f, zf), new Vector3(0.5f, 0.03f, 0.03f), Quaternion.Euler(0f, 0f, -25f));
            // Chimenea hasta el techo
            float yc0 = nv.piso + alto, yc1 = nv.techo + 0.05f;
            CascoProcedural.Caja(vh, uh, new Vector3(0.35f, (yc0 + yc1) / 2f, zf + 0.25f), new Vector3(0.22f, yc1 - yc0, 0.22f), Quaternion.identity);
            // Mesa y bancos de la tripulación detrás del fogón
            float zm = zf - 1.7f;
            if (Libre(zm, 0.7f) && Borde(zm, nv.piso) > 1.4f)
            {
                CascoProcedural.Caja(vc, uc, new Vector3(0f, nv.piso + 0.72f, zm), new Vector3(0.8f, 0.06f, 1.6f), Quaternion.identity);
                nv.solidos.Add(new Rect(-0.8f, zm - 0.8f, 1.6f, 1.6f));
                foreach (float dz in new[] { -0.6f, 0.6f }) CascoProcedural.Caja(vc, uc, new Vector3(0f, nv.piso + 0.36f, zm + dz), new Vector3(0.1f, 0.72f, 0.1f), Quaternion.identity);
                foreach (float sx in new[] { -1f, 1f }) CascoProcedural.Caja(vc, uc, new Vector3(sx * 0.65f, nv.piso + 0.42f, zm), new Vector3(0.3f, 0.06f, 1.5f), Quaternion.identity);
            }
        }

        // Hamacas a los costados, colgadas de las vigas, en la mitad de proa
        int hamacas = 0;
        for (float z = nv.zB - 0.8f; z > nv.zA + largo * 0.4f && hamacas < 16; z -= 0.75f)
            foreach (float s in new[] { -1f, 1f })
            {
                float borde = Borde(z, nv.techo - 0.3f);
                if (borde < 1.6f) continue;
                float xa = s * (borde - 0.25f), xb = s * Mathf.Max(0.55f, borde - 1.85f);
                float yt = nv.techo - 0.2f, yb = nv.techo - 0.55f;
                // La tela: dos tramos que bajan al centro (comba)
                Vector3 a = new Vector3(xa, yt, z), b = new Vector3(xb, yt, z), c = (a + b) / 2f + Vector3.down * (yt - yb);
                foreach (var (p0, p1) in new[] { (a, c), (c, b) })
                {
                    Vector3 d = p1 - p0;
                    CascoProcedural.Caja(vt, ut, (p0 + p1) / 2f, new Vector3(0.55f, 0.04f, d.magnitude), Quaternion.LookRotation(d.normalized, Vector3.up));
                }
                // Cuerdas a la viga
                CascoProcedural.Caja(vc, uc, a + Vector3.up * 0.1f, new Vector3(0.03f, 0.2f, 0.03f), Quaternion.identity);
                CascoProcedural.Caja(vc, uc, b + Vector3.up * 0.1f, new Vector3(0.03f, 0.2f, 0.03f), Quaternion.identity);
                hamacas++;
            }

        Malla("Fogón", vl, ul, Modelos.DeColor(m.vela, new Color(0.55f, 0.27f, 0.18f)), padre);
        Malla("Olla y chimenea", vh, uh, m.canon, padre);
        Malla("Hamacas", vt, ut, Modelos.DeColor(m.vela, new Color(0.78f, 0.72f, 0.58f)), padre);
        Malla("Muebles tripulación", vc, uc, m.mastil, padre);
    }

    // Bodega: barriles, cajas y sacos contra los costados
    void Carga(Nivel nv, Transform padre)
    {
        var vc = new List<Vector3>(); var uc = new List<Vector2>();
        var vs = new List<Vector3>(); var us = new List<Vector2>();
        Mesh esfera = Primitiva(PrimitiveType.Sphere);
        var rnd = new System.Random(Mathf.RoundToInt(L * 100f + w * 10f));
        int barriles = 0;
        for (float z = nv.zA + 0.8f; z < nv.zB - 0.8f; z += 1.1f)
        {
            if (!float.IsNaN(nv.zSubida) && Mathf.Abs(z - nv.zSubida) < LargoHueco / 2f + 0.9f) continue;
            foreach (float s in new[] { -1f, 1f })
            {
                float borde = Borde(z, nv.piso);
                if (borde < 1.1f) continue;
                float x = s * (borde - 0.5f);
                int tipo = rnd.Next(4);
                if (tipo <= 1 && Modelos.Existe("barril") && barriles < 40)
                {
                    barriles++;
                    Modelos.Crear("barril", padre, new Vector3(x, nv.piso, z), Quaternion.Euler(0f, rnd.Next(360), 0f), 1f, nm => nm == "Aros" ? m.canon : m.mastil);
                }
                else if (tipo == 2)
                {
                    float t = 0.6f + (float)rnd.NextDouble() * 0.25f;
                    CascoProcedural.Caja(vc, uc, new Vector3(x, nv.piso + t / 2f, z), new Vector3(t, t, t), Quaternion.Euler(0f, rnd.Next(-12, 12), 0f));
                    if (rnd.Next(2) == 0) CascoProcedural.Caja(vc, uc, new Vector3(x, nv.piso + t + 0.25f, z), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, rnd.Next(-30, 30), 0f));
                }
                else
                {
                    Agregar(vs, us, esfera, new Vector3(x, nv.piso + 0.22f, z - 0.2f), new Vector3(0.6f, 0.45f, 0.45f), Quaternion.Euler(0f, 20f, 0f));
                    Agregar(vs, us, esfera, new Vector3(x, nv.piso + 0.22f, z + 0.25f), new Vector3(0.55f, 0.42f, 0.45f), Quaternion.Euler(0f, -15f, 0f));
                }
            }
        }
        Malla("Cajas", vc, uc, m.mastil, padre);
        Malla("Sacos", vs, us, Modelos.DeColor(m.vela, new Color(0.62f, 0.52f, 0.36f)), padre);
    }

    // Esfera de pocas caras (radio 0.5), para balas y sacos
    static Mesh esferaBaja;
    static Mesh Primitiva(PrimitiveType t)
    {
        if (esferaBaja != null) return esferaBaja;
        const int seg = 8, ani = 6;
        var vs = new List<Vector3>(); var tr = new List<int>();
        for (int j = 0; j <= ani; j++)
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * j / ani, b = 2f * Mathf.PI * i / seg;
                vs.Add(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)) * 0.5f);
            }
        for (int j = 0; j < ani; j++)
            for (int i = 0; i < seg; i++)
            {
                int k = j * (seg + 1) + i;
                tr.Add(k); tr.Add(k + 1); tr.Add(k + seg + 1);
                tr.Add(k + 1); tr.Add(k + seg + 2); tr.Add(k + seg + 1);
            }
        esferaBaja = new Mesh { name = "Esfera baja" };
        esferaBaja.SetVertices(vs); esferaBaja.SetTriangles(tr, 0);
        return esferaBaja;
    }

    static void Agregar(List<Vector3> v, List<Vector2> uv, Mesh me, Vector3 pos, Vector3 esc, Quaternion rot)
    {
        Vector3[] mv = me.vertices; int[] mt = me.triangles;
        for (int i = 0; i < mt.Length; i++)
        {
            v.Add(pos + rot * Vector3.Scale(mv[mt[i]], esc));
            uv.Add(Vector2.zero);
        }
    }

    // Escotilla en la cubierta de arriba: brazola (el marco alto) y el hueco oscuro
    void Escotilla()
    {
        if (niveles.Count < 2) return;
        float zh = niveles[0].zBajada, y = Cubierta.Altura(h);
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float a = AnchoHueco / 2f, b = LargoHueco / 2f, g = 0.12f, alto = 0.28f;
        CascoProcedural.Caja(v, uv, new Vector3(-a - g / 2f, y + alto / 2f, zh), new Vector3(g, alto, LargoHueco + 2f * g), Quaternion.identity);
        CascoProcedural.Caja(v, uv, new Vector3(a + g / 2f, y + alto / 2f, zh), new Vector3(g, alto, LargoHueco + 2f * g), Quaternion.identity);
        CascoProcedural.Caja(v, uv, new Vector3(0f, y + alto / 2f, zh - b - g / 2f), new Vector3(AnchoHueco, alto, g), Quaternion.identity);
        CascoProcedural.Caja(v, uv, new Vector3(0f, y + alto / 2f, zh + b + g / 2f), new Vector3(AnchoHueco, alto, g), Quaternion.identity);
        // La escotilla se ve desde afuera aunque no estés a bordo, así que va colgada del barco, no del interior
        GameObject marco = Astillero.Malla("Escotilla", transform, CascoProcedural.MallaDe("Escotilla", v, uv), m.mastil);
        // Hueco oscuro: desde arriba se ve oscuro; estando abajo se apaga para ver el cielo por la escotilla
        var vh = new List<Vector3>(); var uh = new List<Vector2>();
        CascoProcedural.Cara(vh, uh, new Vector3(-a, y + 0.02f, zh - b), new Vector3(a, y + 0.02f, zh - b), new Vector3(a, y + 0.02f, zh + b), new Vector3(-a, y + 0.02f, zh + b), Vector3.up);
        GameObject tapa = Astillero.Malla("Escotilla tapa", transform, CascoProcedural.MallaDe("Hueco", vh, uh), oscuro);
        tapaEscotilla = tapa;
        Rendimiento.Capa(marco, Rendimiento.CapaBarcoDetalle);
        Rendimiento.Capa(tapa, Rendimiento.CapaBarcoDetalle);
    }
    GameObject tapaEscotilla;

    void LateUpdate()
    {
        if (tapaEscotilla != null)
        {
            Personaje pj = Personaje.Instancia;
            bool abajo = pj != null && pj.Bajo && pj.BarcoActual != null && pj.BarcoActual.gameObject == gameObject;
            if (tapaEscotilla.activeSelf == abajo) tapaEscotilla.SetActive(!abajo);
        }
    }

    // Escalera del nivel i al de abajo: peldaños de arriba (adelante) hacia abajo (atrás)
    void Escalera(int i)
    {
        Nivel a = niveles[i], b = niveles[i + 1];
        float zh = a.zBajada, arriba = a.piso, abajo = b.piso;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        int pasos = Mathf.Max(3, Mathf.RoundToInt((arriba - abajo) / 0.24f));
        float largo = LargoHueco - 0.2f;
        for (int k = 1; k < pasos; k++)
        {
            float t = (float)k / pasos;
            float y = Mathf.Lerp(arriba, abajo, t), z = zh + largo / 2f - largo * t;
            CascoProcedural.Caja(v, uv, new Vector3(0f, y - 0.03f, z), new Vector3(AnchoHueco - 0.3f, 0.05f, largo / pasos + 0.08f), Quaternion.identity);
        }
        // Zancas (las tablas de los lados) inclinadas
        float ang = Mathf.Atan2(arriba - abajo, largo) * Mathf.Rad2Deg;
        float largoZanca = Mathf.Sqrt(largo * largo + (arriba - abajo) * (arriba - abajo));
        foreach (float s in new[] { -1f, 1f })
            CascoProcedural.Caja(v, uv, new Vector3(s * (AnchoHueco / 2f - 0.12f), (arriba + abajo) / 2f, zh), new Vector3(0.07f, 0.22f, largoZanca), Quaternion.Euler(-ang, 0f, 0f));
        // Pasamanos
        foreach (float s in new[] { -1f, 1f })
            CascoProcedural.Caja(v, uv, new Vector3(s * (AnchoHueco / 2f - 0.08f), (arriba + abajo) / 2f + 0.9f, zh), new Vector3(0.05f, 0.05f, largoZanca), Quaternion.Euler(-ang, 0f, 0f));
        // En los niveles de abajo, baranda alrededor del hueco
        if (i > 0)
            foreach (float s in new[] { -1f, 1f })
                CascoProcedural.Caja(v, uv, new Vector3(s * (AnchoHueco / 2f + 0.05f), arriba + 0.5f, zh), new Vector3(0.06f, 0.06f, LargoHueco), Quaternion.identity);
        Malla("Escalera " + (i + 1), v, uv, m.mastil, bajoCubierta.transform);
    }

    // Los mástiles bajan hasta el fondo (atraviesan todos los niveles)
    void Mastiles()
    {
        if (niveles.Count < 2) return;
        float fondo = niveles[niveles.Count - 1].piso;
        foreach (Transform hijo in transform)
        {
            if (hijo.name != "Mástil") continue;
            float g = hijo.localScale.x, z = hijo.localPosition.z;
            float alto = h / 2f - fondo;
            Astillero.Pieza(PrimitiveType.Cylinder, "Mástil interior", bajoCubierta.transform, new Vector3(0f, fondo + alto / 2f, z), Quaternion.identity, new Vector3(g * 1.1f, alto / 2f, g * 1.1f), m.mastil);
        }
    }

    // ---------- Camarote del capitán (dentro del castillo de popa) ----------
    void ArmarCamarote()
    {
        Transform castillo = transform.Find("Camarote");
        if (castillo == null || Cubierta.AltoCamaroteDe(h) < 2f || castillo.localScale.x < 2.2f) return;   // en barcos chicos no cabe un camarote
        tieneCamarote = true;
        float zc = Cubierta.CentroCamarote(L), lc = Cubierta.LargoCamarote(L);
        float ancho = castillo.localScale.x, piso = Cubierta.Altura(h), techo = h / 2f + Cubierta.AltoCamaroteDe(h) - 0.05f;
        float xi = ancho / 2f - 0.08f, z0 = zc - lc / 2f + 0.08f, z1 = zc + lc / 2f - 0.08f;
        puertaZ = zc + lc / 2f;
        camX = Mathf.Max(0.3f, xi - 0.4f); camZ0 = z0 + 0.4f; camZ1 = z1 - 0.35f;

        Transform padre = new GameObject("Camarote del capitán").transform;
        padre.SetParent(raiz.transform, false);
        camarote = padre.gameObject;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        // Paredes, techo y piso mirando hacia adentro
        CascoProcedural.Cara(v, uv, new Vector3(-xi, piso, z0), new Vector3(xi, piso, z0), new Vector3(xi, piso, z1), new Vector3(-xi, piso, z1), Vector3.up);
        CascoProcedural.Cara(v, uv, new Vector3(-xi, techo, z0), new Vector3(xi, techo, z0), new Vector3(xi, techo, z1), new Vector3(-xi, techo, z1), Vector3.down);
        foreach (float s in new[] { -1f, 1f })
            CascoProcedural.Cara(v, uv, new Vector3(s * xi, piso, z0), new Vector3(s * xi, piso, z1), new Vector3(s * xi, techo, z1), new Vector3(s * xi, techo, z0), new Vector3(-s, 0f, 0f));
        // Pared de atrás con las ventanas (huecos donde están las ventanas de afuera)
        float yv = h / 2f + Cubierta.AltoCamaroteDe(h) * 0.55f;
        var ventanas = new List<float>();
        foreach (Transform hijo in transform) if (hijo.name == "Ventana") ventanas.Add(hijo.localPosition.x);
        ventanas.Sort();
        var xs = new List<float> { -xi };
        foreach (float x in ventanas) { xs.Add(x - 0.3f); xs.Add(x + 0.3f); }
        xs.Add(xi);
        for (int k = 0; k < xs.Count - 1; k++)
        {
            float xa = xs[k], xb = xs[k + 1];
            bool hueco = k % 2 == 1;
            if (hueco)
            {
                CascoProcedural.Cara(v, uv, new Vector3(xa, piso, z0), new Vector3(xb, piso, z0), new Vector3(xb, yv - 0.38f, z0), new Vector3(xa, yv - 0.38f, z0), Vector3.forward);
                CascoProcedural.Cara(v, uv, new Vector3(xa, yv + 0.38f, z0), new Vector3(xb, yv + 0.38f, z0), new Vector3(xb, techo, z0), new Vector3(xa, techo, z0), Vector3.forward);
            }
            else CascoProcedural.Cara(v, uv, new Vector3(xa, piso, z0), new Vector3(xb, piso, z0), new Vector3(xb, techo, z0), new Vector3(xa, techo, z0), Vector3.forward);
        }
        // Pared del frente con la puerta
        float pa = 0.5f, pAlto = piso + 2.0f;
        CascoProcedural.Cara(v, uv, new Vector3(-xi, piso, z1), new Vector3(-pa, piso, z1), new Vector3(-pa, techo, z1), new Vector3(-xi, techo, z1), Vector3.back);
        CascoProcedural.Cara(v, uv, new Vector3(pa, piso, z1), new Vector3(xi, piso, z1), new Vector3(xi, techo, z1), new Vector3(pa, techo, z1), Vector3.back);
        CascoProcedural.Cara(v, uv, new Vector3(-pa, pAlto, z1), new Vector3(pa, pAlto, z1), new Vector3(pa, techo, z1), new Vector3(-pa, techo, z1), Vector3.back);
        Malla("Paredes", v, uv, m.cubierta, padre);

        // Puerta vista desde afuera (en la cara del castillo que mira a proa)
        var vd = new List<Vector3>(); var ud = new List<Vector2>();
        float zf = zc + lc / 2f + 0.015f;
        CascoProcedural.Cara(vd, ud, new Vector3(-pa, piso, zf), new Vector3(pa, piso, zf), new Vector3(pa, pAlto, zf), new Vector3(-pa, pAlto, zf), Vector3.forward);
        GameObject puerta = Astillero.Malla("Puerta", transform, CascoProcedural.MallaDe("Puerta", vd, ud), oscuro);
        var vm = new List<Vector3>(); var um = new List<Vector2>();
        CascoProcedural.Caja(vm, um, new Vector3(-pa - 0.06f, (piso + pAlto) / 2f, zf), new Vector3(0.12f, 2.0f, 0.1f), Quaternion.identity);
        CascoProcedural.Caja(vm, um, new Vector3(pa + 0.06f, (piso + pAlto) / 2f, zf), new Vector3(0.12f, 2.0f, 0.1f), Quaternion.identity);
        CascoProcedural.Caja(vm, um, new Vector3(0f, pAlto + 0.06f, zf), new Vector3(2f * pa + 0.24f, 0.12f, 0.1f), Quaternion.identity);
        GameObject marco = Astillero.Malla("Marco puerta", transform, CascoProcedural.MallaDe("Marco", vm, um), m.mastil);
        Rendimiento.Capa(puerta, Rendimiento.CapaBarcoDetalle);
        Rendimiento.Capa(marco, Rendimiento.CapaBarcoDetalle);

        // Muebles
        var vf = new List<Vector3>(); var uf = new List<Vector2>();
        float cx = 0f, cz = Mathf.Lerp(z0, z1, 0.45f);
        float mesaL = Mathf.Min(1.4f, ancho * 0.45f), mesaA = Mathf.Min(0.9f, (z1 - z0) * 0.35f);
        CascoProcedural.Caja(vf, uf, new Vector3(cx, piso + 0.76f, cz), new Vector3(mesaL, 0.06f, mesaA), Quaternion.identity);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                CascoProcedural.Caja(vf, uf, new Vector3(cx + sx * (mesaL / 2f - 0.08f), piso + 0.37f, cz + sz * (mesaA / 2f - 0.08f)), new Vector3(0.07f, 0.74f, 0.07f), Quaternion.identity);
        // Sillas a los lados de la mesa
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 c = new Vector3(cx + sx * (mesaL / 2f + 0.35f), piso, cz);
            CascoProcedural.Caja(vf, uf, c + new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.05f, 0.42f), Quaternion.identity);
            CascoProcedural.Caja(vf, uf, c + new Vector3(sx * 0.19f, 0.8f, 0f), new Vector3(0.05f, 0.7f, 0.42f), Quaternion.identity);
            foreach (float a in new[] { -0.17f, 0.17f })
                foreach (float b in new[] { -0.17f, 0.17f })
                    CascoProcedural.Caja(vf, uf, c + new Vector3(a, 0.22f, b), new Vector3(0.04f, 0.44f, 0.04f), Quaternion.identity);
        }
        // Cama contra la pared de babor, al fondo
        float camaL = Mathf.Min(1.9f, (z1 - z0) - 0.3f);
        Vector3 cama = new Vector3(-xi + 0.47f, piso, z0 + camaL / 2f + 0.1f);
        CascoProcedural.Caja(vf, uf, cama + new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, camaL), Quaternion.identity);
        // Cofre del capitán junto a estribor
        Vector3 cofre = new Vector3(xi - 0.45f, piso, z0 + 0.45f);
        CascoProcedural.Caja(vf, uf, cofre + new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.44f, 0.5f), Quaternion.identity);
        Malla("Muebles", vf, uf, m.mastil, padre);

        var vt = new List<Vector3>(); var ut = new List<Vector2>();
        CascoProcedural.Caja(vt, ut, cama + new Vector3(0f, 0.52f, 0f), new Vector3(0.82f, 0.08f, camaL - 0.1f), Quaternion.identity);
        CascoProcedural.Caja(vt, ut, cama + new Vector3(0f, 0.6f, -camaL / 2f + 0.25f), new Vector3(0.6f, 0.12f, 0.3f), Quaternion.identity);
        Malla("Sábanas", vt, ut, m.vela, padre);
        var vr = new List<Vector3>(); var ur = new List<Vector2>();
        CascoProcedural.Caja(vr, ur, cofre + new Vector3(0f, 0.47f, 0f), new Vector3(0.84f, 0.08f, 0.54f), Quaternion.identity);
        CascoProcedural.Caja(vr, ur, new Vector3(0f, piso + 0.005f, cz), new Vector3(Mathf.Min(2.4f, ancho - 0.6f), 0.01f, Mathf.Min(1.8f, z1 - z0 - 0.4f)), Quaternion.identity);
        Malla("Alfombra y tapa", vr, ur, Modelos.DeColor(m.vela, new Color(0.45f, 0.08f, 0.08f)), padre);
        var vmap = new List<Vector3>(); var umap = new List<Vector2>();
        CascoProcedural.Caja(vmap, umap, new Vector3(cx, piso + 0.795f, cz), new Vector3(mesaL * 0.6f, 0.01f, mesaA * 0.65f), Quaternion.Euler(0f, 8f, 0f));
        Malla("Mapa", vmap, umap, Modelos.DeColor(m.vela, new Color(0.86f, 0.78f, 0.58f)), padre);
        if (Modelos.Existe("farol"))
            Modelos.Crear("farol", padre, new Vector3(cx + mesaL * 0.3f, piso + 0.79f, cz - mesaA * 0.25f), Quaternion.identity, 0.6f,
                nm => nm == "Vidrio" ? Modelos.DeColor(m.vela, new Color(1f, 0.78f, 0.35f), true) : m.canon);

        lucesCamarote = new GameObject("Luz camarote");
        lucesCamarote.transform.SetParent(padre, false);
        lucesCamarote.transform.localPosition = new Vector3(cx, techo - 0.4f, cz);
        Light l = lucesCamarote.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.72f, 0.42f); l.range = 7f; l.intensity = 2.5f; l.shadows = LightShadows.None;
        lucesCamarote.SetActive(false);
    }
}
