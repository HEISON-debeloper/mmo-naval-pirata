using UnityEngine;
using UnityEngine.UIElements;

// Tu pirata (tecla T): camisa, piel, maquillaje, falda, barba y banderita en la espalda. Se ve al momento y es gratis.
// Abajo, Luwyn Antena Caída: bloqueado hasta besar a 5 capitanes y 3 cañoneros; luego se elige con un botón
// (y su barco se pone la crisopa de insignia).
public class VentanaApariencia : Ventana
{
    static VentanaApariencia i;
    public static VentanaApariencia I => i ??= new VentanaApariencia();

    readonly ScrollView lista;

    VentanaApariencia() : base("apariencia", "Tu pirata", "persona", 560f, 640f)
    {
        lista = Componentes.Lista();
        Contenido.Add(lista);
    }

    protected override float Refrescos => 0f;
    protected override void AlAbrir() => Dibujar();

    static Apariencia.Datos D => Apariencia.Jugador;

    void Cambiar(System.Action cambio)
    {
        cambio();
        Apariencia.AplicarJugador();
        Dibujar();
    }

    void Dibujar()
    {
        lista.Clear();
        Grupo("CAMISA", Nombres(Apariencia.Camisas), () => D.camisa, v => D.camisa = v, null);
        Grupo("PIEL", Nombres(Apariencia.Pieles), () => D.piel, v => D.piel = v, null);
        Grupo("MAQUILLAJE", Apariencia.Maquillajes, () => D.maquillaje, v => D.maquillaje = v, null);
        Grupo("FALDA", Apariencia.Faldas, () => D.falda, v => D.falda = v, "falda");
        Grupo("BARBA", Apariencia.Barbas, () => D.barba, v => D.barba = v, "barba");
        Grupo("BANDERITA EN LA ESPALDA", Apariencia.Banderitas, () => D.banderita, v => D.banderita = v, null);
        Grupo("ANTENA", new[] { "Sin antena", "Antena caída" }, () => D.antena ? 1 : 0, v => D.antena = v == 1, "antena");

        // Luwyn Antena Caída
        var tarjeta = Estilo.Columna();
        tarjeta.style.marginTop = 14f;
        Estilo.PanelPergamino(tarjeta);
        Estilo.Relleno(tarjeta, 12f);
        tarjeta.Add(Estilo.Etiqueta(Apariencia.NombreLuwyn, Estilo.TamTitulo * 0.9f, Estilo.Tinta, Estilo.FuenteNegrita));
        var desc = Estilo.Etiqueta("Barba loca, la falda más linda (rosada, de volantes), maquillaje completo y una antena caída sobre el sombrero. " +
                                   "Su barco lleva una crisopa de insignia: en el mascarón y en la bandera.", Estilo.TamTextoChico, Estilo.Tinta);
        desc.style.whiteSpace = WhiteSpace.Normal; desc.style.marginTop = 4f;
        tarjeta.Add(desc);
        if (!Apariencia.LuwynDesbloqueado)
        {
            var bl = Estilo.Etiqueta("Bloqueado. Besa (" + Teclas.Nombre(Accion.Usar) + " junto a ellos, en su cubierta) a " + Apariencia.CapitanesParaLuwyn +
                                     " capitanes y " + Apariencia.CanonerosParaLuwyn + " cañoneros piratas.\n" + Apariencia.Progreso, Estilo.TamTextoChico, Estilo.Tinta, Estilo.FuenteNegrita);
            bl.style.whiteSpace = WhiteSpace.Normal; bl.style.marginTop = 8f;
            tarjeta.Add(bl);
        }
        else
        {
            var fila = Estilo.Fila(8f); fila.style.marginTop = 10f;
            if (Apariencia.EsLuwyn)
            {
                fila.Add(Componentes.Boton("Volver a mi pirata", () => Cambiar(() => Apariencia.Jugador = new Apariencia.Datos { camisa = 0, piel = D.piel, banderita = D.banderita }), "persona", Componentes.Variante.Normal, 34f));
                fila.Add(Estilo.Pequena("Eres " + Apariencia.NombreLuwyn, Estilo.Tinta));
            }
            else
                fila.Add(Componentes.Boton("Ser " + Apariencia.NombreLuwyn, () => Cambiar(() =>
                {
                    Apariencia.Jugador = Apariencia.DeLuwyn();
                    Apariencia.InsigniaCrisopa();
                    Notificaciones.Mostrar("Ahora eres " + Apariencia.NombreLuwyn + ". Tu barco lleva la crisopa de insignia", Notificaciones.Tipo.Exito, 5f);
                }), "persona", Componentes.Variante.Primario, 34f));
            tarjeta.Add(fila);
        }
        lista.Add(tarjeta);
    }

    static string[] Nombres((string nombre, Color color)[] l)
    {
        var r = new string[l.Length];
        for (int k = 0; k < l.Length; k++) r[k] = l[k].nombre;
        return r;
    }

    void Grupo(string titulo, string[] opciones, System.Func<int> actual, System.Action<int> poner, string bloqueo)
    {
        var t = Estilo.Etiqueta(titulo, Estilo.TamTextoChico * 0.9f, Estilo.Oro, Estilo.FuenteNegrita);
        t.style.letterSpacing = 1.5f; t.style.marginTop = 10f; t.style.marginBottom = 4f;
        lista.Add(t);
        var f = Estilo.Fila(4f); f.style.flexWrap = Wrap.Wrap;
        for (int k = 0; k < opciones.Length; k++)
        {
            int v = k;
            bool cerrado = bloqueo != null && Apariencia.Bloqueado(bloqueo, v);
            var b = Componentes.Ficha(cerrado ? opciones[k] + " (de Luwyn)" : opciones[k], () =>
            {
                if (cerrado) { Notificaciones.Alerta("Es de " + Apariencia.NombreLuwyn + ": " + Apariencia.Progreso); return; }
                Cambiar(() => { poner(v); if (Apariencia.EsLuwyn) Apariencia.Jugador.personaje = ""; });
            }, null, actual() == v);
            b.style.marginBottom = 4f;
            f.Add(b);
        }
        lista.Add(f);
    }
}
