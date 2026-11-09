using System.Collections.Generic;
using UnityEngine;

// Preferencias de la interfaz y del juego que se recuerdan entre partidas (PlayerPrefs, en JSON):
// escala y tamaño de texto, animaciones, contraste, transparencia, sonido, gráficos, qué partes del HUD se ven,
// posición de las ventanas, última categoría usada, recetas favoritas, zoom del mapa.
public static class Preferencias
{
    [System.Serializable]
    class Datos
    {
        public float escalaUI = 1f;
        public float tamanoTexto = 1f;
        public float animaciones = 1f;         // 0 = ninguna, 1 = normales
        public bool reducirMovimiento;
        public bool altoContraste;
        public float transparencia = 1f;       // 1 = paneles opacos como están diseñados, 0.5 = más transparentes
        public float volumen = 1f;
        public float volumenInterfaz = 0.8f;
        public bool sonidosInterfaz = true;
        public int calidad = -1;               // -1 = la del proyecto
        public float escalaRender = 1f;
        public bool sombras = true;
        public bool vsync = true;
        public int limiteFps = 0;              // 0 = sin límite
        public bool pantallaCompleta = true;
        public bool hudMochila = true, hudBarco = true, hudAtajos = true, hudBrujula = true, hudEnemigo = true;
        public float zoomMapa = 600f;
        public int categoriaFabricar;
        public List<string> favoritos = new List<string>();
        public List<string> clavesVentanas = new List<string>();
        public List<Vector2> posVentanas = new List<Vector2>();
        public int ordenInventario;            // 0 nombre, 1 cantidad, 2 categoría
        public bool filtroSoloPosibles;
        public bool vistoMenuPrincipal;
    }

    static Datos d;
    const string Clave = "Preferencias";
    static Datos D => d ?? (d = Cargar());

    static Datos Cargar()
    {
        try
        {
            string json = PlayerPrefs.GetString(Clave, "");
            if (!string.IsNullOrEmpty(json)) return JsonUtility.FromJson<Datos>(json) ?? new Datos();
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudieron leer las preferencias: " + e.Message); }
        return new Datos();
    }

    public static void Guardar()
    {
        PlayerPrefs.SetString(Clave, JsonUtility.ToJson(D));
        PlayerPrefs.Save();
        alCambiar?.Invoke();
    }

    public static event System.Action alCambiar;

    // ---------- Interfaz ----------
    public static float EscalaUI { get => D.escalaUI; set { D.escalaUI = Mathf.Clamp(value, 0.7f, 1.6f); } }
    public static float TamanoTexto { get => D.tamanoTexto; set { D.tamanoTexto = Mathf.Clamp(value, 0.8f, 1.4f); } }
    public static float Animaciones { get => D.animaciones; set { D.animaciones = Mathf.Clamp01(value); } }
    public static bool ReducirMovimiento { get => D.reducirMovimiento; set => D.reducirMovimiento = value; }
    public static bool AltoContraste { get => D.altoContraste; set => D.altoContraste = value; }
    public static float Transparencia { get => D.transparencia; set { D.transparencia = Mathf.Clamp(value, 0.4f, 1f); } }
    public static bool HudMochila { get => D.hudMochila; set => D.hudMochila = value; }
    public static bool HudBarco { get => D.hudBarco; set => D.hudBarco = value; }
    public static bool HudAtajos { get => D.hudAtajos; set => D.hudAtajos = value; }
    public static bool HudBrujula { get => D.hudBrujula; set => D.hudBrujula = value; }
    public static bool HudEnemigo { get => D.hudEnemigo; set => D.hudEnemigo = value; }
    public static bool VistoMenuPrincipal { get => D.vistoMenuPrincipal; set => D.vistoMenuPrincipal = value; }

    // ---------- Audio ----------
    public static float Volumen { get => D.volumen; set { D.volumen = Mathf.Clamp01(value); AudioListener.volume = D.volumen; } }
    public static float VolumenInterfaz { get => D.volumenInterfaz; set { D.volumenInterfaz = Mathf.Clamp01(value); } }
    public static bool SonidosInterfaz { get => D.sonidosInterfaz; set => D.sonidosInterfaz = value; }

    // ---------- Gráficos ----------
    public static int Calidad { get => D.calidad; set => D.calidad = value; }
    public static float EscalaRender { get => D.escalaRender; set { D.escalaRender = Mathf.Clamp(value, 0.5f, 1.5f); } }
    public static bool Sombras { get => D.sombras; set => D.sombras = value; }
    public static bool VSync { get => D.vsync; set => D.vsync = value; }
    public static int LimiteFps { get => D.limiteFps; set => D.limiteFps = value; }
    public static bool PantallaCompleta { get => D.pantallaCompleta; set => D.pantallaCompleta = value; }

    // ---------- Juego ----------
    public static float ZoomMapa { get => D.zoomMapa; set => D.zoomMapa = value; }
    public static int CategoriaFabricar { get => D.categoriaFabricar; set => D.categoriaFabricar = value; }
    public static int OrdenInventario { get => D.ordenInventario; set => D.ordenInventario = value; }
    public static bool FiltroSoloPosibles { get => D.filtroSoloPosibles; set => D.filtroSoloPosibles = value; }

    public static bool EsFavorito(string receta) => D.favoritos.Contains(receta);
    public static void AlternarFavorito(string receta)
    {
        if (!D.favoritos.Remove(receta)) D.favoritos.Add(receta);
        Guardar();
    }

    // Dónde quedó cada ventana la última vez (clave = nombre de la ventana)
    public static bool PosicionVentana(string clave, out Vector2 pos)
    {
        int i = D.clavesVentanas.IndexOf(clave);
        if (i >= 0 && i < D.posVentanas.Count) { pos = D.posVentanas[i]; return true; }
        pos = Vector2.zero;
        return false;
    }

    public static void GuardarPosicionVentana(string clave, Vector2 pos)
    {
        int i = D.clavesVentanas.IndexOf(clave);
        if (i >= 0) D.posVentanas[i] = pos;
        else { D.clavesVentanas.Add(clave); D.posVentanas.Add(pos); }
        PlayerPrefs.SetString(Clave, JsonUtility.ToJson(D));
    }

    // Aplica al juego lo que no depende de la interfaz (volumen, gráficos)
    public static void AplicarAlJuego()
    {
        AudioListener.volume = D.volumen;
        if (D.calidad >= 0 && D.calidad < QualitySettings.names.Length) QualitySettings.SetQualityLevel(D.calidad, true);
        QualitySettings.vSyncCount = D.vsync ? 1 : 0;
        Application.targetFrameRate = D.limiteFps > 0 ? D.limiteFps : -1;
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = D.escalaRender;
            urp.shadowDistance = D.sombras ? Rendimiento.DistanciaSombras : 0f;   // la misma distancia que fija Rendimiento
        }
        if (!Application.isMobilePlatform && Screen.fullScreen != D.pantallaCompleta) Screen.fullScreen = D.pantallaCompleta;
    }
}
