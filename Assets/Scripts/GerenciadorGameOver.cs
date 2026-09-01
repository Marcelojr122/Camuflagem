using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorGameOver : MonoBehaviour
{
    private static GerenciadorGameOver instancia;

    public static bool EmGameOver { get; private set; }

    private void Awake()
    {
        instancia = this;
        EmGameOver = false;

        if (!GerenciadorTelasJogo.TelaBloqueanteAtiva)
        {
            Time.timeScale = 1f;
        }
    }

    public static void GameOver()
    {
        if (EmGameOver)
        {
            return;
        }

        if (instancia == null)
        {
            new GameObject("GerenciadorGameOver").AddComponent<GerenciadorGameOver>();
        }

        EmGameOver = true;
        Time.timeScale = 0f;

        if (instancia != null)
        {
            GerenciadorTelasJogo.MostrarGameOver(instancia.ReiniciarFase);
        }
    }

    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        EmGameOver = false;
        GerenciadorTelasJogo.OcultarTelas();
        GerenciadorTelasJogo.RetomarMusicaJogo();
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }
}
