using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movimento : MonoBehaviour
{
    private Rigidbody2D corpo;
    private Vector2 movimento;

    private Animator animator;

    public float movimentoSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        corpo = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        corpo.MovePosition( corpo.position + movimento * movimentoSpeed * Time.fixedDeltaTime);
        //animator.SetBool("direita", false);
        //animator.SetBool("esquerda", false);
        //animator.SetBool("atras", false);
        //animator.SetBool("frente", false);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movimento = context.ReadValue<Vector2>();

        if(movimento.x > 0)
        {
            AnimDir();
        }
        else
        {
            animator.SetBool("direita", false);
        }

        if (movimento.x < 0)
        {
            AnimEsq();
        }
        else
        {
            animator.SetBool("esquerda", false);
        }

        if (movimento.y > 0)
        {
            AnimFrente();
        }
        else
        {
            animator.SetBool("frente", false);
        }

        if (movimento.y < 0)
        {
            AnimTras();
        }
        else
        {
            animator.SetBool("atras", false);
        }

    }

    public void AnimDir()
    {
        animator.SetBool("direita", true);
    }
    public void AnimEsq()
    {
        animator.SetBool("esquerda", true);
    }

    public void AnimTras()
    {
        animator.SetBool("atras", true);
    }

    public void AnimFrente()
    {
        animator.SetBool("frente", true);
    }
}
