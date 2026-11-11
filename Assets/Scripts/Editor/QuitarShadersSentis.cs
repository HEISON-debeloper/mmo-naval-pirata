using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

// El paquete de IA de Unity (com.unity.ai.inference, "Sentis") trae cientos de shaders que el juego no usa:
// en la compilación de Android tardaban más de 20 minutos y engordaban el APK. Aquí se descartan antes de
// compilarlos. El paquete se queda instalado (lo usan las herramientas de IA del editor).
class QuitarShadersSentis : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder => 0;

    static int quitados;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader == null || !shader.name.StartsWith("Hidden/Sentis")) return;
        quitados += data.Count;
        data.Clear();
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (shader == null) return;
        string ruta = AssetDatabase.GetAssetPath(shader);
        if (ruta == null || !ruta.Contains("com.unity.ai.inference")) return;
        quitados += data.Count;
        data.Clear();
    }

    // Para saber cuántos se ahorraron (sale en la consola al terminar el APK)
    public static int Quitados { get { int q = quitados; quitados = 0; return q; } }
}
