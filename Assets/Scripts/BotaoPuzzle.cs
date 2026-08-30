using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BotaoPuzzle : MonoBehaviour
{
    public Sprite spriteSolto;
    public Sprite spritePressionado;
    public SaidaPuzzle saida;

    private SpriteRenderer spriteRenderer;
    private int caixasEmCima;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        AtualizarVisual();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CaixaArrastavel>() == null)
        {
            return;
        }

        caixasEmCima++;
        AtualizarVisual();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<CaixaArrastavel>() == null)
        {
            return;
        }

        caixasEmCima = Mathf.Max(0, caixasEmCima - 1);
        AtualizarVisual();
    }

    private void AtualizarVisual()
    {
        bool pressionado = caixasEmCima > 0;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = pressionado && spritePressionado != null ? spritePressionado : spriteSolto;
        }

        if (saida != null)
        {
            saida.DefinirAberta(pressionado);
        }
    }
}
