using UnityEngine;
using UnityEngine.InputSystem;

// Taller (tecla P, a bordo de tu barco): pinta cada parte con el color que quieras, ponle dibujos a las velas
// y a la bandera, y elige el mascarón de proa. Los cambios se ven al momento; se pagan al guardar
// (5 de oro por parte cambiada, 20 el mascarón; gratis en modo creativo). Cancelar deja todo como estaba.
public class Taller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Crear() => new GameObject("Taller").AddComponent<Taller>();

    public static bool Abierto { get; private set; }
    static Taller instancia;
    public static void Abrir() { if (instancia != null) instancia.IntentarAbrir(); }

    const int CostoParte = 5, CostoMascaron = 20;

    public static Pintura PinturaActual => instancia != null ? instancia.pintura : null;
    public static Pintura.Datos Antes => instancia != null ? instancia.antes : null;
    Pintura pintura;
    Pintura.Datos antes;

    void Awake() => instancia = this;

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (!Abierto)
        {
            if (Teclas.Pulsada(Accion.Taller) && !Fabricacion.MenuAbierto && !Navegacion.MapaAbierto) IntentarAbrir();
            return;
        }
        Personaje p = Personaje.Instancia;
        if (p == null || p.barco == null || pintura == null || p.BarcoActual != p.barco) { Cancelar(); return; }
        if (Teclas.Pulsada(Accion.Taller, true)) Cancelar();
    }

    void IntentarAbrir()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        if (p.barco == null || p.estado == Personaje.Estado.Libre || p.BarcoActual != p.barco) { Notificaciones.Alerta("El taller se abre a bordo de tu barco (" + Teclas.Nombre(Accion.Taller) + ")"); return; }
        pintura = p.barco.GetComponent<Pintura>();
        if (pintura == null) pintura = p.barco.gameObject.AddComponent<Pintura>();
        antes = pintura.datos.Copia();
        Abierto = true;
        CamaraBarco.Orbitar = true;
        Ventanas.Abrir(VentanaTaller.I);
    }

    void Cerrar() { Abierto = false; CamaraBarco.Orbitar = false; Ventanas.Cerrar(VentanaTaller.I); }

    public static void CancelarCambios() { if (instancia != null && Abierto) instancia.Cancelar(); }
    public static void GuardarCambios() { if (instancia != null && Abierto) instancia.Guardar(); }
    public static int CostoActual => instancia != null ? instancia.Costo() : 0;
    public const int PrecioParte = CostoParte, PrecioMascaron = CostoMascaron;

    void Cancelar()
    {
        if (pintura != null && antes != null) { pintura.datos = antes.Copia(); pintura.Aplicar(); }
        Cerrar();
    }

    int Costo()
    {
        if (antes == null || pintura == null) return 0;
        int c = 0;
        for (int i = 0; i < Pintura.NombresParte.Length; i++)
            if (!pintura.De((Pintura.Parte)i).Igual(antes.estilos[i])) c += CostoParte;
        if ((pintura.datos.mascaron ?? "") != (antes.mascaron ?? "")) c += CostoMascaron;
        return ModoCreativo.Activo ? 0 : c;
    }

    void Guardar()
    {
        Personaje p = Personaje.Instancia;
        Inventario inv = p != null ? p.GetComponent<Inventario>() : null;
        int c = Costo();
        if (inv != null && c > 0)
        {
            if (inv.Cantidad(Inventario.Oro) < c) { Notificaciones.Alerta("Te faltan " + (c - inv.Cantidad(Inventario.Oro)) + " de oro"); Sonidos.Reproducir(Sonidos.Tipo.Error); return; }
            inv.Quitar(Inventario.Oro, c);
        }
        Notificaciones.Mostrar(c > 0 ? "Barco pintado (" + c + " de oro)" : "Barco pintado", Notificaciones.Tipo.Exito, 4f, "brocha");
        Cerrar();
    }

}
