using System.Collections.Generic;
using UnityEngine;

// Recetas de fabricación (las muestra VentanaFabricacion, tecla Tab, por categorías) y lo que hace cada una:
// herramientas, materiales, horno, cañones, la balsa, mejorar y agrandar tu barco, cañones por lado, barriles, bodega,
// pintura y los atajos del modo creativo. Junto a un mercader, Tab abre el mercado (VentanaComercio).
// El barco es uno solo desde el principio: la balsa crece y conserva sus mejoras, cañones y bodega.
[RequireComponent(typeof(Inventario), typeof(Personaje))]
public class Fabricacion : MonoBehaviour
{
    // Cualquier ventana o el Taller abiertos: el cursor queda libre y el juego no reacciona a las teclas
    public static bool Abierto => Taller.Abierto || Ventanas.AlgunaAbierta;

    [Header("Materiales para lo que se construye en el mundo")]
    public Material piedra;
    public Material brasa;
    public Material cascoBalandro, velaBalandro, banderaBalandro, cubierta, mastil, hierroCanon;

    // Forma del barco donde estás (cuántas veces se alargó y se ensanchó)

    public class Receta
    {
        public string id, icono, resultado;                   // resultado: lo que entra a la mochila (para su icono)
        public Categoria categoria;
        public bool unaVez;                                   // no se puede repetir varias veces seguidas (cambios de barco)
        public string nombre, descripcion;
        public System.Func<Dictionary<string, int>> costo;   // puede cambiar según el nivel
        public System.Func<string> requisito;                 // null si se puede, o el motivo si no
        public System.Func<string> hacer;                     // null si salió bien, o el motivo si no
        public string mensaje;                                // texto al terminar (null = "Fabricaste: ...", "" = nada)
        public System.Func<string> detalle;                   // descripción que cambia (si no hay, se usa descripcion)
    }

    Inventario inv;
    Personaje p;
    List<Receta>[] paginas;
    static readonly Dictionary<string, int> Gratis = new Dictionary<string, int>();

    void Awake() => Preparar();

    static Dictionary<string, int> C(params object[] pares)
    {
        var d = new Dictionary<string, int>();
        for (int i = 0; i + 1 < pares.Length; i += 2) d[(string)pares[i]] = (int)pares[i + 1];
        return d;
    }

    // Si Unity recompila con el juego corriendo, estas referencias se pierden: se vuelven a crear aquí
    void Preparar()
    {
        if (paginas != null && todas != null && inv != null && p != null) return;
        inv = GetComponent<Inventario>();
        p = GetComponent<Personaje>();
        paginas = new[]
        {
            new List<Receta>
            {
                new Receta { nombre = "Cuerda", descripcion = "Fibra trenzada. Sirve para amarrar.",
                    costo = () => C(Inventario.Fibra, 3),
                    hacer = () => { inv.Agregar(Inventario.Cuerda, 1); return null; } },
                new Receta { nombre = "Hacha", descripcion = "Para talar palmas y sacar troncos.",
                    costo = () => C(Inventario.Palo, 2, Inventario.Cuerda, 1, Inventario.Piedra, 1),
                    hacer = () => { inv.Agregar(Inventario.Hacha, 1); return null; } },
                new Receta { nombre = "Pico", descripcion = "Para rocas grandes y vetas de hierro.",
                    costo = () => C(Inventario.Palo, 2, Inventario.Cuerda, 2, Inventario.Piedra, 3),
                    hacer = () => { inv.Agregar(Inventario.Pico, 1); return null; } },
                new Receta { nombre = "Horno de piedra", descripcion = "Se arma frente a ti. Para fundir hierro.",
                    costo = () => C(Inventario.Piedra, 12, Inventario.Madera, 4),
                    requisito = () => p.estado == Personaje.Estado.Libre && p.EnTierra ? null : "Párate en tierra firme",
                    hacer = ConstruirHorno, mensaje = "Horno armado. Funde el mineral a su lado" },
                new Receta { nombre = "Fundir hierro", descripcion = "Mineral + leña en el horno = lingote.",
                    costo = () => C(Inventario.Mineral, 2, Inventario.Madera, 2),
                    requisito = () => ModoCreativo.Activo || Horno.HayCerca(transform.position) ? null : "Acércate a un horno de piedra",
                    hacer = () => { inv.Agregar(Inventario.Lingote, 1); return null; }, mensaje = "+1 lingote de hierro" },
                new Receta { nombre = "Cañón", descripcion = "Pieza de barco. Se instala en una ranura.",
                    costo = () => C(Inventario.Lingote, 3, Inventario.Madera, 2),
                    requisito = () => ModoCreativo.Activo || Horno.HayCerca(transform.position) ? null : "Se funde junto a un horno",
                    hacer = () => { inv.Agregar(Inventario.Canon, 1); return null; } },
                new Receta { nombre = "Balsa", descripcion = "Troncos amarrados. Se arma en la orilla.",
                    costo = () => C(Inventario.Madera, Balsa.CostoMadera, Inventario.Cuerda, Balsa.CostoCuerda),
                    requisito = () => {
                        if (p.barco != null) return "Ya tienes barco: agrándalo en Tu barco (E)";
                        return p.estado == Personaje.Estado.Libre && p.EnTierra ? null : "Párate en tierra, cerca de la orilla"; },
                    hacer = ConstruirBalsa, mensaje = "¡Balsa armada en la orilla! Es tu barco. Nada hasta ella y presiona F" },
            },
            new List<Receta>
            {
                RecetaMolde(),
                RecetaCrecer(0),
                RecetaCrecer(1),
                RecetaCrecer(2),
                new Receta { nombre = "Reforzar casco", descripcion = "+25 % de vida del casco. Sin tope.",
                    costo = () => { int n = Piezas() != null ? Piezas().nivelCasco : 0;
                        return C(Inventario.Madera, PiezasBarco.Costo(6, n), Inventario.Cuerda, PiezasBarco.Costo(2, n), Inventario.Piedra, PiezasBarco.Costo(2, n)); },
                    requisito = RequisitoBarco, hacer = () => { Piezas(true).MejorarCasco(); return null; }, mensaje = "Casco reforzado" },
                new Receta { nombre = "Mejorar velas", descripcion = "+12 % de empuje. Sin tope.",
                    costo = () => { int n = Piezas() != null ? Piezas().nivelVelas : 0;
                        return C(Inventario.Fibra, PiezasBarco.Costo(8, n), Inventario.Cuerda, PiezasBarco.Costo(3, n), Inventario.Palo, PiezasBarco.Costo(2, n)); },
                    requisito = RequisitoBarco, hacer = () => { Piezas(true).MejorarVelas(); return null; }, mensaje = "Velas mejoradas" },
            },
            new List<Receta>
            {
                RecetaCanon(LadoCanon.Babor),
                RecetaCanon(LadoCanon.Estribor),
                RecetaCanon(LadoCanon.Proa),
                new Receta { nombre = "Barriles en llamas (popa)", descripcion = "Se sueltan por atrás: flotan y explotan si los toca otro barco. Mira atrás y apunta.",
                    costo = () => C(Inventario.Madera, 6, Inventario.Cuerda, 2, Inventario.Fibra, 4),
                    requisito = () => {
                        string r = RequisitoBarco(); if (r != null) return r;
                        Canones c = BarcoAbordo().GetComponent<Canones>();
                        if (c == null || c.RanurasLado(LadoCanon.Popa) == 0) return "La balsa no tiene popa: agrándala";
                        return c.HayRanura(LadoCanon.Popa) ? null : "Ya los tienes"; },
                    hacer = () => BarcoAbordo().GetComponent<Canones>().Instalar(LadoCanon.Popa) ? null : "Ya los tienes",
                    mensaje = "Barriles en llamas listos en la popa" },
                new Receta { nombre = "Mejorar barriles", descripcion = "Suelta un barril más por tanda (hasta 5).",
                    costo = () => { Canones c = BarcoAbordo() != null ? BarcoAbordo().GetComponent<Canones>() : null;
                        int n = c != null ? c.nivelBarriles : 0;
                        return C(Inventario.Madera, 8 * (n + 1), Inventario.Cuerda, 3 * (n + 1), Inventario.Lingote, n + 1); },
                    requisito = () => {
                        string r = RequisitoBarco(); if (r != null) return r;
                        Canones c = BarcoAbordo().GetComponent<Canones>();
                        if (c == null || c.EnLado(LadoCanon.Popa) == 0) return "Primero pon los barriles en llamas";
                        return c.nivelBarriles >= Canones.MaxNivelBarriles ? "Ya sueltas " + c.BarrilesPorTanda + " barriles (máximo)" : null; },
                    detalle = () => { Canones c = BarcoAbordo() != null ? BarcoAbordo().GetComponent<Canones>() : null;
                        return c != null ? "Ahora: " + c.BarrilesPorTanda + " por tanda → " + Mathf.Min(c.BarrilesPorTanda + 1, Canones.MaxNivelBarriles + 1) : null; },
                    hacer = () => { BarcoAbordo().GetComponent<Canones>().nivelBarriles++; return null; },
                    mensaje = "Barriles mejorados" },
                new Receta { nombre = "Guardar comida", descripcion = "Pasa tus cocos y frutas a la bodega.",
                    costo = () => Gratis, requisito = RequisitoBarco, mensaje = "",
                    hacer = () => {
                        Bodega b = BodegaActual(true);
                        int n = b.Guardar(inv, Inventario.Coco) + b.Guardar(inv, Inventario.Fruta);
                        if (n == 0) return b.Libre == 0 ? "La bodega está llena" : "No llevas comida para guardar";
                        p.Avisar("Guardaste " + n + " de comida. Bodega: " + b.Resumen());
                        return null; } },
                new Receta { nombre = "Sacar comida", descripcion = "Pasa la comida de la bodega a tu inventario.",
                    costo = () => Gratis, requisito = RequisitoBarco, mensaje = "",
                    hacer = () => {
                        Bodega b = BodegaActual(true);
                        int n = b.Sacar(inv, Inventario.Coco) + b.Sacar(inv, Inventario.Fruta);
                        if (n == 0) return "La bodega no tiene comida";
                        p.Avisar("Sacaste " + n + " de comida de la bodega");
                        return null; } },
            },
            PaginaPintura(),
            PaginaVender(),
            PaginaComprar(),
            PaginaCreativo(),
        };
        Clasificar();
    }

    // ---------- Pintura de tu barco: se hace en el Taller (tecla P) ----------
    Pintura PinturaActual(bool crear)
    {
        ControlBarco b = BarcoAbordo();
        if (b == null) return null;
        Pintura pt = b.GetComponent<Pintura>();
        if (pt == null && crear) pt = b.gameObject.AddComponent<Pintura>();
        return pt;
    }

    List<Receta> PaginaPintura()
    {
        return new List<Receta>
        {
            new Receta { nombre = "Abrir el taller (P)", descripcion = "Pinta cada parte con el color que quieras, dibujos en velas y bandera, mascarón",
                costo = () => Gratis, requisito = RequisitoPropio, mensaje = "",
                hacer = () => { Ventanas.CerrarTodas(); Taller.Abrir(); return null; } },
            new Receta
            {
                nombre = "Colores originales", descripcion = "Quita toda la pintura (gratis)",
                costo = () => Gratis, requisito = RequisitoPropio, mensaje = "Tu barco volvió a sus colores originales",
                hacer = () => { Pintura pt = PinturaActual(true); string masc = pt.datos.mascaron; pt.datos = new Pintura.Datos { mascaron = masc }; pt.Aplicar(); return null; }
            },
        };
    }

    // ---------- Mercado: vender y comprar con un mercader cerca (precios distintos en cada isla) ----------
    static string RequisitoMercader(Vector3 p) => Mercader.Cercano(p) != null ? null : "Acércate a un mercader (puestos en la playa de algunas islas)";

    List<Receta> PaginaVender()
    {
        var lista = new List<Receta>();
        var lotes = new (string tipo, int n)[] {
            (Inventario.Madera, 10), (Inventario.Palo, 10), (Inventario.Fibra, 10), (Inventario.Piedra, 10), (Inventario.Cuerda, 5),
            (Inventario.Mineral, 5), (Inventario.Lingote, 1), (Inventario.Canon, 1), (Inventario.Coco, 5), (Inventario.Fruta, 5) };
        foreach (var l in lotes)
        {
            string tipo = l.tipo; int n = l.n;
            lista.Add(new Receta
            {
                nombre = "Vender " + n + " " + Corto(tipo),
                descripcion = "El precio cambia en cada isla",
                costo = () => C(tipo, n),
                requisito = () => RequisitoMercader(transform.position),
                detalle = () => {
                    Mercader m = Mercader.Cercano(transform.position);
                    if (m == null) return null;
                    string e = m.Etiqueta(tipo);
                    return "Te paga " + Hud.Col(m.PrecioVenta(tipo, n) + " oro", Hud.Oro) + (e != "" ? "  · " + e : ""); },
                hacer = () => {
                    Mercader m = Mercader.Cercano(transform.position);
                    if (m == null) return "No hay mercader cerca";
                    int oro = m.PrecioVenta(tipo, n);
                    inv.Agregar(Inventario.Oro, oro);
                    m.Vendiste(tipo);
                    p.Avisar("Vendiste " + n + " " + Corto(tipo) + ": +" + oro + " oro");
                    return null; },
                mensaje = ""
            });
        }
        return lista;
    }

    List<Receta> PaginaComprar()
    {
        var lista = new List<Receta>();
        var lotes = new (string tipo, int n)[] {
            (Inventario.Canon, 1), (Inventario.Lingote, 1), (Inventario.Cuerda, 5), (Inventario.Madera, 10), (Inventario.Coco, 5), (Inventario.Fruta, 5) };
        foreach (var l in lotes)
        {
            string tipo = l.tipo; int n = l.n;
            lista.Add(new Receta
            {
                nombre = "Comprar " + n + " " + Corto(tipo),
                descripcion = "El precio cambia en cada isla",
                costo = () => { Mercader m = Mercader.Cercano(transform.position); return C(Inventario.Oro, m != null ? m.PrecioCompra(tipo, n) : 0); },
                requisito = () => RequisitoMercader(transform.position),
                detalle = () => {
                    Mercader m = Mercader.Cercano(transform.position);
                    if (m == null) return null;
                    string e = m.Etiqueta(tipo);
                    return "Llevas " + inv.Cantidad(tipo) + (e != "" ? "  · " + e : ""); },
                hacer = () => {
                    Mercader m = Mercader.Cercano(transform.position);
                    if (m == null) return "No hay mercader cerca";
                    if (!Inventario.SinPeso(tipo) && inv.Espacio < n) return "No te cabe en la mochila (" + inv.Carga + " / " + Inventario.Capacidad + ")";
                    inv.Agregar(tipo, n);
                    m.Compraste(tipo);
                    return null; },
                mensaje = "Compraste " + n + " " + Corto(tipo)
            });
        }
        return lista;
    }

    // ---------- Página del modo creativo: atajos para probar rápido ----------
    List<Receta> PaginaCreativo()
    {
        Receta R(string nombre, string desc, System.Func<string> requisito, System.Func<string> hacer) =>
            new Receta { nombre = nombre, descripcion = desc, costo = () => Gratis, requisito = requisito, hacer = hacer, mensaje = "" };
        Canones Cn() => BarcoAbordo() != null ? BarcoAbordo().GetComponent<Canones>() : null;
        return new List<Receta>
        {
            R("Llenar de cañones", "Pone cañones en todas las ranuras", RequisitoPropio, () => {
                Canones c = Cn();
                c.Fijar(c.RanurasLado(LadoCanon.Babor), c.RanurasLado(LadoCanon.Estribor), c.RanurasLado(LadoCanon.Proa), c.RanurasLado(LadoCanon.Popa));
                p.Avisar(c.Instalados + " cañones instalados"); return null; }),
            R("Quitar cañones", "Deja todas las ranuras vacías", RequisitoPropio, () => { Cn().Fijar(0, 0, 0, 0); p.Avisar("Cañones quitados"); return null; }),
            R("Reparar barco", "Casco al máximo", RequisitoPropio, () => {
                VidaBarco v = BarcoAbordo().GetComponent<VidaBarco>(); v.vida = v.vidaMaxima; p.Avisar("Barco reparado"); return null; }),
            R("Molde anterior", "Vuelve un molde atrás", () => {
                string r = RequisitoPropio(); if (r != null) return r;
                FormaBarco.Leer(BarcoAbordo(), out int mo, out _, out _, out _); return mo > 0 ? null : "Ya es el primer molde"; },
                () => Rehacer(-1, 0, 0, 0)),
            R("Último molde", "Salta al navío de cuatro puentes", () => {
                string r = RequisitoPropio(); if (r != null) return r;
                FormaBarco.Leer(BarcoAbordo(), out int mo, out _, out _, out _); return mo < FormaBarco.UltimoMolde ? null : "Ya es el último molde"; },
                () => { FormaBarco.Leer(BarcoAbordo(), out int mo, out _, out _, out _); return Rehacer(FormaBarco.UltimoMolde - mo, 0, 0, 0); }),
            R("Tamaño original", "Quita lo alargado, ancho y pisos", RequisitoPropio, () => {
                FormaBarco.Leer(BarcoAbordo(), out _, out int a, out int b, out int c); return Rehacer(0, -a, -b, -c); }),
            R("Traer enemigo", "El pirata más cercano a 60 m de tu costado", () => p.barco != null ? null : "Primero necesitas un barco", () => {
                IABarco ia = IABarco.MasCercano(Personaje.Instancia != null ? Personaje.Instancia.transform.position : Vector3.zero); if (ia == null) return "No hay enemigo";
                TeclasPrueba.Traer(ia, p, 60f); return null; }),
            R("Reiniciar enemigo", "Casco y tripulación nuevos, junto a ti", () => p.barco != null ? null : "Primero necesitas un barco", () => {
                IABarco ia = IABarco.MasCercano(Personaje.Instancia != null ? Personaje.Instancia.transform.position : Vector3.zero); if (ia == null) return "No hay enemigo";
                ia.GetComponent<VidaBarco>().Reaparecer(); TeclasPrueba.Traer(ia, p, 60f); p.Avisar("Enemigo nuevo a 60 m"); return null; }),
            R("Mar en calma / con olas", "Cambia el oleaje", null, () => {
                Clima.Calmado = !Clima.Calmado; p.Avisar(Clima.Calmado ? "Mar en calma" : "Olas normales (según el clima)"); return null; }),
            R("+100 de todo", "Materiales, comida y 20 cañones", null, () => {
                foreach (string t in new[] { Inventario.Madera, Inventario.Palo, Inventario.Fibra, Inventario.Cuerda, Inventario.Piedra,
                                             Inventario.Mineral, Inventario.Lingote, Inventario.Coco, Inventario.Fruta }) inv.Agregar(t, 100);
                inv.Agregar(Inventario.Canon, 20); return null; }),
        };
    }

    // Mejorar el barco = pasar al siguiente molde (balsa → bote → chalupa ... → navío de cuatro puentes).
    // Se conserva lo alargado, lo ancho y los pisos; por eso cuesta más mientras más grande lo lleves.
    Receta RecetaMolde()
    {
        return new Receta
        {
            nombre = "Mejorar barco", descripcion = "Siguiente molde. Conserva tamaño y pisos.",
            costo = () => {
                FormaBarco.Leer(BarcoAbordo(), out int mo, out int a, out int b, out int c);
                return mo >= FormaBarco.UltimoMolde ? Gratis : FormaBarco.CostoMolde(mo + 1, a, b, c); },
            requisito = () => {
                string r = RequisitoPropio(); if (r != null) return r;
                FormaBarco.Leer(BarcoAbordo(), out int mo, out _, out _, out _);
                return mo >= FormaBarco.UltimoMolde ? "Ya es el molde más grande" : null; },
            detalle = () => {
                ControlBarco barco = BarcoAbordo() ?? p.barco;
                if (barco == null) return null;
                FormaBarco.Leer(barco, out int mo, out int a, out int b, out int c);
                if (mo >= FormaBarco.UltimoMolde) return null;
                return FormaBarco.De(mo).nombre + " → " + FormaBarco.Nombre(mo + 1, a, b, c) + ", " + FormaBarco.CanonesTotales(mo + 1, a, b, c) + " cañones"; },
            hacer = () => Rehacer(1, 0, 0, 0), mensaje = ""
        };
    }

    // Agrandar sin cambiar el molde: 0 = alargar, 1 = ensanchar, 2 = subir un piso
    Receta RecetaCrecer(int eje)
    {
        string[] nombres = { "Alargar", "Ensanchar", "Subir un piso" };
        string[] descs = { "Más rápido y más cañones por fila; gira peor", "Más vida y bodega, gira mejor; más lento", "Una fila más de cañones y más vida; más lento" };
        return new Receta
        {
            nombre = nombres[eje], descripcion = descs[eje],
            costo = () => { FormaBarco.Leer(BarcoAbordo(), out int mo, out int a, out int b, out int c); return FormaBarco.CostoAgrandar(mo, a, b, c); },
            requisito = () => {
                string r = RequisitoPropio(); if (r != null) return r;
                FormaBarco.Leer(BarcoAbordo(), out _, out int a, out int b, out int c);
                int actual = eje == 0 ? a : eje == 1 ? b : c;
                return actual >= FormaBarco.MaxPasos ? "Ya está al tope (" + FormaBarco.MaxPasos + ")" : null; },
            detalle = () => {
                ControlBarco barco = BarcoAbordo() ?? p.barco;
                if (barco == null) return null;
                FormaBarco.Leer(barco, out int mo, out int a, out int b, out int c);
                if (eje == 0) a++; else if (eje == 1) b++; else c++;
                FormaBarco.Medidas(mo, a, b, c, out float L, out float w, out float h);
                return "→ " + L.ToString("0.#") + " x " + w.ToString("0.#") + " m, " + FormaBarco.Filas(mo, c) + " filas, " + FormaBarco.CanonesTotales(mo, a, b, c) + " cañones"; },
            hacer = () => Rehacer(0, eje == 0 ? 1 : 0, eje == 1 ? 1 : 0, eje == 2 ? 1 : 0), mensaje = ""
        };
    }

    // Un cañón en un lado del barco. Se dispara apuntando con clic derecho (Q y E también disparan los costados).
    Receta RecetaCanon(LadoCanon lado)
    {
        string nombre = Canones.Nombre(lado);
        string desc = lado == LadoCanon.Babor ? "Izquierda. Apunta o dispara con Q"
                    : lado == LadoCanon.Estribor ? "Derecha. Apunta o dispara con E"
                    : lado == LadoCanon.Proa ? "Adelante, en cubierta (2). Solo disparan al frente" : "Atrás. Mira hacia atrás y apunta";
        return new Receta
        {
            nombre = "Cañón a " + nombre, descripcion = desc,
            costo = () => C(Inventario.Canon, 1),
            requisito = () => {
                string r = RequisitoBarco(); if (r != null) return r;
                Canones c = BarcoAbordo().GetComponent<Canones>();
                if (c == null || c.RanurasLado(lado) == 0) return lado == LadoCanon.Proa || lado == LadoCanon.Popa
                    ? "La balsa no tiene " + nombre + ": agrándala" : "Este barco no aguanta cañones";
                return c.HayRanura(lado) ? null : "Ese lado está lleno (" + c.EnLado(lado) + "/" + c.RanurasLado(lado) + ")"; },
            hacer = () => BarcoAbordo().GetComponent<Canones>().Instalar(lado) ? null : "Ese lado está lleno",
            mensaje = "Cañón instalado a " + nombre
        };
    }

    // ---------- Barco donde estás ----------
    ControlBarco BarcoAbordo()
    {
        if (p.estado == Personaje.Estado.Libre || p.BarcoActual == null) return null;
        IABarco ia = p.BarcoActual.GetComponent<IABarco>();
        return ia != null && !ia.Capturado ? null : p.BarcoActual;   // el barco enemigo no se mejora
    }

    string RequisitoBarco() => BarcoAbordo() != null ? null : "Sube a tu barco (cubierta o timón)";
    string RequisitoPropio()
    {
        string r = RequisitoBarco(); if (r != null) return r;
        return BarcoAbordo() == p.barco ? null : "Solo puedes cambiar tu propio barco";
    }

    PiezasBarco Piezas(bool crear = false)
    {
        ControlBarco b = BarcoAbordo();
        if (b == null) return null;
        PiezasBarco pz = b.GetComponent<PiezasBarco>();
        if (pz == null && crear) pz = b.gameObject.AddComponent<PiezasBarco>();
        return pz;
    }

    Bodega BodegaActual(bool crear = false)
    {
        ControlBarco b = BarcoAbordo();
        if (b == null) return null;
        Bodega bd = b.GetComponent<Bodega>();
        if (bd == null && crear) bd = b.gameObject.AddComponent<Bodega>();
        return bd;
    }

    // ---------- Construcciones ----------
    string ConstruirHorno()
    {
        Vector3 frente = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;
        Vector3 pos = transform.position + frente * 2.2f;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit suelo, 10f, ~0, QueryTriggerInteraction.Ignore))
            pos.y = suelo.point.y;
        if (pos.y < Olas.Altura(pos)) return "No se puede armar en el agua";
        Horno.Construir(pos, transform.eulerAngles.y + 180f, piedra, brasa);
        return null;
    }

    string ConstruirBalsa()
    {
        if (!Balsa.BuscarAgua(transform.position, out Vector3 punto, out float rumbo)) return "No hay agua cerca. Acércate a la orilla";
        GameObject balsa = FormaBarco.Construir(0, 0, 0, 0, punto, rumbo, Mats(), p.materialMadera, p.materialLona);
        p.AsignarBarcoPropio(balsa.GetComponent<ControlBarco>());   // tu primer barco: aquí reapareces
        return null;
    }

    // Al cargar la partida: tu barco tal como lo dejaste
    public ControlBarco CrearBarcoPropio(int mo, int a, int b, int c, Vector3 pos, float rumbo)
    {
        if (p == null) p = GetComponent<Personaje>();
        FormaBarco.Medidas(mo, a, b, c, out _, out _, out float h);
        pos.y = Olas.Altura(pos) + h / 2f - (FormaBarco.De(mo).balsa ? 0.3f : 1.2f);
        GameObject g = FormaBarco.Construir(mo, a, b, c, pos, rumbo, Mats(), p.materialMadera, p.materialLona);
        ControlBarco cb = g.GetComponent<ControlBarco>();
        p.AsignarBarcoPropio(cb);
        return cb;
    }

    public void CrearHorno(Vector3 pos, float rumbo) => Horno.Construir(pos, rumbo, piedra, brasa);

    Astillero.Materiales Mats() => new Astillero.Materiales { casco = cascoBalandro, vela = velaBalandro, bandera = banderaBalandro, cubierta = cubierta, mastil = mastil, canon = hierroCanon };

    // Rehace el barco ahí mismo, en el agua, con otro molde o más grande.
    // Es el mismo barco: se quedan las mejoras de casco y velas, los cañones y lo que hay en la bodega.
    string Rehacer(int dMolde, int dLargo, int dAncho, int dAlto)
    {
        ControlBarco viejo = BarcoAbordo();
        FormaBarco.Leer(viejo, out int mo, out int a, out int b, out int c);
        mo += dMolde; a += dLargo; b += dAncho; c += dAlto;
        FormaBarco.Medidas(mo, a, b, c, out float L, out _, out float h);
        Transform tv = viejo.transform;
        Vector3 adelante = Vector3.ProjectOnPlane(tv.forward, Vector3.up).normalized;
        if (!Balsa.HayAguaPara(tv.position, adelante, L, FormaBarco.Hondo(mo, a, b, c)))
            return "Muy cerca de tierra: aléjate de la orilla para cambiar el barco";

        VidaBarco vidaVieja = viejo.GetComponent<VidaBarco>();
        if (vidaVieja != null && vidaVieja.Hundido) return "Tu barco se está hundiendo";
        PiezasBarco pzViejo = viejo.GetComponent<PiezasBarco>();
        Bodega bdVieja = viejo.GetComponent<Bodega>();
        Canones cnViejo = viejo.GetComponent<Canones>();
        int cBabor = cnViejo != null ? cnViejo.enBabor : 0, cEstribor = cnViejo != null ? cnViejo.enEstribor : 0;
        int cProa = cnViejo != null ? cnViejo.enProa : 0, cPopa = cnViejo != null ? cnViejo.enPopa : 0;
        float danoFraccion = vidaVieja != null ? 1f - vidaVieja.vida / vidaVieja.vidaMaxima : 0f;
        // Se apoya el fondo del barco nuevo cerca del agua
        Vector3 pos = tv.position; pos.y = Olas.Altura(pos) + h / 2f - (FormaBarco.De(mo).balsa ? 0.3f : 1.2f);
        float rumbo = tv.eulerAngles.y;

        // El barco viejo se quita ya, para que su casco no choque con el nuevo
        viejo.gameObject.SetActive(false);

        GameObject nuevo = FormaBarco.Construir(mo, a, b, c, pos, rumbo, Mats(), p.materialMadera, p.materialLona);
        VidaBarco vida = nuevo.GetComponent<VidaBarco>();
        if (pzViejo != null && pzViejo.NivelTotal > 0) nuevo.AddComponent<PiezasBarco>().Copiar(pzViejo.nivelCasco, pzViejo.nivelVelas);
        vida.vida = vida.vidaMaxima * (1f - danoFraccion * 0.5f);   // al rehacerlo se repara la mitad del daño
        if (bdVieja != null) nuevo.AddComponent<Bodega>().CopiarDe(bdVieja);
        Pintura ptVieja = viejo.GetComponent<Pintura>();
        if (ptVieja != null) nuevo.AddComponent<Pintura>().Copiar(ptVieja);   // la pintura se conserva
        nuevo.GetComponent<Canones>().Fijar(cBabor, cEstribor, cProa, cPopa);   // lo que no quepa se queda en las ranuras que hay
        if (cnViejo != null) nuevo.GetComponent<Canones>().nivelBarriles = cnViejo.nivelBarriles;

        p.AsignarBarcoPropio(nuevo.GetComponent<ControlBarco>());
        p.IrAlTimonPropio();
        Destroy(viejo.gameObject);
        p.Avisar(dMolde > 0 ? "¡Tu barco ahora es " + FormaBarco.Nombre(mo, a, b, c).ToLower() + "! " + FormaBarco.CanonesTotales(mo, a, b, c) + " ranuras de cañón"
                            : FormaBarco.Nombre(mo, a, b, c) + ": " + L.ToString("0.#") + " m, " + FormaBarco.Filas(mo, c) + " filas de cañones");
        return null;
    }

    // ---------- Recetas para la ventana de fabricación (Tab) ----------
    public enum Categoria { Herramientas, Materiales, Supervivencia, Construccion, Barco, Armamento, Creativo }
    public static readonly string[] NombresCategoria = { "Herramientas", "Materiales", "Supervivencia", "Construcción", "Barco", "Armamento", "Creativo" };
    public static readonly string[] IconosCategoria = { "hacha", "madera", "fruta", "martillo", "barco", "canon", "rayo" };

    public static Fabricacion I { get; private set; }
    // El menú de fabricación ahora es una ventana (VentanaFabricacion); se mantiene el nombre para el resto del juego
    public static bool MenuAbierto => Ventanas.EstaAbierta<VentanaFabricacion>() || Ventanas.EstaAbierta<VentanaComercio>();

    List<Receta> todas;
    public IReadOnlyList<Receta> Todas { get { Preparar(); return todas; } }

    void OnEnable() => I = this;

    void Clasificar()
    {
        todas = new List<Receta>();
        void Poner(Receta r, Categoria c, string icono) { r.categoria = c; r.icono = icono; todas.Add(r); }
        foreach (Receta r in paginas[0])
        {
            switch (r.nombre)
            {
                case "Cuerda": Poner(r, Categoria.Materiales, "cuerda"); r.resultado = Inventario.Cuerda; break;
                case "Hacha": Poner(r, Categoria.Herramientas, "hacha"); r.resultado = Inventario.Hacha; break;
                case "Pico": Poner(r, Categoria.Herramientas, "pico"); r.resultado = Inventario.Pico; break;
                case "Horno de piedra": Poner(r, Categoria.Construccion, "fuego"); break;
                case "Fundir hierro": Poner(r, Categoria.Materiales, "lingote"); r.resultado = Inventario.Lingote; break;
                case "Cañón": Poner(r, Categoria.Armamento, "canon"); r.resultado = Inventario.Canon; break;
                default: Poner(r, Categoria.Barco, "barco"); r.unaVez = true; break;   // balsa
            }
        }
        string[] iconosBarco = { "barco", "regla", "regla", "pisos", "escudo", "vela" };
        for (int k = 0; k < paginas[1].Count; k++) { Poner(paginas[1][k], Categoria.Barco, k < iconosBarco.Length ? iconosBarco[k] : "barco"); paginas[1][k].unaVez = k < 4; }
        foreach (Receta r in paginas[2])
        {
            if (r.nombre.Contains("comida")) Poner(r, Categoria.Supervivencia, "caja");
            else Poner(r, Categoria.Armamento, r.nombre.Contains("arril") ? "fuego" : "canon");
        }
        foreach (Receta r in paginas[3]) { Poner(r, Categoria.Barco, "brocha"); r.unaVez = true; }
        foreach (Receta r in paginas[6]) { Poner(r, Categoria.Creativo, "rayo"); r.unaVez = true; }
        for (int k = 0; k < todas.Count; k++) todas[k].id = todas[k].categoria + "/" + todas[k].nombre;
    }

    // Por qué no se puede hacer ahora (null si se puede): requisito, o materiales que faltan
    public string Impedimento(Receta r)
    {
        string req = r.requisito != null ? r.requisito() : null;
        if (req != null) return req;
        if (!PuedePagar(r)) return "Faltan materiales" + Faltante(r);
        return null;
    }

    public string Requisito(Receta r) => r.requisito != null ? r.requisito() : null;

    // Fabrica una vez (silencioso: sin avisar si falta algo, para las repeticiones)
    public bool Intentar(Receta r, bool silencioso)
    {
        Preparar();
        string req = r.requisito != null ? r.requisito() : null;
        if (req != null) { if (!silencioso) p.Avisar(req); return false; }
        if (!PuedePagar(r)) { if (!silencioso) p.Avisar("Te faltan materiales para: " + r.nombre + Faltante(r)); return false; }
        Dictionary<string, int> costo = r.costo();   // se calcula antes de hacerlo (el nivel puede cambiar)
        string error = r.hacer();
        if (error != null) { if (!silencioso) p.Avisar(error); return false; }
        if (!ModoCreativo.Activo) foreach (var c in costo) inv.Quitar(c.Key, c.Value);
        if (!silencioso)
        {
            if (r.mensaje == null) Notificaciones.Mostrar("Fabricaste: " + r.nombre, Notificaciones.Tipo.Exito, 3f, r.icono);
            else if (r.mensaje != "") Notificaciones.Mostrar(r.mensaje, Notificaciones.Tipo.Exito, 4f, r.icono);
        }
        return true;
    }

    // Fabrica hasta n veces seguidas; devuelve cuántas salieron
    public int Fabricar(Receta r, int n)
    {
        if (n <= 1 || r.unaVez) return Intentar(r, false) ? 1 : 0;
        int hechas = 0;
        while (hechas < n && Intentar(r, true)) hechas++;
        if (hechas == 0) Intentar(r, false);   // para que avise el motivo
        else Notificaciones.Mostrar("Fabricaste " + hechas + " × " + r.nombre, Notificaciones.Tipo.Exito, 3f, r.icono);
        return hechas;
    }

    // " (faltan 3 fibra, 1 cuerda)"
    public string Faltante(Receta r)
    {
        string f = "";
        foreach (var c in r.costo())
        {
            int falta = c.Value - inv.Cantidad(c.Key);
            if (falta > 0) f += (f == "" ? "" : ", ") + falta + " " + Corto(c.Key);
        }
        return f == "" ? "" : " (faltan " + f + ")";
    }

    // Cuántas veces alcanza lo que llevas para hacerla
    public int VecesQueAlcanza(Receta r)
    {
        if (ModoCreativo.Activo) return 99;
        int veces = int.MaxValue;
        foreach (var c in r.costo()) if (c.Value > 0) veces = Mathf.Min(veces, inv.Cantidad(c.Key) / c.Value);
        return veces == int.MaxValue ? 99 : veces;
    }

    public bool PuedePagar(Receta r)
    {
        if (ModoCreativo.Activo) return true;
        foreach (var c in r.costo()) if (inv.Cantidad(c.Key) < c.Value) return false;
        return true;
    }

    public Inventario Inv => inv;

    // Resumen del barco donde estás (para la cabecera de la categoría Barco)
    public string ResumenBarco()
    {
        Preparar();
        ControlBarco b = BarcoAbordo();
        if (b == null) return null;
        PiezasBarco pz = Piezas();
        Bodega bd = BodegaActual();
        Canones cn = b.GetComponent<Canones>();
        return b.name + "  ·  casco +" + (pz != null ? pz.nivelCasco : 0) + "  ·  velas +" + (pz != null ? pz.nivelVelas : 0)
             + "  ·  cañones " + (cn != null ? cn.Instalados + "/" + cn.Ranuras() : "—") + "  ·  bodega " + (bd != null ? bd.Total + "/" + bd.capacidad : "—");
    }

    // Nombre corto para que los costos quepan en una línea
    static string Corto(string tipo) => Objetos.Corto(tipo);

    // ---------- Tab: abre la fabricación, o el mercado si hay un mercader cerca ----------
    void Update()
    {
        Preparar();
        if (Taller.Abierto || Pausa.Visible || Pausa.Retenida || MenuPrincipal.Visible || Ventanas.HayModal) return;
        if (Teclas.Pulsada(Accion.Fabricar, true))
        {
            if (MenuAbierto) { Ventanas.Cerrar(VentanaFabricacion.I); Ventanas.Cerrar(VentanaComercio.I); }
            else if (Mercader.Cercano(transform.position) != null) Ventanas.Abrir(VentanaComercio.I);
            else Ventanas.Abrir(VentanaFabricacion.I);
        }
    }
}
