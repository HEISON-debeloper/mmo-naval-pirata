using UnityEngine;
using UnityEngine.UIElements;

// Controles en pantalla para el teléfono (Android): palanca a la izquierda para caminar o llevar el timón,
// y botones a la derecha (usar, saltar, espada, comer, disparar a cada lado, ancla) más los de las ventanas.
// Solo aparecen en el teléfono. Usan las mismas acciones que el teclado (Teclas), así no hay que tocar el resto del juego.
[DefaultExecutionOrder(-1000)]
public class ControlesTactiles : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() => new GameObject("Controles táctiles").AddComponent<ControlesTactiles>();

    // Para probar en el PC: poner en true muestra los controles aunque no sea un teléfono
    public static bool Forzar;
    VisualElement raiz, palanca, perilla;
    Vector2 centro; int dedo = -1;
    const float RadioPalanca = 70f;

    void Update()
    {
        Teclas.NuevoCuadro();   // primero: los toques de la interfaz pasan a "este cuadro"
        bool movil = Application.isMobilePlatform || Forzar;
        if (!movil) { if (raiz != null) raiz.style.display = DisplayStyle.None; return; }
        if (raiz == null && Lienzo.Listo) Construir();
        if (raiz == null) return;
        bool ver = Personaje.Instancia != null && !MenuPrincipal.Visible && !Ventanas.AlgunaAbierta;
        raiz.style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void Construir()
    {
        raiz = new VisualElement { name = "tactil", pickingMode = PickingMode.Ignore };
        raiz.style.position = Position.Absolute; raiz.style.left = raiz.style.right = raiz.style.top = raiz.style.bottom = 0f;
        Lienzo.CapaHud.Add(raiz);

        // Mitad derecha de la pantalla: arrastrar con el dedo gira la cámara (va debajo de los botones)
        var mirar = new VisualElement { name = "mirar" };
        mirar.style.position = Position.Absolute; mirar.style.right = 0f; mirar.style.top = 0f; mirar.style.bottom = 0f;
        mirar.style.width = Length.Percent(55f);
        int dedoMirar = -1; Vector2 ultimo = Vector2.zero;
        mirar.RegisterCallback<PointerDownEvent>(e => { dedoMirar = e.pointerId; ultimo = e.position; mirar.CapturePointer(e.pointerId); });
        mirar.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (e.pointerId != dedoMirar) return;
            Vector2 d = (Vector2)e.position - ultimo; ultimo = e.position;
            CamaraBarco.DeltaTactil += new Vector2(d.x, -d.y) * 1.6f;   // en el panel la y crece hacia abajo
        });
        mirar.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == dedoMirar) { dedoMirar = -1; mirar.ReleasePointer(e.pointerId); } });
        raiz.Add(mirar);

        // Palanca
        palanca = new VisualElement();
        palanca.style.position = Position.Absolute; palanca.style.left = 40f; palanca.style.bottom = 170f;
        palanca.style.width = RadioPalanca * 2f; palanca.style.height = RadioPalanca * 2f;
        palanca.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
        Estilo.Borde(palanca, new Color(Estilo.Bronce.r, Estilo.Bronce.g, Estilo.Bronce.b, 0.7f), 2f, RadioPalanca);
        perilla = new VisualElement { pickingMode = PickingMode.Ignore };
        perilla.style.position = Position.Absolute; perilla.style.width = 56f; perilla.style.height = 56f;
        perilla.style.left = RadioPalanca - 28f; perilla.style.top = RadioPalanca - 28f;
        perilla.style.backgroundColor = new Color(Estilo.BronceClaro.r, Estilo.BronceClaro.g, Estilo.BronceClaro.b, 0.8f);
        Estilo.Redondear(perilla, 28f);
        palanca.Add(perilla);
        palanca.RegisterCallback<PointerDownEvent>(e => { dedo = e.pointerId; centro = new Vector2(RadioPalanca, RadioPalanca); palanca.CapturePointer(e.pointerId); Mover(e.localPosition); });
        palanca.RegisterCallback<PointerMoveEvent>(e => { if (e.pointerId == dedo) Mover(e.localPosition); });
        palanca.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == dedo) Soltar(); });
        palanca.RegisterCallback<PointerCaptureOutEvent>(_ => Soltar());
        raiz.Add(palanca);

        // Botones de acción (abajo a la derecha, en arco)
        Boton("Usar", "flecha", Accion.Usar, 40f, 190f, 76f);
        Boton("Saltar", "flecha_arriba", Accion.Saltar, 130f, 150f, 64f);
        Boton("Espada", "espada", Accion.Espada, 40f, 290f, 64f);
        Boton("Comer", "fruta", Accion.Comer, 220f, 120f, 54f);
        Boton("Babor", "canon", Accion.DispararBabor, 150f, 250f, 54f);
        Boton("Estribor", "canon", Accion.DispararEstribor, 220f, 200f, 54f);
        Boton("Ancla", "ancla", Accion.Ancla, 300f, 140f, 50f);
        Boton("Correr", "velocidad", Accion.Correr, 130f, 330f, 50f, mantener: true);

        // Ventanas (arriba a la derecha, debajo de la tarjeta del enemigo)
        var fila = Estilo.Fila(6f);
        fila.style.position = Position.Absolute; fila.style.right = 16f; fila.style.top = 130f;
        foreach (var (accion, icono) in new[] { (Accion.Inventario, "caja"), (Accion.Fabricar, "martillo"), (Accion.Barco, "barco"), (Accion.Mapa, "mapa"), (Accion.Pausa, "pausa") })
        {
            var a = accion;
            var b = Componentes.BotonIcono(icono, () =>
            {
                if (a == Accion.Pausa) Ventanas.Abrir(Pausa.I);
                else if (a == Accion.Inventario) Ventanas.Alternar(VentanaInventario.I);
                else if (a == Accion.Barco) Ventanas.Alternar(VentanaBarco.I);
                else if (a == Accion.Mapa) Ventanas.Alternar(VentanaMapa.I);
                else Teclas.Toque(a);
            }, null, 26f, Estilo.Oro);
            b.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            b.style.marginLeft = 6f;
            fila.Add(b);
        }
        raiz.Add(fila);
    }

    void Boton(string texto, string icono, Accion a, float derecha, float abajo, float tam, bool mantener = false)
    {
        var b = new VisualElement();
        b.style.position = Position.Absolute; b.style.right = derecha; b.style.bottom = abajo;
        b.style.width = tam; b.style.height = tam;
        b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
        b.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.6f);
        Estilo.Borde(b, new Color(Estilo.Bronce.r, Estilo.Bronce.g, Estilo.Bronce.b, 0.8f), 2f, tam / 2f);
        var ic = Iconos.Crear(icono, tam * 0.42f, Estilo.Oro); b.Add(ic);
        var l = Estilo.Etiqueta(texto, 11f, Estilo.Texto, Estilo.FuenteNegrita); l.pickingMode = PickingMode.Ignore; b.Add(l);
        b.RegisterCallback<PointerDownEvent>(e =>
        {
            b.CapturePointer(e.pointerId);
            b.style.backgroundColor = new Color(0.5f, 0.38f, 0.18f, 0.75f);
            Teclas.Tocar(a, true);
        });
        System.Action soltar = () => { b.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.6f); Teclas.Tocar(a, false); };
        b.RegisterCallback<PointerUpEvent>(e => { b.ReleasePointer(e.pointerId); soltar(); });
        b.RegisterCallback<PointerCaptureOutEvent>(_ => soltar());
        raiz.Add(b);
    }

    void Mover(Vector2 p)
    {
        Vector2 d = Vector2.ClampMagnitude(p - centro, RadioPalanca);
        perilla.style.left = RadioPalanca + d.x - 28f; perilla.style.top = RadioPalanca + d.y - 28f;
        Vector2 n = d / RadioPalanca;   // y crece hacia abajo
        const float zona = 0.35f;
        Teclas.Tocar(Accion.Adelante, n.y < -zona);
        Teclas.Tocar(Accion.Atras, n.y > zona);
        Teclas.Tocar(Accion.Izquierda, n.x < -zona);
        Teclas.Tocar(Accion.Derecha, n.x > zona);
    }

    void Soltar()
    {
        dedo = -1;
        perilla.style.left = RadioPalanca - 28f; perilla.style.top = RadioPalanca - 28f;
        foreach (var a in new[] { Accion.Adelante, Accion.Atras, Accion.Izquierda, Accion.Derecha }) Teclas.Tocar(a, false);
    }
}
