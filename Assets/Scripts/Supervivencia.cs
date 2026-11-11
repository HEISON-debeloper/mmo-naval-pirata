using UnityEngine;

// Hambre y sed del personaje. Al llegar a 0 empiezas a perder vida.
// En el mar no hay comida: hay que llevarla desde la isla en el inventario.
[RequireComponent(typeof(Personaje))]
public class Supervivencia : MonoBehaviour
{
    [Tooltip("Minutos para pasar de lleno a muerto de hambre.")]
    public float minutosHambre = 15f;
    [Tooltip("Minutos para pasar de hidratado a muerto de sed.")]
    public float minutosSed = 10f;
    [Tooltip("Vida que pierdes por segundo con hambre o sed en 0.")]
    public float danoPorSegundo = 1f;

    public float Hambre { get; private set; } = 100f;
    public float Sed { get; private set; } = 100f;

    Personaje p;
    float acumulado;

    void Awake() => p = GetComponent<Personaje>();

    void Update()
    {
        if (ModoCreativo.Activo) { Hambre = 100f; Sed = 100f; return; }
        if (MenuPrincipal.Visible) return;   // con el menú principal abierto no da hambre ni sed
        float dt = Time.deltaTime;
        Hambre = Mathf.Max(0f, Hambre - 100f / (minutosHambre * 60f) * dt * Pactos.Hambre);   // pactos: Oro Maldito
        Sed = Mathf.Max(0f, Sed - 100f / (minutosSed * 60f) * dt * Pactos.Sed);              // pactos: Sed del Kraken

        if (Hambre <= 0f || Sed <= 0f)
        {
            acumulado += danoPorSegundo * dt;
            if (acumulado >= 5f) { p.RecibirDano(acumulado); acumulado = 0f; }
        }
    }

    public void Comer(float hambre, float sed)
    {
        Hambre = Mathf.Min(100f, Hambre + hambre);
        Sed = Mathf.Min(100f, Sed + sed);
    }

    public void Reiniciar() { Hambre = 100f; Sed = 100f; }
    public void Fijar(float hambre, float sed) { Hambre = Mathf.Clamp(hambre, 0f, 100f); Sed = Mathf.Clamp(sed, 0f, 100f); }
}
