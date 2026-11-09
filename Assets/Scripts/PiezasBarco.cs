using UnityEngine;

// Piezas del barco: el poder del barco sale de sus piezas, no de un "nivel" propio.
// Por ahora: casco (más vida) y velas (más empuje). Sin tope de nivel: cada mejora cuesta más que la anterior.
[RequireComponent(typeof(ControlBarco), typeof(VidaBarco))]
public class PiezasBarco : MonoBehaviour
{
    public int nivelCasco;
    public int nivelVelas;

    [Tooltip("Vida extra por nivel de casco (fracción de la vida base).")]
    public float vidaPorNivel = 0.25f;
    [Tooltip("Empuje extra por nivel de velas (fracción del empuje base).")]
    public float empujePorNivel = 0.12f;

    public int NivelTotal => nivelCasco + nivelVelas;

    VidaBarco vida;
    ControlBarco cb;
    float vidaBase, empujeBase;
    bool listo;

    void Preparar()
    {
        if (listo) return;
        vida = GetComponent<VidaBarco>();
        cb = GetComponent<ControlBarco>();
        vidaBase = vida.vidaMaxima / (1f + vidaPorNivel * nivelCasco);
        empujeBase = cb.fuerzaPorNivel / (1f + empujePorNivel * nivelVelas);
        listo = true;
    }

    void Start() { if (!listo) { Preparar(); Aplicar(); } }

    // Costo de la siguiente mejora: crece 50 % por nivel
    public static int Costo(int baseCosto, int nivelActual) => Mathf.CeilToInt(Mathf.Min(baseCosto * Mathf.Pow(1.5f, Mathf.Min(nivelActual, 40)), 999999f));   // con tope: niveles muy altos (creativo) no se desbordan

    // Pone estos niveles en un barco recién hecho (al agrandar el barco se conservan las mejoras)
    public void Copiar(int casco, int velas)
    {
        nivelCasco = 0; nivelVelas = 0;
        listo = false;
        Preparar();
        nivelCasco = casco; nivelVelas = velas;
        Aplicar();
        vida.vida = vida.vidaMaxima;
    }

    public void MejorarCasco()
    {
        Preparar();
        nivelCasco++;
        Aplicar();
        vida.vida = Mathf.Min(vida.vidaMaxima, vida.vida + vidaBase * vidaPorNivel);
    }

    public void MejorarVelas()
    {
        Preparar();
        nivelVelas++;
        Aplicar();
    }

    void Aplicar()
    {
        vida.vidaMaxima = vidaBase * (1f + vidaPorNivel * nivelCasco);
        cb.fuerzaPorNivel = empujeBase * (1f + empujePorNivel * nivelVelas);
    }
}
