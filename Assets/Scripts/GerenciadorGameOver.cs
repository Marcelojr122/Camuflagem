using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GerenciadorGameOver : MonoBehaviour
{
    private static GerenciadorGameOver instancia;

    public static bool EmGameOver { get; private set; }

    private void Awake()
    {
        instancia = this;
        EmGameOver = false;
        Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (!EmGameOver)
        {
            return;
        }

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float largura = 280f;
        float altura = 70f;
        Rect areaBotao = new Rect((Screen.width - largura) * 0.5f, (Screen.height - altura) * 0.5f, largura, altura);

        if (GUI.Button(areaBotao, "Reiniciar fase"))
        {
            ReiniciarFase();
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
            instancia.MostrarTelaGameOver();
        }
    }

    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        EmGameOver = false;
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    private void MostrarTelaGameOver()
    {
        if (GameObject.Find("TelaGameOver") != null)
        {
            return;
        }

        Canvas canvas = CriarCanvas();
        CriarEventSystemSePreciso();
        CriarFundo(canvas.transform);
        CriarBotaoReiniciar(canvas.transform);
    }

    private static Canvas CriarCanvas()
    {
        GameObject objetoCanvas = new GameObject("TelaGameOver");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = objetoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        objetoCanvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CriarEventSystemSePreciso()
    {
        EventSystem eventSystemExistente = FindFirstObjectByType<EventSystem>();

        if (eventSystemExistente != null)
        {
            if (eventSystemExistente.GetComponent<BaseInputModule>() == null)
            {
                eventSystemExistente.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            return;
        }

        GameObject objetoEventSystem = new GameObject("EventSystem");
        objetoEventSystem.AddComponent<EventSystem>();
        objetoEventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void CriarFundo(Transform pai)
    {
        GameObject fundo = new GameObject("Fundo");
        fundo.transform.SetParent(pai, false);

        Image imagem = fundo.AddComponent<Image>();
        imagem.color = Color.black;

        RectTransform rect = fundo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void CriarBotaoReiniciar(Transform pai)
    {
        GameObject objetoBotao = new GameObject("BotaoReiniciar");
        objetoBotao.transform.SetParent(pai, false);

        Image imagem = objetoBotao.AddComponent<Image>();
        imagem.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button botao = objetoBotao.AddComponent<Button>();
        botao.onClick.AddListener(ReiniciarFase);

        RectTransform rectBotao = objetoBotao.GetComponent<RectTransform>();
        rectBotao.anchorMin = new Vector2(0.5f, 0.5f);
        rectBotao.anchorMax = new Vector2(0.5f, 0.5f);
        rectBotao.anchoredPosition = Vector2.zero;
        rectBotao.sizeDelta = new Vector2(320f, 90f);

        GameObject objetoTexto = new GameObject("Texto");
        objetoTexto.transform.SetParent(objetoBotao.transform, false);

        Text texto = objetoTexto.AddComponent<Text>();
        texto.text = "Reiniciar fase";
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        texto.fontSize = 34;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.black;

        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;
    }
}
