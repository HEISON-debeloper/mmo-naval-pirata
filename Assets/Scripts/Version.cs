// Versión del juego. Cada cambio que se entrega sube este número y queda guardado en GitHub
// en la rama versiones/vX.Y.Z (por ejemplo versiones/v0.18.0) y explicado en CHANGELOG.md.
//   0.X.0 = algo nuevo (una mecánica, un sistema)
//   0.X.Y = arreglos o ajustes pequeños de esa versión
// Al llegar a la primera versión jugable completa (para publicar) pasará a 1.0.0.
public static class Version
{
    public const string Numero = "0.42.1";
    public const string Nombre = "Sin parpadeo en la popa y modelos sólidos";
    public static string Texto => "v" + Numero;
}
