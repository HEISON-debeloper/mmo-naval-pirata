using UnityEngine;
using UnityEngine.UIElements;

// El altar maldito ofrece un pacto: su ventaja (verde), lo que cobra el mar (rojo) y tus pactos actuales.
public class VentanaPacto : Ventana
{
    static VentanaPacto i;
    public static VentanaPacto I => i ??= new VentanaPacto();

    Pactos.Pacto pacto; string isla; Vector3 pos;
    readonly VisualElement cuerpo;

    VentanaPacto() : base("pacto", "Altar maldito", "calavera", 520f, 0f)
    {
        Modal = true; RecordarPosicion = false;
        cuerpo = Estilo.Columna();
        Contenido.Add(cuerpo);
    }

    public static void Ofrecer(Pactos.Pacto p, string isla, Vector3 pos)
    {
        I.pacto = p; I.isla = isla; I.pos = pos;
        Ventanas.Abrir(I);
    }

    protected override float Refrescos => 0f;
    protected override void AlAbrir() => Dibujar();

    void Dibujar()
    {
        cuerpo.Clear();
        if (pacto == null) return;
        var n = Estilo.Etiqueta(pacto.nombre, Estilo.TamTitulo, new Color(0.45f, 1f, 0.6f), Estilo.FuenteNegrita);
        cuerpo.Add(n);
        var lema = Estilo.Etiqueta("«" + pacto.lema + "»", Estilo.TamTextoChico, Estilo.TextoTenue);
        lema.style.whiteSpace = WhiteSpace.Normal; lema.style.marginTop = 4f; lema.style.marginBottom = 10f; lema.style.unityFontStyleAndWeight = FontStyle.Italic;
        cuerpo.Add(lema);
        cuerpo.Add(Linea("Te da", pacto.ventaja, Estilo.Bien));
        cuerpo.Add(Linea("Te cobra", pacto.precio, Estilo.Mal));
        var nota = Estilo.Pequena("El pacto se ata a tu barco. Si el barco se hunde, el mar cobra la deuda y pierdes todos tus pactos. Tu barco carga hasta " + Pactos.Maximo + ".");
        nota.style.whiteSpace = WhiteSpace.Normal; nota.style.marginTop = 10f;
        cuerpo.Add(nota);
        if (Pactos.Activos.Count > 0)
        {
            var act = Estilo.Pequena("Tus pactos (" + Pactos.Activos.Count + "/" + Pactos.Maximo + "): " + Pactos.Resumen());
            act.style.whiteSpace = WhiteSpace.Normal; act.style.marginTop = 6f;
            cuerpo.Add(act);
        }
        var fila = Estilo.Fila(10f); fila.style.marginTop = 14f; fila.style.justifyContent = Justify.FlexEnd;
        fila.Add(Componentes.Boton("Rechazar", () => Ventanas.Cerrar(this), "cerrar", Componentes.Variante.Fantasma, 36f));
        fila.Add(Componentes.Boton("Sellar el pacto", () =>
        {
            string error = Pactos.Aceptar(pacto, isla, pos);
            if (error != null) { Notificaciones.Alerta(error); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            Ventanas.Cerrar(this);
        }, "calavera", Componentes.Variante.Peligro, 36f));
        cuerpo.Add(fila);
    }

    static VisualElement Linea(string titulo, string texto, Color c)
    {
        var f = Estilo.Fila(8f, Align.FlexStart); f.style.marginTop = 4f;
        var t = Estilo.Etiqueta(titulo, Estilo.TamTexto, c, Estilo.FuenteNegrita); t.style.width = 80f; t.style.flexShrink = 0f;
        var d = Estilo.Etiqueta(texto, Estilo.TamTexto, Estilo.Texto); d.style.whiteSpace = WhiteSpace.Normal; d.style.flexShrink = 1f; d.style.minWidth = 0f;
        f.Add(t); f.Add(d);
        return f;
    }
}
