using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSeguirJogador : MonoBehaviour
{
    public Transform alvo;
    public Vector3 deslocamento = new Vector3(0f, 0f, -10f);
    public float suavizacao = 4f;

    private void Awake()
    {
        EncontrarAlvoSePreciso();
    }

    private void LateUpdate()
    {
        EncontrarAlvoSePreciso();

        if (alvo == null)
        {
            return;
        }

        Vector3 posicaoAlvo = alvo.position + deslocamento;
        transform.position = Vector3.Lerp(transform.position, posicaoAlvo, suavizacao * Time.deltaTime);
    }

    private void EncontrarAlvoSePreciso()
    {
        if (alvo != null)
        {
            return;
        }

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        alvo = jogador != null ? jogador.transform : null;
    }
}
