using System.Collections.Generic;
using UnityEngine;

// Catálogo de todo lo que cabe en la mochila: nombre bonito, descripción, categoría, icono y color.
// Así el inventario, la bodega, el mercado y la fabricación hablan del mismo objeto de la misma manera.
public static class Objetos
{
    public enum Categoria { Materiales, Comida, Herramientas, Armamento, Valor }

    public class Info
    {
        public string tipo, nombre, descripcion, icono, origen;
        public Categoria categoria;
        public Color color;
        public bool apilable = true;
        public int orden;
    }

    static readonly Dictionary<string, Info> catalogo = new Dictionary<string, Info>();
    static readonly List<Info> lista = new List<Info>();

    static Objetos()
    {
        Color madera = new Color(0.72f, 0.52f, 0.30f), verde = new Color(0.55f, 0.75f, 0.38f), gris = new Color(0.72f, 0.72f, 0.70f),
              hierro = new Color(0.62f, 0.64f, 0.70f), oro = Estilo.Oro, fruta = new Color(0.90f, 0.42f, 0.34f), coco = new Color(0.62f, 0.45f, 0.30f);
        Agregar(Inventario.Madera, "Madera", "Troncos para construir y mejorar el casco. Se corta con el hacha.", Categoria.Materiales, "madera", madera, "Árboles de la isla, con hacha");
        Agregar(Inventario.Palo, "Palo", "Ramas sueltas. Sirven para herramientas, remos y para encender el horno.", Categoria.Materiales, "palo", madera, "Ramas en el suelo, a mano");
        Agregar(Inventario.Fibra, "Fibra", "Hojas de plantas fibrosas. Con ellas se trenza cuerda.", Categoria.Materiales, "fibra", verde, "Plantas de fibra, a mano");
        Agregar(Inventario.Cuerda, "Cuerda", "Cuerda trenzada para velas, jarcia y herramientas.", Categoria.Materiales, "cuerda", new Color(0.80f, 0.70f, 0.48f), "Se fabrica con fibra");
        Agregar(Inventario.Piedra, "Piedra", "Piedras sueltas para herramientas y el horno.", Categoria.Materiales, "piedra", gris, "Piedras en la playa, a mano");
        Agregar(Inventario.Mineral, "Mineral de hierro", "Veta sin fundir. En el horno se convierte en lingotes.", Categoria.Materiales, "mineral", new Color(0.55f, 0.42f, 0.38f), "Vetas de hierro, con pico");
        Agregar(Inventario.Lingote, "Lingote de hierro", "Hierro fundido y listo para forjar cañones y herrajes.", Categoria.Materiales, "lingote", hierro, "Se funde en el horno");
        Agregar(Inventario.Coco, "Coco", "Quita algo de hambre y bastante sed.", Categoria.Comida, "coco", coco, "Palmeras de la isla");
        Agregar(Inventario.Fruta, "Fruta", "Quita bastante hambre y un poco de sed.", Categoria.Comida, "fruta", fruta, "Arbustos frutales de la isla");
        Agregar(Inventario.Hacha, "Hacha", "Con ella se cortan árboles para sacar madera.", Categoria.Herramientas, "hacha", hierro, "Se fabrica con palos, piedra y cuerda");
        Agregar(Inventario.Pico, "Pico", "Con él se pican las vetas de hierro.", Categoria.Herramientas, "pico", hierro, "Se fabrica con palos, piedra y cuerda");
        Agregar(Inventario.Canon, "Cañón", "Pieza de artillería. Se instala en una porta del barco.", Categoria.Armamento, "canon", new Color(0.45f, 0.45f, 0.48f), "Se forja con lingotes y madera");
        Agregar(Inventario.Oro, "Oro", "Monedas. Se ganan vendiendo al mercader y con el botín de otros barcos.", Categoria.Valor, "moneda", oro, "Mercaderes y botín");
        catalogo[Inventario.Hacha].apilable = false;
        catalogo[Inventario.Pico].apilable = false;
    }

    static void Agregar(string tipo, string nombre, string descripcion, Categoria cat, string icono, Color color, string origen)
    {
        var i = new Info { tipo = tipo, nombre = nombre, descripcion = descripcion, categoria = cat, icono = icono, color = color, origen = origen, orden = lista.Count };
        catalogo[tipo] = i;
        lista.Add(i);
    }

    public static Info De(string tipo)
    {
        if (tipo != null && catalogo.TryGetValue(tipo, out Info i)) return i;
        return new Info { tipo = tipo ?? "?", nombre = tipo ?? "?", descripcion = "", categoria = Categoria.Materiales, icono = "caja", color = Estilo.TextoTenue, orden = 99 };
    }

    public static IReadOnlyList<Info> Todos => lista;
    public static string Nombre(string tipo) => De(tipo).nombre;
    public static string NombreCategoria(Categoria c)
    {
        switch (c)
        {
            case Categoria.Materiales: return "Materiales";
            case Categoria.Comida: return "Comida";
            case Categoria.Herramientas: return "Herramientas";
            case Categoria.Armamento: return "Armamento";
            default: return "Valor";
        }
    }
    public static string IconoCategoria(Categoria c)
    {
        switch (c)
        {
            case Categoria.Materiales: return "madera";
            case Categoria.Comida: return "fruta";
            case Categoria.Herramientas: return "hacha";
            case Categoria.Armamento: return "canon";
            default: return "moneda";
        }
    }

    // Texto del tooltip de una casilla
    public static string Descripcion(string tipo, int cantidad)
    {
        Info i = De(tipo);
        string s = "<b>" + i.nombre + "</b>";
        if (cantidad > 0) s += "  ×" + cantidad;
        s += "\n" + i.descripcion;
        if (!string.IsNullOrEmpty(i.origen)) s += "\n<i>" + i.origen + "</i>";
        return s;
    }

    // Nombre corto para listas de costo ("3 madera, 2 cuerda")
    public static string Corto(string tipo)
    {
        if (tipo == Inventario.Mineral) return "mineral";
        if (tipo == Inventario.Lingote) return "lingote";
        return De(tipo).nombre.ToLowerInvariant();
    }

    public static string Plural(string tipo, int n)
    {
        string c = Corto(tipo);
        if (n == 1) return c;
        if (c.EndsWith("z")) return c.Substring(0, c.Length - 1) + "ces";
        if (c.EndsWith("n") || c.EndsWith("r") || c.EndsWith("l") || c.EndsWith("d")) return c + "es";
        return c + "s";
    }
}
