using UnityEngine;
using UnityEngine.InputSystem;

// Modo creativo para probar sin farmear: se activa o desactiva con F2 (se recuerda entre partidas).
//  · Fabricar y mejorar no cuesta nada (ni horno hace falta)
//  · Tienes hacha y pico sin fabricarlos
//  · Sin hambre ni sed, y ni tú ni tu barco reciben daño
// Los enemigos sí reciben daño, para poder probar cañones y espada.
public class ModoCreativo : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear()
    {
        // El modo creativo es una herramienta de prueba: en el juego publicado no existe
        if (!Application.isEditor && !Debug.isDebugBuild) { Activo = false; return; }
        Activo = PlayerPrefs.GetInt("ModoCreativo", 0) == 1;
        new GameObject("Modo creativo").AddComponent<ModoCreativo>();
    }

    public static bool Activo { get; private set; }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || !Teclas.Pulsada(Accion.Creativo)) return;
        Activo = !Activo;
        PlayerPrefs.SetInt("ModoCreativo", Activo ? 1 : 0);
        PlayerPrefs.Save();
        Personaje p = Personaje.Instancia;
        Notificaciones.Mostrar(Activo ? "Modo creativo activado: todo gratis, sin daño ni hambre (" + Teclas.Nombre(Accion.Creativo) + " para quitarlo)" : "Modo creativo desactivado", Notificaciones.Tipo.Info, 4f, "rayo");
    }
}
