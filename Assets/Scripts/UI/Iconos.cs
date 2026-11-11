using UnityEngine;
using UnityEngine.UIElements;

// Iconos vectoriales dibujados por código (Painter2D): se ven nítidos a cualquier escala y en cualquier resolución.
// Todos están diseñados en una cuadrícula de 24 x 24 y se escalan al tamaño pedido.
public class Icono : VisualElement
{
    string nombre;
    Color color;
    float tam;

    // Redibujar un icono vectorial cuesta: solo si de verdad cambió
    public string Nombre { get => nombre; set { if (nombre == value) return; nombre = value; MarkDirtyRepaint(); } }
    public Color Color { get => color; set { if (color == value) return; color = value; MarkDirtyRepaint(); } }

    public Icono(string nombre, float tam, Color color)
    {
        this.nombre = nombre; this.tam = tam; this.color = color;
        style.width = tam; style.height = tam;
        style.flexShrink = 0f;
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Dibujar;
    }

    void Dibujar(MeshGenerationContext mgc)
    {
        Painter2D p = mgc.painter2D;
        float k = contentRect.width / 24f;
        if (k <= 0f) k = tam / 24f;
        Iconos.Dibujar(p, nombre, k, color);
    }
}

public static class Iconos
{
    public static Icono Crear(string nombre, float tam, Color color) => new Icono(nombre, tam, color);

    // Nombre del icono de cada cosa del inventario
    public static string DeObjeto(string tipo)
    {
        switch (tipo)
        {
            case Inventario.Madera: return "madera";
            case Inventario.Palo: return "palo";
            case Inventario.Fibra: return "fibra";
            case Inventario.Cuerda: return "cuerda";
            case Inventario.Piedra: return "piedra";
            case Inventario.Mineral: return "mineral";
            case Inventario.Lingote: return "lingote";
            case Inventario.Canon: return "canon";
            case Inventario.Coco: return "coco";
            case Inventario.Fruta: return "fruta";
            case Inventario.Oro: return "moneda";
            case Inventario.Hacha: return "hacha";
            case Inventario.Pico: return "pico";
            default: return "caja";
        }
    }

    public static void Dibujar(Painter2D p, string nombre, float k, Color c)
    {
        p.strokeColor = c; p.fillColor = c;
        p.lineWidth = 2f * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
        Vector2 V(float x, float y) => new Vector2(x * k, y * k);
        void Linea(float x0, float y0, float x1, float y1) { p.BeginPath(); p.MoveTo(V(x0, y0)); p.LineTo(V(x1, y1)); p.Stroke(); }
        void Circulo(float x, float y, float r, bool relleno) { p.BeginPath(); p.Arc(V(x, y), r * k, 0f, 360f); p.ClosePath(); if (relleno) p.Fill(); else p.Stroke(); }
        void Poli(bool relleno, params float[] xy)
        {
            p.BeginPath(); p.MoveTo(V(xy[0], xy[1]));
            for (int i = 2; i < xy.Length; i += 2) p.LineTo(V(xy[i], xy[i + 1]));
            p.ClosePath(); if (relleno) p.Fill(); else p.Stroke();
        }
        void Rect(float x, float y, float w, float h, bool relleno) => Poli(relleno, x, y, x + w, y, x + w, y + h, x, y + h);

        switch (nombre)
        {
            case "vida":   // corazón
                p.BeginPath(); p.MoveTo(V(12, 21));
                p.BezierCurveTo(V(4, 15), V(2, 10), V(4.5f, 6.5f)); p.BezierCurveTo(V(7, 4), V(10.5f, 4.5f), V(12, 8));
                p.BezierCurveTo(V(13.5f, 4.5f), V(17, 4), V(19.5f, 6.5f)); p.BezierCurveTo(V(22, 10), V(20, 15), V(12, 21));
                p.ClosePath(); p.Fill(); break;
            case "hambre": // muslo de pollo
                p.BeginPath(); p.Arc(V(14, 10), 6f * k, 0f, 360f); p.ClosePath(); p.Fill();
                p.lineWidth = 3.2f * k; Linea(9.5f, 14.5f, 4.5f, 19.5f);
                Circulo(4f, 20f, 1.8f, true); Circulo(6f, 18f, 1.8f, true); break;
            case "sed":    // gota
                p.BeginPath(); p.MoveTo(V(12, 2.5f));
                p.BezierCurveTo(V(12, 8), V(5, 11), V(5, 15.5f)); p.BezierCurveTo(V(5, 19.5f), V(8, 22), V(12, 22));
                p.BezierCurveTo(V(16, 22), V(19, 19.5f), V(19, 15.5f)); p.BezierCurveTo(V(19, 11), V(12, 8), V(12, 2.5f));
                p.ClosePath(); p.Fill(); break;
            case "moneda":
                Circulo(12, 12, 9, true);
                p.fillColor = new Color(c.r * 0.55f, c.g * 0.5f, c.b * 0.3f, c.a); Circulo(12, 12, 6, true);
                p.fillColor = c; Rect(10.8f, 8f, 2.4f, 8f, true); break;
            case "madera": // tronco
                Rect(3, 8, 15, 8, true);
                p.fillColor = new Color(c.r * 0.5f, c.g * 0.45f, c.b * 0.35f, c.a);
                p.BeginPath(); p.Arc(V(18, 12), 4f * k, 0f, 360f); p.ClosePath(); p.Fill();
                p.fillColor = c; Circulo(18, 12, 2.2f, true); break;
            case "palo":
                p.lineWidth = 2.6f * k; Linea(5, 19, 19, 5); Linea(12, 12, 15, 14); Linea(9, 15, 7, 12); break;
            case "fibra":
                p.lineWidth = 1.8f * k;
                Linea(12, 21, 12, 6); Linea(12, 21, 6, 8); Linea(12, 21, 18, 8); Linea(12, 21, 8, 4); Linea(12, 21, 16, 4); break;
            case "cuerda":
                p.lineWidth = 3f * k; p.BeginPath(); p.Arc(V(12, 12), 6.5f * k, 20f, 340f); p.Stroke();
                p.lineWidth = 2f * k; Linea(17.5f, 9.5f, 21, 5); Linea(17.5f, 14.5f, 21, 19); break;
            case "piedra":
                Poli(true, 5, 16, 7, 9, 13, 6, 19, 10, 20, 16, 15, 20, 8, 20); break;
            case "mineral":
                Poli(true, 4, 17, 6, 9, 12, 5, 19, 9, 21, 16, 16, 21, 8, 21);
                p.fillColor = new Color(1f, 1f, 1f, 0.75f); Circulo(10, 11, 1.4f, true); Circulo(15, 15, 1.2f, true); Circulo(13, 9, 0.9f, true); break;
            case "lingote":
                Poli(true, 3, 17, 6, 8, 18, 8, 21, 17);
                p.fillColor = new Color(1f, 1f, 1f, 0.3f); Poli(true, 7, 10, 17, 10, 18, 12, 6, 12); break;
            case "canon":
                p.lineWidth = 5f * k; Linea(5, 13, 19, 7);
                p.lineWidth = 2f * k; Circulo(8, 17, 3.5f, false); Circulo(8, 17, 1.2f, true); break;
            case "coco":
                Circulo(12, 12, 9, true);
                p.fillColor = new Color(c.r * 0.4f, c.g * 0.35f, c.b * 0.3f, c.a); Circulo(9.5f, 9.5f, 1.4f, true); Circulo(14.5f, 9.5f, 1.4f, true); Circulo(12, 14, 1.4f, true); break;
            case "fruta":  // manzana con hoja
                p.BeginPath(); p.MoveTo(V(12, 8)); p.BezierCurveTo(V(8, 5), V(3, 8), V(4, 14)); p.BezierCurveTo(V(5, 19), V(9, 22), V(12, 20));
                p.BezierCurveTo(V(15, 22), V(19, 19), V(20, 14)); p.BezierCurveTo(V(21, 8), V(16, 5), V(12, 8)); p.ClosePath(); p.Fill();
                p.fillColor = new Color(0.45f, 0.75f, 0.35f, c.a); p.BeginPath(); p.MoveTo(V(12.5f, 7)); p.BezierCurveTo(V(13, 3), V(16, 2.5f), V(17.5f, 3.5f)); p.BezierCurveTo(V(17, 6), V(14.5f, 7.5f), V(12.5f, 7)); p.ClosePath(); p.Fill(); break;
            case "hacha":
                p.lineWidth = 2.6f * k; Linea(6, 20, 15, 11);
                Poli(true, 13, 5, 20, 6, 21, 12, 15, 13, 12, 10); break;
            case "pico":
                p.lineWidth = 2.6f * k; Linea(6, 20, 15, 11);
                p.lineWidth = 3f * k; p.BeginPath(); p.MoveTo(V(8, 8)); p.BezierCurveTo(V(12, 4), V(18, 6), V(21, 11)); p.Stroke(); break;
            case "espada":
                p.lineWidth = 2.6f * k; Linea(6, 18, 18, 6); Linea(16, 4, 20, 8); p.lineWidth = 2f * k; Linea(5, 14, 10, 19); Linea(4, 20, 7, 17); break;
            case "brujula":
                Circulo(12, 12, 9.5f, false);
                Poli(true, 12, 3.5f, 14.2f, 12, 12, 10.5f, 9.8f, 12);
                p.fillColor = new Color(c.r, c.g, c.b, c.a * 0.45f); Poli(true, 12, 20.5f, 14.2f, 12, 12, 13.5f, 9.8f, 12); break;
            case "barco":  // casco y vela
                Poli(true, 3, 15, 21, 15, 18, 20, 6, 20);
                p.lineWidth = 1.8f * k; Linea(12, 14, 12, 3);
                p.fillColor = new Color(c.r, c.g, c.b, c.a * 0.8f); Poli(true, 12.5f, 4, 19, 12, 12.5f, 12); break;
            case "ancla":
                p.lineWidth = 2.2f * k; Circulo(12, 5, 2.2f, false); Linea(12, 7.5f, 12, 20); Linea(7, 10, 17, 10);
                p.BeginPath(); p.Arc(V(12, 14), 7f * k, 20f, 160f); p.Stroke(); break;
            case "vela":
                Poli(true, 5, 19, 18, 19, 18, 4);
                p.fillColor = new Color(c.r, c.g, c.b, c.a * 0.5f); Poli(true, 5, 19, 15, 19, 15, 8); break;
            case "bandera":
                p.lineWidth = 2.2f * k; Linea(5, 3, 5, 21);
                p.BeginPath(); p.MoveTo(V(6, 4)); p.BezierCurveTo(V(10, 2), V(14, 7), V(20, 4)); p.LineTo(V(20, 13)); p.BezierCurveTo(V(14, 16), V(10, 11), V(6, 13)); p.ClosePath(); p.Fill(); break;
            case "mapa":
                p.lineWidth = 1.8f * k; Poli(false, 3, 6, 9, 4, 15, 6, 21, 4, 21, 18, 15, 20, 9, 18, 3, 20);
                Linea(9, 4, 9, 18); Linea(15, 6, 15, 20); break;
            case "martillo":
                p.lineWidth = 2.8f * k; Linea(6, 20, 14, 12);
                Poli(true, 11, 4, 20, 4, 20, 10, 17, 10, 17, 13, 13, 13, 11, 10); break;
            case "caja":
                Rect(4, 7, 16, 13, true);
                p.fillColor = new Color(c.r * 0.5f, c.g * 0.45f, c.b * 0.35f, c.a); Rect(4, 7, 16, 3, true);
                p.strokeColor = new Color(c.r * 0.5f, c.g * 0.45f, c.b * 0.35f, c.a); p.lineWidth = 1.5f * k; Linea(12, 10, 12, 20); break;
            case "balanza":
                p.lineWidth = 2f * k; Linea(12, 3, 12, 20); Linea(7, 20, 17, 20); Linea(4, 7, 20, 7);
                p.BeginPath(); p.Arc(V(5.5f, 13), 3.5f * k, 0f, 180f); p.ClosePath(); p.Fill();
                p.BeginPath(); p.Arc(V(18.5f, 13), 3.5f * k, 0f, 180f); p.ClosePath(); p.Fill(); break;
            case "brocha":
                p.lineWidth = 2.6f * k; Linea(17, 4, 11, 10);
                Poli(true, 8, 10, 13, 10, 13, 14, 11, 20, 8, 20, 6, 14); break;
            case "estrella":
                p.BeginPath();
                for (int i = 0; i < 10; i++)
                {
                    float a = -90f + i * 36f; float r = i % 2 == 0 ? 10f : 4.5f;
                    Vector2 v = V(12 + Mathf.Cos(a * Mathf.Deg2Rad) * r, 12.5f + Mathf.Sin(a * Mathf.Deg2Rad) * r);
                    if (i == 0) p.MoveTo(v); else p.LineTo(v);
                }
                p.ClosePath(); p.Fill(); break;
            case "estrella_vacia":
                p.lineWidth = 1.6f * k; p.BeginPath();
                for (int i = 0; i < 10; i++)
                {
                    float a = -90f + i * 36f; float r = i % 2 == 0 ? 10f : 4.5f;
                    Vector2 v = V(12 + Mathf.Cos(a * Mathf.Deg2Rad) * r, 12.5f + Mathf.Sin(a * Mathf.Deg2Rad) * r);
                    if (i == 0) p.MoveTo(v); else p.LineTo(v);
                }
                p.ClosePath(); p.Stroke(); break;
            case "cerrar":
                p.lineWidth = 2.4f * k; Linea(6, 6, 18, 18); Linea(18, 6, 6, 18); break;
            case "buscar":
                p.lineWidth = 2.2f * k; Circulo(10, 10, 6, false); Linea(14.5f, 14.5f, 20, 20); break;
            case "engranaje":
                p.lineWidth = 2.4f * k;
                for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; Linea(12 + Mathf.Cos(a) * 6f, 12 + Mathf.Sin(a) * 6f, 12 + Mathf.Cos(a) * 9.5f, 12 + Mathf.Sin(a) * 9.5f); }
                Circulo(12, 12, 6, false); Circulo(12, 12, 2, true); break;
            case "teclado":
                p.lineWidth = 1.6f * k; Rect(3, 7, 18, 11, false);
                for (int i = 0; i < 4; i++) Rect(5 + i * 4f, 9, 2.4f, 2.4f, true);
                for (int i = 0; i < 4; i++) Rect(5 + i * 4f, 12.5f, 2.4f, 2.4f, true); Rect(7, 15.5f, 10, 1.6f, true); break;
            case "altavoz":
                Poli(true, 4, 9, 8, 9, 13, 5, 13, 19, 8, 15, 4, 15);
                p.lineWidth = 1.8f * k; p.BeginPath(); p.Arc(V(13, 12), 4f * k, -50f, 50f); p.Stroke();
                p.BeginPath(); p.Arc(V(13, 12), 7.5f * k, -50f, 50f); p.Stroke(); break;
            case "ojo":
                p.lineWidth = 1.8f * k; p.BeginPath(); p.MoveTo(V(2, 12)); p.BezierCurveTo(V(6, 5), V(18, 5), V(22, 12)); p.BezierCurveTo(V(18, 19), V(6, 19), V(2, 12)); p.ClosePath(); p.Stroke();
                Circulo(12, 12, 3.2f, true); break;
            case "mas":
                p.lineWidth = 2.6f * k; Linea(12, 5, 12, 19); Linea(5, 12, 19, 12); break;
            case "menos":
                p.lineWidth = 2.6f * k; Linea(5, 12, 19, 12); break;
            case "check":
                p.lineWidth = 2.8f * k; p.BeginPath(); p.MoveTo(V(4, 12.5f)); p.LineTo(V(9.5f, 18)); p.LineTo(V(20, 6)); p.Stroke(); break;
            case "alerta":
                Poli(true, 12, 3, 22, 20, 2, 20);
                p.fillColor = new Color(0.1f, 0.07f, 0.04f, c.a); Rect(11, 8.5f, 2, 6, true); Circulo(12, 17, 1.2f, true); break;
            case "info":
                Circulo(12, 12, 9.5f, true);
                p.fillColor = new Color(0.1f, 0.07f, 0.04f, c.a); Circulo(12, 7.5f, 1.4f, true); Rect(11, 10.5f, 2, 7, true); break;
            case "calavera":
                p.BeginPath(); p.Arc(V(12, 10), 7.5f * k, 0f, 360f); p.ClosePath(); p.Fill(); Rect(8, 14, 8, 5, true);
                p.fillColor = new Color(0.1f, 0.07f, 0.04f, c.a); Circulo(9, 10, 2, true); Circulo(15, 10, 2, true); Poli(true, 12, 12.5f, 13.3f, 14.5f, 10.7f, 14.5f);
                Rect(9.5f, 16.5f, 1.2f, 2.5f, true); Rect(11.4f, 16.5f, 1.2f, 2.5f, true); Rect(13.3f, 16.5f, 1.2f, 2.5f, true); break;
            case "timon":
                p.lineWidth = 2f * k; Circulo(12, 12, 6, false); Circulo(12, 12, 2, true);
                for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; Linea(12 + Mathf.Cos(a) * 6f, 12 + Mathf.Sin(a) * 6f, 12 + Mathf.Cos(a) * 10f, 12 + Mathf.Sin(a) * 10f); }
                break;
            case "flecha":     // hacia la derecha
                p.lineWidth = 2.6f * k; Linea(4, 12, 19, 12); p.BeginPath(); p.MoveTo(V(13, 6)); p.LineTo(V(19, 12)); p.LineTo(V(13, 18)); p.Stroke(); break;
            case "flecha_izq":
                p.lineWidth = 2.6f * k; Linea(20, 12, 5, 12); p.BeginPath(); p.MoveTo(V(11, 6)); p.LineTo(V(5, 12)); p.LineTo(V(11, 18)); p.Stroke(); break;
            case "flecha_arriba":
                p.lineWidth = 2.6f * k; Linea(12, 20, 12, 5); p.BeginPath(); p.MoveTo(V(6, 11)); p.LineTo(V(12, 5)); p.LineTo(V(18, 11)); p.Stroke(); break;
            case "flecha_abajo":
                p.lineWidth = 2.6f * k; Linea(12, 4, 12, 19); p.BeginPath(); p.MoveTo(V(6, 13)); p.LineTo(V(12, 19)); p.LineTo(V(18, 13)); p.Stroke(); break;
            case "libro":
                p.lineWidth = 1.8f * k; Rect(4, 4, 16, 16, false); Linea(12, 4, 12, 20); Linea(7, 8, 10, 8); Linea(7, 11, 10, 11); Linea(14, 8, 17, 8); Linea(14, 11, 17, 11); break;
            case "pausa":
                Rect(6, 5, 4, 14, true); Rect(14, 5, 4, 14, true); break;
            case "play":
                Poli(true, 7, 4, 20, 12, 7, 20); break;
            case "salir":
                p.lineWidth = 2f * k; Poli(false, 4, 3, 13, 3, 13, 21, 4, 21); Linea(10, 12, 21, 12); p.BeginPath(); p.MoveTo(V(17, 8)); p.LineTo(V(21, 12)); p.LineTo(V(17, 16)); p.Stroke(); break;
            case "persona":
                Circulo(12, 8, 4.2f, true); p.BeginPath(); p.Arc(V(12, 22), 8f * k, 180f, 360f); p.ClosePath(); p.Fill(); break;
            case "tripulacion":
                Circulo(8, 8, 3.2f, true); p.BeginPath(); p.Arc(V(8, 20), 6f * k, 180f, 360f); p.ClosePath(); p.Fill();
                p.fillColor = new Color(c.r, c.g, c.b, c.a * 0.6f); Circulo(16.5f, 9, 2.8f, true); p.BeginPath(); p.Arc(V(16.5f, 20), 5f * k, 180f, 360f); p.ClosePath(); p.Fill(); break;
            case "fuego":
                p.BeginPath(); p.MoveTo(V(12, 2)); p.BezierCurveTo(V(13, 7), V(19, 9), V(19, 15)); p.BezierCurveTo(V(19, 19), V(16, 22), V(12, 22));
                p.BezierCurveTo(V(8, 22), V(5, 19), V(5, 15)); p.BezierCurveTo(V(5, 12), V(7, 10), V(8, 8)); p.BezierCurveTo(V(8, 11), V(10, 12), V(11, 11)); p.BezierCurveTo(V(10, 7), V(11, 4), V(12, 2));
                p.ClosePath(); p.Fill(); break;
            case "rayo":
                Poli(true, 13, 2, 5, 13, 11, 13, 9, 22, 19, 10, 13, 10); break;
            case "escudo":
                p.BeginPath(); p.MoveTo(V(12, 2.5f)); p.LineTo(V(20, 5.5f)); p.BezierCurveTo(V(20, 14), V(17, 19), V(12, 22)); p.BezierCurveTo(V(7, 19), V(4, 14), V(4, 5.5f)); p.ClosePath(); p.Fill(); break;
            case "isla":   // palmera sobre arena
                p.BeginPath(); p.Arc(V(12, 23), 9f * k, 180f, 360f); p.ClosePath(); p.Fill();
                p.lineWidth = 2f * k; Linea(12, 18, 13, 8); p.lineWidth = 2.4f * k; Linea(13, 8, 6, 5); Linea(13, 8, 20, 5); Linea(13, 8, 9, 11); Linea(13, 8, 18, 12); break;
            case "mercader": // puesto con toldo
                Poli(true, 3, 9, 21, 9, 19, 4, 5, 4); Rect(5, 10, 14, 10, false); p.lineWidth = 1.8f * k; Linea(9, 10, 9, 20); Linea(15, 10, 15, 20); break;
            case "pergamino":
                p.lineWidth = 1.8f * k; Rect(5, 3, 14, 18, false); Linea(8, 8, 16, 8); Linea(8, 12, 16, 12); Linea(8, 16, 13, 16); break;
            case "velocidad":
                p.lineWidth = 2f * k; p.BeginPath(); p.Arc(V(12, 14), 9f * k, 160f, 380f); p.Stroke(); Linea(12, 14, 17, 8); Circulo(12, 14, 1.6f, true); break;
            case "objetivo":
                p.lineWidth = 2f * k; Circulo(12, 12, 8, false); Circulo(12, 12, 2.5f, true); Linea(12, 2, 12, 7); Linea(12, 17, 12, 22); Linea(2, 12, 7, 12); Linea(17, 12, 22, 12); break;
            case "casa":
                Poli(true, 12, 3, 22, 11, 19, 11, 19, 21, 5, 21, 5, 11, 2, 11); break;
            case "punto":
                Circulo(12, 12, 4, true); break;
            case "rombo":
                Poli(true, 12, 3, 21, 12, 12, 21, 3, 12); break;
            case "triangulo":
                Poli(true, 12, 4, 21, 20, 3, 20); break;
            case "triangulo_abajo":
                Poli(true, 3, 4, 21, 4, 12, 20); break;
            case "marcador":  // chincheta
                p.BeginPath(); p.MoveTo(V(12, 22)); p.BezierCurveTo(V(6, 14), V(4, 11), V(4, 8)); p.Arc(V(12, 8), 8f * k, 180f, 360f); p.BezierCurveTo(V(20, 11), V(18, 14), V(12, 22)); p.ClosePath(); p.Fill();
                p.fillColor = new Color(0.1f, 0.07f, 0.04f, c.a); Circulo(12, 8.5f, 3, true); break;
            case "catalejo":
                p.lineWidth = 5f * k; Linea(4, 18, 12, 10); p.lineWidth = 7f * k; Linea(12, 10, 19, 3); break;
            case "regla":
                p.lineWidth = 1.8f * k; Rect(3, 9, 18, 6, false); for (int i = 1; i < 6; i++) Linea(3 + i * 3f, 9, 3 + i * 3f, i % 2 == 0 ? 13 : 11.5f); break;
            case "pisos":
                p.lineWidth = 1.8f * k; Rect(4, 14, 16, 5, false); Rect(6, 9, 12, 5, false); Rect(8, 4, 8, 5, false); break;
            case "campana":
                p.BeginPath(); p.MoveTo(V(5, 17)); p.BezierCurveTo(V(7, 15), V(6, 6), V(12, 4)); p.BezierCurveTo(V(18, 6), V(17, 15), V(19, 17)); p.ClosePath(); p.Fill(); Circulo(12, 20, 2, true); break;
            case "reloj":
                p.lineWidth = 2f * k; Circulo(12, 12, 9, false); Linea(12, 12, 12, 6.5f); Linea(12, 12, 16, 14); break;
            default:
                Circulo(12, 12, 6, false); break;
        }
    }
}
