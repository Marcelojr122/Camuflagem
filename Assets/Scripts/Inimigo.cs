using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Inimigo : MonoBehaviour
{
    private static readonly int Andando = Animator.StringToHash("andando");
    private static readonly int Direcao = Animator.StringToHash("direcao");

    [Header("Movimento")]
    public float velocidadePatrulha = 2f;
    public float velocidadePerseguicao = 3f;
    public float distanciaPatrulha = 5f;
    public float intervaloTrocaDestino = 2.5f;
    public Vector2 direcaoInicial = Vector2.right;

    [Header("Visao")]
    public float distanciaVisao = 5f;
    [Range(10f, 160f)] public float anguloVisao = 45f;
    public float toleranciaHue = 2f;

    private Rigidbody2D corpo;
    private Animator animator;
    private Transform alvo;
    private Vector2 centroMovimento;
    private Vector2 destinoAleatorio;
    private Vector2 direcaoAtual;
    private Vector2 direcaoVisao;
    private float tempoNovoDestino;
    private Mesh malhaVisao;
    private MeshFilter filtroVisao;
    private MeshRenderer renderVisao;

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;

        Collider2D colisor = GetComponent<Collider2D>();
        colisor.isTrigger = true;

        direcaoAtual = direcaoInicial.sqrMagnitude > 0.01f ? direcaoInicial.normalized : Vector2.right;
        direcaoVisao = direcaoAtual;
        centroMovimento = transform.position;
        alvo = GameObject.FindGameObjectWithTag("Player")?.transform;
        EscolherNovoDestinoAleatorio();

        PrepararCampoDeVisao();
        AtualizarAnimacao(direcaoAtual, true);
    }

    private void FixedUpdate()
    {
        if (GerenciadorGameOver.EmGameOver)
        {
            return;
        }

        if (alvo == null)
        {
            alvo = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        bool podeVerAlvo = alvo != null && EstaDentroDoCampoDeVisao(alvo) && !AlvoEstaCamuflado(alvo.gameObject);
        Vector2 destino = podeVerAlvo ? (Vector2)alvo.position : ObterDestinoAleatorio();
        float velocidade = podeVerAlvo ? velocidadePerseguicao : velocidadePatrulha;
        Vector2 novaDirecao = destino - corpo.position;

        if (novaDirecao.sqrMagnitude > 0.01f)
        {
            direcaoAtual = novaDirecao.normalized;
            direcaoVisao = podeVerAlvo ? direcaoAtual : Vector2.Lerp(direcaoVisao, direcaoAtual, 0.35f).normalized;
            corpo.MovePosition(Vector2.MoveTowards(corpo.position, destino, velocidade * Time.fixedDeltaTime));
            AtualizarAnimacao(direcaoAtual, true);
        }
        else
        {
            AtualizarAnimacao(direcaoAtual, false);
        }
    }

    private void LateUpdate()
    {
        AtualizarCampoDeVisao();
    }

    private Vector2 ObterDestinoAleatorio()
    {
        tempoNovoDestino -= Time.fixedDeltaTime;

        if (tempoNovoDestino <= 0f || Vector2.Distance(corpo.position, destinoAleatorio) < 0.08f)
        {
            EscolherNovoDestinoAleatorio();
        }

        return destinoAleatorio;
    }

    private void EscolherNovoDestinoAleatorio()
    {
        Vector2 deslocamento = UnityEngine.Random.insideUnitCircle * Mathf.Max(0.5f, distanciaPatrulha);

        if (deslocamento.sqrMagnitude < 0.25f)
        {
            deslocamento = direcaoAtual * Mathf.Min(1.5f, Mathf.Max(0.5f, distanciaPatrulha));
        }

        destinoAleatorio = centroMovimento + deslocamento;
        tempoNovoDestino = UnityEngine.Random.Range(intervaloTrocaDestino * 0.6f, intervaloTrocaDestino * 1.4f);
    }

    private bool EstaDentroDoCampoDeVisao(Transform alvoParaVer)
    {
        Vector2 ateAlvo = (Vector2)alvoParaVer.position - corpo.position;

        if (ateAlvo.sqrMagnitude > distanciaVisao * distanciaVisao)
        {
            return false;
        }

        return Vector2.Angle(direcaoVisao, ateAlvo) <= anguloVisao * 0.5f;
    }

    private bool AlvoEstaCamuflado(GameObject alvoParaChecar)
    {
        Camuflar camuflar = alvoParaChecar.GetComponent<Camuflar>();
        MudarCor mudarCor = alvoParaChecar.GetComponent<MudarCor>();
        return camuflar != null && mudarCor != null && camuflar.EstaNoTapete && mudarCor.EstaComHueDoTapete(toleranciaHue);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TentarCapturar(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TentarCapturar(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TentarCapturar(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TentarCapturar(collision.collider);
    }

    private void TentarCapturar(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || AlvoEstaCamuflado(collision.gameObject))
        {
            return;
        }

        GerenciadorGameOver.GameOver();
    }

    private void AtualizarAnimacao(Vector2 direcao, bool andando)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(Andando, andando);
        animator.SetInteger(Direcao, ObterDirecao(direcao));
    }

    private static int ObterDirecao(Vector2 direcao)
    {
        if (Mathf.Abs(direcao.x) > Mathf.Abs(direcao.y))
        {
            return direcao.x > 0f ? 3 : 2;
        }

        return direcao.y > 0f ? 1 : 0;
    }

    private void PrepararCampoDeVisao()
    {
        Transform filho = transform.Find("Campo de Visao");

        if (filho == null)
        {
            GameObject campo = new GameObject("Campo de Visao");
            campo.transform.SetParent(transform, false);
            filho = campo.transform;
        }

        filtroVisao = filho.GetComponent<MeshFilter>();
        renderVisao = filho.GetComponent<MeshRenderer>();

        if (filtroVisao == null)
        {
            filtroVisao = filho.gameObject.AddComponent<MeshFilter>();
        }

        if (renderVisao == null)
        {
            renderVisao = filho.gameObject.AddComponent<MeshRenderer>();
        }

        malhaVisao = new Mesh { name = "CampoDeVisao" };
        filtroVisao.mesh = malhaVisao;

        if (renderVisao.sharedMaterial == null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = new Color(1f, 0.85f, 0.1f, 0.28f);
            renderVisao.material = material;
        }
    }

    private void AtualizarCampoDeVisao()
    {
        if (malhaVisao == null)
        {
            return;
        }

        Vector3 pontaEsquerda = Rotacionar(direcaoVisao, -anguloVisao * 0.5f) * distanciaVisao;
        Vector3 pontaDireita = Rotacionar(direcaoVisao, anguloVisao * 0.5f) * distanciaVisao;

        malhaVisao.Clear();
        malhaVisao.vertices = new[]
        {
            Vector3.zero,
            pontaEsquerda,
            pontaDireita
        };
        malhaVisao.triangles = new[] { 0, 1, 2 };
        malhaVisao.RecalculateBounds();
    }

    private static Vector2 Rotacionar(Vector2 vetor, float graus)
    {
        float radianos = graus * Mathf.Deg2Rad;
        float seno = Mathf.Sin(radianos);
        float cosseno = Mathf.Cos(radianos);
        return new Vector2(vetor.x * cosseno - vetor.y * seno, vetor.x * seno + vetor.y * cosseno);
    }
}
