using System.Collections.Generic;
using UnityEngine;

// Algo del mundo que se recoge o se golpea.
// A mano, con F: fibra, ramas (palos), piedras sueltas, frutas y cocos caídos.
// Con herramienta, con clic izquierdo: palmas (hacha, dan troncos de madera), rocas grandes y vetas de hierro (pico).
// Cada clic es un golpe; los grandes necesitan varios golpes. Se agotan y vuelven a crecer.
public class Recurso : MonoBehaviour
{
    public enum Tipo { Palma, RocaGrande, Fibra, Arbusto, Rama, Piedra, Coco, Hierro }
    public static readonly List<Recurso> Todos = new List<Recurso>();

    public Tipo tipo = Tipo.Fibra;
    [Tooltip("Cuántas veces se puede sacar algo antes de agotarse.")]
    public int usosMaximos = 3;
    [Tooltip("Golpes necesarios para sacar una tanda (una palma necesita varios hachazos).")]
    public int golpesPorUso = 1;
    [Tooltip("Segundos para recuperarse por completo después de agotarse.")]
    public float tiempoRegenerar = 150f;

    public int Usos { get; private set; }
    public int Golpes { get; private set; }
    public bool Agotado => Usos <= 0;

    public string HerramientaNecesaria =>
        tipo == Tipo.Palma ? Inventario.Hacha : (tipo == Tipo.RocaGrande || tipo == Tipo.Hierro) ? Inventario.Pico : null;

    // Se golpea con herramienta (clic). Si no, se recoge a mano (F).
    public bool ConHerramienta => HerramientaNecesaria != null;

    // Lo pequeño desaparece al agotarse; lo grande queda como tocón o roca pequeña
    bool EsPequeno => tipo == Tipo.Rama || tipo == Tipo.Piedra || tipo == Tipo.Coco;

    float tAgotado;
    Vector3 escalaOriginal;

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Start()
    {
        Usos = usosMaximos;
        escalaOriginal = transform.localScale;
    }

    // Solo los agotados necesitan actualizarse (volver a crecer). Antes cada recurso del mundo
    // tenía su propio Update (miles por frame); ahora un solo actualizador recorre los agotados.
    static readonly List<Recurso> regenerando = new List<Recurso>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CrearActualizador() { regenerando.Clear(); new GameObject("Recursos que crecen").AddComponent<Actualizador>(); }

    class Actualizador : MonoBehaviour
    {
        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = regenerando.Count - 1; i >= 0; i--)
            {
                Recurso r = regenerando[i];
                if (r == null || r.Crecer(dt)) regenerando.RemoveAt(i);
            }
        }
    }

    // Devuelve true cuando terminó de crecer
    bool Crecer(float dt)
    {
        tAgotado += dt;
        float k = Mathf.Clamp01(tAgotado / tiempoRegenerar);
        transform.localScale = Vector3.Lerp(escalaOriginal * (EsPequeno ? 0f : 0.25f), escalaOriginal, k * k);
        if (k < 1f) return false;
        Usos = usosMaximos; Golpes = 0; transform.localScale = escalaOriginal;
        return true;
    }

    public string Nombre
    {
        get
        {
            switch (tipo)
            {
                case Tipo.Palma: return "palma";
                case Tipo.RocaGrande: return "roca grande";
                case Tipo.Fibra: return "planta de fibra";
                case Tipo.Arbusto: return "arbusto de frutas";
                case Tipo.Rama: return "ramas";
                case Tipo.Piedra: return "piedra suelta";
                case Tipo.Hierro: return "veta de hierro";
                default: return "coco caído";
            }
        }
    }

    // Texto corto para la ayuda en pantalla
    public string Accion(Inventario inv)
    {
        if (Agotado) return "Agotado: " + Nombre + " (vuelve a crecer)";
        string h = HerramientaNecesaria;
        if (h != null && !(inv.Tiene(h) || ModoCreativo.Activo)) return Nombre + ": necesitas " + (h == Inventario.Hacha ? "un hacha" : "un pico");
        if (h != null) return "Clic: golpear " + Nombre + " con el " + h.ToLower() + (golpesPorUso > 1 ? " (" + Golpes + " / " + golpesPorUso + ")" : "");
        return "F: recoger " + Nombre;
    }

    // Distancia horizontal desde p hasta el borde del objeto (no hasta su centro)
    Collider col;
    bool colBuscado;

    public float Distancia(Vector3 p)
    {
        if (!colBuscado) { col = GetComponentInChildren<Collider>(); colBuscado = true; }
        Collider c = col;
        Vector3 q = c != null ? c.bounds.ClosestPoint(p) : transform.position;
        q.y = p.y;
        return Vector3.Distance(p, q);
    }

    // Un clic sobre el recurso. Devuelve el texto de lo que pasó.
    public string Golpear(Inventario inv)
    {
        if (Agotado) return "Agotado: " + Nombre + ". Vuelve a crecer en un rato";
        string h = HerramientaNecesaria;
        if (h == Inventario.Hacha && !(inv.Tiene(h) || ModoCreativo.Activo))
            return "No puedes sacar madera con las manos. Fabrica un hacha (Tab)";
        if (h == Inventario.Pico && !(inv.Tiene(h) || ModoCreativo.Activo))
            return "Necesitas un pico para picar esto. Fabrícalo (Tab)";

        Golpes++;
        if (Golpes < golpesPorUso) return "Golpe " + Golpes + " / " + golpesPorUso;
        Golpes = 0;
        Usos--;
        if (Usos <= 0) { tAgotado = 0f; if (!regenerando.Contains(this)) regenerando.Add(this); }

        string texto;
        switch (tipo)
        {
            case Tipo.Palma:
                int cocos = Random.Range(0, 3);
                inv.Agregar(Inventario.Madera, 4);
                if (cocos > 0) inv.Agregar(Inventario.Coco, cocos);
                texto = "¡Palma talada! +4 madera" + (cocos > 0 ? ", +" + cocos + " coco" + (cocos > 1 ? "s" : "") : "");
                break;
            case Tipo.RocaGrande:
                inv.Agregar(Inventario.Piedra, 2);
                texto = "+2 piedra";
                break;
            case Tipo.Fibra:
                inv.Agregar(Inventario.Fibra, 1);
                texto = "+1 fibra";
                break;
            case Tipo.Arbusto:
                inv.Agregar(Inventario.Fruta, 1);
                texto = "+1 fruta";
                break;
            case Tipo.Rama:
                inv.Agregar(Inventario.Palo, 2);
                texto = "+2 palos";
                break;
            case Tipo.Piedra:
                inv.Agregar(Inventario.Piedra, 1);
                texto = "+1 piedra";
                break;
            case Tipo.Hierro:
                inv.Agregar(Inventario.Mineral, 2);
                texto = "+2 mineral de hierro";
                break;
            default:
                inv.Agregar(Inventario.Coco, 1);
                texto = "+1 coco";
                break;
        }
        if (Agotado) { tAgotado = 0f; transform.localScale = escalaOriginal * (EsPequeno ? 0f : 0.25f); }
        return texto;
    }
}
