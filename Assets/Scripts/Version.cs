// Versión del juego. Cada cambio que se entrega sube este número y queda guardado en GitHub
// con la misma etiqueta (por ejemplo v0.17.0) y explicado en CHANGELOG.md.
//   0.X.0 = algo nuevo (una mecánica, un sistema)
//   0.X.Y = arreglos o ajustes pequeños de esa versión
// Al llegar a la primera versión jugable completa (para publicar) pasará a 1.0.0.
public static class Version
{
    public const string Numero = "0.17.0";
    public const string Nombre = "Sistema de versiones";
    public static string Texto => "v" + Numero;
}
