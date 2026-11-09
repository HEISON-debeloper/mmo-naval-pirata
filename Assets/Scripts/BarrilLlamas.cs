using UnityEngine;

// Barril en llamas: se suelta por la popa, flota detrás del barco y explota cuando lo toca otro barco
// (o una bala de cañón). Daña a todos los barcos cerca de la explosión, menos al que lo soltó.
public class BarrilLlamas : MonoBehaviour
{
    public const float Dano = 55f;
    public const float Radio = 7f;
    const float Vida = 60f, Armado = 1.2f;

    VidaBarco dueno;
    Rigidbody rb;
    Light luz;
    Transform[] llamas;
    float edad;
    bool explotado;

    public static BarrilLlamas Soltar(Vector3 pos, Quaternion rot, Vector3 velocidad, VidaBarco dueno, Material madera, Material hierro)
    {
        GameObject g = new GameObject("Barril en llamas");
        g.transform.SetPositionAndRotation(pos, rot);
        if (Modelos.Existe("barril"))
            Modelos.Crear("barril", g.transform, new Vector3(0f, -0.45f, 0f), Quaternion.identity, 1.1f, n => n == "Aros" ? hierro : madera);
        else
            Astillero.Pieza(PrimitiveType.Cylinder, "Barril", g.transform, Vector3.zero, Quaternion.identity, new Vector3(0.7f, 0.5f, 0.7f), madera);

        CapsuleCollider cc = g.AddComponent<CapsuleCollider>();
        cc.radius = 0.4f; cc.height = 1f;
        SphereCollider sensor = g.AddComponent<SphereCollider>();   // lo que lo hace explotar
        sensor.isTrigger = true; sensor.radius = 1.4f;

        Rigidbody rb = g.AddComponent<Rigidbody>();
        rb.mass = 80f; rb.linearDamping = 0.6f; rb.angularDamping = 1.5f;
        rb.linearVelocity = velocidad;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BarrilLlamas b = g.AddComponent<BarrilLlamas>();
        b.dueno = dueno;
        if (dueno != null)
            foreach (Collider c in dueno.GetComponentsInChildren<Collider>()) { Physics.IgnoreCollision(cc, c); Physics.IgnoreCollision(sensor, c); }
        b.Fuego();
        return b;
    }

    void Awake() => rb = GetComponent<Rigidbody>();

    // Llamas: conos que titilan y una luz anaranjada
    void Fuego()
    {
        Material fuego = Modelos.DeColor(MaterialBase(), new Color(1f, 0.45f, 0.08f), true);
        Material centro = Modelos.DeColor(MaterialBase(), new Color(1f, 0.85f, 0.3f), true);
        llamas = new Transform[4];
        for (int i = 0; i < llamas.Length; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            Vector3 p = i == 0 ? new Vector3(0f, 0.55f, 0f) : new Vector3(Mathf.Cos(a) * 0.18f, 0.5f, Mathf.Sin(a) * 0.18f);
            GameObject l = Astillero.Pieza(PrimitiveType.Sphere, "Llama", transform, p, Quaternion.identity, new Vector3(0.3f, 0.6f, 0.3f), i == 0 ? centro : fuego);
            l.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            llamas[i] = l.transform;
        }
        GameObject gl = new GameObject("Luz");
        gl.transform.SetParent(transform, false);
        gl.transform.localPosition = new Vector3(0f, 1f, 0f);
        luz = gl.AddComponent<Light>();
        luz.type = LightType.Point; luz.color = new Color(1f, 0.55f, 0.2f); luz.range = 9f; luz.intensity = 3f;
    }

    static Material baseMat;
    static Material MaterialBase()
    {
        if (baseMat == null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = g.GetComponent<Renderer>().sharedMaterial;
            Destroy(g);
        }
        return baseMat;
    }

    void Update()
    {
        edad += Time.deltaTime;
        float t = Time.time * 12f;
        for (int i = 0; i < llamas.Length; i++)
        {
            float f = 0.8f + 0.35f * Mathf.PerlinNoise(t + i * 7.3f, i);
            llamas[i].localScale = new Vector3(0.3f, 0.6f * f, 0.3f) * (i == 0 ? 1.2f : 0.9f);
            llamas[i].rotation = Quaternion.identity;   // las llamas siempre hacia arriba aunque el barril se mueva
        }
        if (luz != null) luz.intensity = 2.5f + Mathf.PerlinNoise(t, 3f) * 1.5f;
        if (edad > Vida) Apagar();
    }

    // Flota: empuje hacia arriba cuando está bajo el agua
    void FixedUpdate()
    {
        float agua = Olas.Altura(transform.position);
        float hundido = agua - (transform.position.y - 0.2f);
        if (hundido > 0f)
        {
            rb.AddForce(Vector3.up * (Mathf.Min(hundido, 1f) * 25f - rb.linearVelocity.y * 2f), ForceMode.Acceleration);
            rb.AddForce(-rb.linearVelocity * 0.8f, ForceMode.Acceleration);
            // que se quede parado (las llamas arriba)
            Vector3 eje = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(eje * 8f - rb.angularVelocity * 2f, ForceMode.Acceleration);
        }
    }

    void OnTriggerEnter(Collider c) => Tocar(c);
    void OnCollisionEnter(Collision c) => Tocar(c.collider);

    void Tocar(Collider c)
    {
        if (explotado || edad < Armado) return;
        if (c.GetComponentInParent<BalaCanon>() != null) { Explotar(); return; }
        VidaBarco v = c.GetComponentInParent<VidaBarco>();
        if (v != null && v != dueno && !v.Hundido) Explotar();
    }

    public void Explotar()
    {
        if (explotado) return;
        explotado = true;
        Vector3 centro = transform.position;
        var tocados = new System.Collections.Generic.HashSet<VidaBarco>();
        foreach (Collider c in Physics.OverlapSphere(centro, Radio, ~0, QueryTriggerInteraction.Ignore))
        {
            VidaBarco v = c.GetComponentInParent<VidaBarco>();
            if (v == null || v == dueno || !tocados.Add(v)) continue;
            Vector3 cerca = c.ClosestPoint(centro);
            float f = 1f - Mathf.Clamp01(Vector3.Distance(cerca, centro) / Radio) * 0.6f;
            v.Dano(Dano * f);
            Rigidbody r = v.GetComponent<Rigidbody>();
            if (r != null) r.AddExplosionForce(r.mass * 4f, centro, Radio * 2f, 0.5f, ForceMode.Impulse);
            Personaje p = Personaje.Instancia;
            if (p != null && p.barco != null && dueno != null && dueno.gameObject == p.barco.gameObject)
                p.Avisar("¡Barril en llamas! " + Mathf.RoundToInt(Dano * f) + " de daño");
        }
        // Barriles cercanos explotan en cadena
        foreach (Collider c in Physics.OverlapSphere(centro, Radio * 0.6f, ~0, QueryTriggerInteraction.Collide))
        {
            BarrilLlamas otro = c.GetComponent<BarrilLlamas>();
            if (otro != null && otro != this) otro.Invoke(nameof(Explotar), 0.15f);
        }
        Explosion.Crear(centro, Radio);
        Destroy(gameObject);
    }

    void Apagar() => Destroy(gameObject);
}

// Bola de fuego que crece y se apaga en medio segundo, con un destello de luz
public class Explosion : MonoBehaviour
{
    float t, radio;
    Renderer r;
    Light luz;
    Material mat;

    public static void Crear(Vector3 pos, float radio)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "Explosión";
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.position = pos;
        Explosion e = g.AddComponent<Explosion>();
        e.radio = radio;
        e.r = g.GetComponent<Renderer>();
        e.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        e.mat = new Material(e.r.sharedMaterial);
        e.r.sharedMaterial = e.mat;
        GameObject gl = new GameObject("Destello");
        gl.transform.SetParent(g.transform, false);
        e.luz = gl.AddComponent<Light>();
        e.luz.type = LightType.Point; e.luz.color = new Color(1f, 0.6f, 0.25f); e.luz.range = radio * 4f;
    }

    void Update()
    {
        t += Time.deltaTime / 0.6f;
        float k = Mathf.Clamp01(t);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, radio * 1.6f, Mathf.Sqrt(k));
        Color c = Color.Lerp(new Color(1f, 0.9f, 0.4f), new Color(0.25f, 0.2f, 0.18f), k);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * (1f - k) * 3f); }
        luz.intensity = 8f * (1f - k);
        if (t >= 1f) Destroy(gameObject);
    }
}
