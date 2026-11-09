using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Guarda la partida en un archivo (se crea solo al darle Play):
// dónde estás, vida, hambre y sed, lo que llevas, tu barco (molde, tamaño, mejoras, cañones, daño y bodega),
// los hornos que armaste y las islas que descubriste. El mundo infinito sale igual siempre (misma semilla),
// así que no hace falta guardarlo.
// Se guarda sola cada 30 s y al salir. F5 guarda ya. F8 borra la partida (al volver a entrar empiezas de cero).
public class Guardado : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("Guardado").AddComponent<Guardado>();

    const float CadaSegundos = 30f;
    public static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");

    [System.Serializable] class Cosa { public string tipo; public int n; }
    [System.Serializable] class IslaConocida { public string id, nombre; public Vector3 centro; public float orilla; public bool mercado; }
    [System.Serializable] class HornoGuardado { public Vector3 pos; public float rumbo; }

    [System.Serializable]
    class Datos
    {
        public int version = 1;
        public Vector3 posJugador;
        public bool aBordo;
        public float vida = 100f, hambre = 100f, sed = 100f;
        public List<Cosa> inventario = new List<Cosa>();

        public bool tieneBarco;
        public int molde, largo, ancho, alto, nivelCasco, nivelVelas;
        public int cBabor, cEstribor, cProa, cPopa, nivelBarriles;
        public float vidaBarco = 1f;   // fracción de la vida máxima
        public Vector3 posBarco;
        public float rumboBarco;
        public List<Cosa> bodega = new List<Cosa>();
        public int pinturaCasco = -1, pinturaVelas = -1, pinturaBandera = -1;   // partidas viejas (-1 = color original)
        public string pintura = "";   // pintura del Taller (colores por parte, dibujos y mascarón)

        public List<HornoGuardado> hornos = new List<HornoGuardado>();
        public List<IslaConocida> islas = new List<IslaConocida>();
    }

    float t;
    bool listo;      // no se guarda hasta terminar de cargar
    bool borrada;    // con F8 se deja de guardar en esta sesión

    IEnumerator Start()
    {
        yield return null;
        yield return null;   // que el personaje y el mundo arranquen primero
        Datos d = Leer();
        if (d != null) yield return Aplicar(d);
        listo = true;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        Personaje p = Personaje.Instancia;
        if (k != null && p != null && !Fabricacion.Abierto)
        {
            if (k.f5Key.wasPressedThisFrame && listo) { borrada = false; Guardar(); p.Avisar("Partida guardada"); }
            if (k.f8Key.wasPressedThisFrame)
            {
                if (File.Exists(Ruta)) File.Delete(Ruta);
                borrada = true;
                p.Avisar("Partida borrada. Al volver a entrar empiezas de cero (F5 la vuelve a guardar)");
            }
        }
        if (!listo || borrada) return;
        t += Time.deltaTime;
        if (t >= CadaSegundos) { t = 0f; Guardar(); }
    }

    void OnApplicationQuit() { if (listo && !borrada) Guardar(); }

    // ---------- Guardar ----------
    void Guardar()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Datos d = new Datos();
        d.posJugador = p.transform.position;
        d.vida = p.Vida;
        Supervivencia sup = p.GetComponent<Supervivencia>();
        if (sup != null) { d.hambre = sup.Hambre; d.sed = sup.Sed; }
        Inventario inv = p.GetComponent<Inventario>();
        if (inv != null) foreach (var c in inv.Todo) if (c.Value > 0) d.inventario.Add(new Cosa { tipo = c.Key, n = c.Value });

        ControlBarco b = p.barco;
        VidaBarco vb = b != null ? b.GetComponent<VidaBarco>() : null;
        if (b != null && b.gameObject.activeInHierarchy && (vb == null || !vb.Hundido))
        {
            d.tieneBarco = true;
            d.aBordo = p.estado != Personaje.Estado.Libre && p.BarcoActual == b;
            FormaBarco.Leer(b, out d.molde, out d.largo, out d.ancho, out d.alto);
            PiezasBarco pz = b.GetComponent<PiezasBarco>();
            if (pz != null) { d.nivelCasco = pz.nivelCasco; d.nivelVelas = pz.nivelVelas; }
            Canones cn = b.GetComponent<Canones>();
            if (cn != null) { d.cBabor = cn.enBabor; d.cEstribor = cn.enEstribor; d.cProa = cn.enProa; d.cPopa = cn.enPopa; d.nivelBarriles = cn.nivelBarriles; }
            if (vb != null && vb.vidaMaxima > 0f) d.vidaBarco = vb.vida / vb.vidaMaxima;
            d.posBarco = b.transform.position;
            d.rumboBarco = b.transform.eulerAngles.y;
            Pintura pt = b.GetComponent<Pintura>();
            if (pt != null && pt.AlgoPintado()) d.pintura = pt.AJson();
            Bodega bd = b.GetComponent<Bodega>();
            if (bd != null) foreach (var c in bd.Todo) if (c.Value > 0) d.bodega.Add(new Cosa { tipo = c.Key, n = c.Value });
        }

        foreach (Horno h in Horno.Todos)
            if (h != null) d.hornos.Add(new HornoGuardado { pos = h.transform.position, rumbo = h.transform.eulerAngles.y });

        Navegacion nav = Navegacion.Instancia;
        if (nav != null)
            foreach (var kv in nav.Descubiertas)
                d.islas.Add(new IslaConocida { id = kv.Key, nombre = kv.Value.nombre, centro = kv.Value.centro, orilla = kv.Value.orilla, mercado = kv.Value.mercado });

        try
        {
            string tmp = Ruta + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
            if (File.Exists(Ruta)) File.Delete(Ruta);
            File.Move(tmp, Ruta);   // así un corte a mitad de guardado no daña la partida anterior
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo guardar la partida: " + e.Message); }
    }

    // ---------- Cargar ----------
    static Datos Leer()
    {
        if (!File.Exists(Ruta)) return null;
        try { return JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta)); }
        catch (System.Exception e) { Debug.LogWarning("No se pudo leer la partida: " + e.Message); return null; }
    }

    IEnumerator Aplicar(Datos d)
    {
        Personaje p = Personaje.Instancia;
        if (p == null) yield break;

        // Islas descubiertas y lo que llevas
        Navegacion nav = Navegacion.Instancia;
        if (nav != null)
            foreach (IslaConocida i in d.islas)
                nav.Recordar(i.id, new Navegacion.Conocida { nombre = i.nombre, centro = i.centro, orilla = i.orilla, mercado = i.mercado });
        Inventario inv = p.GetComponent<Inventario>();
        if (inv != null) foreach (Cosa c in d.inventario) inv.Fijar(c.tipo, c.n);
        p.FijarVida(d.vida);
        Supervivencia sup = p.GetComponent<Supervivencia>();
        if (sup != null) sup.Fijar(d.hambre, d.sed);

        // Primero se lleva al personaje a donde estaba, para que el mundo infinito arme las islas de esa zona
        Vector3 donde = d.tieneBarco && d.aBordo ? d.posBarco : d.posJugador;
        p.Teletransportar(donde + Vector3.up * 2f);
        float espera = 0f;
        while (espera < 1.5f) { espera += Time.deltaTime; yield return null; }

        Fabricacion fab = p.GetComponent<Fabricacion>();
        foreach (HornoGuardado h in d.hornos)
            if (fab != null) fab.CrearHorno(h.pos, h.rumbo);

        if (d.tieneBarco && fab != null)
        {
            // Si había un barco propio de la escena (el balandro de prueba), se cambia por el guardado
            if (p.barco != null) Destroy(p.barco.gameObject);
            ControlBarco b = fab.CrearBarcoPropio(d.molde, d.largo, d.ancho, d.alto, d.posBarco, d.rumboBarco);
            if (d.nivelCasco + d.nivelVelas > 0) b.gameObject.AddComponent<PiezasBarco>().Copiar(d.nivelCasco, d.nivelVelas);
            Canones cn = b.GetComponent<Canones>();
            if (cn != null) { cn.Fijar(d.cBabor, d.cEstribor, d.cProa, d.cPopa); cn.nivelBarriles = d.nivelBarriles; }
            VidaBarco vb = b.GetComponent<VidaBarco>();
            if (vb != null) vb.vida = Mathf.Max(1f, vb.vidaMaxima * Mathf.Clamp01(d.vidaBarco));
            if (d.bodega.Count > 0)
            {
                Bodega bd = b.GetComponent<Bodega>();
                if (bd == null) bd = b.gameObject.AddComponent<Bodega>();
                foreach (Cosa c in d.bodega) bd.Fijar(c.tipo, c.n);
            }
            if (!string.IsNullOrEmpty(d.pintura)) b.gameObject.AddComponent<Pintura>().DeJson(d.pintura);
            else if (d.pinturaCasco >= 0 || d.pinturaVelas >= 0 || d.pinturaBandera >= 0)
            {
                Pintura pt = b.gameObject.AddComponent<Pintura>();
                pt.DesdePaleta(d.pinturaCasco, d.pinturaVelas, d.pinturaBandera);
                pt.Aplicar();
            }
            yield return null;   // que el barco nuevo arranque (física, cubierta)
        }

        if (d.tieneBarco && d.aBordo) p.IrAlTimonPropio();
        else p.Teletransportar(d.posJugador + Vector3.up * 0.3f);
        p.Avisar("Partida cargada");
    }
}
