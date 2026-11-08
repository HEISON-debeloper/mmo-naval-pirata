using System.Collections.Generic;
using UnityEngine;

// Mundo infinito: el mar está dividido en celdas cuadradas. Al navegar, se crean las islas
// de las celdas cercanas y se quitan las lejanas. Cada celda sale siempre igual (misma semilla),
// así que si vuelves a un lugar, sus islas siguen ahí.
// Las islas de la escena (la inicial, la grande y la pequeña) se quedan; las nuevas nunca se les montan encima.
public class MundoInfinito : MonoBehaviour
{
    public static MundoInfinito Instancia { get; private set; }

    public GeneradorIsla.Materiales materiales;

    [Header("Barcos enemigos")]
    public Astillero.Materiales materialesEnemigo;
    public Material maderaBalsa, lonaBalsa, tripulante, espada;
    [Tooltip("Cerca del inicio no aparecen enemigos (metros).")]
    public float zonaSegura = 450f;
    [Tooltip("Cada tantos metros desde el inicio, los enemigos pueden ser un molde más grande.")]
    public float metrosPorMolde = 500f;

    [Tooltip("Los enemigos más lejos que esto se duermen para que el juego no se ponga lento (metros).")]
    public float distanciaDespierto = 900f;
    float tDormir;

    public int EnemigosCreados { get; private set; }
    int zonaAnterior = -1;
    [Tooltip("Semilla del mundo: cambia todas las islas.")]
    public int semilla = 1717;
    [Tooltip("Lado de cada celda en metros.")]
    public float tamanoCelda = 600f;
    [Tooltip("Celdas alrededor del personaje que se mantienen creadas.")]
    public int radioCeldas = 2;

    readonly Dictionary<Vector2Int, List<GameObject>> celdas = new Dictionary<Vector2Int, List<GameObject>>();
    readonly List<Vector2Int> pendientes = new List<Vector2Int>();
    Vector2Int celdaActual = new Vector2Int(int.MinValue, int.MinValue);

    public int IslasCreadas { get; private set; }

    void Awake() => Instancia = this;

    public Vector2Int Celda(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / tamanoCelda), Mathf.FloorToInt(p.z / tamanoCelda));

    void Update()
    {
        Personaje p = Personaje.Instancia;
        if (p == null) return;
        Vector3 foco = p.transform.position;
        Vector2Int c = Celda(foco);

        if (c != celdaActual)
        {
            celdaActual = c;
            // Pedir las celdas cercanas que falten, las más cercanas primero
            pendientes.Clear();
            for (int dx = -radioCeldas; dx <= radioCeldas; dx++)
            for (int dz = -radioCeldas; dz <= radioCeldas; dz++)
            {
                Vector2Int k = new Vector2Int(c.x + dx, c.y + dz);
                if (!celdas.ContainsKey(k)) pendientes.Add(k);
            }
            pendientes.Sort((a, b) => (a - c).sqrMagnitude.CompareTo((b - c).sqrMagnitude));

            // Quitar las que quedaron lejos (una celda de margen para no crear y borrar en la frontera)
            var quitar = new List<Vector2Int>();
            foreach (var kv in celdas)
                if (Mathf.Abs(kv.Key.x - c.x) > radioCeldas + 1 || Mathf.Abs(kv.Key.y - c.y) > radioCeldas + 1) quitar.Add(kv.Key);
            foreach (Vector2Int k in quitar)
            {
                foreach (GameObject g in celdas[k])
                {
                    if (g == null) continue;
                    // Un barco en el que vas (o que capturaste) no desaparece
                    ControlBarco cb = g.GetComponent<ControlBarco>();
                    if (cb != null && (p.barco == cb || p.BarcoActual == cb)) { g.transform.SetParent(null, true); continue; }
                    Destroy(g);
                }
                celdas.Remove(k);
            }
        }

        // Aviso al cambiar de zona de peligro (cada 800 m desde el inicio)
        int zona = Mathf.FloorToInt(new Vector2(foco.x, foco.z).magnitude / 800f);
        if (zonaAnterior >= 0 && zona != zonaAnterior)
            p.Avisar(zona > zonaAnterior ? "Aguas más peligrosas (zona " + zona + "): barcos más grandes y mejor armados"
                                         : "Aguas más tranquilas (zona " + zona + ")");
        zonaAnterior = zona;

        // Los barcos enemigos lejanos se duermen (sin física ni IA) y despiertan al acercarte
        tDormir -= Time.deltaTime;
        if (tDormir <= 0f)
        {
            tDormir = 0.5f;
            foreach (var kv in celdas)
                foreach (GameObject g in kv.Value)
                {
                    if (g == null || g.GetComponent<Isla>() != null) continue;
                    Vector3 d = g.transform.position - foco;
                    float d2 = d.x * d.x + d.z * d.z;
                    if (g.activeSelf && d2 > distanciaDespierto * distanciaDespierto * 1.2f) g.SetActive(false);
                    else if (!g.activeSelf && d2 < distanciaDespierto * distanciaDespierto) g.SetActive(true);
                }
        }

        // Una celda por frame, para que no se trabe
        if (pendientes.Count > 0)
        {
            Vector2Int k = pendientes[0];
            pendientes.RemoveAt(0);
            if (!celdas.ContainsKey(k)) Generar(k);
        }
    }

    int Hash(Vector2Int c, int extra = 0)
    {
        unchecked
        {
            int h = semilla;
            h = h * 486187739 + c.x * 73856093;
            h = h * 486187739 + c.y * 19349663;
            h = h * 486187739 + extra * 83492791;
            return h;
        }
    }

    void Generar(Vector2Int c)
    {
        var lista = new List<GameObject>();
        celdas[c] = lista;
        var rnd = new System.Random(Hash(c));

        // 0, 1 o 2 islas por celda
        double tirada = rnd.NextDouble();
        int n = tirada < 0.3 ? 0 : tirada < 0.82 ? 1 : 2;
        var hechas = new List<(Vector3 pos, float orilla)>();
        for (int i = 0; i < n; i++)
        {
            double t = rnd.NextDouble();
            float r = 22f + (float)(t * t) * 70f;            // muchas pequeñas, pocas grandes
            float margen = r * 1.2f + 60f;                    // así nunca se pisan islas de celdas vecinas
            if (margen * 2f >= tamanoCelda) continue;
            float x = c.x * tamanoCelda + margen + (float)rnd.NextDouble() * (tamanoCelda - 2f * margen);
            float z = c.y * tamanoCelda + margen + (float)rnd.NextDouble() * (tamanoCelda - 2f * margen);
            Vector3 pos = new Vector3(x, 0f, z);
            int semillaIsla = Hash(c, i + 1);
            string nombre = Nombre(new System.Random(semillaIsla));

            if (Choca(pos, r, hechas)) continue;
            hechas.Add((pos, r * 1.2f));

            Transform isla = GeneradorIsla.Crear(nombre, pos, r, semillaIsla, false, materiales);
            Isla comp = isla.GetComponent<Isla>();
            comp.id = c.x + "," + c.y + "," + i;
            comp.generada = true;
            isla.SetParent(transform, true);
            lista.Add(isla.gameObject);
            IslasCreadas++;
        }

        // Barco enemigo: más probable y más grande mientras más lejos del inicio
        Vector2 centroCelda = new Vector2((c.x + 0.5f) * tamanoCelda, (c.y + 0.5f) * tamanoCelda);
        float dist = centroCelda.magnitude;
        if (dist > zonaSegura)
        {
            float prob = Mathf.Clamp(0.12f + dist / 8000f, 0.12f, 0.45f);   // menos barcos = menos carga
            if (rnd.NextDouble() < prob)
            {
                GameObject e = CrearEnemigo(c, rnd, dist, hechas);
                if (e != null) lista.Add(e);
            }
        }
    }

    // Un barco enemigo con la misma lógica que el del jugador: molde, alargado, ancho, pisos y cañones instalados
    GameObject CrearEnemigo(Vector2Int c, System.Random rnd, float dist, List<(Vector3 pos, float orilla)> hechas)
    {
        int moldeMax = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, dist - zonaSegura) / metrosPorMolde), 0, FormaBarco.UltimoMolde);
        int molde = Mathf.Clamp(moldeMax - rnd.Next(0, 3), 0, FormaBarco.UltimoMolde);   // justo afuera de la zona segura salen balsas; luego botes y chalupas
        int extra = Mathf.Min(FormaBarco.MaxPasos, Mathf.FloorToInt(dist / 1500f));
        int a = rnd.Next(0, extra + 1), b = rnd.Next(0, extra + 1), cp = rnd.Next(0, extra / 2 + 1);
        FormaBarco.Medidas(molde, a, b, cp, out float L, out _, out float h);

        // Buscar agua libre dentro de la celda (lejos de islas y de ti)
        Personaje p = Personaje.Instancia;
        for (int intento = 0; intento < 8; intento++)
        {
            float margen = L + 20f;
            if (margen * 2f >= tamanoCelda) return null;
            Vector3 pos = new Vector3(c.x * tamanoCelda + margen + (float)rnd.NextDouble() * (tamanoCelda - 2f * margen), 0f,
                                      c.y * tamanoCelda + margen + (float)rnd.NextDouble() * (tamanoCelda - 2f * margen));
            if (p != null && Vector3.Distance(pos, p.transform.position) < 180f) continue;
            bool libre = true;
            foreach (Isla s in Isla.Todas)
            {
                float d = Vector2.Distance(new Vector2(s.cx, s.cz), new Vector2(pos.x, pos.z));
                if (d < s.orilla + L + 40f) { libre = false; break; }
            }
            foreach (var hc in hechas) if (Vector3.Distance(hc.pos, pos) < hc.orilla + L + 40f) libre = false;
            if (!libre) continue;

            pos.y = Olas.Altura(pos) + h / 2f - (FormaBarco.De(molde).balsa ? 0.3f : 1.2f);
            float rumbo = (float)rnd.NextDouble() * 360f;
            GameObject g = FormaBarco.Construir(molde, a, b, cp, pos, rumbo, materialesEnemigo, maderaBalsa, lonaBalsa);
            g.name = FormaBarco.Nombre(molde, a, b, cp) + " pirata";

            // Cañones: entre la mitad y todas las ranuras, y a veces los de proa y popa
            Canones cn = g.GetComponent<Canones>();
            float f = 0.5f + (float)rnd.NextDouble() * 0.5f;
            cn.controlJugador = false;
            cn.recarga = 3.6f;
            cn.Fijar(Mathf.Max(1, Mathf.RoundToInt(cn.RanurasLado(LadoCanon.Babor) * f)),
                     Mathf.Max(1, Mathf.RoundToInt(cn.RanurasLado(LadoCanon.Estribor) * f)),
                     rnd.NextDouble() < 0.5 ? 1 : 0, rnd.NextDouble() < 0.3 ? 1 : 0);

            IABarco ia = g.AddComponent<IABarco>();
            ia.tripulacion = Mathf.Clamp(2 + molde / 2 + cp, 2, 12);
            ia.materialTripulante = tripulante;
            ia.materialEspada = espada;
            ia.patrullaPropia = true;
            ia.casa = pos;
            ia.radioPatrulla = 220f;
            ia.distanciaAlerta = 260f + L * 2f;
            ia.distanciaAtaque = Mathf.Max(170f, L * 3f);

            g.transform.SetParent(transform, true);
            EnemigosCreados++;
            return g;
        }
        return null;
    }

    // No encima de las islas de la escena ni de otra de la misma celda
    static bool Choca(Vector3 pos, float r, List<(Vector3 pos, float orilla)> hechas)
    {
        float orilla = r * 1.2f;
        foreach (Isla s in Isla.Todas)
        {
            if (s == null || s.generada) continue;
            float dx = s.cx - pos.x, dz = s.cz - pos.z, min = s.orilla + orilla + 120f;
            if (dx * dx + dz * dz < min * min) return true;
        }
        foreach (var h in hechas)
            if (Vector3.Distance(h.pos, pos) < h.orilla + orilla + 60f) return true;
        return false;
    }

    // Nombres de pirata para las islas nuevas (el que la descubre podrá cambiarlo más adelante)
    static readonly string[] Tipos = { "Isla", "Isla", "Cayo", "Islote", "Peñón", "Atolón" };
    static readonly string[] Apellidos =
    {
        "de la Calavera", "del Tiburón", "de los Ahorcados", "Esmeralda", "del Ron", "de la Niebla", "del Coral",
        "Perdida", "del Kraken", "de las Gaviotas", "del Ahogado", "de la Sirena", "del Tesoro", "Maldita",
        "de los Huesos", "del Cangrejo", "de la Tortuga", "del Loro", "Sombría", "de la Bruma", "del Cuervo",
        "de Plata", "Negra", "del Diablo", "de la Luna", "del Náufrago", "de las Perlas", "del Ancla", "Roja", "del Fuego"
    };

    static string Nombre(System.Random r)
    {
        string tipo = Tipos[r.Next(Tipos.Length)];
        string ap = Apellidos[r.Next(Apellidos.Length)];
        // Concordancia: "Cayo Perdido", "Islote Negro", "Atolón Maldito"...
        bool masculino = tipo != "Isla";
        if (masculino)
        {
            if (ap == "Perdida") ap = "Perdido";
            else if (ap == "Maldita") ap = "Maldito";
            else if (ap == "Sombría") ap = "Sombrío";
            else if (ap == "Negra") ap = "Negro";
            else if (ap == "Roja") ap = "Rojo";
        }
        // Un apellido de lugar para que no se repitan tanto: "Isla del Ahogado del Norte", "Cayo Negro Mayor"...
        string[] lugar = { "", "", "", " Mayor", " Menor", " del Norte", " del Sur", " del Este", " del Oeste", masculino ? " Viejo" : " Vieja", " Gemela", " Escondida" };
        string l = lugar[r.Next(lugar.Length)];
        if (masculino && l == " Gemela") l = " Gemelo";
        if (masculino && l == " Escondida") l = " Escondido";
        return tipo + " " + ap + l;
    }
}
