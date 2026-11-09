using UnityEngine;

// Teclas que abren y cierran ventanas (I, B, M, G, F1…). Funcionan aunque haya otra ventana abierta,
// salvo mientras se escribe en un campo de texto o se está eligiendo una tecla en Opciones.
public static class AtajosUI
{
    internal static void Actualizar()
    {
        if (Personaje.Instancia == null || MenuPrincipal.Visible) return;
        if (Teclas.Pulsada(Accion.Ayuda, true)) Ventanas.Alternar(VentanaAyuda.I);
        if (Teclas.Pulsada(Accion.Inventario, true)) Ventanas.Alternar(VentanaInventario.I);
    }
}
