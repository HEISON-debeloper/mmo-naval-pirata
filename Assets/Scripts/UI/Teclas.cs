using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Atajos de teclado configurables. Todo el juego pregunta aquí ("¿se pulsó Usar?") en vez de mirar teclas fijas,
// así el jugador puede cambiarlas en Opciones → Controles. Se guardan en PlayerPrefs.
public enum Accion
{
    Adelante, Atras, Izquierda, Derecha, Correr, Saltar, Usar, Comer, Ancla, Espada,
    DispararBabor, DispararEstribor,
    Fabricar, Inventario, Barco, Mapa, Taller, Bitacora, Ayuda, Pausa,
    Creativo, Guardar, BorrarPartida,
}

public static class Teclas
{
    public struct Info { public string nombre, grupo; public Key tecla; }

    static readonly Dictionary<Accion, Key> porDefecto = new Dictionary<Accion, Key>
    {
        { Accion.Adelante, Key.W }, { Accion.Atras, Key.S }, { Accion.Izquierda, Key.A }, { Accion.Derecha, Key.D },
        { Accion.Correr, Key.LeftShift }, { Accion.Saltar, Key.Space }, { Accion.Usar, Key.F }, { Accion.Comer, Key.C },
        { Accion.Ancla, Key.R }, { Accion.Espada, Key.K },
        { Accion.DispararBabor, Key.Q }, { Accion.DispararEstribor, Key.E },
        { Accion.Fabricar, Key.Tab }, { Accion.Inventario, Key.I }, { Accion.Barco, Key.B }, { Accion.Mapa, Key.M },
        { Accion.Taller, Key.P }, { Accion.Bitacora, Key.G }, { Accion.Ayuda, Key.F1 }, { Accion.Pausa, Key.Escape },
        { Accion.Creativo, Key.F2 }, { Accion.Guardar, Key.F5 }, { Accion.BorrarPartida, Key.F8 },
    };

    public static readonly (Accion accion, string nombre, string grupo)[] Lista =
    {
        (Accion.Adelante, "Adelante / subir velas", "Movimiento"), (Accion.Atras, "Atrás / bajar velas", "Movimiento"),
        (Accion.Izquierda, "Izquierda / timón a babor", "Movimiento"), (Accion.Derecha, "Derecha / timón a estribor", "Movimiento"),
        (Accion.Correr, "Correr", "Movimiento"), (Accion.Saltar, "Saltar", "Movimiento"),
        (Accion.Usar, "Usar (recoger, timón, subir, puertas)", "Acciones"), (Accion.Comer, "Comer", "Acciones"),
        (Accion.Ancla, "Echar o levar el ancla", "Acciones"), (Accion.Espada, "Espada (también clic izquierdo)", "Acciones"),
        (Accion.DispararBabor, "Disparar babor", "Combate"), (Accion.DispararEstribor, "Disparar estribor", "Combate"),
        (Accion.Fabricar, "Fabricar / comerciar", "Ventanas"), (Accion.Inventario, "Inventario", "Ventanas"),
        (Accion.Barco, "Tu barco", "Ventanas"), (Accion.Mapa, "Carta de navegación", "Ventanas"),
        (Accion.Taller, "Taller de pintura", "Ventanas"), (Accion.Bitacora, "Bitácora (avisos)", "Ventanas"),
        (Accion.Ayuda, "Controles", "Ventanas"), (Accion.Pausa, "Pausa / cerrar", "Ventanas"),
        (Accion.Creativo, "Modo creativo", "Partida"), (Accion.Guardar, "Guardar ya", "Partida"), (Accion.BorrarPartida, "Borrar la partida", "Partida"),
    };

    static Dictionary<Accion, Key> mapa;
    const string Clave = "Teclas";

    // Mientras se está eligiendo una tecla en Opciones, el juego no reacciona al teclado
    public static bool Capturando;

    static Dictionary<Accion, Key> Mapa
    {
        get
        {
            if (mapa != null) return mapa;
            mapa = new Dictionary<Accion, Key>(porDefecto);
            try
            {
                string json = PlayerPrefs.GetString(Clave, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var g = JsonUtility.FromJson<Guardadas>(json);
                    if (g != null) for (int i = 0; i < g.acciones.Count && i < g.teclas.Count; i++)
                        if (System.Enum.TryParse(g.acciones[i], out Accion a) && System.Enum.TryParse(g.teclas[i], out Key k)) mapa[a] = k;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("No se pudieron leer los atajos: " + e.Message); }
            return mapa;
        }
    }

    [System.Serializable] class Guardadas { public List<string> acciones = new List<string>(); public List<string> teclas = new List<string>(); }

    public static void Guardar()
    {
        var g = new Guardadas();
        foreach (var kv in Mapa) { g.acciones.Add(kv.Key.ToString()); g.teclas.Add(kv.Value.ToString()); }
        PlayerPrefs.SetString(Clave, JsonUtility.ToJson(g));
        PlayerPrefs.Save();
    }

    public static Key De(Accion a) => Mapa.TryGetValue(a, out Key k) ? k : Key.None;
    public static Key PorDefecto(Accion a) => porDefecto[a];
    public static bool EsPorDefecto(Accion a) => De(a) == porDefecto[a];

    public static void Asignar(Accion a, Key k)
    {
        if (k == Key.None) return;
        // Si otra acción usaba esa tecla, se intercambian (nunca quedan dos acciones con la misma tecla)
        Key anterior = De(a);
        foreach (var kv in new List<KeyValuePair<Accion, Key>>(Mapa))
            if (kv.Key != a && kv.Value == k) Mapa[kv.Key] = anterior;
        Mapa[a] = k;
        Guardar();
    }

    public static void Restablecer()
    {
        mapa = new Dictionary<Accion, Key>(porDefecto);
        Guardar();
    }

    static KeyControl Control(Accion a)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return null;
        Key k = De(a);
        if (k == Key.None) return null;
        try { return kb[k]; } catch { return null; }
    }

    // Botones táctiles (teléfono): los controles en pantalla marcan aquí lo que se está tocando.
    // Los toques llegan con los eventos de la interfaz; ControlesTactiles los pasa a "este cuadro" al principio de cada cuadro,
    // así cada toque cuenta exactamente una vez para todos los scripts.
    static readonly HashSet<Accion> tocadas = new HashSet<Accion>();
    static readonly HashSet<Accion> pendientes = new HashSet<Accion>();
    static readonly HashSet<Accion> esteCuadro = new HashSet<Accion>();
    public static void Tocar(Accion a, bool si)
    {
        if (si && !tocadas.Contains(a)) pendientes.Add(a);
        if (si) tocadas.Add(a); else tocadas.Remove(a);
    }
    public static void Toque(Accion a) => pendientes.Add(a);
    internal static void NuevoCuadro() { esteCuadro.Clear(); foreach (var a in pendientes) esteCuadro.Add(a); pendientes.Clear(); }
    static bool TocadaAhora(Accion a) => esteCuadro.Contains(a);

    // Se pulsó en este cuadro (y no hay una ventana que se quede con el teclado)
    public static bool Pulsada(Accion a, bool aunqueHayaVentana = false)
    {
        if (Capturando) return false;
        if (!aunqueHayaVentana && Ventanas.AlgunaAbierta) return false;
        if (TocadaAhora(a)) return true;
        KeyControl c = Control(a);
        return c != null && c.wasPressedThisFrame;
    }

    public static bool Presionada(Accion a, bool aunqueHayaVentana = false)
    {
        if (Capturando) return false;
        if (!aunqueHayaVentana && Ventanas.AlgunaAbierta) return false;
        if (tocadas.Contains(a)) return true;
        KeyControl c = Control(a);
        return c != null && c.isPressed;
    }

    // Nombre corto para mostrar en pantalla ("F", "Espacio", "Tab", "Mayús")
    public static string Nombre(Accion a) => NombreTecla(De(a));
    public static string NombreTecla(Key k)
    {
        switch (k)
        {
            case Key.Space: return "Espacio";
            case Key.LeftShift: case Key.RightShift: return "Mayús";
            case Key.LeftCtrl: case Key.RightCtrl: return "Ctrl";
            case Key.LeftAlt: case Key.RightAlt: return "Alt";
            case Key.Escape: return "Esc";
            case Key.Enter: return "Enter";
            case Key.Backspace: return "Retroceso";
            case Key.Tab: return "Tab";
            case Key.None: return "—";
            case Key.UpArrow: return "↑"; case Key.DownArrow: return "↓"; case Key.LeftArrow: return "←"; case Key.RightArrow: return "→";
        }
        string s = k.ToString();
        if (s.StartsWith("Digit")) return s.Substring(5);
        // El nombre que tiene esa tecla en el teclado de verdad (Ñ, ´, + … según la distribución), no el del teclado de EE. UU.
        Keyboard kb = Keyboard.current;
        if (kb != null && !s.StartsWith("Numpad") && k != Key.None)
        {
            try { string real = kb[k].displayName; if (!string.IsNullOrEmpty(real)) return real.Length == 1 ? real.ToUpperInvariant() : real; } catch { }
        }
        if (s.StartsWith("Numpad")) return "Num " + s.Substring(6);
        return s;
    }

    // Durante la captura: la primera tecla que se pulse (null si ninguna este cuadro). Esc cancela.
    public static Key? TeclaPulsadaAhora()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return null;
        foreach (KeyControl c in kb.allKeys)
            if (c.wasPressedThisFrame) return c.keyCode;
        return null;
    }
}
