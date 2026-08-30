using System.Collections.Generic;
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
    public float raioCaptura = 0.65f;
    public Vector2 direcaoInicial = Vector2.right;
    public float intervaloRecalculoCaminho = 0.35f;

    [Header("Visao")]
    public float distanciaVisao = 6f;
    [Range(10f, 160f)] public float anguloVisao = 70f;
    public float velocidadeGiroVisao = 140f;
    public float toleranciaHue = 2f;
    [Range(3, 32)] public int segmentosVisao = 14;
    public ProceduralMap mapaProcedural;
    public bool manterVisaoDurantePerseguicao = true;

    private Rigidbody2D corpo;
    private Collider2D colisorProprio;
    private Collider2D colisorAlvo;
    private Animator animator;
    private Transform alvo;
    private Vector2 centroMovimento;
    private Vector2 destinoAleatorio;
    private Vector2 direcaoAtual;
    private Vector2 direcaoVisao;
    private bool perseguindoAlvo;
    private float tempoNovoDestino;
    private Mesh malhaVisao;
    private MeshFilter filtroVisao;
    private MeshRenderer renderVisao;
    private Material materialVisao;
    private readonly List<Vector2> caminhoAtual = new List<Vector2>();
    private int indiceCaminho;
    private Vector2 destinoCaminho;
    private bool caminhoEraPerseguicao;
    private float tempoRecalculoCaminho;
    private Vector2 ultimaPosicao;
    private float tempoTravado;

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.rotation = 0f;
        corpo.angularVelocity = 0f;

        colisorProprio = GetComponent<Collider2D>();
        colisorProprio.isTrigger = false;

        direcaoAtual = direcaoInicial.sqrMagnitude > 0.01f ? direcaoInicial.normalized : Vector2.right;
        direcaoVisao = direcaoAtual;
        centroMovimento = transform.position;
        ultimaPosicao = transform.position;
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

        AtualizarColisaoComAlvo();

        if (alvo != null && EstaTocandoAlvo(alvo.gameObject) && !AlvoEstaCamuflado(alvo.gameObject))
        {
            GerenciadorGameOver.GameOver();
            return;
        }

        bool podeVerAlvo = DevePerseguirAlvo();

        if (!podeVerAlvo && perseguindoAlvo)
        {
            EscolherDestinoAposPerderAlvo();
        }

        perseguindoAlvo = podeVerAlvo;

        Vector2 destinoFinal = podeVerAlvo ? (Vector2)alvo.position : ObterDestinoAleatorio();
        Vector2 destino = ObterProximoDestinoPeloCaminho(destinoFinal, podeVerAlvo);
        float velocidade = podeVerAlvo ? velocidadePerseguicao : velocidadePatrulha;
        Vector2 novaDirecao = destino - corpo.position;

        if (novaDirecao.sqrMagnitude > 0.01f)
        {
            direcaoAtual = novaDirecao.normalized;
            direcaoVisao = direcaoAtual;

            corpo.rotation = 0f;
            corpo.angularVelocity = 0f;
            corpo.MovePosition(Vector2.MoveTowards(corpo.position, destino, velocidade * Time.fixedDeltaTime));
            AtualizarAnimacao(direcaoAtual, true);
            VerificarTravamento(destino, podeVerAlvo);
        }
        else
        {
            AtualizarAnimacao(direcaoAtual, false);
            tempoTravado = 0f;
            ultimaPosicao = corpo.position;
        }
    }

    public void ConfigurarPatrulhaProcedural(ProceduralMap mapa, Vector2 novaDirecaoInicial)
    {
        mapaProcedural = mapa;
        direcaoInicial = novaDirecaoInicial.sqrMagnitude > 0.01f ? novaDirecaoInicial.normalized : Vector2.right;
        direcaoAtual = direcaoInicial;
        direcaoVisao = direcaoInicial;
        centroMovimento = transform.position;

        if (corpo != null)
        {
            LimparCaminho();
            EscolherNovoDestinoAleatorio();
        }

        AtualizarAnimacao(direcaoAtual, true);
    }

    private bool DevePerseguirAlvo()
    {
        if (alvo == null || !EstaDentroDoCampoDeVisao(alvo))
        {
            return false;
        }

        if (mapaProcedural == null)
        {
            mapaProcedural = FindFirstObjectByType<ProceduralMap>();
        }

        if (mapaProcedural != null && mapaProcedural.TemParedeEntreMundo(corpo.position, alvo.position))
        {
            return false;
        }

        return !AlvoEstaCamuflado(alvo.gameObject);
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
        if (mapaProcedural == null)
        {
            mapaProcedural = FindFirstObjectByType<ProceduralMap>();
        }

        if (mapaProcedural != null && mapaProcedural.TemMapaGerado)
        {
            destinoAleatorio = mapaProcedural.ObterPosicaoMundoAleatoriaDeChao(corpo.position, distanciaPatrulha);
            tempoNovoDestino = UnityEngine.Random.Range(intervaloTrocaDestino * 0.6f, intervaloTrocaDestino * 1.4f);
            LimparCaminho();
            return;
        }

        Vector2 deslocamento = UnityEngine.Random.insideUnitCircle * Mathf.Max(0.5f, distanciaPatrulha);

        if (deslocamento.sqrMagnitude < 0.25f)
        {
            deslocamento = direcaoAtual * Mathf.Min(1.5f, Mathf.Max(0.5f, distanciaPatrulha));
        }

        destinoAleatorio = centroMovimento + deslocamento;
        tempoNovoDestino = UnityEngine.Random.Range(intervaloTrocaDestino * 0.6f, intervaloTrocaDestino * 1.4f);
        LimparCaminho();
    }

    private void EscolherDestinoAposPerderAlvo()
    {
        if (alvo == null)
        {
            EscolherNovoDestinoAleatorio();
            return;
        }

        if (mapaProcedural == null)
        {
            mapaProcedural = FindFirstObjectByType<ProceduralMap>();
        }

        if (mapaProcedural != null && mapaProcedural.TemMapaGerado)
        {
            destinoAleatorio = mapaProcedural.ObterPosicaoMundoAleatoriaLongeDe(corpo.position, alvo.position, distanciaPatrulha);
            AtualizarDirecaoVisao((destinoAleatorio - corpo.position).normalized);
            tempoNovoDestino = UnityEngine.Random.Range(intervaloTrocaDestino * 0.6f, intervaloTrocaDestino * 1.4f);
            LimparCaminho();
            return;
        }

        Vector2 direcaoParaLonge = (corpo.position - (Vector2)alvo.position).normalized;

        if (direcaoParaLonge.sqrMagnitude <= 0.01f)
        {
            direcaoParaLonge = -direcaoAtual;
        }

        destinoAleatorio = corpo.position + direcaoParaLonge * Mathf.Max(1.5f, distanciaPatrulha * 0.6f);
        AtualizarDirecaoVisao((destinoAleatorio - corpo.position).normalized);
        tempoNovoDestino = UnityEngine.Random.Range(intervaloTrocaDestino * 0.6f, intervaloTrocaDestino * 1.4f);
        LimparCaminho();
    }

    private Vector2 ObterProximoDestinoPeloCaminho(Vector2 destinoFinal, bool perseguicao)
    {
        if (mapaProcedural == null)
        {
            mapaProcedural = FindFirstObjectByType<ProceduralMap>();
        }

        if (mapaProcedural == null || !mapaProcedural.TemMapaGerado)
        {
            return destinoFinal;
        }

        bool precisaRecalcular =
            caminhoAtual.Count == 0 ||
            indiceCaminho >= caminhoAtual.Count ||
            caminhoEraPerseguicao != perseguicao ||
            tempoRecalculoCaminho <= 0f ||
            Vector2.Distance(destinoCaminho, destinoFinal) > 0.75f;

        tempoRecalculoCaminho -= Time.fixedDeltaTime;

        if (precisaRecalcular)
        {
            RecalcularCaminho(destinoFinal, perseguicao);
        }

        if (caminhoAtual.Count == 0 || indiceCaminho >= caminhoAtual.Count)
        {
            return corpo.position;
        }

        while (indiceCaminho < caminhoAtual.Count - 1 && Vector2.Distance(corpo.position, caminhoAtual[indiceCaminho]) < 0.08f)
        {
            indiceCaminho++;
        }

        return caminhoAtual[indiceCaminho];
    }

    private void RecalcularCaminho(Vector2 destinoFinal, bool perseguicao)
    {
        caminhoAtual.Clear();
        indiceCaminho = 0;
        destinoCaminho = destinoFinal;
        caminhoEraPerseguicao = perseguicao;
        tempoRecalculoCaminho = Mathf.Max(0.1f, perseguicao ? intervaloRecalculoCaminho : intervaloRecalculoCaminho * 2f);

        if (!mapaProcedural.TentarObterCaminhoMundo(corpo.position, destinoFinal, caminhoAtual) && !perseguicao)
        {
            EscolherNovoDestinoAleatorio();
            mapaProcedural.TentarObterCaminhoMundo(corpo.position, destinoAleatorio, caminhoAtual);
        }
    }

    private void LimparCaminho()
    {
        caminhoAtual.Clear();
        indiceCaminho = 0;
        tempoRecalculoCaminho = 0f;
    }

    private void VerificarTravamento(Vector2 destino, bool perseguicao)
    {
        if (Vector2.Distance(corpo.position, destino) < 0.12f)
        {
            tempoTravado = 0f;
            ultimaPosicao = corpo.position;
            return;
        }

        if (Vector2.Distance(corpo.position, ultimaPosicao) < 0.01f)
        {
            tempoTravado += Time.fixedDeltaTime;
        }
        else
        {
            tempoTravado = 0f;
        }

        ultimaPosicao = corpo.position;

        if (tempoTravado < 0.6f)
        {
            return;
        }

        tempoTravado = 0f;
        LimparCaminho();

        if (!perseguicao)
        {
            EscolherNovoDestinoAleatorio();
        }
    }

    private bool EstaDentroDoCampoDeVisao(Transform alvoParaVer)
    {
        Vector2 ateAlvo = (Vector2)alvoParaVer.position - corpo.position;

        if (direcaoVisao.sqrMagnitude <= 0.01f)
        {
            return false;
        }

        if (ateAlvo.sqrMagnitude > distanciaVisao * distanciaVisao)
        {
            return false;
        }

        Vector2 pontaEsquerda = Rotacionar(direcaoVisao, -anguloVisao * 0.5f) * distanciaVisao;
        Vector2 pontaDireita = Rotacionar(direcaoVisao, anguloVisao * 0.5f) * distanciaVisao;

        return PontoDentroDoTriangulo(ateAlvo, Vector2.zero, pontaEsquerda, pontaDireita);
    }

    private static bool PontoDentroDoTriangulo(Vector2 ponto, Vector2 a, Vector2 b, Vector2 c)
    {
        float lado1 = SinalTriangulo(ponto, a, b);
        float lado2 = SinalTriangulo(ponto, b, c);
        float lado3 = SinalTriangulo(ponto, c, a);

        bool temNegativo = lado1 < 0f || lado2 < 0f || lado3 < 0f;
        bool temPositivo = lado1 > 0f || lado2 > 0f || lado3 > 0f;

        return !(temNegativo && temPositivo);
    }

    private static float SinalTriangulo(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private void AtualizarDirecaoVisao(Vector2 direcaoDesejada)
    {
        if (direcaoDesejada.sqrMagnitude <= 0.01f)
        {
            return;
        }

        if (direcaoVisao.sqrMagnitude <= 0.01f)
        {
            direcaoVisao = direcaoDesejada.normalized;
            return;
        }

        float passoRadianos = velocidadeGiroVisao * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 direcaoGirando = Vector3.RotateTowards(direcaoVisao, direcaoDesejada.normalized, passoRadianos, 0f);
        direcaoVisao = ((Vector2)direcaoGirando).normalized;
    }

    private bool AlvoEstaCamuflado(GameObject alvoParaChecar)
    {
        Camuflar camuflar = alvoParaChecar.GetComponent<Camuflar>();
        MudarCor mudarCor = alvoParaChecar.GetComponent<MudarCor>();
        return camuflar != null && mudarCor != null && camuflar.EstaCamuflado && mudarCor.EstaComHueDoTapete(toleranciaHue);
    }

    private void AtualizarColisaoComAlvo()
    {
        if (alvo == null || colisorProprio == null)
        {
            return;
        }

        if (colisorAlvo == null)
        {
            colisorAlvo = alvo.GetComponent<Collider2D>();
        }

        if (colisorAlvo == null)
        {
            return;
        }

        Physics2D.IgnoreCollision(colisorProprio, colisorAlvo, AlvoEstaCamuflado(alvo.gameObject));
    }

    private bool EstaTocandoAlvo(GameObject alvoParaChecar)
    {
        if (colisorProprio == null)
        {
            return false;
        }

        Collider2D colisorAlvo = alvoParaChecar.GetComponent<Collider2D>();

        if (colisorAlvo == null)
        {
            return false;
        }

        ColliderDistance2D distancia = colisorProprio.Distance(colisorAlvo);
        float distanciaPelosCentros = Vector2.Distance(transform.position, alvoParaChecar.transform.position);
        return distancia.isOverlapped || distancia.distance <= 0.02f || distanciaPelosCentros <= raioCaptura;
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

        filho.localPosition = Vector3.zero;
        filho.localRotation = Quaternion.identity;
        filho.localScale = Vector3.one;

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

        materialVisao = renderVisao.material;
    }

    private void AtualizarCampoDeVisao()
    {
        if (malhaVisao == null)
        {
            return;
        }

        int segmentos = Mathf.Max(3, segmentosVisao);
        Vector3[] vertices = new Vector3[segmentos + 2];
        int[] triangulos = new int[segmentos * 3];
        Vector2 origem = corpo != null ? corpo.position : (Vector2)transform.position;
        bool algumRaioBloqueado = false;

        vertices[0] = Vector3.zero;

        if (mapaProcedural == null)
        {
            mapaProcedural = FindFirstObjectByType<ProceduralMap>();
        }

        for (int i = 0; i <= segmentos; i++)
        {
            float t = i / (float)segmentos;
            float angulo = Mathf.Lerp(-anguloVisao * 0.5f, anguloVisao * 0.5f, t);
            Vector2 direcaoRaio = Rotacionar(direcaoVisao, angulo).normalized;
            Vector2 destinoRaio = origem + direcaoRaio * distanciaVisao;

            if (mapaProcedural != null)
            {
                destinoRaio = mapaProcedural.ObterPontoAntesDaParedeMundo(origem, destinoRaio, out bool bateuNaParede);
                algumRaioBloqueado |= bateuNaParede;
            }

            vertices[i + 1] = ObterVerticeLocalDoCampo(destinoRaio - origem);
        }

        for (int i = 0; i < segmentos; i++)
        {
            int indice = i * 3;
            triangulos[indice] = 0;
            triangulos[indice + 1] = i + 1;
            triangulos[indice + 2] = i + 2;
        }

        malhaVisao.Clear();
        malhaVisao.vertices = vertices;
        malhaVisao.triangles = triangulos;
        malhaVisao.RecalculateBounds();

        AtualizarCorVisao(algumRaioBloqueado);
    }

    private void AtualizarCorVisao(bool bloqueadaPorParede)
    {
        if (materialVisao == null)
        {
            return;
        }

        materialVisao.color = bloqueadaPorParede
            ? new Color(1f, 0.35f, 0.1f, 0.32f)
            : new Color(1f, 0.85f, 0.1f, 0.28f);
    }

    private Vector3 ObterVerticeLocalDoCampo(Vector2 deslocamentoMundo)
    {
        if (filtroVisao == null)
        {
            return deslocamentoMundo;
        }

        Vector3 local = filtroVisao.transform.InverseTransformVector(deslocamentoMundo);
        local.z = 0f;
        return local;
    }

    private static Vector2 Rotacionar(Vector2 vetor, float graus)
    {
        float radianos = graus * Mathf.Deg2Rad;
        float seno = Mathf.Sin(radianos);
        float cosseno = Mathf.Cos(radianos);
        return new Vector2(vetor.x * cosseno - vetor.y * seno, vetor.x * seno + vetor.y * cosseno);
    }
}
