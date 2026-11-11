using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Origen flotante. Los números de Unity (float) pierden precisión lejos del centro del mundo: a varios kilómetros
// los barcos, la cámara y los personajes empiezan a temblar. Cuando te alejas más de 1 km del centro, todo el mundo
// se mueve de golpe (en saltos de 1 km) para que tú vuelvas cerca del (0, 0, 0). No se nota: se mueve todo junto.
//
// Desplazamiento guarda cuánto se ha corrido el mundo. Lo que necesita la posición "de verdad" (las olas, las celdas
// del mundo infinito, las zonas de clima y de peligro, el guardado) suma Desplazamiento. Lo que guarda posiciones
// por su cuenta (islas descubiertas, marcas, pecios, rutas de los piratas) se corre con AlMover.
[DefaultExecutionOrder(-500)]
public class OrigenFlotante : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() { if (I == null) new GameObject("Origen flotante").AddComponent<OrigenFlotante>(); }

    public static OrigenFlotante I { get; private set; }
    public static Vector3 Desplazamiento { get; private set; }   // solo x y z, en múltiplos de Salto
    public static event System.Action<Vector3> AlMover;           // cuánto se corrió el mundo (hay que restarlo)

    const float Limite = 1000f, Salto = 1000f;
    static Scene persistente;
    static bool hayPersistente;

    public static Vector3 AMundo(Vector3 local) => local + Desplazamiento;
    public static Vector3 ALocal(Vector3 mundo) => mundo - Desplazamiento;

    static readonly int idDesp = Shader.PropertyToID("_OlasDesp");

    void Awake() { I = this; Shader.SetGlobalVector(idDesp, Vector4.zero); }
    void OnDestroy() { if (I == this) I = null; }

    // Al empezar o recargar la escena todo vuelve al centro
    public static void Reiniciar() => Desplazamiento = Vector3.zero;   // el shader se pone en cero al crear el objeto

    void LateUpdate()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Vector3 pos = p.transform.position;
        if (Mathf.Abs(pos.x) < Limite && Mathf.Abs(pos.z) < Limite) return;
        Vector3 corrida = new Vector3(Mathf.Round(pos.x / Salto) * Salto, 0f, Mathf.Round(pos.z / Salto) * Salto);
        Correr(corrida);
    }

    // Mueve todo el mundo -corrida (y suma corrida al desplazamiento)
    public static void Correr(Vector3 corrida)
    {
        if (corrida == Vector3.zero) return;
        var raices = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded) s.GetRootGameObjects(raices);
            foreach (GameObject g in raices) Mover(g.transform, corrida);
            raices.Clear();
        }
        // Lo que vive en DontDestroyOnLoad (los sonidos, etc.): esa escena no aparece en la lista de arriba
        if (!hayPersistente)
        {
            var prueba = new GameObject("prueba");
            DontDestroyOnLoad(prueba);
            persistente = prueba.scene;
            DestroyImmediate(prueba);
            hayPersistente = true;
        }
        if (persistente.IsValid())
        {
            persistente.GetRootGameObjects(raices);
            foreach (GameObject g in raices) Mover(g.transform, corrida);
        }
        Physics.SyncTransforms();

        Desplazamiento += corrida;
        Shader.SetGlobalVector(idDesp, new Vector4(Desplazamiento.x, 0f, Desplazamiento.z, 0f));
        foreach (Isla i in Isla.Todas) if (i != null) i.Refrescar();
        Olas.IslasCambiaron = true;
        AlMover?.Invoke(corrida);
    }

    static void Mover(Transform t, Vector3 corrida)
    {
        if (t == null) return;
        // Las partículas que viven en coordenadas del mundo también se corren
        foreach (ParticleSystem ps in t.GetComponentsInChildren<ParticleSystem>())
            if (ps.main.simulationSpace == ParticleSystemSimulationSpace.World) Correr(ps, corrida);
        Rigidbody rb = t.GetComponent<Rigidbody>();
        t.position -= corrida;
        if (rb != null) rb.position = t.position;
    }

    static readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[4000];
    static void Correr(ParticleSystem ps, Vector3 corrida)
    {
        int n = ps.GetParticles(buffer);
        for (int i = 0; i < n; i++) buffer[i].position -= corrida;
        ps.SetParticles(buffer, n);
    }
}
