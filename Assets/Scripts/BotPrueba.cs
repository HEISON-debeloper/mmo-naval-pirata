using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Bot de prueba: juega solo durante horas y anota todo lo que sale mal.
// Solo existe en el editor o en una compilación de desarrollo. Se prende y apaga con F12
// (o desde el menú Pirata → Bot de prueba, que además le da Play).
//
// Qué hace, en vueltas:
//   - Navega tu barco hacia puntos lejanos (así se generan islas nuevas, corre el origen flotante y cambian las zonas).
//   - Cada tanto trae un pirata, le dispara con los dos costados y a veces lo hunde (botín, pecios, cajas).
//   - Cambia el clima y la hora, abre y cierra las ventanas, baja del barco y vuelve a subir.
//   - Guarda la partida y recarga la escena para probar que todo vuelve igual (y que no quedan restos de la anterior).
// Qué anota (carpeta Builds/bot del proyecto, o la carpeta de datos en el teléfono):
//   - errores.txt: cada error o excepción distinto, cuántas veces salió y cuándo la primera vez.
//   - medidas.csv: cada minuto, FPS, memoria, recolecciones de basura, objetos vivos, islas, barcos…
//   - informe.txt: resumen, con avisos si algo crece sin parar (fuga) o el jugador termina en un lugar raro.
// Antes de empezar se copia tu partida a partida.antes-del-bot.json (el bot guarda encima).
public class BotPrueba : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        if (I != null) return;
        var g = new GameObject("Bot de prueba");
        DontDestroyOnLoad(g);
        I = g.AddComponent<BotPrueba>();
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("Pirata.Bot", false)) { UnityEditor.SessionState.SetBool("Pirata.Bot", false); I.Empezar(); }
#endif
    }

    public static BotPrueba I { get; private set; }
    public static bool Activo => I != null && I.activo;

    bool activo;
    float inicio, tVuelta, tMedir, tRumbo, tAtorado, tSuelo;
    int vuelta, recargas, hundidos;
    Vector3 destino;
    Vector3 ultimaPos;
    string carpeta;
    readonly Dictionary<string, (int veces, float primera, string pila)> errores = new Dictionary<string, (int, float, string)>();
    readonly List<string> avisos = new List<string>();
    readonly List<float[]> medidas = new List<float[]>();
    StringBuilder csv;
    float sumaDt, peorDt; int cuadros;
    Vector3 posAntesDeRecargar; bool esperandoRecarga; float tRecarga;

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.f12Key.wasPressedThisFrame) { if (activo) Terminar("apagado con F12"); else Empezar(); }
        if (!activo) return;

        float dt = Time.unscaledDeltaTime;
        sumaDt += dt; cuadros++; if (dt > peorDt) peorDt = dt;

        tMedir -= dt;
        if (tMedir <= 0f) { tMedir = 60f; Medir(); }

        if (esperandoRecarga) { DespuesDeRecargar(); return; }

        Personaje p = Personaje.Instancia;
        if (p == null) return;
        if (MenuPrincipal.Visible) { MenuPrincipal.Ocultar(); return; }

        Conducir(p, dt);

        tVuelta -= dt;
        if (tVuelta > 0f) return;
        tVuelta = 20f;
        vuelta++;
        try { Accion(p); }
        catch (System.Exception e) { Anotar("BOT: " + e.GetType().Name + ": " + e.Message, e.StackTrace); }
    }

    // ---------- Empezar y terminar ----------
    public void Empezar()
    {
        carpeta = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "bot"))
                                       : Path.Combine(Application.persistentDataPath, "bot");
        Directory.CreateDirectory(carpeta);
        // La copia se hace una sola vez (si ya existe, es la de antes de la primera prueba y no se pisa)
        string copia = Path.Combine(Path.GetDirectoryName(Guardado.Ruta), "partida.antes-del-bot.json");
        try { if (File.Exists(Guardado.Ruta) && !File.Exists(copia)) File.Copy(Guardado.Ruta, copia); }
        catch (System.Exception e) { Debug.LogWarning("Bot: no se pudo respaldar la partida: " + e.Message); }
        errores.Clear(); avisos.Clear(); medidas.Clear();
        csv = new StringBuilder("minuto,fps,peor_ms,mono_mb,total_mb,gc,objetos,renderers,islas,recursos,piratas,tripulantes,pecios,cajas,x_mundo,z_mundo,desplaz_km\n");
        inicio = Time.realtimeSinceStartup;
        vuelta = recargas = hundidos = 0;
        tVuelta = 3f; tMedir = 60f; tRumbo = 0f;
        Application.logMessageReceived -= AlLog;
        Application.logMessageReceived += AlLog;
        activo = true;
        Notificaciones.Mostrar("Bot de prueba jugando (F12 para parar). Informe en " + carpeta, Notificaciones.Tipo.Info, 6f);
        Debug.Log("Bot de prueba: empieza. Informe en " + carpeta);
    }

    void Terminar(string porque)
    {
        if (!activo) return;
        activo = false;
        Application.logMessageReceived -= AlLog;
        Personaje p = Personaje.Instancia;
        if (p != null && p.barco != null) { p.barco.FijarVelas(0); p.IrAlTimonPropio(); }
        EscribirInforme(porque);
        Notificaciones.Mostrar("Bot de prueba terminado: " + errores.Count + " errores distintos", Notificaciones.Tipo.Info, 6f);
    }

    void OnApplicationQuit() { if (activo) Terminar("se cerró el juego"); }

    // ---------- Errores ----------
    void AlLog(string mensaje, string pila, LogType tipo)
    {
        if (tipo != LogType.Error && tipo != LogType.Exception && tipo != LogType.Assert) return;
        Anotar(mensaje, pila);
    }

    void Anotar(string mensaje, string pila)
    {
        string primera = pila != null ? pila.Split('\n')[0] : "";
        string clave = mensaje.Length > 300 ? mensaje.Substring(0, 300) : mensaje;
        clave += " @ " + primera;
        float min = (Time.realtimeSinceStartup - inicio) / 60f;
        if (errores.TryGetValue(clave, out var e)) errores[clave] = (e.veces + 1, e.primera, e.pila);
        else
        {
            errores[clave] = (1, min, pila);
            EscribirInforme(null);   // que quede escrito aunque Unity se cierre
        }
    }

    // ---------- Manejar el barco ----------
    void Conducir(Personaje p, float dt)
    {
        ControlBarco b = p.barco;
        if (b == null) return;
        VidaBarco vb = b.GetComponent<VidaBarco>();
        if (vb != null && vb.Hundido) return;
        // El bot no quiere perder el barco (salvo cuando lo prueba a propósito)
        if (vb != null && !dejarHundir) vb.vida = Mathf.Max(vb.vida, vb.vidaMaxima * 0.6f);
        if (p.Vida < p.vidaMaxima * 0.5f) p.FijarVida(p.vidaMaxima);

        if (p.estado != Personaje.Estado.Timon || p.BarcoActual != b) return;
        b.controlActivo = false;   // timón en manos del bot
        if (b.Anclado) b.AlternarAncla();

        Vector3 pos = b.transform.position;
        tRumbo -= dt;
        if (tRumbo <= 0f || (destino - pos).sqrMagnitude < 80f * 80f) NuevoDestino(pos);

        Vector3 hacia = destino - pos; hacia.y = 0f;
        float giro = Vector3.SignedAngle(Vector3.ProjectOnPlane(b.transform.forward, Vector3.up), hacia, Vector3.up);
        b.timonExterno = Mathf.Clamp(giro / 30f, -1f, 1f);
        b.FijarVelas(Mathf.Abs(giro) > 70f ? 1 : b.nivelesVela);

        // ¿Atorado contra una isla? (casi no avanza): otro rumbo; si sigue, se le saca a mar abierto
        tAtorado -= dt;
        if (tAtorado <= 0f)
        {
            tAtorado = 15f;
            float avance = Vector3.Distance(pos, ultimaPos);
            ultimaPos = pos;
            if (avance < 10f)
            {
                if (++atascos >= 2) { MasLejos(p, 300f); atascos = 0; }
                else NuevoDestino(pos);
            }
            else atascos = 0;
        }
    }
    int atascos;
    bool dejarHundir;

    void NuevoDestino(Vector3 desde)
    {
        float ang = Random.Range(0f, 360f), dist = Random.Range(500f, 1600f);
        destino = desde + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * dist;
        tRumbo = 240f;
    }

    // ---------- Lo que hace en cada vuelta (cada 20 s) ----------
    void Accion(Personaje p)
    {
        // Asegurar que va al timón de su barco
        if (p.barco != null && (p.estado != Personaje.Estado.Timon || p.BarcoActual != p.barco) && !Ventanas.AlgunaAbierta)
            p.IrAlTimonPropio();

        // Cada 30 vueltas (10 min): guardar y recargar la escena (primero, con todo cerrado)
        if (vuelta % 30 == 0) { Ventanas.CerrarTodas(); Recargar(p); return; }

        switch (vuelta % 12)
        {
            case 2: Combate(p, false); break;
            case 3: Disparar(p); break;
            case 4: Disparar(p); break;
            case 5: Clima.Forzado = Random.Range(-1, 4); CicloDia.Hora = Random.Range(0f, 24f); break;
            case 6: Ventanitas(); break;
            case 7: Combate(p, true); break;
            case 8: Disparar(p); break;
            case 9: ABordoYAPie(p); break;
            case 10: if (Random.value < 0.4f) MasLejos(p, Random.Range(600f, 1500f)); break;
            case 11: break;
        }
        // Cada 90 vueltas (30 min): dejar que hundan tu barco una vez (reaparecer, tripulación, pactos)
        if (vuelta % 90 == 45) { dejarHundir = true; VidaBarco vb = p.barco != null ? p.barco.GetComponent<VidaBarco>() : null; if (vb != null) vb.Dano(vb.vida + 1f); }
        else if (vuelta % 90 == 47) dejarHundir = false;
    }

    void Combate(Personaje p, bool hundir)
    {
        if (p.barco == null) return;
        IABarco ia = IABarco.MasCercano(p.transform.position);
        if (ia == null) { MasLejos(p, 900f); return; }   // lejos del inicio hay piratas
        TeclasPrueba.Traer(ia, p, 35f);
        if (hundir)
        {
            VidaBarco v = ia.GetComponent<VidaBarco>();
            if (v != null && !v.Hundido) { v.Dano(v.vida + 1f); hundidos++; }
        }
        else if (Random.value < 0.3f) { ia.ForzarAbordaje(); }
    }

    void Disparar(Personaje p)
    {
        Canones c = p.barco != null ? p.barco.GetComponent<Canones>() : null;
        if (c == null) return;
        c.Disparar(true); c.Disparar(false);
    }

    void Ventanitas()
    {
        Ventana[] lista = { VentanaInventario.I, VentanaMapa.I, VentanaBarco.I, VentanaBitacora.I, VentanaAyuda.I, VentanaApariencia.I, VentanaOpciones.I };
        Ventana v = lista[Random.Range(0, lista.Length)];
        Ventanas.Abrir(v);
        StartCoroutine(CerrarLuego(v, 3f));
    }

    System.Collections.IEnumerator CerrarLuego(Ventana v, float s)
    {
        yield return new WaitForSecondsRealtime(s);
        Ventanas.Cerrar(v);
    }

    void ABordoYAPie(Personaje p)
    {
        // A pie en la isla más cercana un rato (recursos, caminar), y la próxima vuelta vuelve al timón
        Isla mejor = null; float d = float.MaxValue;
        foreach (Isla i in Isla.Todas)
        {
            if (i == null) continue;
            float x = Vector3.Distance(i.transform.position, p.transform.position);
            if (x < d) { d = x; mejor = i; }
        }
        if (mejor == null || d > 1200f) return;
        p.Teletransportar(mejor.transform.position + Vector3.right * mejor.radio * 0.5f + Vector3.up * 6f);
    }

    static void MasLejos(Personaje p, float metros)
    {
        if (p.barco == null) return;
        Rigidbody rb = p.barco.GetComponent<Rigidbody>();
        Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        for (int i = 0; i < 20; i++)
        {
            Vector3 d = rb.position + dir * metros; d.y = rb.position.y;
            if (Balsa.HayAguaPara(d, p.barco.transform.forward, 60f, 2f))
            {
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.position = d; p.barco.transform.position = d;
                if (p.estado != Personaje.Estado.Timon) p.IrAlTimonPropio();
                return;
            }
            dir = Quaternion.Euler(0f, 18f, 0f) * dir;
        }
    }

    // ---------- Guardar y recargar ----------
    void Recargar(Personaje p)
    {
        Guardado g = Object.FindFirstObjectByType<Guardado>();
        if (g == null || !g.Listo || MenuPrincipal.Visible) return;
        g.GuardarAhora();
        posAntesDeRecargar = OrigenFlotante.AMundo(p.transform.position);
        esperandoRecarga = true; tRecarga = 0f;
        recargas++;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void DespuesDeRecargar()
    {
        tRecarga += Time.unscaledDeltaTime;
        if (MenuPrincipal.Visible) MenuPrincipal.Ocultar();
        Guardado g = Object.FindFirstObjectByType<Guardado>();
        Personaje p = Personaje.Instancia;
        if (g == null || !g.Listo || p == null)
        {
            if (tRecarga > 60f) { Anotar("BOT: la partida no terminó de cargar en 60 s tras recargar", ""); esperandoRecarga = false; }
            return;
        }
        esperandoRecarga = false;
        Vector3 ahora = OrigenFlotante.AMundo(p.transform.position);
        float salto = Vector3.Distance(new Vector3(ahora.x, 0f, ahora.z), new Vector3(posAntesDeRecargar.x, 0f, posAntesDeRecargar.z));
        if (salto > 40f) Anotar("BOT: tras guardar y recargar, el jugador apareció a " + Mathf.RoundToInt(salto) + " m de donde estaba", "");
        if (p.barco != null) p.IrAlTimonPropio();
        ultimaPos = Vector3.zero; tRumbo = 0f;
    }

    // ---------- Medidas ----------
    void Medir()
    {
        float min = (Time.realtimeSinceStartup - inicio) / 60f;
        float fps = sumaDt > 0f ? cuadros / sumaDt : 0f;
        float peor = peorDt * 1000f;
        sumaDt = 0f; cuadros = 0; peorDt = 0f;
        Personaje p = Personaje.Instancia;
        Vector3 w = p != null ? OrigenFlotante.AMundo(p.transform.position) : Vector3.zero;
        float[] m =
        {
            min, fps, peor,
            Profiler.GetMonoUsedSizeLong() / 1048576f, Profiler.GetTotalAllocatedMemoryLong() / 1048576f,
            System.GC.CollectionCount(0),
            Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,
            Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,
            Isla.Todas.Count, Recurso.Todos.Count, IABarco.Registrados.Count, Tripulante.Vivos.Count, Pecios.Todos.Count, CajaBotin.Todas.Count,
            w.x, w.z, OrigenFlotante.Desplazamiento.magnitude / 1000f,
        };
        medidas.Add(m);
        var sb = new StringBuilder();
        for (int i = 0; i < m.Length; i++) { if (i > 0) sb.Append(','); sb.Append(m[i].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)); }
        csv.AppendLine(sb.ToString());

        if (p != null)
        {
            Vector3 lp = p.transform.position;
            if (float.IsNaN(lp.x) || float.IsNaN(lp.y) || float.IsInfinity(lp.x)) Anotar("BOT: posición del jugador NaN/infinita", "");
            else if (lp.y < -30f) Anotar("BOT: el jugador está " + Mathf.RoundToInt(-lp.y) + " m bajo el agua", "");
            if (p.barco != null && Vector3.Dot(p.barco.transform.up, Vector3.up) < 0.2f) Anotar("BOT: tu barco quedó volcado", "");
        }
        EscribirInforme(null);
    }

    // Busca columnas que crecen sin parar (posible fuga): compara el promedio del primer tercio con el último
    void Tendencias()
    {
        avisos.Clear();
        if (medidas.Count < 15) return;
        string[] nombres = { "", "", "", "memoria del juego (MB)", "memoria total (MB)", "", "objetos", "renderers", "islas", "recursos", "piratas", "tripulantes", "", "cajas de botín" };   // los pecios quedan para siempre a propósito (tope 300)
        int n = medidas.Count / 3;
        for (int c = 3; c < nombres.Length; c++)
        {
            if (nombres[c] == "") continue;
            float a = 0f, b = 0f;
            for (int i = 0; i < n; i++) { a += medidas[i][c]; b += medidas[medidas.Count - 1 - i][c]; }
            a /= n; b /= n;
            if (b > a * 1.5f && b - a > 5f) avisos.Add("Crece sin parar: " + nombres[c] + " de " + a.ToString("0") + " a " + b.ToString("0") + " (posible fuga)");
        }
        float fpsA = 0f; foreach (var m in medidas) fpsA += m[1];
        fpsA /= medidas.Count;
        float fpsFin = 0f; for (int i = 0; i < n; i++) fpsFin += medidas[medidas.Count - 1 - i][1];
        fpsFin /= n;
        if (fpsFin < fpsA * 0.75f) avisos.Add("Los FPS bajan con el tiempo: promedio " + fpsA.ToString("0") + ", al final " + fpsFin.ToString("0"));
    }

    void EscribirInforme(string fin)
    {
        if (carpeta == null) return;
        try
        {
            Tendencias();
            float min = (Time.realtimeSinceStartup - inicio) / 60f;
            var sb = new StringBuilder();
            sb.AppendLine("BOT DE PRUEBA · versión " + Version.Numero + " · " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Jugó " + min.ToString("0") + " min · " + vuelta + " vueltas · " + recargas + " recargas de escena · " + hundidos + " piratas hundidos");
            if (fin != null) sb.AppendLine("Terminó: " + fin);
            sb.AppendLine();
            sb.AppendLine("ERRORES DISTINTOS: " + errores.Count);
            foreach (var kv in errores) sb.AppendLine("  [" + kv.Value.veces + "x, desde el min " + kv.Value.primera.ToString("0") + "] " + kv.Key);
            sb.AppendLine();
            sb.AppendLine("AVISOS: " + (avisos.Count == 0 ? "ninguno" : ""));
            foreach (string a in avisos) sb.AppendLine("  " + a);
            if (medidas.Count > 0)
            {
                float fMin = float.MaxValue, fMax = 0f, fSum = 0f, peor = 0f;
                foreach (var m in medidas) { fMin = Mathf.Min(fMin, m[1]); fMax = Mathf.Max(fMax, m[1]); fSum += m[1]; peor = Mathf.Max(peor, m[2]); }
                var u = medidas[medidas.Count - 1];
                sb.AppendLine();
                sb.AppendLine("FPS (editor, no es el teléfono): promedio " + (fSum / medidas.Count).ToString("0") + " · mínimo " + fMin.ToString("0") + " · máximo " + fMax.ToString("0") + " · peor cuadro " + peor.ToString("0") + " ms");
                sb.AppendLine("Lo más lento de armar (un solo cuadro): celda de islas " + MundoInfinito.PeorCeldaMs.ToString("0") + " ms · barco pirata " + MundoInfinito.PeorBarcoMs.ToString("0") + " ms · salto del origen flotante " + OrigenFlotante.PeorCorridaMs.ToString("0") + " ms");
                sb.AppendLine("Ahora: memoria " + u[3].ToString("0") + "/" + u[4].ToString("0") + " MB · " + u[6] + " objetos · " + u[7] + " renderers · " + u[8] + " islas · " + u[10] + " piratas · a " + (new Vector2(u[14], u[15]).magnitude / 1000f).ToString("0.0") + " km del inicio");
            }
            sb.AppendLine();
            sb.AppendLine("LO QUE MÁS TARDA EN UN SOLO CUADRO:");
            sb.Append(Cronos.Informe());
            File.WriteAllText(Path.Combine(carpeta, "informe.txt"), sb.ToString());
            File.WriteAllText(Path.Combine(carpeta, "medidas.csv"), csv.ToString());
            var er = new StringBuilder();
            foreach (var kv in errores) { er.AppendLine("=== " + kv.Value.veces + "x · min " + kv.Value.primera.ToString("0") + " ==="); er.AppendLine(kv.Key); er.AppendLine(kv.Value.pila); }
            File.WriteAllText(Path.Combine(carpeta, "errores.txt"), er.ToString());
        }
        catch (System.Exception e) { Debug.LogWarning("Bot: no se pudo escribir el informe: " + e.Message); }
    }
}
