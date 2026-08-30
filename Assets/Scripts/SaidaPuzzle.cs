using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SaidaPuzzle : MonoBehaviour
{
    public Color corFechada = new Color(0.15f, 0.08f, 0.04f, 1f);
    public Color corAberta = new Color(0.25f, 0.85f, 0.45f, 0.75f);

    private Collider2D colisor;
    private SpriteRenderer spriteRenderer;

    public bool EstaAberta { get; private set; }

    private void Awake()
    {
        colisor = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        AtualizarEstado();
    }

    public void DefinirAberta(bool aberta)
    {
        EstaAberta = aberta;
        AtualizarEstado();
    }

    private void AtualizarEstado()
    {
        if (colisor == null)
        {
            colisor = GetComponent<Collider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (colisor != null)
        {
            colisor.isTrigger = EstaAberta;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = EstaAberta ? corAberta : corFechada;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EstaAberta && collision.CompareTag("Player"))
        {
            Debug.Log("Saida aberta alcancada.");
        }
    }
}
