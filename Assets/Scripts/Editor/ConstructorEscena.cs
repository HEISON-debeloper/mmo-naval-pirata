using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú "Pirata": arma toda la escena del prototipo con formas low poly.
// · Crear escena (manos vacías): empiezas sin nada en una playa de la isla inicial, como en el juego real.
// · Crear escena de prueba (con balandro): empiezas al timón de un balandro, para probar combate naval.
public static class ConstructorEscena
{
    const string CarpetaMat = "Assets/Materiales";
    const string RutaEscena = "Assets/Scenes/PrototipoNaval.unity";

    [MenuItem("Pirata/Crear escena (manos vacías)")]
    public static void CrearManosVacias() => Crear(false);

    [MenuItem("Pirata/Crear escena de prueba (con balandro)")]
    public static void CrearConBalandro() => Crear(true);

    static void Crear(bool conBalandro)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Cielo y niebla
        Color cielo = Hex("9FD3E0");
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = cielo;
        RenderSettings.fogStartDistance = 180f;
        RenderSettings.fogEndDistance = 480f;

        GameObject luz = GameObject.Find("Directional Light");
        if (luz != null) luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

        // Barcos a escala real
        GameObject jugador = null;
        if (conBalandro)
            jugador = Astillero.CrearBarco("Balandro (jugador)", 18f, new Vector3(0f, 1f, 0f), 0f,
                MatsBarco("CascoJugador", "6B4226", "VelaCrema", "F3EAD2", "BanderaRoja", "C0392B"), true, 6);
        // Sin enemigos junto a la isla inicial: los piratas los crea el mundo infinito fuera de la zona segura

        // Islas
        Vector3 posInicial = new Vector3(0f, 0f, 140f);
        const float radioInicial = 42f;
        Transform islaInicial = CrearIsla("Isla inicial", posInicial, radioInicial, 11, true);
        CrearIsla("Isla grande", new Vector3(-260f, 0f, 320f), 60f, 22, false);
        CrearIsla("Isla pequeña", new Vector3(300f, 0f, -200f), 32f, 33, false);

        // Mundo infinito: crea islas nuevas alrededor mientras navegas
        GameObject mundo = new GameObject("Mundo infinito");
        MundoInfinito mi = mundo.AddComponent<MundoInfinito>();
        mi.materiales = MatsIsla();
        mi.materialesEnemigo = MatsBarco("CascoBlanco", "D9D4C5", "VelaBlanca", "FFFFFF", "BanderaNegra", "1D1D1D");
        mi.maderaBalsa = Mat("Tronco", "8B6A43");
        mi.lonaBalsa = Mat("VelaBlanca", "FFFFFF");
        mi.tripulante = Mat("CamisaRoja", "B8322A");
        mi.espada = Mat("Acero", "C9CED3", 0.6f);

        // Cámara y personaje
        Camera cam = Camera.main;
        GameObject pirata = null;
        if (cam != null)
        {
            cam.farClipPlane = 1500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cielo;
            cam.transform.position = posInicial + new Vector3(0f, 20f, -80f);
            cam.gameObject.AddComponent<CamaraBarco>();
            pirata = CrearPersonaje(jugador, cam, islaInicial, radioInicial);
            cam.GetComponent<CamaraBarco>().objetivo = jugador != null ? jugador.transform : pirata.transform;
        }

        // Mar: sigue al personaje para que siempre haya agua alrededor
        GameObject mar = new GameObject("Mar");
        mar.AddComponent<MeshFilter>();
        mar.AddComponent<MeshRenderer>().sharedMaterial = Mat("Agua", "2E8A99", 0.75f);
        mar.AddComponent<AguaOlas>().seguir = pirata != null ? pirata.transform : null;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaEscena);
        Selection.activeGameObject = pirata;
        Debug.Log("Escena creada en " + RutaEscena + (conBalandro ? " (con balandro)" : " (manos vacías)") + ". Dale Play.");
    }

    // ---------- Barco (la construcción está en Astillero.cs, compartida con el juego) ----------
    static Astillero.Materiales MatsBarco(string casco, string hexCasco, string vela, string hexVela, string bandera, string hexBandera) =>
        new Astillero.Materiales
        {
            casco = Mat(casco, hexCasco), vela = Mat(vela, hexVela), bandera = Mat(bandera, hexBandera),
            cubierta = Mat("Cubierta", "B08A5A"), mastil = Mat("Mastil", "6E4F2E"), canon = Mat("Canon", "2B2B2B")
        };

    // ---------- Personaje (cápsula de 1.8 m) ----------
    static GameObject CrearPersonaje(GameObject barco, Camera cam, Transform islaInicio, float radioIsla)
    {
        GameObject p = new GameObject("Pirata (jugador)");
        p.transform.position = barco != null ? barco.transform.position + Vector3.up * 3f
                                             : islaInicio.position + new Vector3(0f, 3f, -radioIsla * 0.72f);
        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.4f;
        cc.slopeLimit = 50f;

        Material piel = Mat("Piel", "E0B48A");
        Pieza(PrimitiveType.Capsule, "Cuerpo", p.transform, new Vector3(0, 0.9f, 0), Quaternion.identity, new Vector3(0.6f, 0.9f, 0.6f), Mat("Camisa", "2F6FB0"));
        Pieza(PrimitiveType.Cube, "Nariz (mira al frente)", p.transform, new Vector3(0, 1.55f, 0.3f), Quaternion.identity, new Vector3(0.12f, 0.08f, 0.14f), piel);
        Pieza(PrimitiveType.Cylinder, "Sombrero", p.transform, new Vector3(0, 1.82f, 0), Quaternion.identity, new Vector3(0.7f, 0.03f, 0.7f), Mat("Sombrero", "1D1D1D"));

        GameObject espada = new GameObject("Espada");
        espada.transform.SetParent(p.transform, false);
        espada.transform.localPosition = new Vector3(0.38f, 1.15f, 0.1f);
        Pieza(PrimitiveType.Cube, "Hoja", espada.transform, new Vector3(0, 0, 0.45f), Quaternion.identity, new Vector3(0.06f, 0.06f, 0.9f), Mat("Acero", "C9CED3", 0.6f));
        Pieza(PrimitiveType.Cube, "Guarda", espada.transform, Vector3.zero, Quaternion.identity, new Vector3(0.25f, 0.06f, 0.06f), Mat("Tronco", "8B6A43"));

        p.AddComponent<Inventario>();
        Personaje pj = p.AddComponent<Personaje>();
        pj.barco = barco != null ? barco.GetComponent<ControlBarco>() : null;
        pj.islaInicio = islaInicio;
        pj.radioIslaInicio = radioIsla;
        pj.camara = cam.GetComponent<CamaraBarco>();
        pj.espada = espada.transform;
        pj.materialMadera = Mat("Tronco", "8B6A43");
        pj.materialLona = Mat("VelaCrema", "F3EAD2");
        p.AddComponent<Supervivencia>();
        Fabricacion fab = p.AddComponent<Fabricacion>();
        fab.piedra = Mat("Roca", "8A8F8C");
        fab.brasa = Mat("Brasa", "E0652B");
        fab.cascoBalandro = Mat("CascoJugador", "6B4226");
        fab.velaBalandro = Mat("VelaCrema", "F3EAD2");
        fab.banderaBalandro = Mat("BanderaRoja", "C0392B");
        fab.cubierta = Mat("Cubierta", "B08A5A");
        fab.mastil = Mat("Mastil", "6E4F2E");
        fab.hierroCanon = Mat("Canon", "2B2B2B");
        return p;
    }

    // ---------- Isla (la construcción está en GeneradorIsla.cs, compartida con el mundo infinito) ----------
    static GeneradorIsla.Materiales MatsIsla() => new GeneradorIsla.Materiales
    {
        arena = Mat("Arena", "E3CF95"), pasto = Mat("Pasto", "4F9A4A"), roca = Mat("Roca", "8A8F8C"),
        tronco = Mat("Tronco", "8B6A43"), hojas = Mat("Hojas", "3F8A3A"),
        oxido = Mat("VetaHierro", "8E4B32"), mineral = Mat("MineralHierro", "3B3533"),
        fibra = Mat("Fibra", "7FA845"), arbusto = Mat("Arbusto", "2F6B33"), fruta = Mat("FrutaRoja", "D2402E"),
        coco = Mat("Coco", "5B4027"), piedrita = Mat("PiedraSuelta", "9A9E9B")
    };

    static Transform CrearIsla(string nombre, Vector3 pos, float r, int semilla, bool inicial) =>
        GeneradorIsla.Crear(nombre, pos, r, semilla, inicial, MatsIsla());

    // ---------- Utilidades ----------
    static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m, bool colision = false)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        if (colision)
        {
            MeshCollider mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
        }
        return g;
    }

    static Material Mat(string nombre, string hex, float brillo = 0.1f)
    {
        Directory.CreateDirectory(CarpetaMat);
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        Color c = Hex(hex);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
