using UnityEditor;

// Ajustes del editor para este proyecto.
// Unity, por defecto, recompila los scripts aunque el juego esté corriendo y eso borra datos en memoria
// (por ejemplo, la lista de recetas del menú de fabricación). Aquí se pide recompilar solo al detener Play.
public static class ConfiguracionEditor
{
    [InitializeOnLoadMethod]
    static void Aplicar()
    {
        // 0 = recompilar y seguir jugando · 1 = recompilar al terminar de jugar · 2 = detener Play y recompilar
        if (EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0) != 1)
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", 1);
    }
}
