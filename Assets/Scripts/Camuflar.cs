using UnityEngine;
using UnityEngine.InputSystem;

public class Camuflar : MonoBehaviour
{
    private bool camuflar = false;
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!camuflar)
        {
            animator.SetBool("taCamuflando", false);
            animator.SetBool("noTapete", false);
        }
    }

    public bool SeCamuflar()
    {
        return camuflar;
    }


    public void OnHide(InputAction.CallbackContext context) 
    {
        camuflar = context.ReadValueAsButton();
        animator.SetBool("taCamuflando", true);
        animator.SetBool("noTapete", true);
    }
}
