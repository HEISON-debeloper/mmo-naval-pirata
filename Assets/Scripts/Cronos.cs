using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Cronómetros por sección para encontrar lo que traba un cuadro (los muestra el bot de prueba en su informe).
// Cuesta casi nada: solo guarda el peor tiempo y el total de cada sección.
public static class Cronos
{
    static readonly Dictionary<string, (double peor, double total, int veces)> datos = new Dictionary<string, (double, double, int)>();
    static readonly Stopwatch reloj = Stopwatch.StartNew();

    public static double Ahora => reloj.Elapsed.TotalMilliseconds;

    // Uso: double t = Cronos.Ahora; ...; Cronos.Anotar("casco", t);
    public static void Anotar(string seccion, double desde)
    {
        double ms = Ahora - desde;
        datos.TryGetValue(seccion, out var d);
        datos[seccion] = (System.Math.Max(d.peor, ms), d.total + ms, d.veces + 1);
    }

    public static string Informe()
    {
        var l = new List<KeyValuePair<string, (double peor, double total, int veces)>>(datos);
        l.Sort((a, b) => b.Value.peor.CompareTo(a.Value.peor));
        var sb = new StringBuilder();
        foreach (var kv in l)
            sb.AppendLine("  " + kv.Key + ": peor " + kv.Value.peor.ToString("0") + " ms · promedio " + (kv.Value.total / kv.Value.veces).ToString("0.0") + " ms · " + kv.Value.veces + " veces");
        return sb.ToString();
    }
}
