using UnityEngine;
using UnityEngine.InputSystem;

// Teclas de prueba para el prototipo (se crean solas al darle Play; borra este archivo para quitarlas).
// 1 = dañar el bergantín 25 · 2 = traerlo a 30 m por estribor · 3 = pegarlo al costado para abordar
// 4 = materiales de la balsa (madera y cuerda) y +2 cocos · 5 = ir junto a la palma más cercana
public class TeclasPrueba : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Solo en el editor o en una compilación de desarrollo ("Development Build"): en el juego publicado no existen
    internal static void Crear() { if (Application.isEditor || Debug.isDebugBuild) new GameObject("Teclas de prueba").AddComponent<TeclasPrueba>(); }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (!k.anyKey.wasPressedThisFrame) return;   // nada que hacer: no buscar enemigos cada frame
        Personaje p = Personaje.Instancia;
        if (p == null || Fabricacion.Abierto || Teclas.Capturando) return;   // con una ventana abierta, las teclas son de la ventana
        IABarco ia = IABarco.MasCercano(p.transform.position);

        if (ia != null && k.digit1Key.wasPressedThisFrame) ia.GetComponent<VidaBarco>().Dano(25f);
        if (ia != null && k.digit2Key.wasPressedThisFrame) Traer(ia, p, 30f);
        if (ia != null && k.digit3Key.wasPressedThisFrame) Traer(ia, p, 9f);
        if (k.vKey.wasPressedThisFrame)
        {
            if (ia == null) p.Avisar("Prueba: no hay piratas cerca (aléjate del inicio con K)");
            else { ia.ForzarAbordaje(); Traer(ia, p, 40f); p.Avisar("Prueba: " + ia.name + " viene a abordarte"); }
        }
        if (k.digit4Key.wasPressedThisFrame)
        {
            Inventario inv = p.GetComponent<Inventario>();
            if (inv != null)
            {
                inv.Agregar(Inventario.Madera, 100);
                inv.Agregar(Inventario.Cuerda, 40);
                inv.Agregar(Inventario.Piedra, 20);
                inv.Agregar(Inventario.Palo, 10);
                inv.Agregar(Inventario.Fibra, 40);
                inv.Agregar(Inventario.Mineral, 8);
                inv.Agregar(Inventario.Lingote, 12);
                inv.Agregar(Inventario.Canon, 2);
                inv.Agregar(Inventario.Coco, 3);
                inv.Agregar(Inventario.Fruta, 3);
            }
        }
        if (k.digit5Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.Palma);
        if (k.digit6Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.Fibra);
        if (k.digit7Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.Rama);
        if (k.digit8Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.Piedra);
        if (k.digit9Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.Arbusto);
        if (k.digit0Key.wasPressedThisFrame) IrA(p, Recurso.Tipo.RocaGrande);
        if (k.hKey.wasPressedThisFrame) IrA(p, Recurso.Tipo.Hierro);
        if (k.jKey.wasPressedThisFrame) IrAIslaNueva(p);
        if (k.uKey.wasPressedThisFrame) MasLejos(p);
        if (k.xKey.wasPressedThisFrame) { Clima.Forzado = Clima.Forzado >= 3 ? -1 : Clima.Forzado + 1; p.Avisar("Prueba: clima " + (Clima.Forzado < 0 ? "según la zona" : ((Clima.Tiempo)Clima.Forzado).ToString())); }   // X: cambiar el clima
        if (k.oKey.wasPressedThisFrame) { CicloDia.Hora = Mathf.Repeat(CicloDia.Hora + 3f, 24f); p.Avisar("Prueba: son las " + CicloDia.Texto); }   // O: +3 horas
        if (k.zKey.wasPressedThisFrame && !p.BajarPrueba()) p.Avisar("Prueba: sube a un barco con interior para bajar");   // Z: bajar un nivel   // U (la K es la espada)
        if (k.lKey.wasPressedThisFrame) IrACaja(p);
        if (k.nKey.wasPressedThisFrame) IrAMercader(p);
        if (k.f9Key.wasPressedThisFrame) { Apariencia.BesosCapitanes = Mathf.Max(Apariencia.BesosCapitanes, Apariencia.CapitanesParaLuwyn - 1); Apariencia.BesosCanoneros = Mathf.Max(Apariencia.BesosCanoneros, Apariencia.CanonerosParaLuwyn); Apariencia.ContarBeso(Tripulante.Rol.Capitan); }   // F9: desbloquear a Luwyn
        if (k.f10Key.wasPressedThisFrame)   // F10: ir al altar maldito más cercano
        {
            if (Pactos.AltarCercano(p.transform.position, out Vector3 altar)) p.Teletransportar(altar + new Vector3(2.5f, 1.5f, 0f));
            else p.Avisar("Prueba: no hay altares cargados cerca (están en islas a más de 1,2 km del inicio)");
        }
        if (k.f7Key.wasPressedThisFrame) { Punteria.forzar = !Punteria.forzar; p.Avisar("Prueba: puntería fija " + (Punteria.forzar ? "activada" : "desactivada")); }
        if (k.yKey.wasPressedThisFrame && !p.IrAlTimonPropio()) p.Avisar("Prueba: aún no tienes barco propio");
    }

    // Lleva tu barco (contigo al timón) 800 m más lejos del inicio, para probar aguas más peligrosas
    static void MasLejos(Personaje p)
    {
        if (p.estado != Personaje.Estado.Timon || p.BarcoActual == null) { p.Avisar("Prueba: ponte al timón de tu barco primero"); return; }
        Rigidbody rb = p.BarcoActual.GetComponent<Rigidbody>();
        Vector3 afuera = rb.position; afuera.y = 0f;
        afuera = afuera.sqrMagnitude > 1f ? afuera.normalized : Vector3.forward;
        for (int i = 0; i < 20; i++)
        {
            Vector3 destino = rb.position + afuera * 800f;
            destino.y = rb.position.y;
            if (Balsa.HayAguaPara(destino, p.BarcoActual.transform.forward, 60f, 2f))
            {
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.position = destino; p.BarcoActual.transform.position = destino;
                p.Avisar("Prueba: " + Mathf.RoundToInt(new Vector2(destino.x, destino.z).magnitude) + " m del inicio");
                return;
            }
            afuera = Quaternion.Euler(0f, 18f, 0f) * afuera;   // si hay isla, prueba otro rumbo
        }
    }

    // Lleva al personaje (a pie) frente al mercader más cercano
    static void IrAMercader(Personaje p)
    {
        Mercader mejor = null;
        float dMejor = float.MaxValue;
        foreach (Mercader m in Mercader.Todos)
        {
            if (m == null) continue;
            float d = Vector3.Distance(m.transform.position, p.transform.position);
            if (d < dMejor) { dMejor = d; mejor = m; }
        }
        if (mejor == null) { p.Avisar("Prueba: no hay mercaderes cargados cerca"); return; }
        p.Teletransportar(mejor.transform.position + mejor.transform.forward * 3f + Vector3.up * 1.5f);
    }

    // Lleva tu barco (o a ti, si vas a pie) junto a la caja de botín más cercana
    static void IrACaja(Personaje p)
    {
        CajaBotin mejor = null;
        float dMejor = float.MaxValue;
        foreach (CajaBotin c in CajaBotin.Todas)
        {
            float d = Vector3.Distance(c.transform.position, p.transform.position);
            if (d < dMejor) { dMejor = d; mejor = c; }
        }
        if (mejor == null) { p.Avisar("Prueba: no hay cajas de botín (hunde un pirata)"); return; }
        Vector3 destino = mejor.transform.position + Vector3.right * 2f;
        if (p.estado != Personaje.Estado.Libre && p.BarcoActual != null)
        {
            Rigidbody rb = p.BarcoActual.GetComponent<Rigidbody>();
            destino.y = rb.position.y;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = destino; p.BarcoActual.transform.position = destino;
        }
        else p.Teletransportar(destino + Vector3.up);
        p.Avisar("Prueba: junto a una caja de botín (" + Mathf.RoundToInt(dMejor) + " m)");
    }

    // Lleva al personaje a la playa de la isla sin descubrir más cercana (a pie)
    static void IrAIslaNueva(Personaje p)
    {
        if (p.estado != Personaje.Estado.Libre) { p.Avisar("Prueba: bájate del barco primero (F)"); return; }
        Navegacion nav = Navegacion.Instancia;
        Isla mejor = null;
        float dMejor = float.MaxValue;
        foreach (Isla i in Isla.Todas)
        {
            if (i == null || (nav != null && nav.Descubierta(i))) continue;
            float d = Vector3.Distance(i.transform.position, p.transform.position);
            if (d < dMejor) { dMejor = d; mejor = i; }
        }
        if (mejor == null) { p.Avisar("Prueba: no hay islas nuevas cerca"); return; }
        Vector3 hacia = p.transform.position - mejor.transform.position; hacia.y = 0f;
        hacia = hacia.sqrMagnitude > 1f ? hacia.normalized : Vector3.forward;
        p.Teletransportar(mejor.transform.position + hacia * mejor.radio * 0.8f + Vector3.up * 4f);
    }

    // Lleva al personaje junto al recurso de ese tipo más cercano que no esté agotado
    static void IrA(Personaje p, Recurso.Tipo tipo)
    {
        Recurso mejor = null;
        float dMejor = float.MaxValue;
        foreach (Recurso r in Recurso.Todos)
        {
            if (r.tipo != tipo || r.Agotado) continue;
            float d = Vector3.Distance(r.transform.position, p.transform.position);
            if (d < dMejor) { dMejor = d; mejor = r; }
        }
        if (mejor == null) return;
        Collider c = mejor.GetComponentInChildren<Collider>();
        float borde = c != null && (tipo == Recurso.Tipo.RocaGrande || tipo == Recurso.Tipo.Hierro) ? c.bounds.extents.x : 0f;   // no aparecer dentro de la roca
        p.Teletransportar(mejor.transform.position + new Vector3(1.2f + borde, 2f, 0f));
    }

    public static void Traer(IABarco ia, Personaje p, float distancia)
    {
        if (p.barco == null) { p.Avisar("Prueba: primero necesitas un barco propio (construye una balsa)"); return; }
        Rigidbody jug = p.barco.GetComponent<Rigidbody>();
        Rigidbody enemigo = ia.GetComponent<Rigidbody>();
        Vector3 derecha = Vector3.ProjectOnPlane(jug.transform.right, Vector3.up).normalized;
        Vector3 pos = jug.position + derecha * distancia;
        Quaternion rot = Quaternion.Euler(0f, jug.transform.eulerAngles.y, 0f);
        enemigo.linearVelocity = Vector3.zero;
        enemigo.angularVelocity = Vector3.zero;
        enemigo.position = pos;
        enemigo.rotation = rot;
        ia.transform.SetPositionAndRotation(pos, rot);
    }
}
