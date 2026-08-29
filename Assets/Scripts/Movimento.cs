using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movimento : MonoBehaviour
{
    private Rigidbody2D corpo;
    private Vector2 movimento;

    public float movimentoSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        corpo = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        corpo.MovePosition( corpo.position + movimento * movimentoSpeed * Time.fixedDeltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movimento = context.ReadValue<Vector2>();
    }
}
