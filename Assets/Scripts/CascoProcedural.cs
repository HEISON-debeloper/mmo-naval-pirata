using System.Collections.Generic;
using UnityEngine;

// Formas de barco hechas por código, para cualquier tamaño (los barcos se alargan, ensanchan y suben de pisos,
// así que un modelo fijo no serviría): casco con proa en punta, fondo curvo y popa plana; cubierta;
// borda (la baranda alrededor de la cubierta); y velas curvas, infladas por el viento.
// Todo en las mismas medidas que el BoxCollider del barco: ancho w, alto h, largo L, centrado.
public static class CascoProcedural
{
    const int Estaciones = 18;   // cortes a lo largo del barco
    const int Lados = 6;          // puntos de cada corte, de la borda a la quilla

    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    // Dónde termina el barco: popa en -L/2, punta de la proa como la tenía el modelo de cubos
    public static float ZPopa(float L) => -0.5f * L;
    public static float ZProa(float w, float L) => 0.3f * L + 0.5f * w;

    // Medio ancho (fracción) en la estación s (0 = popa, 1 = proa)
    static float Ancho(float s, float w, float L)
    {
        float z = Mathf.Lerp(ZPopa(L), ZProa(w, L), s);
        float z0 = 0.3f * L - 0.6f * w;                     // donde la proa empieza a cerrarse
        float f = 1f;
        if (s < 0.08f) f = Mathf.Lerp(0.82f, 1f, s / 0.08f);  // espejo de popa un poco más angosto
        if (z > z0)
        {
            float t = Mathf.Clamp01((z - z0) / (ZProa(w, L) - z0));
            f *= Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(t, 1.8f)), 0.7f);
        }
        return f;
    }

    // Altura de la quilla en la estación s: sube en la proa (pie de roda) y un poco en la popa
    static float Quilla(float s, float w, float h, float L)
    {
        float y = -h / 2f;
        float z = Mathf.Lerp(ZPopa(L), ZProa(w, L), s), z0 = 0.3f * L - 0.6f * w;
        if (z > z0) { float t = Mathf.Clamp01((z - z0) / (ZProa(w, L) - z0)); y += h * 0.6f * t * t; }
        if (s < 0.1f) y += h * 0.12f * (1f - s / 0.1f);
        return y;
    }

    // Punto k del corte (0 = borde de cubierta, Lados = quilla) en un costado (lado = +1 estribor, -1 babor)
    static Vector3 Punto(int e, int k, float lado, float w, float h, float L)
    {
        float s = (float)e / Estaciones;
        float z = Mathf.Lerp(ZPopa(L), ZProa(w, L), s);
        float hw = 0.5f * w * Ancho(s, w, L);
        float arriba = h / 2f, abajo = Quilla(s, w, h, L);
        float th = (float)k / Lados * Mathf.PI / 2f;
        const float n = 2.6f;   // forma del pantoque: 2 = redondo, más = más cuadrado
        // Max(0): cos(90°) sale -0.00000004 y una potencia fraccionaria de un negativo da NaN
        float cx = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(th)), 2f / n), sy = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(th)), 2f / n);
        return new Vector3(lado * hw * cx, arriba - (arriba - abajo) * sy, z);
    }

    // Medio ancho del casco a la altura y (local) en el punto z (local): dónde está el costado
    public static float Costado(float z, float y, float w, float h, float L)
    {
        float s = Mathf.Clamp01((z - ZPopa(L)) / (ZProa(w, L) - ZPopa(L)));
        float hw = 0.5f * w * Ancho(s, w, L);
        float arriba = h / 2f, abajo = Quilla(s, w, h, L);
        if (y >= arriba) return hw;
        if (y <= abajo) return 0f;
        const float n = 2.6f;
        float sy = (arriba - y) / (arriba - abajo);
        float sen = Mathf.Pow(sy, n / 2f);
        float cos = Mathf.Sqrt(Mathf.Max(0f, 1f - sen * sen));
        return hw * Mathf.Pow(cos, 2f / n);
    }

    // ---------- Portas de cañón (huecos en el casco con marco y tapa abierta) ----------
    // puestos: (y, z) de cada cañón de un costado; se ponen en los dos costados.
    // Devuelve tres mallas: el hueco oscuro, los marcos y las tapas.
    public const float AnchoPorta = 1.0f, AltoPorta = 0.85f;
    public static void Portas(List<Vector2> puestos, float w, float h, float L, out Mesh huecos, out Mesh marcos, out Mesh tapas)
    {
        var vh = new List<Vector3>(); var uh = new List<Vector2>();
        var vm = new List<Vector3>(); var um = new List<Vector2>();
        var vt = new List<Vector3>(); var ut = new List<Vector2>();
        float a = AnchoPorta / 2f, b = AltoPorta / 2f, g = 0.09f;
        foreach (float lado in new[] { 1f, -1f })
            foreach (Vector2 p in puestos)
            {
                float y = p.x, z = p.y;
                float x = Mathf.Max(Costado(z, y - b, w, h, L), Costado(z, y + b, w, h, L)) + 0.03f;
                Vector3 c = new Vector3(lado * x, y, z);
                Vector3 n = new Vector3(lado, 0f, 0f);
                if (y < h / 2f - 0.01f)
                {
                    // Porta del casco: hueco de verdad. Las jambas (el grosor del casco) van del costado de afuera a la pared
                    // de adentro, y detrás un fondo oscuro que solo se ve desde afuera (se esconde al bajar al interior).
                    float Xo(float zz, float yy) => lado * Costado(zz, yy, w, h, L);
                    float Xi(float zz) => lado * Mathf.Min(Costado(zz, y - 0.43f, w, h, L) - 0.08f, Costado(zz, y, w, h, L) - 0.12f);
                    float z0 = z - a, z1 = z + a, y0 = y - b, y1 = y + b;
                    CaraH(vm, um, new Vector3(Xo(z0, y1), y1, z0), new Vector3(Xo(z1, y1), y1, z1), new Vector3(Xi(z1), y1, z1), new Vector3(Xi(z0), y1, z0), Vector3.down);
                    CaraH(vm, um, new Vector3(Xo(z0, y0), y0, z0), new Vector3(Xo(z1, y0), y0, z1), new Vector3(Xi(z1), y0, z1), new Vector3(Xi(z0), y0, z0), Vector3.up);
                    CaraH(vm, um, new Vector3(Xo(z0, y0), y0, z0), new Vector3(Xo(z0, y1), y1, z0), new Vector3(Xi(z0), y1, z0), new Vector3(Xi(z0), y0, z0), Vector3.forward);
                    CaraH(vm, um, new Vector3(Xo(z1, y0), y0, z1), new Vector3(Xo(z1, y1), y1, z1), new Vector3(Xi(z1), y1, z1), new Vector3(Xi(z1), y0, z1), Vector3.back);
                    // Fondo oscuro: una caja abierta hacia el hueco
                    float fi = Mathf.Min(Mathf.Abs(Xi(z0)), Mathf.Abs(Xi(z1)));
                    float xf = lado * Mathf.Max(0.2f, fi - 1.6f), xb = lado * fi;
                    CaraH(vh, uh, new Vector3(xf, y0, z0), new Vector3(xf, y0, z1), new Vector3(xf, y1, z1), new Vector3(xf, y1, z0), n);
                    CaraH(vh, uh, new Vector3(xb, y1, z0), new Vector3(xb, y1, z1), new Vector3(xf, y1, z1), new Vector3(xf, y1, z0), Vector3.down);
                    CaraH(vh, uh, new Vector3(xb, y0, z0), new Vector3(xb, y0, z1), new Vector3(xf, y0, z1), new Vector3(xf, y0, z0), Vector3.up);
                    CaraH(vh, uh, new Vector3(xb, y0, z0), new Vector3(xb, y1, z0), new Vector3(xf, y1, z0), new Vector3(xf, y0, z0), Vector3.forward);
                    CaraH(vh, uh, new Vector3(xb, y0, z1), new Vector3(xb, y1, z1), new Vector3(xf, y1, z1), new Vector3(xf, y0, z1), Vector3.back);
                }
                else
                {
                    // Porta en la borda: un cuadro oscuro hundido un poco
                    Vector3 A = c + new Vector3(0, -b, -a), B = c + new Vector3(0, -b, a), C = c + new Vector3(0, b, a), D = c + new Vector3(0, b, -a);
                    CaraH(vh, uh, A, B, C, D, n);
                }
                // Marco: cuatro listones alrededor
                Caja(vm, um, c + new Vector3(lado * 0.04f, b + g / 2f, 0f), new Vector3(0.12f, g, AnchoPorta + 2f * g), Quaternion.identity);
                Caja(vm, um, c + new Vector3(lado * 0.04f, -b - g / 2f, 0f), new Vector3(0.12f, g, AnchoPorta + 2f * g), Quaternion.identity);
                Caja(vm, um, c + new Vector3(lado * 0.04f, 0f, a + g / 2f), new Vector3(0.12f, AltoPorta, g), Quaternion.identity);
                Caja(vm, um, c + new Vector3(lado * 0.04f, 0f, -a - g / 2f), new Vector3(0.12f, AltoPorta, g), Quaternion.identity);
                // Tapa: colgada del borde de arriba y abierta hacia afuera y arriba
                Quaternion giro = Quaternion.Euler(0f, 0f, lado * 120f);
                Vector3 bisagra = c + new Vector3(lado * 0.1f, b + g, 0f);
                Caja(vt, ut, bisagra + giro * new Vector3(0f, -(AltoPorta + g) / 2f, 0f), new Vector3(0.07f, AltoPorta + g, AnchoPorta + g), giro);
            }
        huecos = Armar("Portas", vh, uh); Propias.Add(huecos);
        marcos = Armar("Marcos", vm, um); Propias.Add(marcos);
        tapas = Armar("Tapas", vt, ut); Propias.Add(tapas);
    }

    public static void Cara(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal) => CaraH(v, uv, a, b, c, d, normal);
    // Mallas hechas para un solo barco (no compartidas): se destruyen con él (ver MallasPropias)
    public static readonly HashSet<Mesh> Propias = new HashSet<Mesh>();
    public static Mesh MallaDe(string nombre, List<Vector3> v, List<Vector2> uv) { Mesh m = Armar(nombre, v, uv); Propias.Add(m); return m; }

    // Una cara de 4 puntos que mira hacia "normal"
    static void CaraH(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) { Vector3 t = b; b = d; d = t; }
        v.Add(a); v.Add(b); v.Add(c); v.Add(a); v.Add(c); v.Add(d);
        uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.zero); uv.Add(Vector2.one); uv.Add(Vector2.up);
    }

    // Una caja (centro, tamaño, giro) agregada a la lista de triángulos
    public static void Caja(List<Vector3> v, List<Vector2> uv, Vector3 c, Vector3 t, Quaternion r)
    {
        Vector3 P(float x, float y, float z) => c + r * new Vector3(x * t.x / 2f, y * t.y / 2f, z * t.z / 2f);
        Vector3 cen = c;
        void Cara(Vector3 a, Vector3 b, Vector3 cc, Vector3 d) => CaraH(v, uv, a, b, cc, d, (a + b + cc + d) / 4f - cen);
        Cara(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1));
        Cara(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
        Cara(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1));
        Cara(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        Cara(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
        Cara(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1));
    }

    // Casco simplificado para el collider (convexo, pocas caras): la misma forma, tapada arriba a la altura de la cubierta
    public static Mesh CascoColision(float w, float h, float L)
    {
        string clave = "col" + w.ToString("0.00") + "_" + h.ToString("0.00") + "_" + L.ToString("0.00");
        if (cache.TryGetValue(clave, out Mesh m) && m != null) return m;
        const int ne = 10, nk = 3;
        var vs = new List<Vector3>();
        for (int e = 0; e <= ne; e++)
        {
            float s = (float)e / ne;
            float z = Mathf.Lerp(ZPopa(L), ZProa(w, L), s);
            float hw = 0.5f * w * Ancho(s, w, L), arriba = h / 2f + 0.1f, abajo = Quilla(s, w, h, L);
            for (int k = 0; k <= nk; k++)
            {
                float th = (float)k / nk * Mathf.PI / 2f;
                const float n = 2.6f;
                float cx = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(th)), 2f / n), sy = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(th)), 2f / n);
                float y = arriba - (arriba - abajo) * sy;
                vs.Add(new Vector3(hw * cx, y, z));
                if (k < nk) vs.Add(new Vector3(-hw * cx, y, z));
            }
        }
        // El collider convexo solo usa los puntos: basta un triángulo que los nombre a todos
        var tr = new List<int>();
        for (int i = 2; i < vs.Count; i++) { tr.Add(0); tr.Add(i - 1); tr.Add(i); }
        m = new Mesh { name = "Casco (colisión)" };
        m.SetVertices(vs); m.SetTriangles(tr, 0);
        m.RecalculateBounds();
        cache[clave] = m;
        return m;
    }

    // ---------- Casco ----------
    public static Mesh Casco(float w, float h, float L, List<Vector2> portas = null)
    {
        if (portas != null)
        {
            var delCasco = new List<Vector2>();
            foreach (Vector2 p in portas) if (p.x < h / 2f - 0.01f) delCasco.Add(p);
            if (delCasco.Count > 0) return CascoConPortas(w, h, L, delCasco);
        }
        string clave = "casco" + w.ToString("0.00") + "_" + h.ToString("0.00") + "_" + L.ToString("0.00");
        if (cache.TryGetValue(clave, out Mesh m) && m != null) return m;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        foreach (float lado in new[] { 1f, -1f })
            for (int e = 0; e < Estaciones; e++)
                for (int k = 0; k < Lados; k++)
                {
                    Vector3 a = Punto(e, k, lado, w, h, L), b = Punto(e + 1, k, lado, w, h, L);
                    Vector3 c = Punto(e + 1, k + 1, lado, w, h, L), d = Punto(e, k + 1, lado, w, h, L);
                    // Hacia afuera: el orden depende del costado
                    if (lado > 0) Quad(v, uv, a, b, c, d, w, h, L); else Quad(v, uv, a, d, c, b, w, h, L);
                }
        // Espejo de popa (la pared plana de atrás)
        for (int k = 0; k < Lados; k++)
        {
            Vector3 a = Punto(0, k, -1f, w, h, L), b = Punto(0, k, 1f, w, h, L);
            Vector3 c = Punto(0, k + 1, 1f, w, h, L), d = Punto(0, k + 1, -1f, w, h, L);
            Quad(v, uv, a, b, c, d, w, h, L);
        }
        m = Armar("Casco", v, uv);
        cache[clave] = m;
        return m;
    }

    static float QuillaZ(float z, float w, float h, float L) => Quilla(Mathf.Clamp01((z - ZPopa(L)) / (ZProa(w, L) - ZPopa(L))), w, h, L);

    // Casco con huecos de verdad en las portas: los costados se arman en una rejilla (largo × alto) que incluye los bordes
    // de cada porta y se salta las celdas de adentro. Abajo de la franja de las portas sigue la curva hasta la quilla.
    static Mesh CascoConPortas(float w, float h, float L, List<Vector2> portas)
    {
        var sb = new System.Text.StringBuilder("cascoP" + w.ToString("0.00") + "_" + h.ToString("0.00") + "_" + L.ToString("0.00"));
        foreach (Vector2 p in portas) sb.Append('_').Append(p.x.ToString("0.00")).Append(',').Append(p.y.ToString("0.00"));
        string clave = sb.ToString();
        if (cache.TryGetValue(clave, out Mesh m) && m != null) return m;

        float a = AnchoPorta / 2f, b = AltoPorta / 2f, arriba = h / 2f;
        var huecos = new List<Rect>();
        float yMin = arriba;
        foreach (Vector2 p in portas) { huecos.Add(new Rect(p.y - a, p.x - b, AnchoPorta, AltoPorta)); yMin = Mathf.Min(yMin, p.x - b); }
        float yB = yMin - 0.3f;   // donde termina la franja de las portas

        var zs = new List<float>();
        for (int e = 0; e <= Estaciones * 2; e++) zs.Add(Mathf.Lerp(ZPopa(L), ZProa(w, L), (float)e / (Estaciones * 2)));
        foreach (Rect r in huecos) { zs.Add(r.xMin); zs.Add(r.xMax); }
        Ordenar(zs);
        var ys = new List<float> { yB, arriba };
        for (float y = yB + 0.6f; y < arriba - 0.05f; y += 0.6f) ys.Add(y);
        foreach (Rect r in huecos) { ys.Add(r.yMin); ys.Add(r.yMax); }
        Ordenar(ys);

        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(float lado, float z, float y) => new Vector3(lado * Costado(z, y, w, h, L), y, z);
        // Altura de la fila j (0 = franja, n = quilla) en el punto z: más filas cerca del fondo, donde se curva
        const int nL = 8;
        float Ybajo(float z, int j) { float ab = Mathf.Min(QuillaZ(z, w, h, L), yB); return yB - (yB - ab) * Mathf.Sin((float)j / nL * Mathf.PI / 2f); }
        void Q(float lado, Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd) { if (lado > 0) Quad(v, uv, pa, pb, pc, pd, w, h, L); else Quad(v, uv, pa, pd, pc, pb, w, h, L); }
        foreach (float lado in new[] { 1f, -1f })
            for (int i = 0; i < zs.Count - 1; i++)
            {
                float z0 = zs[i], z1 = zs[i + 1], zm = (z0 + z1) / 2f;
                // Franja de las portas (de arriba hacia abajo, como el resto del casco)
                for (int k = ys.Count - 1; k > 0; k--)
                {
                    float yA = ys[k], yC = ys[k - 1], ym = (yA + yC) / 2f;
                    bool enHueco = false;
                    foreach (Rect r in huecos) if (zm > r.xMin && zm < r.xMax && ym > r.yMin && ym < r.yMax) { enHueco = true; break; }
                    if (enHueco) continue;
                    Q(lado, P(lado, z0, yA), P(lado, z1, yA), P(lado, z1, yC), P(lado, z0, yC));
                }
                // De la franja a la quilla
                for (int j = 0; j < nL; j++)
                    Q(lado, P(lado, z0, Ybajo(z0, j)), P(lado, z1, Ybajo(z1, j)), P(lado, z1, Ybajo(z1, j + 1)), P(lado, z0, Ybajo(z0, j + 1)));
            }
        // Espejo de popa con los mismos puntos del borde
        float zp = zs[0];
        var bordes = new List<float>();
        for (int k = ys.Count - 1; k >= 0; k--) bordes.Add(ys[k]);
        for (int j = 1; j <= nL; j++) bordes.Add(Ybajo(zp, j));
        for (int k = 0; k < bordes.Count - 1; k++)
            Quad(v, uv, P(-1f, zp, bordes[k]), P(1f, zp, bordes[k]), P(1f, zp, bordes[k + 1]), P(-1f, zp, bordes[k + 1]), w, h, L);
        m = Armar("Casco", v, uv);
        cache[clave] = m;
        return m;
    }

    static void Ordenar(List<float> l)
    {
        l.Sort();
        for (int k = l.Count - 1; k > 0; k--) if (l[k] - l[k - 1] < 0.02f) l.RemoveAt(k);
    }

    // ---------- Cubierta (la superficie donde se camina) ----------
    public static Mesh CubiertaMalla(float w, float h, float L)
    {
        string clave = "cub" + w.ToString("0.00") + "_" + h.ToString("0.00") + "_" + L.ToString("0.00");
        if (cache.TryGetValue(clave, out Mesh m) && m != null) return m;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float y = h / 2f + 0.1f;   // misma altura que Cubierta.Altura
        for (int e = 0; e < Estaciones; e++)
        {
            Vector3 a = Punto(e, 0, -1f, w, h, L), b = Punto(e, 0, 1f, w, h, L);
            Vector3 c = Punto(e + 1, 0, 1f, w, h, L), d = Punto(e + 1, 0, -1f, w, h, L);
            a.y = b.y = c.y = d.y = y;
            Quad(v, uv, a, d, c, b, w, h, L);
            // Canto de la cubierta (para que no se vea hueca desde abajo)
        }
        m = Armar("Cubierta", v, uv);
        cache[clave] = m;
        return m;
    }

    // ---------- Borda: baranda alrededor de la cubierta, más alta en proa y popa ----------
    public static Mesh Borda(float w, float h, float L)
    {
        string clave = "borda" + w.ToString("0.00") + "_" + h.ToString("0.00") + "_" + L.ToString("0.00");
        if (cache.TryGetValue(clave, out Mesh m) && m != null) return m;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float grosor = Mathf.Clamp(0.06f * w, 0.1f, 0.35f);
        float alto = Mathf.Clamp(0.12f * h + 0.35f, 0.45f, 1.4f);
        Vector3 Base(int e, float lado) => Punto(e, 0, lado, w, h, L);
        float Arrufo(int e) { float s = (float)e / Estaciones; return alto * (1f + 0.6f * Mathf.Pow(Mathf.Abs(2f * s - 1f), 2f)); }   // sube en los extremos
        Vector3 Adentro(int e, float lado)
        {
            Vector3 p = Base(e, lado);
            float hw = Mathf.Abs(p.x);
            p.x = lado * Mathf.Max(0f, hw - grosor);
            return p;
        }
        foreach (float lado in new[] { 1f, -1f })
            for (int e = 0; e < Estaciones; e++)
            {
                Vector3 a = Base(e, lado), b = Base(e + 1, lado);
                Vector3 ai = Adentro(e, lado), bi = Adentro(e + 1, lado);
                Vector3 up0 = Vector3.up * Arrufo(e), up1 = Vector3.up * Arrufo(e + 1);
                QuadH(v, uv, a, b, b + up1, a + up0, new Vector3(lado, 0f, 0f), w, h, L);      // cara de afuera
                QuadH(v, uv, a + up0, b + up1, bi + up1, ai + up0, Vector3.up, w, h, L);      // tope
                QuadH(v, uv, ai, bi, bi + up1, ai + up0, new Vector3(-lado, 0f, 0f), w, h, L); // cara de adentro
            }
        // Tablón de popa entre los dos costados
        {
            Vector3 a = Base(0, -1f), b = Base(0, 1f), up = Vector3.up * Arrufo(0);
            Vector3 ai = a + Vector3.forward * grosor, bi = b + Vector3.forward * grosor;
            QuadH(v, uv, a, b, b + up, a + up, Vector3.back, w, h, L);
            QuadH(v, uv, ai, bi, bi + up, ai + up, Vector3.forward, w, h, L);
            QuadH(v, uv, a + up, b + up, bi + up, ai + up, Vector3.up, w, h, L);
        }
        m = Armar("Borda", v, uv);
        cache[clave] = m;
        return m;
    }

    // Altura de la borda en un punto (para poner cañones de cubierta, faroles, etc.)
    public static float GrosorBorda(float w) => Mathf.Clamp(0.06f * w, 0.1f, 0.35f);
    public static float AltoBorda(float h) => Mathf.Clamp(0.12f * h + 0.35f, 0.45f, 1.4f);

    // ---------- Vela curva ----------
    // En el espacio del pivote de la vela: x de -0.5 a 0.5, y de 0 (verga) a -1 (abajo), y se infla hacia +z.
    // Con la escala del pivote (ancho, alto, ancho * 0.18) queda proporcional a cualquier vela.
    public static Mesh Vela()
    {
        if (cache.TryGetValue("vela", out Mesh m) && m != null) return m;
        const int nx = 8, ny = 6;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(int i, int j, float desfase)
        {
            float x = (float)i / nx - 0.5f, y = -(float)j / ny;
            float inflado = (1f - 4f * x * x) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(-y * 0.92f + 0.04f));
            return new Vector3(x, y, inflado + desfase);
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                // Cara de adelante y de atrás (la vela se ve de los dos lados)
                Vector3 a = P(i, j, 0.04f), b = P(i + 1, j, 0.04f), c = P(i + 1, j + 1, 0.04f), d = P(i, j + 1, 0.04f);
                QuadUV(v, uv, a, d, c, b, i, j, nx, ny);
                Vector3 a2 = P(i, j, -0.04f), b2 = P(i + 1, j, -0.04f), c2 = P(i + 1, j + 1, -0.04f), d2 = P(i, j + 1, -0.04f);
                QuadUV(v, uv, a2, b2, c2, d2, i, j, nx, ny);
            }
        m = Armar("Vela", v, uv);
        cache["vela"] = m;
        return m;
    }

    // ---------- Velas de cuchillo (en el plano del barco, se inflan hacia los lados) ----------
    // Foque: triángulo con la punta arriba (0,0,0), el puño de atrás abajo (0,-1,0) y el de adelante en (0,-1,1).
    // Escala del pivote: (inflado, alto, largo).
    public static Mesh VelaFoque()
    {
        if (cache.TryGetValue("foque", out Mesh m) && m != null) return m;
        const int n = 7;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(float a, float b, float desfase)   // a: 0 arriba → 1 abajo; b: 0 atrás → 1 adelante (sobre esa altura)
        {
            float inflado = Mathf.Sin(Mathf.PI * a) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(b)) * 0.9f;
            return new Vector3(inflado + desfase, -a, a * b);
        }
        for (int i = 0; i < n; i++)
            for (int j = 0; j <= i; j++)
            {
                float a0 = (float)i / n, a1 = (float)(i + 1) / n;
                float b00 = i == 0 ? 0f : (float)j / i, b10 = (float)j / (i + 1), b11 = (float)(j + 1) / (i + 1);
                foreach (float d in new[] { 0.05f, -0.05f })
                {
                    Vector3 A = P(a0, b00, d), B = P(a1, b10, d), C = P(a1, b11, d);
                    if (d > 0) TriUV(v, uv, A, C, B); else TriUV(v, uv, A, B, C);
                    if (j < i)
                    {
                        float b01 = (float)(j + 1) / i;
                        Vector3 D = P(a0, b01, d);
                        if (d > 0) TriUV(v, uv, A, D, C); else TriUV(v, uv, A, C, D);
                    }
                }
            }
        m = Armar("Foque", v, uv);
        cache["foque"] = m;
        return m;
    }

    // Cangreja: trapecio detrás del mástil; el borde de arriba (el pico) sube hacia atrás.
    // Va de z = 0 (mástil) a z = -1 (atrás), y de y = 0 (arriba) a y = -1 (botavara). Escala: (inflado, alto, largo).
    public static Mesh VelaCangreja()
    {
        if (cache.TryGetValue("cangreja", out Mesh m) && m != null) return m;
        const int nz = 6, ny = 6;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(int i, int j, float desfase)
        {
            float u = (float)i / nz, t = (float)j / ny;
            float arriba = 0.35f * u;   // el pico sube hacia atrás
            float y = Mathf.Lerp(arriba, -1f, t);
            float inflado = Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * t) * 0.9f;
            return new Vector3(inflado + desfase, y, -u);
        }
        for (int i = 0; i < nz; i++)
            for (int j = 0; j < ny; j++)
                foreach (float d in new[] { 0.05f, -0.05f })
                {
                    Vector3 a = P(i, j, d), b = P(i + 1, j, d), c = P(i + 1, j + 1, d), e = P(i, j + 1, d);
                    if (d > 0) { TriUV(v, uv, a, b, c); TriUV(v, uv, a, c, e); }
                    else { TriUV(v, uv, a, c, b); TriUV(v, uv, a, e, c); }
                }
        m = Armar("Cangreja", v, uv);
        cache["cangreja"] = m;
        return m;
    }

    static void TriUV(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c)
    {
        foreach (Vector3 p in new[] { a, b, c }) { v.Add(p); uv.Add(new Vector2(-p.z, 1f + p.y)); }
    }

    // ---------- Ayudas ----------
    static void Quad(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float w, float h, float L)
    {
        Tri(v, uv, a, b, c, w, h, L);
        Tri(v, uv, a, c, d, w, h, L);
    }

    // Como Quad, pero se voltea solo si la cara quedaría mirando hacia "normal" al revés
    static void QuadH(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, float w, float h, float L)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) Quad(v, uv, a, d, c, b, w, h, L);
        else Quad(v, uv, a, b, c, d, w, h, L);
    }

    static void Tri(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, float w, float h, float L)
    {
        if ((Vector3.Cross(b - a, c - a)).sqrMagnitude < 1e-10f) return;   // triángulos aplastados en la punta de la proa
        v.Add(a); v.Add(b); v.Add(c);
        foreach (Vector3 p in new[] { a, b, c }) uv.Add(new Vector2(p.z / L + 0.5f, p.y / Mathf.Max(h, 0.1f) + 0.5f));
    }

    static void QuadUV(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int i, int j, int nx, int ny)
    {
        Vector2 UV(Vector3 p) => new Vector2(p.x + 0.5f, 1f + p.y);
        v.Add(a); v.Add(b); v.Add(c); uv.Add(UV(a)); uv.Add(UV(b)); uv.Add(UV(c));
        v.Add(a); v.Add(c); v.Add(d); uv.Add(UV(a)); uv.Add(UV(c)); uv.Add(UV(d));
    }

    static Mesh Armar(string nombre, List<Vector3> v, List<Vector2> uv)
    {
        Mesh m = new Mesh { name = nombre };
        if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetUVs(0, uv);
        var t = new int[v.Count];
        for (int i = 0; i < t.Length; i++) t[i] = i;
        m.triangles = t;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
