using UnityEngine;
using UnityEngine.UIElements;

// Pausa (Esc sin ninguna ventana abierta): el juego se detiene. Continuar, opciones, controles, bitácora,
// guardar, volver al menú principal o salir del juego (con confirmación).
public class Pausa : Ventana
{
    static Pausa i;
    public static Pausa I => i ??= new Pausa();
    public static bool Visible => i != null && i.Abierta;
    static float escalaAntes = 1f;
    static bool detenido;
    // Una ventana abierta desde la pausa (opciones, guía, bitácora, confirmar salida): el juego sigue detenido
    // mientras está abierta y al cerrarla se vuelve a la pausa
    public static bool Retenida { get; private set; }
    static void DesdePausa(System.Action abrir) { Retenida = true; abrir(); }

    Pausa() : base("pausa", "Pausa", "pausa", 380f)
    {
        Modal = true; RecordarPosicion = false; Arrastrable = false;
        var sub = Estilo.Pequena("El juego está detenido. " + Version.Texto + " · " + Version.Nombre);
        sub.style.marginBottom = 12f; sub.style.unityTextAlign = TextAnchor.MiddleCenter;
        Contenido.Add(sub);
        void B(string t, string ic, System.Action a, Componentes.Variante v = Componentes.Variante.Normal)
        {
            var b = Componentes.Boton(t, a, ic, v, 42f);
            b.style.marginBottom = 8f;
            Contenido.Add(b);
        }
        B("Continuar", "play", () => Ventanas.Cerrar(this), Componentes.Variante.Primario);
        B("Opciones", "engranaje", () => DesdePausa(() => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaOpciones.I); }));
        B("Controles y guía", "libro", () => DesdePausa(() => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaAyuda.I); }));
        B("Bitácora", "pergamino", () => DesdePausa(() => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaBitacora.I); }));
        B("Guardar la partida", "check", () => { var g = Object.FindFirstObjectByType<Guardado>(); if (g != null) g.GuardarAhora(); });
        B("Menú principal", "casa", () => { Ventanas.Cerrar(this); MenuPrincipal.Mostrar(); });
        B("Salir del juego", "salir", () => DesdePausa(() => Ventanas.Confirmar("Salir del juego", "La partida se guarda al salir.", "Salir", MenuPrincipal.Salir, true)), Componentes.Variante.Peligro);
    }

    protected override float Refrescos => 0f;

    // Cada cuadro (desde Lienzo): detiene el tiempo con la pausa abierta o con una ventana que se abrió desde ella
    internal static void ControlarTiempo()
    {
        bool sub = Retenida && !Visible && (Ventanas.HayModal || Ventanas.HayAbiertas);
        if (Retenida && !Visible && !sub)
        {
            Retenida = false;
            if (!MenuPrincipal.Visible && Personaje.Instancia != null) Ventanas.Abrir(I);   // al cerrar esa ventana, de vuelta a la pausa
        }
        if (MenuPrincipal.Visible) Retenida = false;
        bool detener = Visible || sub;
        if (detener && !detenido) { escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; AudioListener.pause = true; detenido = true; }
        else if (!detener && detenido) { Time.timeScale = escalaAntes; AudioListener.pause = false; detenido = false; }
    }
}
