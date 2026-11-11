using UnityEditor;
using UnityEngine;

// Cómo se importan los sonidos del juego (Resources/Audio), sobre todo para el teléfono:
//   - Los ambientes largos que suenan en bucle (lluvia, viento, olas…) quedan comprimidos en memoria: descomprimidos
//     ocupaban unas 10 veces más.
//   - Los efectos cortos se descomprimen al cargar (no gastan procesador al sonar) y van en mono: suenan en 3D,
//     desde un punto, así que el estéreo no se nota y ocupan la mitad.
class AjustesAudio : AssetPostprocessor
{
    const int Version = 1;
    static readonly string[] Ambientes = { "lluvia", "viento", "olas", "gaviotas", "agua_casco", "fuego", "trueno", "hundimiento" };
    static readonly string[] Estereo = { "lluvia", "viento", "olas", "trueno" };   // suenan alrededor, no desde un punto

    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Audio/")) return;
        var imp = (AudioImporter)assetImporter;
        string grupo = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(assetPath));
        bool ambiente = System.Array.IndexOf(Ambientes, grupo) >= 0;
        imp.forceToMono = System.Array.IndexOf(Estereo, grupo) < 0;
        imp.loadInBackground = false;
        var s = imp.defaultSampleSettings;
        s.loadType = ambiente ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.5f;
        s.preloadAudioData = true;   // listo desde el principio (los bucles arrancan en un punto al azar)
        imp.defaultSampleSettings = s;
    }

    // Una vez por versión de estos ajustes: volver a importar los sonidos para que se apliquen
    [InitializeOnLoadMethod]
    static void Revisar()
    {
        if (EditorPrefs.GetInt("Pirata.AjustesAudio", 0) >= Version) return;
        EditorPrefs.SetInt("Pirata.AjustesAudio", Version);
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.IsValidFolder("Assets/Resources/Audio"))
                AssetDatabase.ImportAsset("Assets/Resources/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        };
    }
}
