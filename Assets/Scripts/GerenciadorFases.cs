using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GerenciadorFases : MonoBehaviour
{
    private const int TotalTutoriais = 3;
    private const int TotalFasesPrincipais = 15;
    private const int TotalFases = TotalTutoriais + TotalFasesPrincipais;

    private static GerenciadorFases instancia;
    private static int indiceFaseAtual;

    private ProceduralMap mapaAtual;
    private bool mostrandoVitoria;
    private string tituloFase;
    private string instrucaoFase;

    public static void ConcluirFaseAtual()
    {
        if (instancia != null)
        {
            instancia.ConcluirFase();
        }
    }

    private void Awake()
    {
        instancia = this;
        GerenciadorTelasJogo.GarantirInstancia();

        if (!GerenciadorTelasJogo.TelaBloqueanteAtiva)
        {
            Time.timeScale = 1f;
        }
    }

    public void IniciarFase(ProceduralMap mapa)
    {
        mapaAtual = mapa;

        if (indiceFaseAtual >= TotalFases)
        {
            MostrarVitoria();
            return;
        }

        mostrandoVitoria = false;
        AplicarFaseNoMapa();
        mapaAtual.GenerateMap();
    }

    private void AplicarFaseNoMapa()
    {
        if (mapaAtual == null)
        {
            return;
        }

        if (indiceFaseAtual < TotalTutoriais)
        {
            int tutorial = indiceFaseAtual + 1;
            mapaAtual.ConfigurarTutorialDoJogo(tutorial);
            tituloFase = $"Tutorial {tutorial}/3";

            switch (tutorial)
            {
                case 1:
                    instrucaoFase = "Pare em cima do tapete e segure C para mudar de cor. Solte C ainda no tapete para continuar camuflado.";
                    break;

                case 2:
                    instrucaoFase = "Empurre a caixa ate o botao para abrir a saida.";
                    break;

                default:
                    instrucaoFase = "Use o tapete para se camuflar. O inimigo so persegue se voce estiver no campo de visao sem a cor do tapete.";
                    break;
            }

            return;
        }

        int fasePrincipal = indiceFaseAtual - TotalTutoriais + 1;
        mapaAtual.ConfigurarFasePrincipalDoJogo(fasePrincipal);
        tituloFase = $"Fase {fasePrincipal}/15";
        instrucaoFase = "Chegue na saida. Use tapetes, caixas, botoes e camuflagem para evitar os inimigos.";
    }

    private void ConcluirFase()
    {
        if (mostrandoVitoria || GerenciadorGameOver.EmGameOver)
        {
            return;
        }

        indiceFaseAtual++;

        if (indiceFaseAtual >= TotalFases)
        {
            MostrarVitoria();
            return;
        }

        Time.timeScale = 1f;
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    private void MostrarVitoria()
    {
        mostrandoVitoria = true;
        Time.timeScale = 0f;
        GerenciadorTelasJogo.MostrarVitoria(ReiniciarJogo);
    }

    private void ReiniciarJogo()
    {
        indiceFaseAtual = 0;
        mostrandoVitoria = false;
        Time.timeScale = 1f;
        GerenciadorTelasJogo.OcultarTelas();
        GerenciadorTelasJogo.RetomarMusicaJogo();
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    private void OnGUI()
    {
        if (GerenciadorGameOver.EmGameOver || GerenciadorTelasJogo.TelaBloqueanteAtiva)
        {
            return;
        }

        DesenharHUD();
    }

    private void DesenharHUD()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(16f, 16f, 560f, 96f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle texto = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(32f, 24f, 520f, 28f), tituloFase, titulo);
        GUI.Label(new Rect(32f, 54f, 520f, 48f), instrucaoFase, texto);
    }

}
