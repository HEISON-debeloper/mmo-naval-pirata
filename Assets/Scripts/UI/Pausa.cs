using UnityEngine;
using UnityEngine.UIElements;

// Pausa (Esc sin ninguna ventana abierta): el juego se detiene. Continuar, opciones, controles, bitácora,
// guardar, volver al menú principal o salir del juego (con confirmación).
public class Pausa : Ventana
{
    static Pausa i;
    public static Pausa I => i ??= new Pausa();
    public static bool Visible => i != null && i.Abierta;
    float escalaAntes = 1f;

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
        B("Opciones", "engranaje", () => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaOpciones.I); });
        B("Controles y guía", "libro", () => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaAyuda.I); });
        B("Bitácora", "pergamino", () => { Ventanas.Cerrar(this); Ventanas.Abrir(VentanaBitacora.I); });
        B("Guardar la partida", "check", () => { var g = Object.FindFirstObjectByType<Guardado>(); if (g != null) g.GuardarAhora(); });
        B("Menú principal", "casa", () => { Ventanas.Cerrar(this); MenuPrincipal.Mostrar(); });
        B("Salir del juego", "salir", () => Ventanas.Confirmar("Salir del juego", "La partida se guarda al salir.", "Salir", MenuPrincipal.Salir, true), Componentes.Variante.Peligro);
    }

    protected override float Refrescos => 0f;

    protected override void AlAbrir()
    {
        escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    protected override void AlCerrar()
    {
        Time.timeScale = escalaAntes;
        AudioListener.pause = false;
    }
}
