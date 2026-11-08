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
    static void Crear()
    {
        Activo = PlayerPrefs.GetInt("ModoCreativo", 0) == 1;
        new GameObject("Modo creativo").AddComponent<ModoCreativo>();
    }

    public static bool Activo { get; private set; }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || !k.f2Key.wasPressedThisFrame) return;
        Activo = !Activo;
        PlayerPrefs.SetInt("ModoCreativo", Activo ? 1 : 0);
        PlayerPrefs.Save();
        Personaje p = Personaje.Instancia;
        if (p != null) p.Avisar(Activo ? "Modo creativo activado: todo gratis, sin daño ni hambre (F2 para quitarlo)" : "Modo creativo desactivado");
    }
}
