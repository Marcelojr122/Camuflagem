using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CaixaArrastavel : MonoBehaviour
{
    private const float IntervaloSomEmpurrando = 0.35f;
    private float proximoSomEmpurrando;

    private void Awake()
    {
        Rigidbody2D corpo = GetComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.linearDamping = 8f;
        corpo.angularDamping = 8f;

        Collider2D colisor = GetComponent<Collider2D>();
        colisor.isTrigger = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player") || Time.unscaledTime < proximoSomEmpurrando)
        {
            return;
        }

        Rigidbody2D corpo = GetComponent<Rigidbody2D>();

        if (corpo != null && corpo.linearVelocity.sqrMagnitude < 0.02f)
        {
            return;
        }

        proximoSomEmpurrando = Time.unscaledTime + IntervaloSomEmpurrando;
        GerenciadorTelasJogo.TocarPushBox();
    }
}
