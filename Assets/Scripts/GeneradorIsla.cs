using UnityEngine;

// Construye una isla low poly con sus recursos. Lo usan el editor (islas de la escena)
// y el mundo infinito (islas que aparecen al navegar). Con la misma semilla, la isla sale igual.
public static class GeneradorIsla
{
    [System.Serializable]
    public struct Materiales
    {
        public Material arena, pasto, roca, tronco, hojas, oxido, mineral, fibra, arbusto, fruta, coco, piedrita;
    }

    // Altura de la arena (playa en forma de domo) a una distancia d del centro de la isla
    public static float AlturaArena(float d, float r)
    {
        float k = d / (1.2f * r);
        return -1.5f + 3.5f * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k));   // la playa llega a 2 m sobre el mar
    }

    // inicial: la isla donde empiezas trae más cosas a mano y menos hierro
    public static Transform Crear(string nombre, Vector3 pos, float r, int semilla, bool inicial, Materiales m)
    {
        Random.State antes = Random.state;
        Random.InitState(semilla);

        GameObject isla = new GameObject(nombre);
        isla.transform.position = pos;
        Transform t = isla.transform;

        // Playa en forma de domo, 2 m sobre el mar; el mar se calma cerca de la orilla (componente Isla)
        Isla comp = isla.AddComponent<Isla>();
        comp.radio = r;
        comp.Refrescar();
        Pieza(PrimitiveType.Sphere, "Playa", t, new Vector3(0, -1.5f, 0), Quaternion.Euler(0, Random.value * 360f, 0), new Vector3(2.4f * r, 7f, 2.4f * r), m.arena, true);
        // Colina medio enterrada, en pendiente suave para poder subirla
        Vector3 colina = new Vector3((Random.value - 0.5f) * r * 0.3f, 2f - 0.1f * r, (Random.value - 0.5f) * r * 0.3f);
        Pieza(PrimitiveType.Sphere, "Colina", t, colina, Quaternion.identity, new Vector3(1.3f * r, 0.35f * r, 1.3f * r), m.pasto, true);

        float Suelo(float x, float z)
        {
            float arena = AlturaArena(Mathf.Sqrt(x * x + z * z), r);
            float kx = (x - colina.x) / (0.65f * r), kz = (z - colina.z) / (0.65f * r), k2 = kx * kx + kz * kz;
            float cerro = k2 < 1f ? colina.y + 0.175f * r * Mathf.Sqrt(1f - k2) : float.MinValue;
            return Mathf.Max(arena, cerro);
        }

        // Más grande la isla, más cosas
        float f = Mathf.Clamp(r / 42f, 0.5f, 2.2f);

        int rocas = Mathf.RoundToInt((3 + Random.Range(0, 4)) * f);
        for (int i = 0; i < rocas; i++)
        {
            float a = Random.value * Mathf.PI * 2f, d = r * (0.75f + Random.value * 0.2f), tam = 2f + Random.value * 3f;
            GameObject rk = Pieza(PrimitiveType.Cube, "Roca grande", t, new Vector3(Mathf.Cos(a) * d, AlturaArena(d, r), Mathf.Sin(a) * d), Random.rotation, Vector3.one * tam, m.roca, true);
            Recurso rr = rk.AddComponent<Recurso>();
            rr.tipo = Recurso.Tipo.RocaGrande; rr.usosMaximos = 3; rr.golpesPorUso = 4; rr.tiempoRegenerar = 300f;
        }

        int palmas = Mathf.RoundToInt((6 + Random.Range(0, 7)) * f);
        for (int i = 0; i < palmas; i++)
        {
            float a = Random.value * Mathf.PI * 2f, d = r * (0.4f + Random.value * 0.35f), alto = 6f + Random.value * 4f;
            GameObject palma = new GameObject("Palma");
            palma.transform.SetParent(t, false);
            float px = Mathf.Cos(a) * d, pz = Mathf.Sin(a) * d;
            palma.transform.localPosition = new Vector3(px, Suelo(px, pz) - 0.2f, pz);
            palma.transform.localRotation = Quaternion.Euler((Random.value - 0.5f) * 15f, Random.value * 360f, (Random.value - 0.5f) * 15f);
            Pieza(PrimitiveType.Cylinder, "Tronco", palma.transform, new Vector3(0, alto / 2, 0), Quaternion.identity, new Vector3(0.5f, alto / 2, 0.5f), m.tronco, true);
            Pieza(PrimitiveType.Sphere, "Hojas", palma.transform, new Vector3(0, alto, 0), Quaternion.identity, new Vector3(6f, 1.3f, 6f), m.hojas);
            Recurso rp = palma.AddComponent<Recurso>();
            rp.tipo = Recurso.Tipo.Palma; rp.usosMaximos = 1; rp.golpesPorUso = 5; rp.tiempoRegenerar = 240f;
        }

        // Vetas de hierro: pocas en la isla inicial, más en las otras para obligar a viajar
        int vetas = inicial ? 2 : Mathf.RoundToInt(4 * f);
        for (int i = 0; i < vetas; i++)
        {
            float a = Random.value * Mathf.PI * 2f, d = r * (0.6f + Random.value * 0.25f);
            float vx = Mathf.Cos(a) * d, vz = Mathf.Sin(a) * d;
            GameObject veta = Pieza(PrimitiveType.Cube, "Veta de hierro", t, new Vector3(vx, Suelo(vx, vz) + 0.3f, vz), Random.rotation, Vector3.one * 2.2f, m.oxido, true);
            for (int j = 0; j < 4; j++)
                Pieza(PrimitiveType.Cube, "Mineral", veta.transform, Random.insideUnitSphere * 0.45f, Random.rotation, Vector3.one * 0.3f, m.mineral);
            Recurso rh = veta.AddComponent<Recurso>();
            rh.tipo = Recurso.Tipo.Hierro; rh.usosMaximos = 3; rh.golpesPorUso = 5; rh.tiempoRegenerar = 300f;
        }

        // Cosas que se agarran a mano (F). La isla inicial tiene de sobra para las primeras herramientas.
        int n = inicial ? 14 : Mathf.RoundToInt(4 * f);
        for (int i = 0; i < n; i++)
        {
            Transform g = Grupo("Planta de fibra", t, Suelo, r, 0.3f, 0.85f);
            for (int j = 0; j < 5; j++)
                Pieza(PrimitiveType.Cube, "Hoja", g, new Vector3((Random.value - 0.5f) * 0.6f, 0.6f, (Random.value - 0.5f) * 0.6f),
                    Quaternion.Euler((Random.value - 0.5f) * 30f, Random.value * 180f, (Random.value - 0.5f) * 30f), new Vector3(0.08f, 1.2f, 0.25f), m.fibra);
            Recurso rf = g.gameObject.AddComponent<Recurso>();
            rf.tipo = Recurso.Tipo.Fibra; rf.usosMaximos = 4; rf.tiempoRegenerar = 120f;
        }
        n = inicial ? 10 : Mathf.RoundToInt(4 * f);
        for (int i = 0; i < n; i++)
        {
            Transform g = Grupo("Ramas", t, Suelo, r, 0.3f, 0.8f);
            Pieza(PrimitiveType.Cube, "Rama", g, new Vector3(0f, 0.08f, 0f), Quaternion.Euler(0f, Random.value * 180f, 0f), new Vector3(0.1f, 0.1f, 1.4f), m.tronco);
            Pieza(PrimitiveType.Cube, "Rama", g, new Vector3(0.2f, 0.1f, 0f), Quaternion.Euler(0f, Random.value * 180f, 0f), new Vector3(0.08f, 0.08f, 1.1f), m.tronco);
            Recurso rr2 = g.gameObject.AddComponent<Recurso>();
            rr2.tipo = Recurso.Tipo.Rama; rr2.usosMaximos = 1; rr2.tiempoRegenerar = 150f;
        }
        n = inicial ? 8 : Mathf.RoundToInt(3 * f);
        for (int i = 0; i < n; i++)
        {
            Transform g = Grupo("Piedra suelta", t, Suelo, r, 0.5f, 0.9f);
            Pieza(PrimitiveType.Cube, "Piedra", g, new Vector3(0f, 0.12f, 0f), Random.rotation, Vector3.one * (0.3f + Random.value * 0.15f), m.piedrita);
            Recurso rs = g.gameObject.AddComponent<Recurso>();
            rs.tipo = Recurso.Tipo.Piedra; rs.usosMaximos = 1; rs.tiempoRegenerar = 200f;
        }
        n = inicial ? 6 : Mathf.RoundToInt(2 * f);
        for (int i = 0; i < n; i++)
        {
            Transform g = Grupo("Arbusto de frutas", t, Suelo, r, 0.25f, 0.7f);
            Pieza(PrimitiveType.Sphere, "Follaje", g, new Vector3(0f, 0.7f, 0f), Quaternion.identity, new Vector3(1.8f, 1.4f, 1.8f), m.arbusto);
            for (int j = 0; j < 4; j++)
            {
                float fa = j * Mathf.PI / 2f + Random.value;
                Pieza(PrimitiveType.Sphere, "Fruta", g, new Vector3(Mathf.Cos(fa) * 0.8f, 0.8f + Random.value * 0.4f, Mathf.Sin(fa) * 0.8f), Quaternion.identity, Vector3.one * 0.22f, m.fruta);
            }
            Recurso ra = g.gameObject.AddComponent<Recurso>();
            ra.tipo = Recurso.Tipo.Arbusto; ra.usosMaximos = 3; ra.tiempoRegenerar = 180f;
        }
        n = inicial ? 5 : Mathf.RoundToInt(2 * f);
        for (int i = 0; i < n; i++)
        {
            Transform g = Grupo("Coco caído", t, Suelo, r, 0.4f, 0.8f);
            Pieza(PrimitiveType.Sphere, "Coco", g, new Vector3(0f, 0.15f, 0f), Quaternion.identity, Vector3.one * 0.3f, m.coco);
            Recurso rc = g.gameObject.AddComponent<Recurso>();
            rc.tipo = Recurso.Tipo.Coco; rc.usosMaximos = 1; rc.tiempoRegenerar = 240f;
        }
        // Mercader: siempre en la isla inicial y en más o menos una de cada tres islas (al final, para no cambiar lo demás)
        if (inicial || Random.value < 0.35f)
        {
            float am = Random.value * Mathf.PI * 2f, dm = r * 0.78f;
            float mx = Mathf.Cos(am) * dm, mz = Mathf.Sin(am) * dm;
            float haciaMar = Mathf.Atan2(mx, mz) * Mathf.Rad2Deg;   // el mercader mira al mar: llegas en barco y lo tienes de frente
            Mercader.Crear(t, new Vector3(mx, AlturaArena(dm, r), mz), haciaMar, nombre, semilla, m.tronco, m.fruta, m.arbusto);
        }

        // Capas para dejar de dibujar lo pequeño de lejos (ver Rendimiento)
        foreach (Recurso rec in t.GetComponentsInChildren<Recurso>())
        {
            bool grande = rec.tipo == Recurso.Tipo.Palma || rec.tipo == Recurso.Tipo.RocaGrande || rec.tipo == Recurso.Tipo.Hierro;
            Rendimiento.Capa(rec.gameObject, grande ? Rendimiento.CapaMedio : Rendimiento.CapaDetalle);
        }
        Random.state = antes;
        return t;
    }

    // Un objeto vacío apoyado en el suelo de la isla, a una distancia aleatoria del centro (fracción del radio)
    static Transform Grupo(string nombre, Transform isla, System.Func<float, float, float> suelo, float r, float dMin, float dMax)
    {
        float a = Random.value * Mathf.PI * 2f, d = r * (dMin + Random.value * (dMax - dMin));
        float x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d;
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(isla, false);
        g.transform.localPosition = new Vector3(x, suelo(x, z), z);
        return g.transform;
    }

    static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m, bool colision = false)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = escala;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        if (colision)
        {
            MeshCollider mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
        }
        return g;
    }
}
