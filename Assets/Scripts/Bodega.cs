using System.Collections.Generic;
using UnityEngine;

// Bodega del barco: provisiones para el viaje. Cabe más en un barco más grande.
// La comida guardada aquí se puede comer en el mar con C cuando no llevas nada encima.
// Guarda cualquier cosa de la mochila (materiales, cañones, oro); se maneja desde la mochila (I) y la ventana del barco (B).
public class Bodega : MonoBehaviour
{
    [Tooltip("Unidades que caben. Si es 0, se calcula según el largo del barco (2 por metro).")]
    public int capacidad;

    readonly Dictionary<string, int> cosas = new Dictionary<string, int>();

    void Awake()
    {
        if (capacidad <= 0)
        {
            BoxCollider c = GetComponent<BoxCollider>();
            capacidad = c != null ? Mathf.Max(10, Mathf.RoundToInt(c.size.x * c.size.y * c.size.z * 0.15f)) : 10;   // según el volumen del casco
        }
    }

    // Copia lo que había en otra bodega (al agrandar el barco). Lo que no quepa se pierde.
    public void CopiarDe(Bodega otra)
    {
        foreach (var c in otra.cosas)
        {
            int n = Mathf.Min(c.Value, Libre);
            if (n > 0) cosas[c.Key] = Cantidad(c.Key) + n;
        }
    }

    public IEnumerable<KeyValuePair<string, int>> Todo => cosas;
    public void Fijar(string tipo, int n) => cosas[tipo] = Mathf.Max(0, n);

    public int Cantidad(string tipo) => cosas.TryGetValue(tipo, out int n) ? n : 0;

    public int Total { get { int t = 0; foreach (var c in cosas) t += c.Value; return t; } }

    public int Libre => Mathf.Max(0, capacidad - Total);

    // Mueve del inventario a la bodega lo que quepa. Devuelve cuántas unidades guardó.
    public int Guardar(Inventario inv, string tipo)
    {
        int n = Mathf.Min(inv.Cantidad(tipo), Libre);
        if (n <= 0) return 0;
        inv.Quitar(tipo, n);
        cosas[tipo] = Cantidad(tipo) + n;
        return n;
    }

    // Mueve de la bodega al inventario todo lo de ese tipo. Devuelve cuántas unidades sacó.
    public int Sacar(Inventario inv, string tipo)
    {
        int n = Cantidad(tipo);
        if (n <= 0) return 0;
        cosas[tipo] = 0;
        inv.Agregar(tipo, n);
        return n;
    }

    // Con cantidad: guarda hasta n (lo que quepa). Devuelve cuántas guardó.
    public int Guardar(Inventario inv, string tipo, int n)
    {
        n = Mathf.Min(n, Mathf.Min(inv.Cantidad(tipo), Libre));
        if (n <= 0) return 0;
        inv.Quitar(tipo, n);
        cosas[tipo] = Cantidad(tipo) + n;
        return n;
    }

    // Con cantidad: saca hasta n. Devuelve cuántas sacó.
    public int Sacar(Inventario inv, string tipo, int n)
    {
        n = Mathf.Min(n, Cantidad(tipo));
        if (n <= 0) return 0;
        cosas[tipo] = Cantidad(tipo) - n;
        inv.Fijar(tipo, inv.Cantidad(tipo) + n);   // sin el aviso de "+n" (es un traslado, no algo nuevo)
        return n;
    }

    public bool Consumir(string tipo)
    {
        if (Cantidad(tipo) <= 0) return false;
        cosas[tipo] = Cantidad(tipo) - 1;
        return true;
    }

    public string Resumen() =>
        Total + " / " + capacidad + " (" + Cantidad(Inventario.Coco) + " cocos, " + Cantidad(Inventario.Fruta) + " frutas)";
}
