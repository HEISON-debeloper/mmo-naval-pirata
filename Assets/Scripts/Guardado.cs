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
    internal static void Crear() => new GameObject("Guardado").AddComponent<Guardado>();

    const float CadaSegundos = 30f;
    public static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");
    static string Respaldo => Ruta + ".bak";                                                            // la partida anterior a la última guardada
    public static string RutaAnterior => Path.Combine(Application.persistentDataPath, "partida.anterior.json");   // la que se apartó con "Nueva partida"
    public static bool HayAnterior => File.Exists(RutaAnterior);
    static Guardado instancia;
    static string avisoAlCargar;   // si la partida estaba dañada

    [System.Serializable] class Cosa { public string tipo; public int n; }
    [System.Serializable] class IslaConocida { public string id, nombre; public Vector3 centro; public float orilla; public bool mercado; }
    [System.Serializable] class HornoGuardado { public Vector3 pos; public float rumbo; }
    [System.Serializable] class MercadoGuardado { public int semilla; public List<string> k = new List<string>(); public List<float> v = new List<float>(); }

    [System.Serializable]
    class Datos
    {
        public int version = 1;
        public Vector3 posJugador;
        public bool aBordo;
        public float vida = 100f, hambre = 100f, sed = 100f;
        public float hora = -1f;   // hora del día (-1 = partidas viejas)
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
        public List<Navegacion.Marca> marcas = new List<Navegacion.Marca>();   // marcas puestas en la carta
        public List<Vector2Int> celdasLimpias = new List<Vector2Int>();         // zonas sin pirata (ya lo hundiste o capturaste)
        public List<MercadoGuardado> mercados = new List<MercadoGuardado>();    // cómo quedó la oferta en cada mercado
    }

    float t;
    bool listo;      // no se guarda hasta terminar de cargar
    bool borrada;    // con F8 se deja de guardar en esta sesión

    void Awake() => instancia = this;

    IEnumerator Start()
    {
        Datos d = Leer();
        if (d != null) RecordarMundo(d);
        CicloDia.Hora = d != null && d.hora >= 0f ? d.hora : 9f;   // partida nueva: de mañana   // antes de que el mundo genere las primeras zonas
        yield return null;
        yield return null;   // que el personaje y el mundo arranquen primero
        if (d != null) yield return Aplicar(d);
        listo = true;
        if (avisoAlCargar != null) { Notificaciones.Mostrar(avisoAlCargar, Notificaciones.Tipo.Alerta, 10f); avisoAlCargar = null; }
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        Personaje p = Personaje.Instancia;
        if (k != null && p != null)
        {
            if (Teclas.Pulsada(Accion.Guardar) && listo) { GuardarAhora(); }
            if (Teclas.Pulsada(Accion.BorrarPartida))
                Ventanas.Confirmar("Borrar la partida", "Se borra todo lo guardado: tu barco, la mochila, las islas descubiertas y la pintura. Al volver a entrar empiezas de cero.\n\nEsto no se puede deshacer.",
                    "Borrar la partida", BorrarPartida, true);
        }
        if (!listo || borrada) return;
        t += Time.deltaTime;
        if (t >= CadaSegundos) { t = 0f; Guardar(); }
    }

    void OnApplicationQuit() { if (listo && !borrada) Guardar(); }
    // En el teléfono el sistema puede cerrar la app en segundo plano sin avisar: se guarda al salir de ella
    void OnApplicationPause(bool pausa) { if (pausa && listo && !borrada) Guardar(); }

    // Guardar ya (F5, menú de pausa)
    public void GuardarAhora()
    {
        if (!listo) return;
        borrada = false; Guardar();
        Notificaciones.Mostrar("Partida guardada", Notificaciones.Tipo.Exito, 3f, "check");
    }

    // Borra el archivo; la partida actual sigue en memoria hasta cerrar (si vuelves a guardar, se recupera)
    public void BorrarPartida()
    {
        Apartar();
        borrada = true;
        Notificaciones.Mostrar("Partida borrada. Al volver a entrar empiezas de cero (" + Teclas.Nombre(Accion.Guardar) + " la vuelve a guardar). Se puede recuperar desde el menú principal.", Notificaciones.Tipo.Alerta, 6f);
    }

    // La partida actual no se borra: se aparta como "anterior" (se puede recuperar desde el menú principal)
    public static void Apartar()
    {
        try
        {
            if (!File.Exists(Ruta)) return;
            if (File.Exists(RutaAnterior)) File.Delete(RutaAnterior);
            File.Move(Ruta, RutaAnterior);
            if (File.Exists(Respaldo)) File.Delete(Respaldo);
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo apartar la partida: " + e.Message); }
    }

    // Cambia la partida actual por la anterior (y la actual queda como anterior)
    public static void RecuperarAnterior()
    {
        if (!HayAnterior) return;
        if (instancia != null) instancia.borrada = true;   // que esta sesión no la pise antes de recargar
        try
        {
            string cambio = Ruta + ".cambio";
            if (File.Exists(cambio)) File.Delete(cambio);
            if (File.Exists(Ruta)) File.Move(Ruta, cambio);
            File.Move(RutaAnterior, Ruta);
            if (File.Exists(cambio)) File.Move(cambio, RutaAnterior);
            if (File.Exists(Respaldo)) File.Delete(Respaldo);
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo recuperar la partida: " + e.Message); }
    }

    public static bool HayPartida => File.Exists(Ruta);
    public bool Listo => listo;

    // ---------- Guardar ----------
    void Guardar()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Datos d = new Datos();
        d.posJugador = p.transform.position;
        d.vida = p.Vida;
        d.hora = CicloDia.Hora;
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
        if (nav != null) d.marcas.AddRange(nav.Marcas);
        d.celdasLimpias.AddRange(MundoInfinito.Limpias);
        foreach (var kv in Mercader.Recordados)
        {
            var mg = new MercadoGuardado { semilla = kv.Key };
            foreach (var e in kv.Value.mercado) { mg.k.Add(e.Key); mg.v.Add(e.Value); }
            d.mercados.Add(mg);
        }

        try
        {
            string tmp = Ruta + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
            // Cambio atómico: la partida anterior queda como respaldo (.bak) por si la nueva sale dañada
            if (File.Exists(Ruta))
            {
                try { File.Replace(tmp, Ruta, Respaldo); }
                catch   // algunos sistemas no permiten Replace: copia de respaldo a mano
                {
                    File.Copy(Ruta, Respaldo, true);
                    File.Delete(Ruta);
                    File.Move(tmp, Ruta);
                }
            }
            else File.Move(tmp, Ruta);
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo guardar la partida: " + e.Message); }
    }

    // ---------- Cargar ----------
    static Datos Leer()
    {
        // Un guardado cortado a mitad (solo quedó el .tmp) se recupera
        if (!File.Exists(Ruta) && File.Exists(Ruta + ".tmp") && LeerDe(Ruta + ".tmp") != null)
            try { File.Move(Ruta + ".tmp", Ruta); } catch { }
        if (!File.Exists(Ruta)) return null;
        Datos d = LeerDe(Ruta);
        if (d != null) return d;
        // La partida está dañada: no se pisa, se aparta con otro nombre y se intenta el respaldo
        string danada = Path.Combine(Application.persistentDataPath, "partida.dañada-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
        try { File.Move(Ruta, danada); } catch { }
        d = File.Exists(Respaldo) ? LeerDe(Respaldo) : null;
        if (d != null)
        {
            try { File.Copy(Respaldo, Ruta, true); } catch { }
            avisoAlCargar = "La partida guardada estaba dañada: se cargó la copia anterior (unos 30 s antes).";
            return d;
        }
        avisoAlCargar = "La partida guardada estaba dañada y no se pudo leer. Se guardó aparte (" + Path.GetFileName(danada) + ") y empiezas de cero.";
        return null;
    }

    static void RecordarMundo(Datos d)
    {
        if (d.celdasLimpias != null) foreach (Vector2Int c in d.celdasLimpias) MundoInfinito.Limpias.Add(c);
        if (d.mercados != null)
            foreach (MercadoGuardado mg in d.mercados)
            {
                var dic = new Dictionary<string, float>();
                for (int i = 0; i < mg.k.Count && i < mg.v.Count; i++) dic[mg.k[i]] = mg.v[i];
                Mercader.Restaurar(mg.semilla, dic);
            }
    }

    static Datos LeerDe(string ruta)
    {
        try
        {
            string texto = File.ReadAllText(ruta);
            if (string.IsNullOrWhiteSpace(texto) || !texto.TrimStart().StartsWith("{")) return null;
            return JsonUtility.FromJson<Datos>(texto);
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo leer " + Path.GetFileName(ruta) + ": " + e.Message); return null; }
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
        if (nav != null && d.marcas != null) { nav.Marcas.Clear(); nav.Marcas.AddRange(d.marcas); }
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
