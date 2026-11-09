using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Bitácora (G): todo lo que ha pasado en esta sesión, lo más reciente arriba, con hora, icono y color por tipo.
// Filtros por tipo y búsqueda. Así no se pierde un aviso que pasó rápido.
public class VentanaBitacora : Ventana
{
    static VentanaBitacora i;
    public static VentanaBitacora I => i ??= new VentanaBitacora();

    readonly ScrollView lista;
    readonly VisualElement fichas;
    readonly Componentes.CampoBusqueda buscar;
    readonly Label vacio;
    int filtro = -1;
    int dibujadas = -1;

    VentanaBitacora() : base("bitacora", "Bitácora", "pergamino", 620f, 560f)
    {
        var barra = Estilo.Fila(8f); barra.style.marginBottom = 8f;
        buscar = new Componentes.CampoBusqueda("Buscar en la bitácora…", 260f);
        buscar.alCambiar = _ => { dibujadas = -1; Dibujar(); };
        barra.Add(buscar);
        Contenido.Add(barra);
        fichas = Estilo.Fila(4f); fichas.style.flexWrap = Wrap.Wrap; fichas.style.marginBottom = 8f;
        Contenido.Add(fichas);
        lista = Componentes.Lista();
        Estilo.PanelPergamino(lista);
        Estilo.Relleno(lista, 8f);
        Contenido.Add(lista);
        vacio = Estilo.Etiqueta("Aún no hay nada anotado.", Estilo.TamTexto, Estilo.Tinta);
        Notificaciones.alRegistrar += _ => { if (Abierta) { dibujadas = -1; Dibujar(); } };
    }

    protected override float Refrescos => 0f;

    protected override void AlAbrir()
    {
        Notificaciones.MarcarLeidas();
        Fichas();
        dibujadas = -1;
        Dibujar();
    }

    void Fichas()
    {
        fichas.Clear();
        fichas.Add(Componentes.Ficha("Todo", () => { filtro = -1; Fichas(); dibujadas = -1; Dibujar(); }, null, filtro == -1));
        foreach (Notificaciones.Tipo t in new[] { Notificaciones.Tipo.Descubrimiento, Notificaciones.Tipo.Peligro, Notificaciones.Tipo.Comercio, Notificaciones.Tipo.Objeto, Notificaciones.Tipo.Exito, Notificaciones.Tipo.Alerta, Notificaciones.Tipo.Info })
        {
            var tt = t;
            fichas.Add(Componentes.Ficha(Nombre(t), () => { filtro = (int)tt; Fichas(); dibujadas = -1; Dibujar(); }, Notificaciones.IconoDe(t), filtro == (int)t));
        }
    }

    static string Nombre(Notificaciones.Tipo t)
    {
        switch (t)
        {
            case Notificaciones.Tipo.Descubrimiento: return "Descubrimientos";
            case Notificaciones.Tipo.Peligro: return "Combate";
            case Notificaciones.Tipo.Comercio: return "Comercio";
            case Notificaciones.Tipo.Objeto: return "Objetos";
            case Notificaciones.Tipo.Exito: return "Logros";
            case Notificaciones.Tipo.Alerta: return "Avisos";
            default: return "Otros";
        }
    }

    void Dibujar()
    {
        if (dibujadas == Notificaciones.Registro.Count) return;
        dibujadas = Notificaciones.Registro.Count;
        lista.Clear();
        string q = buscar.Texto.Trim().ToLowerInvariant();
        int n = 0;
        for (int k = Notificaciones.Registro.Count - 1; k >= 0; k--)
        {
            var e = Notificaciones.Registro[k];
            if (filtro >= 0 && (int)e.tipo != filtro) continue;
            if (q != "" && !e.texto.ToLowerInvariant().Contains(q)) continue;
            var f = Estilo.Fila(8f); f.style.marginBottom = 4f; f.style.alignItems = Align.FlexStart;
            var hora = Estilo.Etiqueta(Notificaciones.Hora(e), Estilo.TamTextoChico, new Color(0.45f, 0.35f, 0.22f), Estilo.FuenteNegrita);
            hora.style.width = 42f;
            f.Add(hora);
            Color c = Notificaciones.ColorDe(e.tipo);
            var ic = Iconos.Crear(e.icono, 16f, Color.Lerp(c, Estilo.Tinta, 0.45f)); ic.style.marginTop = 1f; ic.style.marginRight = 6f;
            f.Add(ic);
            var t = Estilo.Etiqueta(e.texto + (e.veces > 1 ? "  ×" + e.veces : ""), Estilo.TamTextoChico, Estilo.Tinta, Estilo.FuenteMedia);
            t.style.flexShrink = 1f;
            f.Add(t);
            lista.Add(f);
            if (++n >= 200) break;
        }
        if (n == 0) lista.Add(vacio);
    }
}
