using System.Collections.Generic;
using UnityEngine;

// Marca una isla: el mar se calma cerca de su orilla (aguas poco profundas) y la navegación la puede descubrir.
public class Isla : MonoBehaviour
{
    public static readonly List<Isla> Todas = new List<Isla>();

    [Tooltip("Radio de la isla en metros (el de la playa es 1.2 veces este).")]
    public float radio = 40f;
    [Tooltip("Identificador único (las del mundo infinito usan su celda). Vacío = su nombre.")]
    public string id;
    [Tooltip("La creó el mundo infinito (no es de la escena).")]
    public bool generada;

    public string Id => string.IsNullOrEmpty(id) ? name : id;

    // Datos copiados para que Olas no toque el Transform miles de veces por frame
    [System.NonSerialized] public float cx, cz, orilla;

    void OnEnable()
    {
        Refrescar();
        if (!Todas.Contains(this)) Todas.Add(this);
    }

    // Vuelve a leer posición y radio (llamarlo si se cambian después de crearla)
    public void Refrescar()
    {
        cx = transform.position.x;
        cz = transform.position.z;
        orilla = 1.2f * radio;
        Olas.IslasCambiaron = true;
    }

    void OnDisable() { Todas.Remove(this); Olas.IslasCambiaron = true; }
}
