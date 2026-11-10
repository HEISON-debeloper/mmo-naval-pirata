using UnityEngine;
using UnityEngine.SceneManagement;

// Los sistemas del juego (interfaz, HUD, guardado, controles táctiles, navegación, taller…) se crean solos al abrir el juego
// con [RuntimeInitializeOnLoadMethod], pero eso solo corre UNA vez. "Nueva partida" recarga la escena y los borra:
// aquí, un cuadro después de cargar cada escena, se vuelven a crear los que falten.
public static class ArranqueEscena
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Preparar()
    {
        Olvidar();
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;
        Olvidar();   // lo que se recuerda del mundo lo vuelve a poner el guardado (o nada, en una partida nueva)
        new GameObject("Revisar sistemas").AddComponent<Revisar>();
    }

    static void Olvidar()
    {
        MundoInfinito.Limpias.Clear();
        Mercader.Recordados.Clear();
    }

    class Revisar : MonoBehaviour
    {
        int cuadros;
        void Update()
        {
            if (++cuadros < 2) return;   // que corran primero los [RuntimeInitializeOnLoadMethod] del arranque
            Destroy(gameObject);
            if (Lienzo.I != null) return;   // todo sigue en pie (la primera carga)
            Lienzo.Crear();
            MenuPrincipal.AlCargar();
            if (Falta<HudNuevo>()) HudNuevo.Crear();
            if (Falta<ControlesTactiles>()) ControlesTactiles.Crear();
            if (Falta<Guardado>()) Guardado.Crear();
            if (Falta<Navegacion>()) Navegacion.Crear();
            if (Falta<Punteria>()) Punteria.Crear();
            if (Falta<Rendimiento>()) Rendimiento.Crear();
            if (Falta<Taller>()) Taller.Crear();
            if (Falta<VelasSinEstorbo>()) VelasSinEstorbo.Crear();
            if (GameObject.Find("Recursos que crecen") == null) Recurso.CrearActualizador();
            if (Falta<ModoCreativo>()) ModoCreativo.Crear();
            if (Falta<TeclasPrueba>()) TeclasPrueba.Crear();
            CicloDia.Crear();
            Clima.Crear();
            Pecios.Crear();
        }
        static bool Falta<T>() where T : Object => Object.FindFirstObjectByType<T>() == null;
    }
}
