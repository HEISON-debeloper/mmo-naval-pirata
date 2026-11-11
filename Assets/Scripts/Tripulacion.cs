using UnityEngine;

// Tu tripulación: marineros que contratas con el mercader (Mercado → pestaña Tripulación).
// Van contigo en el barco propio que tengas, y lo que hacen depende de cuántos son y de su moral:
//   - Recargan los cañones más rápido: hasta un 30 % con un marinero por cada 2 cañones y buena moral.
//   - Manejan mejor las velas: hasta un 10 % más de empuje con el barco lleno de gente.
//   - Pelean contra los piratas que te abordan (y a veces cae alguno).
// A cambio cobran 2 de oro por cabeza cada día del juego y comen: 1 coco o fruta por cada 2 marineros
// cada 6 horas (primero de la bodega, después de tu mochila). Sin paga o sin comida baja la moral;
// con la moral por el suelo, al llegar a un puerto con mercader se van de a uno.
// Si tu barco se hunde, se pierde la mitad. Si cambias a un barco más chico, los que no caben se quedan en tierra.
public class Tripulacion : MonoBehaviour
{
    public const int Sueldo = 2;            // oro por marinero y por día
    public const int PrecioContrato = 30;   // oro por contratar uno
    const float SegundosPorHora = 150f;     // un día del juego = 1 hora real (como el ciclo de día)

    public int marineros;
    public float moral = 70f;

    float horasComida, horasPaga, tPelea, tBajas, tDesercion;
    bool hundidoAntes;
    Personaje p;
    Inventario inv;

    static Tripulacion mia;
    public static Tripulacion Mia
    {
        get
        {
            Personaje pj = Personaje.Instancia;
            if (pj == null) return null;
            if (mia != null && mia.p == pj) return mia;
            mia = pj.GetComponent<Tripulacion>();
            if (mia == null) mia = pj.gameObject.AddComponent<Tripulacion>();
            return mia;
        }
    }

    void Awake()
    {
        p = GetComponent<Personaje>();
        inv = GetComponent<Inventario>();
    }

    ControlBarco Barco => p != null ? p.barco : null;

    // Cuántos caben: 2 más uno por cada 2 ranuras de cañón (la balsa, uno)
    public int Maximo
    {
        get
        {
            ControlBarco b = Barco;
            if (b == null) return 0;
            if (Cubierta.EsPlana(b)) return 1;
            Canones c = b.GetComponent<Canones>();
            return Mathf.Clamp(2 + (c != null ? c.Ranuras() : 0) / 2, 2, 40);
        }
    }

    // Los que hacen falta para tener todos los cañones atendidos (uno por cada 2)
    public int Necesarios
    {
        get
        {
            Canones c = Barco != null ? Barco.GetComponent<Canones>() : null;
            return Mathf.Max(1, Mathf.CeilToInt((c != null ? c.Instalados : 0) / 2f));
        }
    }

    float Animo => Mathf.Clamp01(moral / 70f);   // de 70 para arriba rinden a pleno
    public float FactorRecarga => 1f - 0.3f * Mathf.Clamp01(marineros / (float)Necesarios) * Animo;
    public float FactorVelas => 1f + 0.1f * Mathf.Clamp01(marineros / (float)Mathf.Max(1, Maximo)) * Animo;

    // Para los cañones y las velas: el factor si ese barco es el tuyo, 1 si no
    public static float Recarga(GameObject barco)
    {
        Personaje pj = Personaje.Instancia;
        if (pj == null || pj.barco == null || pj.barco.gameObject != barco) return 1f;
        Tripulacion t = Mia;
        return t != null && t.marineros > 0 ? t.FactorRecarga : 1f;
    }
    public static float Velas(GameObject barco)
    {
        Personaje pj = Personaje.Instancia;
        if (pj == null || pj.barco == null || pj.barco.gameObject != barco) return 1f;
        Tripulacion t = Mia;
        return t != null && t.marineros > 0 ? t.FactorVelas : 1f;
    }

    public string Estado =>
        marineros == 0 ? "Sin tripulación" :
        marineros + " / " + Maximo + " marineros · moral " + Mathf.RoundToInt(moral) + "% (" + Humor + ")";

    public string Humor => moral >= 75f ? "contentos" : moral >= 45f ? "tranquilos" : moral >= 20f ? "descontentos" : "a punto de irse";

    // ---------- Contratar y despedir ----------
    public string PuedeContratar()
    {
        if (Barco == null) return "Necesitas un barco propio para llevar tripulación";
        if (marineros >= Maximo) return "Tu barco no tiene lugar para más (" + Maximo + ")";
        if (!ModoCreativo.Activo && inv.Cantidad(Inventario.Oro) < PrecioContrato) return "Te falta oro: contratar cuesta " + PrecioContrato;
        return null;
    }

    public bool Contratar()
    {
        if (PuedeContratar() != null) return false;
        if (!ModoCreativo.Activo) inv.Quitar(Inventario.Oro, PrecioContrato);
        marineros++;
        moral = Mathf.Min(100f, moral + 2f);   // gente nueva con ganas
        return true;
    }

    public bool Despedir()
    {
        if (marineros <= 0) return false;
        marineros--;
        return true;
    }

    // Al cargar la partida
    public void Fijar(int n, float m) { marineros = Mathf.Max(0, n); moral = m >= 0f ? Mathf.Clamp(m, 0f, 100f) : 70f; }

    // ---------- Vida a bordo ----------
    void Update()
    {
        if (p == null || inv == null || MenuPrincipal.Visible || marineros <= 0) { hundidoAntes = false; return; }
        ControlBarco b = Barco;
        float dt = Time.deltaTime;

        // Barco hundido: se pierde la mitad (los demás nadan con el capitán)
        VidaBarco vb = b != null ? b.GetComponent<VidaBarco>() : null;
        bool hundido = vb != null && vb.Hundido;
        if (hundido && !hundidoAntes)
        {
            int perdidos = Mathf.CeilToInt(marineros / 2f);
            marineros -= perdidos;
            moral = Mathf.Max(0f, moral - 20f);
            p.Avisar("Se ahogaron " + perdidos + " marinero" + (perdidos > 1 ? "s" : "") + " con el barco");
        }
        hundidoAntes = hundido;

        // En un barco más chico no caben todos
        int max = Maximo;
        if (b != null && marineros > max)
        {
            int sobran = marineros - max;
            marineros = max;
            p.Avisar(sobran + " marinero" + (sobran > 1 ? "s se quedaron" : " se quedó") + " en tierra: no caben en este barco");
        }
        if (marineros <= 0) return;

        // Comida cada 6 horas y paga cada día
        float horas = dt / SegundosPorHora;
        horasComida += horas; horasPaga += horas;
        if (horasComida >= 6f) { horasComida -= 6f; Comer(); }
        if (horasPaga >= 24f) { horasPaga -= 24f; Pagar(); }

        Pelear(b, dt);

        // Con la moral por el suelo, en un puerto se van de a uno
        if (marineros > 0 && moral < 20f && Mercader.Cercano(p.transform.position) != null)
        {
            tDesercion -= dt;
            if (tDesercion <= 0f)
            {
                tDesercion = 30f;
                marineros--;
                p.Avisar("Un marinero desertó: la tripulación está " + Humor + ". Págales y dales de comer");
            }
        }
    }

    void Comer()
    {
        int hace = Mathf.CeilToInt(marineros / 2f), comieron = 0;
        Bodega bd = Barco != null ? Barco.GetComponent<Bodega>() : null;
        for (int i = 0; i < hace; i++)
        {
            if (bd != null && (bd.Consumir(Inventario.Coco) || bd.Consumir(Inventario.Fruta))) { comieron++; continue; }
            if (inv.Quitar(Inventario.Coco, 1) || inv.Quitar(Inventario.Fruta, 1)) { comieron++; continue; }
        }
        if (Pactos.Tiene("oro")) moral = Mathf.Max(0f, moral - 6f);   // el Oro Maldito los pone nerviosos
        if (comieron >= hace) moral = Mathf.Min(100f, moral + 4f);
        else
        {
            moral = Mathf.Max(0f, moral - (comieron == 0 ? 12f : 6f));
            p.Avisar("La tripulación pasa hambre: lleva cocos o fruta en la bodega");
        }
    }

    void Pagar()
    {
        int cuesta = Sueldo * marineros;
        if (ModoCreativo.Activo || inv.Quitar(Inventario.Oro, cuesta))
        {
            moral = Mathf.Min(100f, moral + 5f);
            Notificaciones.Mostrar("Pagaste a la tripulación: −" + cuesta + " oro", Notificaciones.Tipo.Comercio, 4f, "moneda");
        }
        else
        {
            moral = Mathf.Max(0f, moral - 15f);
            p.Avisar("No alcanzó el oro para pagar a la tripulación (" + cuesta + "). La moral baja");
        }
    }

    // Los marineros pelean con los piratas que suben a tu cubierta
    void Pelear(ControlBarco b, float dt)
    {
        if (b == null) return;
        tPelea -= dt; tBajas -= dt;
        int enemigos = 0;
        foreach (Tripulante t in Tripulante.Vivos) if (t != null && !t.Muerto && t.Barco == b) enemigos++;
        if (enemigos == 0) return;

        if (tPelea <= 0f)
        {
            tPelea = 1.5f;
            int luchan = Mathf.Min(marineros, 6);
            for (int i = 0; i < luchan; i++)
            {
                Tripulante blanco = AlAzar(b);
                if (blanco == null) break;
                blanco.Dano(10f * (0.5f + 0.5f * Animo));
                SonidosMundo.Uno("espada_golpe", blanco.transform.position, 0.6f, Random.Range(0.9f, 1.1f), 25f, 0.15f);
            }
        }
        if (tBajas <= 0f)
        {
            tBajas = 3f;
            for (int i = 0; i < enemigos && marineros > 0; i++)
            {
                if (Random.value >= 0.15f) continue;
                marineros--;
                moral = Mathf.Max(0f, moral - 5f);
                p.Avisar("Cayó un marinero defendiendo el barco (quedan " + marineros + ")");
            }
        }
    }

    static Tripulante AlAzar(ControlBarco b)
    {
        int n = 0;
        Tripulante elegido = null;
        foreach (Tripulante t in Tripulante.Vivos)
        {
            if (t == null || t.Muerto || t.Barco != b) continue;
            n++;
            if (Random.Range(0, n) == 0) elegido = t;   // uno al azar sin armar una lista
        }
        return elegido;
    }
}
