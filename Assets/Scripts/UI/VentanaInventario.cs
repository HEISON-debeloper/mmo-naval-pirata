using UnityEngine;
using UnityEngine.UIElements;

// (Provisional) Inventario: se construye en la etapa 3 de la renovación de interfaz.
public class VentanaInventario : Ventana
{
    static VentanaInventario i;
    public static VentanaInventario I => i ??= new VentanaInventario();
    VentanaInventario() : base("inventario", "Mochila", "caja", 520f) { Contenido.Add(Estilo.Etiqueta("Pronto…")); }
}
