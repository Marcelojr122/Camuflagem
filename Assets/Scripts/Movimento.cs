using UnityEngine;
using UnityEngine.InputSystem;

public class Movimento : MonoBehaviour
{
    private static readonly int Andando = Animator.StringToHash("andando");
    private static readonly int Direcao = Animator.StringToHash("direcao");

    private Rigidbody2D corpo;
    private Vector2 movimento;
    private Animator animator;
    private int ultimaDirecao = 0;

    public float movimentoSpeed = 5f;
    public bool EstaAndando => movimento.sqrMagnitude > 0.01f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        corpo = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (corpo == null)
        {
            return;
        }

        corpo.MovePosition(corpo.position + movimento * movimentoSpeed * Time.fixedDeltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movimento = context.ReadValue<Vector2>();
        AtualizarAnimacao();
    }

    private void AtualizarAnimacao()
    {
        if (animator == null)
        {
            return;
        }

        bool estaMovendo = EstaAndando;

        if (estaMovendo)
        {
            ultimaDirecao = ObterDirecao(movimento);
        }

        animator.SetBool(Andando, estaMovendo);
        animator.SetInteger(Direcao, ultimaDirecao);
    }

    private static int ObterDirecao(Vector2 direcao)
    {
        if (Mathf.Abs(direcao.x) > Mathf.Abs(direcao.y))
        {
            return direcao.x > 0f ? 3 : 2;
        }

        return direcao.y > 0f ? 1 : 0;
    }
}
