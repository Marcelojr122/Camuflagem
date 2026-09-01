using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-300)]
public class GerenciadorTelasJogo : MonoBehaviour
{
    private static GerenciadorTelasJogo instancia;
    private static bool menuInicialJaFechado;

    [Header("Telas")]
    public Sprite fundoMenu;
    public Sprite fundoGameOver;
    public Sprite fundoVitoria;

    [Header("Audios")]
    public AudioClip musicaJogo;
    public AudioClip musicaGameOver;
    public AudioClip somPushBox;
    public AudioClip musicaVitoria;

    [Range(0f, 1f)] public float volumeMusica = 0.55f;
    [Range(0f, 1f)] public float volumeEfeitos = 0.8f;
    public bool mostrarMenuAoIniciar = true;

    private AudioSource musica;
    private AudioSource efeitos;
    private Canvas canvasAtual;
    private bool telaBloqueanteAtiva;

    public static bool TelaBloqueanteAtiva => instancia != null && instancia.telaBloqueanteAtiva;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        PrepararAudio();
        TocarMusicaJogo();

        if (mostrarMenuAoIniciar && !menuInicialJaFechado)
        {
            MostrarMenuInicial();
        }
    }

    public static GerenciadorTelasJogo GarantirInstancia()
    {
        if (instancia != null)
        {
            return instancia;
        }

        GerenciadorTelasJogo existente = FindFirstObjectByType<GerenciadorTelasJogo>();

        if (existente != null)
        {
            instancia = existente;
            return instancia;
        }

        GameObject objeto = new GameObject("GerenciadorTelasJogo");
        instancia = objeto.AddComponent<GerenciadorTelasJogo>();
        return instancia;
    }

    public static void TocarMusicaJogo()
    {
        GerenciadorTelasJogo gerente = GarantirInstancia();
        gerente.TocarMusica(gerente.musicaJogo, true);
    }

    public static void TocarPushBox()
    {
        GerenciadorTelasJogo gerente = GarantirInstancia();

        if (gerente.somPushBox != null && gerente.efeitos != null)
        {
            gerente.efeitos.PlayOneShot(gerente.somPushBox, gerente.volumeEfeitos);
        }
    }

    public static void MostrarGameOver(Action aoReiniciar)
    {
        GarantirInstancia().MostrarTela(
            "TelaGameOver",
            GarantirInstancia().fundoGameOver,
            GarantirInstancia().musicaGameOver,
            false,
            "Reiniciar fase",
            aoReiniciar
        );
    }

    public static void MostrarVitoria(Action aoJogarNovamente)
    {
        GarantirInstancia().MostrarTela(
            "TelaVitoria",
            GarantirInstancia().fundoVitoria,
            GarantirInstancia().musicaVitoria,
            false,
            "Jogar novamente",
            aoJogarNovamente
        );
    }

    public static void OcultarTelas()
    {
        if (instancia != null)
        {
            instancia.DestruirCanvasAtual();
            instancia.telaBloqueanteAtiva = false;
        }
    }

    private void MostrarMenuInicial()
    {
        Time.timeScale = 0f;
        MostrarTela(
            "TelaMenu",
            fundoMenu,
            musicaJogo,
            true,
            "Jogar",
            () =>
            {
                menuInicialJaFechado = true;
                telaBloqueanteAtiva = false;
                DestruirCanvasAtual();
                Time.timeScale = 1f;
                TocarMusica(musicaJogo, true);
            }
        );
    }

    private void MostrarTela(
        string nome,
        Sprite fundo,
        AudioClip audio,
        bool loopAudio,
        string textoBotao,
        Action acaoBotao
    )
    {
        DestruirCanvasAtual();
        CriarEventSystemSePreciso();

        telaBloqueanteAtiva = true;
        canvasAtual = CriarCanvas(nome);
        CriarFundo(canvasAtual.transform, fundo);
        CriarBotao(canvasAtual.transform, textoBotao, acaoBotao);
        TocarMusica(audio, loopAudio);
    }

    private void PrepararAudio()
    {
        musica = ObterOuCriarAudioSource("Musica");
        musica.loop = true;
        musica.playOnAwake = false;
        musica.volume = volumeMusica;

        efeitos = ObterOuCriarAudioSource("Efeitos");
        efeitos.loop = false;
        efeitos.playOnAwake = false;
        efeitos.volume = volumeEfeitos;
    }

    private AudioSource ObterOuCriarAudioSource(string nome)
    {
        Transform filho = transform.Find(nome);

        if (filho == null)
        {
            filho = new GameObject(nome).transform;
            filho.SetParent(transform, false);
        }

        AudioSource audio = filho.GetComponent<AudioSource>();
        return audio != null ? audio : filho.gameObject.AddComponent<AudioSource>();
    }

    private void TocarMusica(AudioClip clip, bool loop)
    {
        if (musica == null || clip == null)
        {
            return;
        }

        if (musica.clip == clip && musica.isPlaying)
        {
            musica.loop = loop;
            return;
        }

        musica.clip = clip;
        musica.loop = loop;
        musica.volume = volumeMusica;
        musica.Play();
    }

    private Canvas CriarCanvas(string nome)
    {
        GameObject objetoCanvas = new GameObject(nome);
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = objetoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        objetoCanvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CriarFundo(Transform pai, Sprite sprite)
    {
        GameObject fundo = new GameObject("Fundo");
        fundo.transform.SetParent(pai, false);

        Image imagem = fundo.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.color = Color.white;
        imagem.type = Image.Type.Simple;
        imagem.preserveAspect = false;

        RectTransform rect = fundo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CriarBotao(Transform pai, string textoBotao, Action acaoBotao)
    {
        GameObject objetoBotao = new GameObject("Botao");
        objetoBotao.transform.SetParent(pai, false);

        Image imagem = objetoBotao.AddComponent<Image>();
        imagem.color = new Color(0.96f, 0.94f, 0.86f, 0.95f);

        Button botao = objetoBotao.AddComponent<Button>();
        botao.onClick.AddListener(() => acaoBotao?.Invoke());

        RectTransform rectBotao = objetoBotao.GetComponent<RectTransform>();
        rectBotao.anchorMin = new Vector2(0.5f, 0f);
        rectBotao.anchorMax = new Vector2(0.5f, 0f);
        rectBotao.anchoredPosition = new Vector2(0f, 115f);
        rectBotao.sizeDelta = new Vector2(340f, 92f);

        GameObject objetoTexto = new GameObject("Texto");
        objetoTexto.transform.SetParent(objetoBotao.transform, false);

        Text texto = objetoTexto.AddComponent<Text>();
        texto.text = textoBotao;
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        texto.fontSize = 36;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.black;

        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;
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

    private void DestruirCanvasAtual()
    {
        if (canvasAtual == null)
        {
            return;
        }

        Destroy(canvasAtual.gameObject);
        canvasAtual = null;
    }

    public static void RetomarMusicaJogo()
    {
        TocarMusicaJogo();
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
        }
    }
}
