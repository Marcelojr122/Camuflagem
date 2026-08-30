using UnityEngine;
using UnityEngine.InputSystem;

public class Camuflar : MonoBehaviour
{
    private static readonly int TaCamuflando = Animator.StringToHash("taCamuflando");
    private static readonly int NoTapete = Animator.StringToHash("noTapete");
    private static readonly int TrocandoCor = Animator.StringToHash("trocandoCor");

    private bool noTapete = false;
    private bool camuflado = false;
    private bool teclaCamuflagemPressionada = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Movimento movimento;
    private bool temParametroTaCamuflando;
    private bool temParametroNoTapete;
    private bool temParametroTrocandoCor;

    public Sprite spriteTrocaCor;

    public bool EstaNoTapete => noTapete;
    public bool EstaCamuflado => camuflado && noTapete;
    public bool TeclaCamuflagemPressionada => teclaCamuflagemPressionada;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movimento = GetComponent<Movimento>();

        temParametroTaCamuflando = TemParametro(animator, TaCamuflando);
        temParametroNoTapete = TemParametro(animator, NoTapete);
        temParametroTrocandoCor = TemParametro(animator, TrocandoCor);
    }

    private void LateUpdate()
    {
        if (PodeUsarCamuflagem() && noTapete)
        {
            camuflado = true;
        }

        AtualizarAnimator();

        if (PodeUsarCamuflagem() && spriteRenderer != null && spriteTrocaCor != null)
        {
            spriteRenderer.sprite = spriteTrocaCor;
        }
    }

    public bool SeCamuflar()
    {
        return EstaCamuflado;
    }

    public void DefinirNoTapete(bool valor)
    {
        noTapete = valor;

        if (!noTapete)
        {
            camuflado = false;
        }

        AtualizarAnimator();
    }

    public void OnHide(InputAction.CallbackContext context)
    {
        teclaCamuflagemPressionada = context.ReadValueAsButton();

        if (PodeUsarCamuflagem() && noTapete)
        {
            camuflado = true;
        }
        else if (!noTapete)
        {
            camuflado = false;
        }

        AtualizarAnimator();
    }

    private void AtualizarAnimator()
    {
        if (animator == null)
        {
            return;
        }

        if (temParametroTaCamuflando)
        {
            animator.SetBool(TaCamuflando, EstaCamuflado);
        }

        if (temParametroNoTapete)
        {
            animator.SetBool(NoTapete, noTapete);
        }

        if (temParametroTrocandoCor)
        {
            animator.SetBool(TrocandoCor, PodeUsarCamuflagem());
        }
    }

    private bool PodeUsarCamuflagem()
    {
        return teclaCamuflagemPressionada && (movimento == null || !movimento.EstaAndando);
    }

    private static bool TemParametro(Animator animatorParaChecar, int hash)
    {
        if (animatorParaChecar == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parametro in animatorParaChecar.parameters)
        {
            if (parametro.nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }
}
