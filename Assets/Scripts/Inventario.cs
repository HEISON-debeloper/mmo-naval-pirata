using System.Collections.Generic;
using UnityEngine;

// Inventario del personaje: cantidad de cada cosa que lleva encima.
// La comida se lleva aquí: lo que no recolectes en la isla no lo tendrás en el mar.
public class Inventario : MonoBehaviour
{
    // Materiales
    public const string Madera = "Madera";   // troncos: solo con hacha
    public const string Palo = "Palo";       // ramas sueltas: a mano
    public const string Fibra = "Fibra";     // plantas de fibra: a mano
    public const string Cuerda = "Cuerda";   // se fabrica con fibra
    public const string Piedra = "Piedra";   // piedras sueltas: a mano
    public const string Mineral = "Mineral de hierro";   // vetas de hierro: con pico
    public const string Lingote = "Lingote de hierro";   // se funde en el horno
    public const string Canon = "Cañón";                 // pieza de barco: se instala en una ranura
    // Comida
    public const string Coco = "Coco";
    public const string Fruta = "Fruta";
    // Herramientas
    public const string Hacha = "Hacha";
    public const string Pico = "Pico";

    readonly Dictionary<string, int> cosas = new Dictionary<string, int>();

    public int Cantidad(string tipo) => cosas.TryGetValue(tipo, out int n) ? n : 0;
    public bool Tiene(string tipo) => Cantidad(tipo) > 0;

    // Avisa cuando entra algo a la mochila (la interfaz muestra "+2 madera" un momento)
    public event System.Action<string, int> alRecibir;

    public void Agregar(string tipo, int n)
    {
        cosas[tipo] = Cantidad(tipo) + n;
        if (n > 0) alRecibir?.Invoke(tipo, n);
    }

    // Todo lo que llevas (para guardar la partida)
    public IEnumerable<KeyValuePair<string, int>> Todo => cosas;
    // Pone una cantidad sin avisar en pantalla (al cargar la partida)
    public void Fijar(string tipo, int n) => cosas[tipo] = Mathf.Max(0, n);

    public bool Quitar(string tipo, int n)
    {
        if (Cantidad(tipo) < n) return false;
        cosas[tipo] = Cantidad(tipo) - n;
        return true;
    }
}
