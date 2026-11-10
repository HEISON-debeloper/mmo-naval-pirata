using System.Collections.Generic;
using UnityEngine;

// Detalles del barco hechos por código (se adaptan a cualquier largo, ancho, alto y número de mástiles):
//  · Jarcia: obenques con flechates (las escalas de cuerda de cada mástil) y estays de mástil a mástil y al bauprés.
//  · Cofas: plataformas con baranda en los mástiles altos.
//  · Cintas: las franjas oscuras que recorren el casco bajo cada fila de portas y bajo la borda.
//  · Castillo de popa: baranda arriba, galería (balcón) atrás, escaleras al timón, faroles en las esquinas y moldura dorada.
//  · Mascarón de proa, cabrestante y campana.
//  · Tablones de cubierta (juntas oscuras), bote salvavidas boca abajo en los barcos grandes,
//    bitácora con brújula junto al timón y catalejo en la baranda de la toldilla.
public static class Detalles
{
    public struct Mastil { public float z, alto, grosor; }

    static readonly Color Cuerda = new Color(0.30f, 0.23f, 0.15f);
    static readonly Color Oro = new Color(0.85f, 0.68f, 0.25f);

    public static void Armar(Transform t, float L, float w, float h, List<Mastil> mastiles, Vector3 puntaBaupres, Astillero.Materiales m)
    {
        var vj = new List<Vector3>(); var uj = new List<Vector2>();   // jarcia (cuerda)
        var vm = new List<Vector3>(); var um = new List<Vector2>();   // madera (cofas, barandas, galería, cintas)
        var vo = new List<Vector3>(); var uo = new List<Vector2>();   // dorado (molduras)

        mastiles.Sort((a, b) => a.z.CompareTo(b.z));
        foreach (Mastil ma in mastiles) Obenques(vj, uj, vm, um, ma, w, h, L);
        Estays(vj, uj, mastiles, puntaBaupres, h);
        Cintas(vm, um, t, L, w, h);
        float frenteCastillo = Castillo(vm, um, vo, uo, t, L, w, h, m);

        Material cuerda = Modelos.DeColor(m.vela, Cuerda);
        Material oro = Modelos.DeColor(m.vela, Oro);
        Rendimiento.Capa(Astillero.Malla("Jarcia", t, CascoProcedural.MallaDe("Jarcia", vj, uj), cuerda), Rendimiento.CapaDetalle);
        Rendimiento.Capa(Astillero.Malla("Detalles", t, CascoProcedural.MallaDe("Detalles", vm, um), m.mastil), Rendimiento.CapaBarcoDetalle);
        if (vo.Count > 0) Rendimiento.Capa(Astillero.Malla("Molduras", t, CascoProcedural.MallaDe("Molduras", vo, uo), oro), Rendimiento.CapaBarcoDetalle);
        foreach (string n in new[] { "Jarcia", "Detalles", "Molduras" })
        {
            Transform x = t.Find(n);
            if (x != null) x.GetComponent<MeshRenderer>().shadowCastingMode = n == "Jarcia" ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        Mascaron(t, L, w, h, m, oro);
        Tablones(t, L, w, h, m);
        BoteSalvavidas(t, L, w, h, mastiles, frenteCastillo, m);
        Bitacora(t, L, w, h, m, oro);
        // Cabrestante en cubierta, detrás del mástil mayor; campana en el frente del castillo de popa
        if (mastiles.Count > 0 && Modelos.Existe("cabrestante") && w >= 3.5f)
        {
            Mastil mayor = mastiles[mastiles.Count / 2];
            float zc = mayor.z - Mathf.Clamp(0.08f * L, 1.6f, 3.5f);
            if (zc > frenteCastillo + 2.5f)
            {
                float e = Mathf.Clamp(w / 7f, 0.7f, 1.5f);
                GameObject c = Modelos.Crear("cabrestante", t, new Vector3(0f, h / 2f + 0.1f, zc), Quaternion.identity, e, n => n == "Aros" ? m.canon : m.mastil);
                ObstaculosCubierta ob = t.GetComponent<ObstaculosCubierta>();
                if (ob != null) ob.Agregar(0f, zc, 0.55f * e);
                Rendimiento.Capa(c, Rendimiento.CapaBarcoDetalle);
            }
        }
        if (Modelos.Existe("campana") && t.Find("Camarote") != null && w >= 3.5f)
        {
            float yTop = h / 2f + Cubierta.AltoCamaroteDe(h);
            GameObject c = Modelos.Crear("campana", t, new Vector3(0f, yTop, frenteCastillo - 0.35f), Quaternion.identity, Mathf.Clamp(w / 9f, 0.6f, 1.1f),
                n => n == "Campana" ? oro : m.mastil);
            Rendimiento.Capa(c, Rendimiento.CapaBarcoDetalle);
        }
    }

    // ---------- Tablones de cubierta ----------
    // Juntas oscuras a lo largo de la cubierta, cada ~0,32 m, siguiendo la forma real del casco (se cierran en la proa)
    static void Tablones(Transform t, float L, float w, float h, Astillero.Materiales m)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float y = h / 2f + 0.1f + 0.012f;   // bien despegadas de la cubierta: más cerca titilan con ella a lo lejos
        float borda = CascoProcedural.GrosorBorda(w) + 0.05f;
        float zA = CascoProcedural.ZPopa(L) + 0.15f, zB = CascoProcedural.ZProa(w, L) - 0.2f;
        const float paso = 0.32f, tramo = 0.5f;
        for (float x = -w / 2f + paso; x < w / 2f; x += paso)
        {
            float inicio = float.NaN;
            for (float z = zA; z <= zB + 0.001f; z += tramo)
            {
                bool dentro = Mathf.Abs(x) < CascoProcedural.Costado(z, h / 2f, w, h, L) - borda;
                if (dentro && float.IsNaN(inicio)) inicio = z;
                if ((!dentro || z + tramo > zB) && !float.IsNaN(inicio))
                {
                    float fin = dentro ? z : z - tramo;
                    if (fin - inicio > 0.3f) Caja(v, uv, new Vector3(x, y, (inicio + fin) / 2f), new Vector3(0.025f, 0.008f, fin - inicio));
                    inicio = float.NaN;
                }
            }
        }
        // Juntas de testa: cortes cruzados escalonados cada ~2,4 m
        for (float x = -w / 2f + paso / 2f; x < w / 2f; x += paso)
        {
            int fila = Mathf.RoundToInt((x + w) / paso);
            for (float z = zA + (fila % 3) * 0.8f; z < zB; z += 2.4f)
                if (Mathf.Abs(x) + paso / 2f < CascoProcedural.Costado(z, h / 2f, w, h, L) - borda)
                    Caja(v, uv, new Vector3(x, y, z), new Vector3(paso - 0.03f, 0.008f, 0.025f));
        }
        if (v.Count == 0) return;
        GameObject g = Astillero.Malla("Tablones", t, CascoProcedural.MallaDe("Tablones", v, uv), Modelos.DeColor(m.vela, new Color(0.16f, 0.11f, 0.07f)));
        g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        g.GetComponent<MeshRenderer>().receiveShadows = false;
        // Son líneas de 2,5 cm: lejos solo se ven como rayas que titilan, así que se dejan de dibujar a 180 m
        Rendimiento.Capa(g, Rendimiento.CapaDetalle);
    }

    // ---------- Bote salvavidas ----------
    // Un bote de remos boca abajo sobre dos calzos, entre el mástil mayor y el de proa (solo en barcos que lo aguantan)
    static void BoteSalvavidas(Transform t, float L, float w, float h, List<Mastil> mastiles, float frenteCastillo, Astillero.Materiales m)
    {
        if (L < 16f || w < 4.5f || mastiles.Count == 0) return;
        float lb = Mathf.Clamp(0.16f * L, 3f, 6f), ab = Mathf.Clamp(0.3f * w, 1.2f, 2f), hb = ab * 0.42f;
        // Hueco libre en el eje: entre dos mástiles o delante del castillo, lejos de la escotilla del centro
        float mejorZ = float.NaN, mejorHueco = 0f;
        var zs = new List<float> { frenteCastillo + 0.8f };
        foreach (var ma in mastiles) zs.Add(ma.z);
        zs.Add(0.3f * L - 0.6f * w);
        zs.Sort();
        for (int i = 0; i < zs.Count - 1; i++)
        {
            float hueco = zs[i + 1] - zs[i] - 1.6f;
            float zc = (zs[i] + zs[i + 1]) / 2f;
            if (Mathf.Abs(zc) < 1.8f) zc += zc >= 0f ? 1.8f : -1.8f;   // la escotilla está cerca del centro
            if (hueco > lb + 0.6f && hueco > mejorHueco) { mejorHueco = hueco; mejorZ = zc; }
        }
        if (float.IsNaN(mejorZ)) return;
        // El bote va a un costado de la cubierta para no tapar el paso
        float xb = Mathf.Min(CascoProcedural.Costado(mejorZ, h / 2f, w, h, L) - CascoProcedural.GrosorBorda(w) - ab / 2f - 0.25f, w * 0.22f);
        if (xb < ab / 2f + 0.4f) return;
        float y0 = h / 2f + 0.1f + 0.35f;   // sobre los calzos

        var v = new List<Vector3>(); var uv = new List<Vector2>();
        const int est = 12, lados = 8;
        Vector3 P(int e, int k)
        {
            float s = e / (float)est;                       // 0 popa → 1 proa del bote
            float z = Mathf.Lerp(-lb / 2f, lb / 2f, s);
            float fin = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.06f, 0.94f, s)), 0.6f);
            if (s > 0.5f) fin *= Mathf.Pow(1f - (s - 0.5f) * 2f, 0.25f) * 0.9f + 0.1f;
            float a = Mathf.PI * k / lados;                 // media vuelta: del borde de un lado al del otro
            float x = Mathf.Cos(a) * ab / 2f * fin;
            float yy = Mathf.Sin(a) * hb * Mathf.Lerp(0.6f, 1f, fin);
            return new Vector3(xb + x, y0 + yy, mejorZ + z);   // boca abajo: la quilla arriba
        }
        for (int e = 0; e < est; e++)
            for (int k = 0; k < lados; k++)
            {
                Vector3 a = P(e, k), b = P(e + 1, k), c = P(e + 1, k + 1), d = P(e, k + 1);
                Vector3 n = Vector3.Cross(b - a, d - a).normalized;
                if (Vector3.Dot(n, (a + c) / 2f - new Vector3(xb, y0, mejorZ)) < 0f) n = -n;
                CascoProcedural.Cara(v, uv, a, b, c, d, n);
            }
        // Quilla y regala (borde)
        Linea(v, uv, P(0, lados / 2) + Vector3.up * 0.04f, P(est, lados / 2) + Vector3.up * 0.04f, 0.08f);
        for (int e = 0; e < est; e++) { Linea(v, uv, P(e, 0), P(e + 1, 0), 0.07f); Linea(v, uv, P(e, lados), P(e + 1, lados), 0.07f); }
        // Calzos
        foreach (float f in new[] { -0.28f, 0.28f })
            Caja(v, uv, new Vector3(xb, h / 2f + 0.1f + 0.18f, mejorZ + f * lb), new Vector3(ab * 1.1f, 0.36f, 0.18f));
        // Remos encima, cruzados
        Linea(v, uv, new Vector3(xb - ab * 0.25f, y0 + hb + 0.1f, mejorZ - lb * 0.42f), new Vector3(xb - ab * 0.15f, y0 + hb + 0.1f, mejorZ + lb * 0.42f), 0.06f);
        Linea(v, uv, new Vector3(xb + ab * 0.2f, y0 + hb + 0.1f, mejorZ - lb * 0.4f), new Vector3(xb + ab * 0.1f, y0 + hb + 0.1f, mejorZ + lb * 0.4f), 0.06f);

        GameObject g = Astillero.Malla("Bote salvavidas", t, CascoProcedural.MallaDe("Bote salvavidas", v, uv), m.casco != null ? m.casco : m.mastil);
        Rendimiento.Capa(g, Rendimiento.CapaBarcoDetalle);
        ObstaculosCubierta ob = t.GetComponent<ObstaculosCubierta>();
        if (ob != null) for (float f = -0.35f; f <= 0.351f; f += 0.35f) ob.Agregar(xb, mejorZ + f * lb, ab * 0.55f);
    }

    // ---------- Bitácora con brújula y catalejo ----------
    // La bitácora (el pedestal de la brújula) va delante del timón; el catalejo descansa sobre la baranda de la toldilla.
    static void Bitacora(Transform t, float L, float w, float h, Astillero.Materiales m, Material oro)
    {
        bool plana = Cubierta.EsPlana(t);
        if (plana || w < 3f) return;
        Vector3 timon = Cubierta.Timon(h, L);
        float yPiso = timon.y - 0.1f + 0.0f;
        Transform cast = t.Find("Camarote");
        if (cast == null) return;
        var vm = new List<Vector3>(); var um = new List<Vector2>();
        var vo = new List<Vector3>(); var uo = new List<Vector2>();
        var vv = new List<Vector3>(); var uvv = new List<Vector2>();
        float zb = timon.z + 1.0f;
        // Pedestal de madera con tapa
        Caja(vm, um, new Vector3(0f, yPiso + 0.5f, zb), new Vector3(0.42f, 1.0f, 0.42f));
        Caja(vm, um, new Vector3(0f, yPiso + 1.02f, zb), new Vector3(0.52f, 0.06f, 0.52f));
        // Cúpula de bronce (octágono escalonado) con la rosa de la brújula adentro
        for (int i = 0; i < 4; i++)
        {
            float r = 0.22f * Mathf.Cos(i * 0.35f), yy = yPiso + 1.07f + i * 0.06f;
            CascoProcedural.Caja(vo, uo, new Vector3(0f, yy, zb), new Vector3(r * 2f, 0.06f, r * 2f), Quaternion.Euler(0f, 22.5f, 0f));
            CascoProcedural.Caja(vo, uo, new Vector3(0f, yy, zb), new Vector3(r * 2f, 0.06f, r * 2f), Quaternion.identity);
        }
        // Dos esferas compensadoras a los lados (como en las bitácoras de verdad)
        foreach (float sx in new[] { -1f, 1f })
        {
            Linea(vo, uo, new Vector3(sx * 0.21f, yPiso + 1.05f, zb), new Vector3(sx * 0.36f, yPiso + 1.08f, zb), 0.04f);
            CascoProcedural.Caja(vv, uvv, new Vector3(sx * 0.4f, yPiso + 1.1f, zb), new Vector3(0.12f, 0.12f, 0.12f), Quaternion.Euler(45f, 45f, 0f));
        }

        // Catalejo sobre la baranda de babor de la toldilla, apoyado en dos horquillas
        float cw = cast.localScale.x;
        float xs = -(cw / 2f - 0.05f), ys = timon.y - 0.1f + 0.9f + 0.08f, zs = timon.z - 0.2f;
        foreach (float dz in new[] { -0.25f, 0.25f })
            Caja(vm, um, new Vector3(xs, ys - 0.02f, zs + dz), new Vector3(0.08f, 0.08f, 0.04f));
        // Tres tramos que se van afinando (latón) y el ocular
        float zc0 = zs - 0.45f;
        float[] grosores = { 0.09f, 0.075f, 0.06f };
        for (int i = 0; i < 3; i++)
            Linea(vo, uo, new Vector3(xs, ys + 0.04f, zc0 + i * 0.3f), new Vector3(xs, ys + 0.04f, zc0 + (i + 1) * 0.3f + 0.02f), grosores[i]);
        Linea(vv, uvv, new Vector3(xs, ys + 0.04f, zc0 - 0.04f), new Vector3(xs, ys + 0.04f, zc0 + 0.02f), 0.1f);

        GameObject gm = Astillero.Malla("Bitácora madera", t, CascoProcedural.MallaDe("Bitácora madera", vm, um), m.mastil);
        GameObject go = Astillero.Malla("Bitácora bronce", t, CascoProcedural.MallaDe("Bitácora bronce", vo, uo), oro);
        GameObject gv = Astillero.Malla("Bitácora hierro", t, CascoProcedural.MallaDe("Bitácora hierro", vv, uvv), m.canon);
        foreach (var g in new[] { gm, go, gv }) Rendimiento.Capa(g, Rendimiento.CapaBarcoDetalle);
    }

    // ---------- Ayudas ----------
    // Una cuerda o palo recto de a a b (caja delgada)
    static void Linea(List<Vector3> v, List<Vector2> uv, Vector3 a, Vector3 b, float grosor)
    {
        Vector3 d = b - a;
        float largo = d.magnitude;
        if (largo < 0.01f) return;
        CascoProcedural.Caja(v, uv, (a + b) / 2f, new Vector3(grosor, grosor, largo), Quaternion.LookRotation(d / largo, Mathf.Abs(d.y) > largo * 0.99f ? Vector3.forward : Vector3.up));
    }

    static void Caja(List<Vector3> v, List<Vector2> uv, Vector3 c, Vector3 tam) => CascoProcedural.Caja(v, uv, c, tam, Quaternion.identity);

    // Los pisos de velas, igual que en Astillero.Mastil (para saber dónde va la cofa)
    static void Pisos(float hm, float h, out int pisos, out float abajo, out float tramo)
    {
        pisos = hm < 9f ? 1 : hm < 18f ? 2 : hm < 30f ? 3 : 4;
        abajo = h / 2 + Mathf.Max(2.2f, hm * 0.12f);
        float arriba = h / 2 + hm * 0.95f;
        tramo = (arriba - abajo) / pisos;
    }

    // ---------- Obenques, flechates y cofa ----------
    static void Obenques(List<Vector3> vj, List<Vector2> uj, List<Vector3> vm, List<Vector2> um, Mastil ma, float w, float h, float L)
    {
        Pisos(ma.alto, h, out int pisos, out float abajo, out float tramo);
        bool cofa = pisos >= 2;
        float yTope = h / 2f + ma.alto * 0.93f;
        float yCofa = cofa ? abajo + tramo + 0.15f : yTope;
        float rC = Mathf.Clamp(0.05f * ma.alto + 0.4f, 0.7f, 2.2f);
        int n = Mathf.Clamp(Mathf.RoundToInt(ma.alto / 7f) + 2, 3, 6);
        float grueso = Mathf.Clamp(0.035f + ma.alto * 0.0015f, 0.04f, 0.09f);
        float borda = CascoProcedural.AltoBorda(h);

        foreach (float s in new[] { -1f, 1f })
        {
            // Abajo: cadenas en el costado, repartidas un poco hacia popa del mástil
            var pie = new Vector3[n]; var cabeza = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float z = ma.z + 0.3f - i * 0.8f;
                float x = CascoProcedural.Costado(z, h / 2f, w, h, L) + 0.12f;
                pie[i] = new Vector3(s * x, h / 2f + borda * 0.8f, z);
                cabeza[i] = cofa ? new Vector3(s * rC * 0.85f, yCofa, ma.z - rC * 0.35f - i * 0.05f) : new Vector3(s * ma.grosor * 0.6f, yTope, ma.z);
                Linea(vj, uj, pie[i], cabeza[i], grueso);
            }
            Flechates(vj, uj, pie, cabeza, grueso * 0.7f);

            if (cofa)
            {
                // Obenques de arriba: del borde de la cofa al tope del mastelero
                int k = Mathf.Max(2, n - 1);
                var p2 = new Vector3[k]; var c2 = new Vector3[k];
                for (int i = 0; i < k; i++)
                {
                    p2[i] = new Vector3(s * rC * 0.95f, yCofa + 0.1f, ma.z - rC * 0.2f - i * 0.35f);
                    c2[i] = new Vector3(s * ma.grosor * 0.5f, yTope, ma.z - 0.05f * i);
                    Linea(vj, uj, p2[i], c2[i], grueso * 0.8f);
                }
                Flechates(vj, uj, p2, c2, grueso * 0.6f);
            }
        }

        if (!cofa) return;
        // Cofa: plataforma de tablones con baranda (corrida hacia popa para no chocar con las velas)
        float zc = ma.z - rC * 0.45f, ancho = 2f * rC, fondo = 1.5f * rC;
        Caja(vm, um, new Vector3(0f, yCofa, zc), new Vector3(ancho, 0.12f, fondo));
        Caja(vm, um, new Vector3(0f, yCofa - 0.12f, zc), new Vector3(ancho * 0.85f, 0.12f, fondo * 0.85f));
        float yb = yCofa + 0.75f;
        Vector3[] esquinas = { new Vector3(-ancho / 2f, 0, zc - fondo / 2f), new Vector3(ancho / 2f, 0, zc - fondo / 2f), new Vector3(ancho / 2f, 0, zc + fondo / 2f), new Vector3(-ancho / 2f, 0, zc + fondo / 2f) };
        for (int i = 0; i < 4; i++)
        {
            Vector3 a = esquinas[i], b = esquinas[(i + 1) % 4];
            Linea(vm, um, a + Vector3.up * yb, b + Vector3.up * yb, 0.07f);
            for (float t = 0f; t < 1f; t += 0.34f)
            {
                Vector3 p = Vector3.Lerp(a, b, t);
                Caja(vm, um, new Vector3(p.x, yCofa + 0.4f, p.z), new Vector3(0.06f, 0.7f, 0.06f));
            }
        }
    }

    // Flechates: travesaños horizontales entre los obenques, cada 45 cm (la escala para subir)
    static void Flechates(List<Vector3> v, List<Vector2> uv, Vector3[] pie, Vector3[] cabeza, float grueso)
    {
        int n = pie.Length;
        float y0 = pie[0].y + 0.45f, y1 = Mathf.Min(cabeza[0].y, cabeza[n - 1].y) - 0.35f;
        for (float y = y0; y < y1; y += 0.45f)
        {
            Vector3 a = EnAltura(pie[0], cabeza[0], y), b = EnAltura(pie[n - 1], cabeza[n - 1], y);
            Linea(v, uv, a, b, grueso);
        }
    }

    static Vector3 EnAltura(Vector3 a, Vector3 b, float y) => Vector3.Lerp(a, b, Mathf.InverseLerp(a.y, b.y, y));

    // ---------- Estays: de cada mástil hacia el de adelante, y del de proa al bauprés ----------
    static void Estays(List<Vector3> v, List<Vector2> uv, List<Mastil> mastiles, Vector3 puntaBaupres, float h)
    {
        for (int i = 0; i < mastiles.Count; i++)
        {
            Mastil a = mastiles[i];
            Vector3 tope = new Vector3(0f, h / 2f + a.alto * 0.9f, a.z);
            float g = Mathf.Clamp(0.05f + a.alto * 0.0015f, 0.05f, 0.1f);
            if (i < mastiles.Count - 1)
            {
                Mastil b = mastiles[i + 1];
                Linea(v, uv, tope, new Vector3(0f, h / 2f + b.alto * 0.45f, b.z), g);
            }
            else
            {
                Linea(v, uv, tope, puntaBaupres, g);
                Linea(v, uv, new Vector3(0f, h / 2f + a.alto * 0.6f, a.z), Vector3.Lerp(puntaBaupres, new Vector3(0f, h / 2f, puntaBaupres.z), 0.15f), g);
            }
            // Brandales: hacia atrás, a los costados (sostienen el mástil)
        }
    }

    // ---------- Cintas del casco ----------
    static void Cintas(List<Vector3> v, List<Vector2> uv, Transform t, float L, float w, float h)
    {
        var alturas = new List<float> { h / 2f - 0.15f };
        foreach (Transform hijo in t)
            if (hijo.name.StartsWith("Cañón babor") && hijo.localPosition.y < h / 2f)
            {
                float y = hijo.localPosition.y - 0.62f;
                bool nueva = true;
                foreach (float a in alturas) if (Mathf.Abs(a - y) < 0.1f) nueva = false;
                if (nueva) alturas.Add(y);
            }
        if (alturas.Count == 1 && h > 1.8f) alturas.Add(h / 2f - h * 0.35f);   // barcos sin portas: una cinta más abajo
        const float alto = 0.24f, sale = 0.07f;
        float z0 = CascoProcedural.ZPopa(L) + 0.2f, z1 = CascoProcedural.ZProa(w, L) - 0.4f;
        foreach (float y in alturas)
        {
            if (y < -h / 2f + 0.4f) continue;
            var zs = new List<float>();
            for (float z = z0; z < z1; z += 0.8f) zs.Add(z);
            zs.Add(z1);
            foreach (float s in new[] { -1f, 1f })
                for (int k = 0; k < zs.Count - 1; k++)
                {
                    float za = zs[k], zb = zs[k + 1];
                    float xa = CascoProcedural.Costado(za, y, w, h, L), xb = CascoProcedural.Costado(zb, y, w, h, L);
                    if (xa < 0.3f || xb < 0.3f) continue;
                    Vector3 a0 = new Vector3(s * (xa + sale), y - alto / 2f, za), b0 = new Vector3(s * (xb + sale), y - alto / 2f, zb);
                    Vector3 a1 = new Vector3(s * (xa + sale), y + alto / 2f, za), b1 = new Vector3(s * (xb + sale), y + alto / 2f, zb);
                    Vector3 ai0 = new Vector3(s * (xa - 0.02f), y - alto / 2f, za), bi0 = new Vector3(s * (xb - 0.02f), y - alto / 2f, zb);
                    Vector3 ai1 = new Vector3(s * (xa - 0.02f), y + alto / 2f, za), bi1 = new Vector3(s * (xb - 0.02f), y + alto / 2f, zb);
                    CascoProcedural.Cara(v, uv, a0, b0, b1, a1, new Vector3(s, 0f, 0f));
                    CascoProcedural.Cara(v, uv, a1, b1, bi1, ai1, Vector3.up);
                    CascoProcedural.Cara(v, uv, a0, b0, bi0, ai0, Vector3.down);
                }
        }
    }

    // ---------- Castillo de popa ----------
    // Devuelve el z del frente del castillo
    static float Castillo(List<Vector3> vm, List<Vector2> um, List<Vector3> vo, List<Vector2> uo, Transform t, float L, float w, float h, Astillero.Materiales m)
    {
        Transform cast = t.Find("Camarote");
        float zc = Cubierta.CentroCamarote(L), lc = Cubierta.LargoCamarote(L);
        float frente = zc + lc / 2f;
        if (cast == null) return frente;
        float cw = cast.localScale.x, yTop = h / 2f + Cubierta.AltoCamaroteDe(h);
        float x = cw / 2f - 0.05f, za = zc - lc / 2f - Cubierta.SalienteCamarote + 0.05f, zb = frente - 0.05f;

        // Baranda alrededor de la toldilla (arriba del castillo), con hueco al frente para las escaleras
        void Baranda(Vector3 a, Vector3 b)
        {
            Linea(vm, um, a + Vector3.up * 0.9f, b + Vector3.up * 0.9f, 0.09f);
            Linea(vm, um, a + Vector3.up * 0.08f, b + Vector3.up * 0.08f, 0.1f);
            float largo = Vector3.Distance(a, b);
            int n = Mathf.Max(2, Mathf.RoundToInt(largo / 0.45f));
            for (int i = 0; i <= n; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, (float)i / n);
                Caja(vm, um, p + Vector3.up * 0.48f, new Vector3(0.07f, 0.8f, 0.07f));
            }
        }
        Vector3 Y(float px, float pz) => new Vector3(px, yTop, pz);
        Baranda(Y(-x, za), Y(x, za));
        Baranda(Y(-x, za), Y(-x, zb));
        Baranda(Y(x, za), Y(x, zb));
        bool escaleras = cw / 2f - 0.45f > 1.2f;
        float hueco = escaleras ? cw / 2f - 0.95f : 0f;
        if (escaleras) { Baranda(Y(-x, zb), Y(-hueco - 0.05f, zb)); Baranda(Y(x, zb), Y(hueco + 0.05f, zb)); }
        else Baranda(Y(-x, zb), Y(x, zb));

        // Moldura dorada alrededor del borde de arriba y bajo las ventanas
        float yv = h / 2f + Cubierta.AltoCamaroteDe(h) * 0.55f;
        CascoProcedural.Caja(vo, uo, new Vector3(0f, yTop - 0.08f, za - 0.06f), new Vector3(cw + 0.1f, 0.16f, 0.06f), Quaternion.identity);
        CascoProcedural.Caja(vo, uo, new Vector3(0f, yv - 0.5f, za - 0.06f), new Vector3(cw + 0.1f, 0.1f, 0.06f), Quaternion.identity);
        foreach (float s in new[] { -1f, 1f })
            CascoProcedural.Caja(vo, uo, new Vector3(s * (cw / 2f + 0.03f), yTop - 0.08f, zc), new Vector3(0.06f, 0.16f, lc), Quaternion.identity);

        // Galería: balcón detrás de las ventanas de popa
        if (cw >= 2.5f)
        {
            float zg = zc - lc / 2f - 0.45f, gw = cw * 0.9f, yg = h / 2f + 0.35f;
            Caja(vm, um, new Vector3(0f, yg, zg), new Vector3(gw, 0.12f, 0.9f));
            foreach (float s in new[] { -0.35f, 0f, 0.35f })   // ménsulas
                Linea(vm, um, new Vector3(s * gw, yg - 0.05f, zg + 0.4f), new Vector3(s * gw, yg - 0.7f, zg + 0.42f), 0.12f);
            Baranda(new Vector3(-gw / 2f, yg, zg - 0.4f), new Vector3(gw / 2f, yg, zg - 0.4f));
            Baranda(new Vector3(-gw / 2f, yg, zg - 0.4f), new Vector3(-gw / 2f, yg, zg + 0.4f));
            Baranda(new Vector3(gw / 2f, yg, zg - 0.4f), new Vector3(gw / 2f, yg, zg + 0.4f));
        }

        // Escaleras de la cubierta a la toldilla (donde está el timón), una a cada lado del frente
        if (escaleras)
            foreach (float s in new[] { -1f, 1f })
            {
                float xe = s * (cw / 2f - 0.5f), alto = Cubierta.AltoCamaroteDe(h), largo = alto * 0.9f;
                int pasos = Mathf.RoundToInt(alto / 0.25f);
                for (int i = 1; i <= pasos; i++)
                {
                    float f = (float)i / pasos;
                    Caja(vm, um, new Vector3(xe, h / 2f + 0.1f + alto * f - 0.03f, frente + largo * (1f - f) + 0.05f), new Vector3(0.75f, 0.05f, largo / pasos + 0.06f));
                }
                Vector3 abajo = new Vector3(xe, h / 2f + 0.1f, frente + largo + 0.1f), arriba = new Vector3(xe, yTop, frente + 0.05f);
                foreach (float lx in new[] { -0.4f, 0.4f })
                {
                    Linea(vm, um, abajo + new Vector3(lx, 0.05f, 0f), arriba + new Vector3(lx, 0.05f, 0f), 0.08f);
                    Linea(vm, um, abajo + new Vector3(lx, 0.9f, 0f), arriba + new Vector3(lx, 0.9f, 0f), 0.06f);
                }
            }

        // Faroles en las esquinas de atrás
        if (Modelos.Existe("farol") && cw >= 2.5f)
        {
            float e = Mathf.Clamp(w / 4f, 1f, 2.5f) * 0.8f;
            foreach (float s in new[] { -1f, 1f })
                Rendimiento.Capa(Modelos.Crear("farol", t, new Vector3(s * (cw / 2f - 0.15f), yTop + 0.95f, za + 0.1f), Quaternion.identity, e,
                    n => n == "Vidrio" ? Modelos.DeColor(m.vela, new Color(1f, 0.78f, 0.35f), true) : m.canon), Rendimiento.CapaBarcoDetalle);
        }
        return frente;
    }

    // ---------- Mascarón de proa ----------
    static readonly string[] Mascarones = { "mascaron_leon", "mascaron_sirena", "mascaron_dragon", "mascaron_calavera" };
    static void Mascaron(Transform t, float L, float w, float h, Astillero.Materiales m, Material oro)
    {
        string nombre = Mascarones[Mathf.Abs(Mathf.RoundToInt(w * 10f)) % Mascarones.Length];
        PonerMascaron(t, nombre, L, w, h, oro, Modelos.DeColor(m.canon, new Color(0.12f, 0.1f, 0.08f)));
    }

    static GameObject PonerMascaron(Transform t, string nombre, float L, float w, float h, Material oro, Material oscuro)
    {
        if (!Modelos.Existe(nombre) || h < 1.5f) return null;
        float e = Mathf.Clamp(h / 4.5f, 0.6f, 2.2f);
        GameObject g = Modelos.Crear(nombre, t, new Vector3(0f, h * 0.3f, CascoProcedural.ZProa(w, L) - 0.35f * e), Quaternion.identity, e,
            n => n == "Detalle" ? oscuro : oro);
        Rendimiento.Capa(g, Rendimiento.CapaBarcoDetalle);
        return g;
    }

    // Cambia el mascarón de un barco ya armado ("ninguno" lo quita). Lo usa la pintura (Taller).
    public static void CambiarMascaron(Transform t, string nombre)
    {
        Transform actual = null;
        foreach (Transform hijo in t) if (hijo.name.StartsWith("mascaron_")) { actual = hijo; break; }
        if (actual != null && actual.name == nombre) return;
        if (actual == null && nombre == "ninguno") return;
        BoxCollider bc = t.GetComponent<BoxCollider>();
        if (bc == null || Cubierta.EsPlana(t.GetComponent<ControlBarco>())) return;
        Cubierta.Medidas(bc, out float w, out float h, out float L);
        Material baseMat = null;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) if (r.name == "Casco") { baseMat = r.sharedMaterial; break; }
        if (actual != null) Object.Destroy(actual.gameObject);
        if (nombre == "ninguno" || baseMat == null) return;
        PonerMascaron(t, nombre, L, w, h, Modelos.DeColor(baseMat, Oro), Modelos.DeColor(baseMat, new Color(0.12f, 0.1f, 0.08f)));
    }
}
