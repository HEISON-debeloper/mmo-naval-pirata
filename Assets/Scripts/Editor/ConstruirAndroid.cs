using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Pirata → Construir APK de Android: deja listos los ajustes del teléfono y arma el APK en la carpeta Builds
// del proyecto (Builds/MMO-Naval-Pirata-vX.apk). Solo pantalla horizontal, IL2CPP de 64 bits (lo piden los
// teléfonos nuevos), nombre del paquete propio y la versión del juego.
public static class ConstruirAndroid
{
    [MenuItem("Pirata/Construir APK de Android")]
    static void Construir()
    {
        Ajustar();
        // Cambiar los ajustes puede hacer que Unity recompile (y eso reinicia los scripts del editor): se anota
        // que hay un APK pendiente y se construye cuando Unity termine de compilar
        SessionState.SetBool(Pendiente, true);
        EditorApplication.delayCall += Esperar;
    }

    const string Pendiente = "Pirata.APKPendiente";

    [InitializeOnLoadMethod]
    static void AlRecargar()
    {
        // La empresa NO se cambia: en la PC la partida se guarda en una carpeta con su nombre
        // (LocalLow/DefaultCompany/MMO Naval Pirata) y cambiarla hacía que el juego no encontrara la partida
        if (PlayerSettings.companyName == "HEISON") PlayerSettings.companyName = "DefaultCompany";
        if (SessionState.GetBool(Pendiente, false)) EditorApplication.delayCall += Esperar;
    }

    static void Esperar()
    {
        if (!SessionState.GetBool(Pendiente, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Esperar; return; }
        SessionState.SetBool(Pendiente, false);
        Armar();
    }

    // Para GitHub (GameCI): arma el APK sin abrir el editor y termina con error si falla
    public static void DesdeConsola()
    {
        Ajustar();
        Armar();
        bool ok = File.Exists("Builds/ultimo-apk.txt") && File.ReadAllText("Builds/ultimo-apk.txt").StartsWith("Succeeded");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static void Armar()
    {
        Directory.CreateDirectory("Builds");
        string ruta = "Builds/MMO-Naval-Pirata-v" + Version.Numero + ".apk";
        var opciones = new BuildPlayerOptions
        {
            scenes = Escenas(),
            locationPathName = ruta,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };
        BuildReport r = BuildPipeline.BuildPlayer(opciones);
        string resumen = r.summary.result + " · " + (r.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB · " +
                         r.summary.totalTime.TotalMinutes.ToString("0.0") + " min · errores: " + r.summary.totalErrors;
        Debug.Log("APK: " + resumen + " → " + Path.GetFullPath(ruta) + " · shaders de IA descartados: " + QuitarShadersSentis.Quitados);
        File.WriteAllText("Builds/ultimo-apk.txt", resumen + "\n" + Path.GetFullPath(ruta));
        if (r.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(ruta);
    }

    [MenuItem("Pirata/Ajustes de Android (sin construir)")]
    static void Ajustar()
    {
        PlayerSettings.productName = "MMO Naval Pirata";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.heison.mmonavalpirata");
        PlayerSettings.bundleVersion = Version.Numero;
        string[] p = Version.Numero.Split('.');
        int codigo = int.Parse(p[0]) * 10000 + int.Parse(p[1]) * 100 + int.Parse(p[2]);   // 0.47.0 → 4700
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, codigo);
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Solo horizontal (el juego no está hecho para pantalla vertical)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        // Ritmo de cuadros parejo (Swappy): sin tirones aunque el juego vaya a 30
        PlayerSettings.Android.optimizedFramePacing = true;
        // Gestor de basura por partes: sin pausas largas cuando limpia memoria
        PlayerSettings.gcIncremental = true;
        // Vulkan primero y OpenGL ES 3 para los teléfonos viejos
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        // Texturas comprimidas para teléfono (ASTC) y código sin lo que no se usa
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        AssetDatabase.SaveAssets();
        Debug.Log("Ajustes de Android listos: " + PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android) + " v" + Version.Numero);
    }

    // El juego es una sola escena: PrototipoNaval (la lista de escenas traía la SampleScene de la plantilla)
    const string Escena = "Assets/Scenes/PrototipoNaval.unity";
    static string[] Escenas()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
        return new[] { Escena };
    }
}

// Pirata → Bot de prueba: le da Play con el bot jugando solo (F12 lo para; el informe queda en Builds/bot)
public static class MenuBotPrueba
{
    [MenuItem("Pirata/Bot de prueba (jugar solo por horas)")]
    static void Jugar()
    {
        if (EditorApplication.isPlaying)
        {
            if (BotPrueba.I != null && !BotPrueba.Activo) BotPrueba.I.Empezar();
            return;
        }
        SessionState.SetBool("Pirata.Bot", true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Pirata/Abrir informe del bot")]
    static void Informe()
    {
        string ruta = System.IO.Path.GetFullPath("Builds/bot/informe.txt");
        if (System.IO.File.Exists(ruta)) EditorUtility.OpenWithDefaultApp(ruta);
        else Debug.Log("Todavía no hay informe del bot (" + ruta + ")");
    }
}
