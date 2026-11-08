using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Optimización general (se crea solo al darle Play):
// 1) Las cosas pequeñas dejan de dibujarse lejos de la cámara (por capas):
//    detalles de isla (fibra, ramas, piedras, frutas, cocos) a 180 m, palmas/rocas/vetas a 550 m,
//    cañones y tripulantes de los barcos a 400 m. Islas y cascos se siguen viendo de lejos.
// 2) Las sombras solo se calculan cerca (120 m).
// 3) Los barcos enemigos lejanos "duermen": sin física, sin IA, sin tripulación moviéndose.
//    Se despiertan solos cuando te acercas.
public class Rendimiento : MonoBehaviour
{
    public const int CapaBarcoDetalle = 27;
    public const int CapaDetalle = 28;
    public const int CapaMedio = 29;

    const float DistDetalle = 180f, DistMedio = 550f, DistBarcoDetalle = 400f;
    const float DistanciaSombras = 120f;
    const float Dormir = 650f, Despertar = 550f;   // margen para que no se prendan y apaguen en el borde

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() => new GameObject("Rendimiento").AddComponent<Rendimiento>();

    // Pone la capa a un objeto y a todos sus hijos
    public static void Capa(GameObject g, int capa)
    {
        if (g == null) return;
        g.layer = capa;
        foreach (Transform h in g.transform) Capa(h.gameObject, capa);
    }

    Camera camConfigurada;
    float tRevisar;
    readonly HashSet<IABarco> dormidos = new HashSet<IABarco>();

    void Start()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.shadowDistance > DistanciaSombras)
            urp.shadowDistance = DistanciaSombras;
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam != null && cam != camConfigurada)
        {
            float[] d = new float[32];
            d[CapaBarcoDetalle] = DistBarcoDetalle;
            d[CapaDetalle] = DistDetalle;
            d[CapaMedio] = DistMedio;
            cam.layerCullDistances = d;
            camConfigurada = cam;
        }

        tRevisar -= Time.deltaTime;
        if (tRevisar > 0f) return;
        tRevisar = 0.5f;

        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Vector3 yo = p.transform.position;
        for (int i = IABarco.Registrados.Count - 1; i >= 0; i--)
        {
            IABarco ia = IABarco.Registrados[i];
            if (ia == null) { IABarco.Registrados.RemoveAt(i); continue; }
            VidaBarco v = ia.GetComponent<VidaBarco>();
            if (v != null && v.Hundido) continue;   // uno que se hunde termina su animación
            ControlBarco cb = ia.GetComponent<ControlBarco>();
            bool tuyo = ia.Capturado || (cb != null && (p.barco == cb || p.BarcoActual == cb));
            Vector3 d = ia.transform.position - yo; d.y = 0f;
            float dist = d.magnitude;
            bool dormido = dormidos.Contains(ia);
            if (!dormido && !tuyo && dist > Dormir) { Poner(ia, false); dormidos.Add(ia); }
            else if (dormido && (tuyo || dist < Despertar)) { Poner(ia, true); dormidos.Remove(ia); }
        }
        dormidos.RemoveWhere(x => x == null);
    }

    static void Poner(IABarco ia, bool activo)
    {
        Rigidbody rb = ia.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!activo) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = !activo;
        }
        Flotacion f = ia.GetComponent<Flotacion>();
        if (f != null) f.activa = activo;
        ia.enabled = activo;
        ControlBarco cb = ia.GetComponent<ControlBarco>();
        if (cb != null) cb.enabled = activo;
        Canones c = ia.GetComponent<Canones>();
        if (c != null) c.enabled = activo;
        foreach (Tripulante t in ia.GetComponentsInChildren<Tripulante>()) t.enabled = activo;
    }
}
