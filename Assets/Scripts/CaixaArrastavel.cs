using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CaixaArrastavel : MonoBehaviour
{
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
}
