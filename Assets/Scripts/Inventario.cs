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
    // Moneda (se gana vendiendo al mercader y con el botín)
    public const string Oro = "Oro";
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

    // ---------- Límite de la mochila ----------
    // Caben 300 cosas (el oro y las herramientas no cuentan). Lo que no quepa se queda donde estaba:
    // en el árbol, en la caja de botín o en la bodega. En modo creativo no hay límite.
    public const int Capacidad = 300;
    public static bool SinPeso(string tipo) => tipo == Oro || tipo == Hacha || tipo == Pico;
    public int Carga { get { int n = 0; foreach (var kv in cosas) if (!SinPeso(kv.Key)) n += kv.Value; return n; } }
    public int Espacio => ModoCreativo.Activo ? 1000000 : Mathf.Max(0, Capacidad - Carga);
    public bool Llena => Espacio <= 0;

    // Recibe lo que quepa (recolectar, botín, sacar de la bodega). Devuelve cuánto entró.
    public int Recibir(string tipo, int n)
    {
        if (n <= 0) return 0;
        if (!SinPeso(tipo)) n = Mathf.Min(n, Espacio);
        if (n > 0) Agregar(tipo, n);
        return n;
    }

    public bool Quitar(string tipo, int n)
    {
        if (Cantidad(tipo) < n) return false;
        cosas[tipo] = Cantidad(tipo) - n;
        return true;
    }
}
