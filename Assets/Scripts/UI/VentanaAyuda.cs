using UnityEngine;
using UnityEngine.UIElements;

// Controles y cómo se juega (F1). Los nombres de las teclas salen de Teclas, así que si el jugador las cambia, aquí también cambian.
public class VentanaAyuda : Ventana
{
    static VentanaAyuda i;
    public static VentanaAyuda I => i ??= new VentanaAyuda();

    ScrollView lista;

    VentanaAyuda() : base("ayuda", "Controles y guía", "libro", 720f, 560f) { }

    protected override float Refrescos => 0f;

    protected override void AlAbrir()
    {
        Contenido.Clear();
        var cab = Estilo.Fila(8f);
        cab.style.marginBottom = 8f;
        cab.Add(Estilo.Pequena("Las teclas se cambian en Opciones → Controles.  Versión " + Version.Texto));
        Contenido.Add(cab);
        lista = Componentes.Lista();
        Contenido.Add(lista);
        string T(Accion a) => Estilo.Col(Teclas.Nombre(a), Estilo.OroVivo);
        string K(string s) => Estilo.Col(s, Estilo.OroVivo);

        Grupo("A pie", "persona",
            T(Accion.Adelante) + " " + T(Accion.Izquierda) + " " + T(Accion.Atras) + " " + T(Accion.Derecha) + " caminar o nadar · " + T(Accion.Correr) + " correr · " + T(Accion.Saltar) + " saltar · " +
            T(Accion.Usar) + " recoger a mano (fibra, ramas, piedras, frutas) · " + K("Clic") + " golpear con hacha o pico · " + T(Accion.Comer) + " comer · " + T(Accion.Usar) + " subir al barco / tomar el timón");
        Grupo("Al timón", "timon",
            T(Accion.Adelante) + " / " + T(Accion.Atras) + " subir o bajar velas (bajar del todo: reversa) · " + T(Accion.Izquierda) + " / " + T(Accion.Derecha) + " timón · " + T(Accion.Ancla) + " ancla · " + T(Accion.Usar) + " soltar");
        Grupo("Cañones", "canon",
            "Mantén " + K("clic derecho") + " y mira hacia un lado (o al frente, o atrás): ves por dónde irán las balas y la cámara sigue la zona de caída. Mouse hacia el cielo: arco alto y lejos; hacia el barco: al agua, cerca. " +
            K("Clic izquierdo") + " dispara. " + T(Accion.DispararBabor) + " y " + T(Accion.DispararEstribor) + " disparan babor y estribor directo. Más cañones en un lado = recarga un poco más lenta (máximo el doble), y disparan en ola de popa a proa.");
        Grupo("Proa y popa", "flecha",
            "Proa: dos cañones en cubierta que solo disparan al frente, y el espolón: embiste a otro barco yendo rápido y le haces mucho daño (más si eres más grande). Popa: mira atrás y haz clic para soltar un barril en llamas; flota y explota cuando lo toca otro barco o una bala.");
        Grupo("Interiores", "pisos",
            "En cubierta, " + T(Accion.Usar) + " junto a la escotilla baja a las cubiertas de cañones y a la bodega (" + T(Accion.Usar) + " al pie de la escalera sube). " + T(Accion.Usar) + " frente a la puerta del castillo de popa entra al camarote del capitán. Adentro la cámara va en primera persona.");
        Grupo("Combate", "espada", K("Clic") + " o " + T(Accion.Espada) + ": espada (cerca de un enemigo). Algunos piratas te cañonean de costado y otros vienen a abordarte: cuando te alcanzan, sus piratas saltan a tu cubierta. Suelta el timón y pelea. Sin tripulación, su barco queda para capturar.");
        Grupo("Ventanas", "pergamino",
            T(Accion.Inventario) + " mochila · " + T(Accion.Fabricar) + " fabricar (junto a un mercader: comerciar) · " + T(Accion.Barco) + " tu barco · " + T(Accion.Mapa) + " carta de navegación · " + T(Accion.Taller) + " taller de pintura (a bordo de tu barco) · " +
            T(Accion.Bitacora) + " bitácora · " + T(Accion.Ayuda) + " esta ayuda · " + T(Accion.Pausa) + " cierra la ventana de arriba o abre la pausa. Las ventanas se arrastran por el título y recuerdan su posición.");
        Grupo("Navegar", "brujula", "Brújula arriba: islas descubiertas (verde), con mercader (dorado), tu barco (dorado) y enemigos (rojo). Las islas se descubren al acercarte. En la carta, rueda o +/- para el zoom, arrastra para moverte, clic derecho vuelve a ti.");
        Grupo("Mercado", "mercader", "Algunas islas tienen un mercader en la playa (la inicial siempre). Junto a él, " + T(Accion.Fabricar) + " abre Vender y Comprar. Cada isla paga distinto: lleva lo barato de una a donde es caro. Si vendes mucho de lo mismo, el precio baja un rato.");
        Grupo("Botín", "caja", "Hunde un pirata y suelta cajas que flotan: pasa cerca (a pie, nadando o con tu barco) y se recogen solas. Si lo capturas (sin tripulación), todo su botín va a tu mochila.");
        Grupo("Pintura", "brocha", "A bordo de tu barco, " + T(Accion.Taller) + ": el Taller. Pinta cada parte con cualquier color, dibujos en velas y bandera (franjas, cruz, calavera, sol…) y elige el mascarón. Se paga al guardar (5 de oro por parte). Se conserva al mejorar el barco y al guardar la partida.");
        Grupo("Tu pirata", "persona", T(Accion.Pirata) + ": cambia tu aspecto (camisa, piel, maquillaje, falda, barba y banderita de orgullo, trans, bisexual, lesbiana o no binaria). " +
            T(Accion.Usar) + " junto a un pirata en su cubierta le da un beso: se queda embobado y no ataca un rato. Besa a " + Apariencia.CapitanesParaLuwyn + " capitanes (sombrero con pluma) y " +
            Apariencia.CanonerosParaLuwyn + " cañoneros (pañuelo rojo) para desbloquear a " + Apariencia.NombreLuwyn + ": barba loca, la falda más linda, maquillaje y una antena caída. Su barco lleva una crisopa de insignia.");
        Grupo("Pactos malditos", "calavera", "En algunas islas lejanas hay altares de piedra con una luz verde. " + T(Accion.Usar) + " junto al altar: te ofrece un pacto con el mar, una ventaja con su precio (velas más rápidas pero el casco se pudre, balas más fuertes pero recarga lenta…). Tu barco carga hasta " + Pactos.Maximo + ". Si se hunde, el mar cobra la deuda y los pierdes todos.");
        Grupo("Cámara", "ojo", "Mover el mouse mirar · Rueda acercar o alejar (al timón: desde el timón hasta ver el barco pequeñito) · " + T(Accion.Pausa) + " soltar el cursor · Clic volver a capturarlo.");
        Grupo("Partida", "pergamino", "Se guarda sola cada 30 s y al salir · " + T(Accion.Guardar) + " guardar ya · " + T(Accion.BorrarPartida) + " borrar la partida (pide confirmación).");
        if (!Application.isEditor && !Debug.isDebugBuild) return;   // creativo y teclas de prueba solo existen al desarrollar
        Grupo("Creativo", "rayo", T(Accion.Creativo) + " activar o quitar: todo gratis, todas las herramientas, sin daño ni hambre. En Fabricar aparece la categoría Creativo: llenar de cañones, reparar, saltar de molde, traer o reiniciar el enemigo, mar en calma, +100 de todo.");
        Grupo("Teclas de prueba", "teclado", K("1") + " dañar al pirata más cercano · " + K("2") + "/" + K("3") + " traerlo · " + K("4") + " materiales · " + K("5") + "–" + K("0") + " y " + K("H") + " ir a un recurso · " + K("Y") + " a tu barco · " + K("F7") + " puntería fija · " + K("F9") + " desbloquear a Luwyn · " + K("F10") + " ir a un altar maldito · " + K("J") + " isla nueva · " + K("U") + " tu barco 800 m más lejos · " + K("Z") + " bajar un nivel dentro del barco · " + K("O") + " adelantar 3 horas · " + K("X") + " cambiar el clima · " + K("L") + " caja de botín · " + K("N") + " mercader · " + K("V") + " un pirata viene a abordarte");
    }

    void Grupo(string titulo, string icono, string texto)
    {
        var f = Componentes.FilaLista(false);
        f.style.alignItems = Align.FlexStart;
        var ic = Iconos.Crear(icono, 18f, Estilo.Oro);
        ic.style.marginTop = 2f; ic.style.marginRight = 8f;
        f.Add(ic);
        var col = Estilo.Columna(); col.style.flexGrow = 1f; col.style.flexShrink = 1f;
        var t = Estilo.Etiqueta(titulo, Estilo.TamTexto, Estilo.Oro, Estilo.FuenteTitulo);
        t.style.marginBottom = 2f;
        col.Add(t);
        col.Add(Estilo.Etiqueta(texto, Estilo.TamTextoChico, Estilo.Texto));
        f.Add(col);
        lista.Add(f);
    }
}
