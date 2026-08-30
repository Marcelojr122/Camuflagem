using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurarProjetoCamuflagem
{
    private const string PastaSprites = "Assets/Sprites";
    private const string PastaAnimJogador = "Assets/Prefabs/Jogador/Animations";
    private const string PastaInimigos = "Assets/Prefabs/Inimigos";
    private const string PastaAnimInimigos = "Assets/Prefabs/Inimigos/Animations";
    private const string CaminhoControllerJogador = "Assets/Prefabs/Jogador/Animations/Jogador.controller";
    private const string CaminhoClipTrocarCor = "Assets/Prefabs/Jogador/Animations/Camaleao_Trocar_Cor.anim";
    private const string CaminhoPrefabJogador = "Assets/Prefabs/Jogador/Jogador.prefab";
    private const string CaminhoCena = "Assets/Scenes/SampleScene.unity";
    private const string CaminhoMaterialVisao = "Assets/Materials/CampoVisaoMaterial.mat";

    private static readonly (string sufixo, int direcao, string nome)[] Direcoes =
    {
        ("D", 0, "Baixo"),
        ("U", 1, "Cima"),
        ("L", 2, "Esquerda"),
        ("R", 3, "Direita")
    };

    [MenuItem("Camuflagem/Configurar Projeto")]
    public static void ConfigurarTudo()
    {
        GarantirPastas();

        Dictionary<int, AnimationClip> clipsJogador = CriarClipsDirecionais("C", PastaAnimJogador, "Camaleao", 6f);
        AnimationClip clipTrocarCor = CriarClipTrocarCor();
        AnimatorController controllerJogador = ConfigurarController(CaminhoControllerJogador, clipsJogador, true, clipTrocarCor);
        ConfigurarPrefabJogador(controllerJogador);

        Material materialVisao = ObterMaterialVisao();
        GameObject cobra = CriarOuAtualizarInimigo(
            "Cobra",
            "M",
            "Assets/Prefabs/Inimigos/Cobra.prefab",
            1.15f,
            2.35f,
            18f,
            8f,
            materialVisao);

        GameObject cientista = CriarOuAtualizarInimigo(
            "Cientista",
            "S",
            "Assets/Prefabs/Inimigos/Cientista.prefab",
            2.05f,
            3.4f,
            14f,
            7f,
            materialVisao);

        ConfigurarCena(cobra, cientista, controllerJogador);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Projeto Camuflagem configurado.");
    }

    private static void GarantirPastas()
    {
        CriarPastaSePreciso("Assets", "Editor");
        CriarPastaSePreciso("Assets/Prefabs", "Inimigos");
        CriarPastaSePreciso(PastaInimigos, "Animations");
    }

    private static void CriarPastaSePreciso(string pai, string nome)
    {
        string caminho = $"{pai}/{nome}";

        if (!AssetDatabase.IsValidFolder(caminho))
        {
            AssetDatabase.CreateFolder(pai, nome);
        }
    }

    private static Dictionary<int, AnimationClip> CriarClipsDirecionais(string prefixo, string pastaDestino, string nomeBase, float sampleRate)
    {
        Dictionary<int, AnimationClip> clips = new Dictionary<int, AnimationClip>();

        foreach ((string sufixo, int direcao, string nome) in Direcoes)
        {
            Sprite idle = CarregarSprite($"{prefixo}_{sufixo}_1");
            List<Sprite> framesAndando = CarregarFrames(prefixo, sufixo).Where(sprite => ObterNumeroFinal(sprite.name) != 1).ToList();

            if (idle == null)
            {
                Debug.LogWarning($"Sprite idle nao encontrada: {prefixo}_{sufixo}_1");
                continue;
            }

            if (framesAndando.Count == 0)
            {
                framesAndando.Add(idle);
            }

            clips[direcao] = CriarClip($"{pastaDestino}/{nomeBase}_Idle_{nome}.anim", new[] { idle }, 1f, false);
            clips[direcao + 10] = CriarClip($"{pastaDestino}/{nomeBase}_Andar_{nome}.anim", framesAndando, sampleRate, true);
        }

        return clips;
    }

    private static AnimationClip CriarClipTrocarCor()
    {
        Sprite sprite = CarregarSprite("C_W");

        if (sprite == null)
        {
            Debug.LogWarning("Sprite de troca de cor nao encontrada: C_W");
            return null;
        }

        return CriarClip(CaminhoClipTrocarCor, new[] { sprite }, 1f, true);
    }

    private static Sprite CarregarSprite(string nome)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaSprites}/{nome}.png");
    }

    private static List<Sprite> CarregarFrames(string prefixo, string sufixo)
    {
        return Directory.GetFiles(PastaSprites, $"{prefixo}_{sufixo}_*.png")
            .Select(caminho => caminho.Replace("\\", "/"))
            .OrderBy(caminho => ObterNumeroFinal(Path.GetFileNameWithoutExtension(caminho)))
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(sprite => sprite != null)
            .ToList();
    }

    private static int ObterNumeroFinal(string nome)
    {
        string[] partes = nome.Split('_');

        for (int indice = partes.Length - 1; indice >= 0; indice--)
        {
            if (!int.TryParse(partes[indice], out int numero))
            {
                continue;
            }

            if (indice == partes.Length - 1 && numero == 0 && indice > 0 && int.TryParse(partes[indice - 1], out int numeroAnterior))
            {
                return numeroAnterior;
            }

            return numero;
        }

        return 0;
    }

    private static AnimationClip CriarClip(string caminho, IReadOnlyList<Sprite> sprites, float sampleRate, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminho);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, caminho);
        }

        clip.frameRate = sampleRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = sprites.Select((sprite, index) => new ObjectReferenceKeyframe
        {
            time = index / sampleRate,
            value = sprite
        }).ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController ConfigurarController(
        string caminho,
        IReadOnlyDictionary<int, AnimationClip> clips,
        bool incluirParametrosCamuflagem,
        AnimationClip clipTrocarCor = null)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        }

        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        foreach (AnimatorControllerParameter parametro in controller.parameters.ToArray())
        {
            controller.RemoveParameter(parametro);
        }

        controller.AddParameter("andando", AnimatorControllerParameterType.Bool);
        controller.AddParameter("direcao", AnimatorControllerParameterType.Int);

        if (incluirParametrosCamuflagem)
        {
            controller.AddParameter("taCamuflando", AnimatorControllerParameterType.Bool);
            controller.AddParameter("noTapete", AnimatorControllerParameterType.Bool);
            controller.AddParameter("trocandoCor", AnimatorControllerParameterType.Bool);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (ChildAnimatorState estado in stateMachine.states.ToArray())
        {
            stateMachine.RemoveState(estado.state);
        }

        foreach (AnimatorStateTransition transicao in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transicao);
        }

        foreach (ChildAnimatorStateMachine maquina in stateMachine.stateMachines.ToArray())
        {
            stateMachine.RemoveStateMachine(maquina.stateMachine);
        }

        Dictionary<int, AnimatorState> idles = new Dictionary<int, AnimatorState>();
        Dictionary<int, AnimatorState> andando = new Dictionary<int, AnimatorState>();
        AnimatorState estadoTrocarCor = null;

        if (incluirParametrosCamuflagem && clipTrocarCor != null)
        {
            estadoTrocarCor = stateMachine.AddState("Trocar_Cor");
            estadoTrocarCor.motion = clipTrocarCor;
        }

        foreach ((string _, int direcao, string nome) in Direcoes)
        {
            if (!clips.TryGetValue(direcao, out AnimationClip clipIdle) || !clips.TryGetValue(direcao + 10, out AnimationClip clipAndando))
            {
                continue;
            }

            AnimatorState estadoIdle = stateMachine.AddState($"Idle_{nome}");
            estadoIdle.motion = clipIdle;
            idles[direcao] = estadoIdle;

            AnimatorState estadoAndando = stateMachine.AddState($"Andar_{nome}");
            estadoAndando.motion = clipAndando;
            andando[direcao] = estadoAndando;
        }

        if (idles.TryGetValue(0, out AnimatorState idleBaixo))
        {
            stateMachine.defaultState = idleBaixo;
        }

        foreach ((string _, int direcao, string _) in Direcoes)
        {
            if (idles.TryGetValue(direcao, out AnimatorState estadoIdle))
            {
                CriarTransicaoAnyState(stateMachine, estadoIdle, false, direcao, incluirParametrosCamuflagem);
            }

            if (andando.TryGetValue(direcao, out AnimatorState estadoAndando))
            {
                CriarTransicaoAnyState(stateMachine, estadoAndando, true, direcao, incluirParametrosCamuflagem);
            }
        }

        if (estadoTrocarCor != null)
        {
            CriarTransicaoTrocarCor(stateMachine, estadoTrocarCor);
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CriarTransicaoAnyState(AnimatorStateMachine stateMachine, AnimatorState destino, bool andando, int direcao, bool bloquearTrocaCor)
    {
        AnimatorStateTransition transicao = stateMachine.AddAnyStateTransition(destino);
        transicao.hasExitTime = false;
        transicao.duration = 0f;
        transicao.canTransitionToSelf = false;
        transicao.AddCondition(andando ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "andando");
        transicao.AddCondition(AnimatorConditionMode.Equals, direcao, "direcao");

        if (bloquearTrocaCor)
        {
            transicao.AddCondition(AnimatorConditionMode.IfNot, 0f, "trocandoCor");
        }
    }

    private static void CriarTransicaoTrocarCor(AnimatorStateMachine stateMachine, AnimatorState destino)
    {
        AnimatorStateTransition transicao = stateMachine.AddAnyStateTransition(destino);
        transicao.hasExitTime = false;
        transicao.duration = 0f;
        transicao.canTransitionToSelf = false;
        transicao.AddCondition(AnimatorConditionMode.If, 0f, "trocandoCor");
    }

    private static void ConfigurarPrefabJogador(AnimatorController controller)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(CaminhoPrefabJogador);
        ConfigurarObjetoJogador(prefab, controller);
        PrefabUtility.SaveAsPrefabAsset(prefab, CaminhoPrefabJogador);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static void ConfigurarObjetoJogador(GameObject jogador, AnimatorController controller)
    {
        SpriteRenderer spriteRenderer = jogador.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Sprite idleBaixo = CarregarSprite("C_D_1");
            spriteRenderer.sprite = idleBaixo != null ? idleBaixo : spriteRenderer.sprite;
            spriteRenderer.sortingOrder = 2;
        }

        Animator animator = jogador.GetComponent<Animator>();

        if (animator == null)
        {
            animator = jogador.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        if (jogador.GetComponent<Movimento>() == null)
        {
            jogador.AddComponent<Movimento>();
        }

        if (jogador.GetComponent<MudarCor>() == null)
        {
            jogador.AddComponent<MudarCor>();
        }

        Camuflar camuflar = jogador.GetComponent<Camuflar>();

        if (camuflar == null)
        {
            camuflar = jogador.AddComponent<Camuflar>();
        }

        camuflar.spriteTrocaCor = CarregarSprite("C_W");
    }

    private static Material ObterMaterialVisao()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterialVisao);

        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1f, 0.85f, 0.1f, 0.28f);
        AssetDatabase.CreateAsset(material, CaminhoMaterialVisao);
        return material;
    }

    private static GameObject CriarOuAtualizarInimigo(
        string nome,
        string prefixoSprites,
        string caminhoPrefab,
        float velocidadePatrulha,
        float velocidadePerseguicao,
        float distanciaVisao,
        float distanciaPatrulha,
        Material materialVisao)
    {
        Dictionary<int, AnimationClip> clips = CriarClipsInimigo(prefixoSprites, PastaAnimInimigos, nome, 6f);
        AnimatorController controller = ConfigurarController($"{PastaAnimInimigos}/{nome}.controller", clips, false);

        GameObject prefabExistente = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
        bool carregadoDePrefab = prefabExistente != null;
        GameObject objeto = carregadoDePrefab ? PrefabUtility.LoadPrefabContents(caminhoPrefab) : new GameObject(nome);

        if (objeto == null)
        {
            objeto = new GameObject(nome);
        }

        objeto.name = nome;
        SpriteRenderer spriteRenderer = ObterOuAdicionar<SpriteRenderer>(objeto);
        Sprite idleBaixo = CarregarSprite($"{prefixoSprites}_D_1");
        spriteRenderer.sprite = idleBaixo != null ? idleBaixo : spriteRenderer.sprite;
        spriteRenderer.sortingOrder = 3;

        Rigidbody2D rigidbody = ObterOuAdicionar<Rigidbody2D>(objeto);
        rigidbody.gravityScale = 0f;
        rigidbody.freezeRotation = true;

        BoxCollider2D boxCollider = ObterOuAdicionar<BoxCollider2D>(objeto);
        boxCollider.isTrigger = true;

        if (idleBaixo != null)
        {
            boxCollider.size = idleBaixo.bounds.size * 0.65f;
            boxCollider.offset = Vector2.zero;
        }

        Animator animator = ObterOuAdicionar<Animator>(objeto);
        animator.runtimeAnimatorController = controller;

        Inimigo inimigo = ObterOuAdicionar<Inimigo>(objeto);
        inimigo.velocidadePatrulha = velocidadePatrulha;
        inimigo.velocidadePerseguicao = velocidadePerseguicao;
        inimigo.distanciaVisao = distanciaVisao;
        inimigo.distanciaPatrulha = distanciaPatrulha;
        inimigo.intervaloTrocaDestino = 2.4f;
        inimigo.direcaoInicial = Vector2.right;
        inimigo.anguloVisao = 105f;
        inimigo.toleranciaHue = 2f;

        Transform campo = objeto.transform.Find("Campo de Visao");

        if (campo == null)
        {
            campo = new GameObject("Campo de Visao").transform;
            campo.SetParent(objeto.transform, false);
        }

        MeshRenderer meshRenderer = ObterOuAdicionar<MeshRenderer>(campo.gameObject);
        ObterOuAdicionar<MeshFilter>(campo.gameObject);
        meshRenderer.sharedMaterial = materialVisao;
        meshRenderer.sortingOrder = 1;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, caminhoPrefab);

        if (carregadoDePrefab)
        {
            PrefabUtility.UnloadPrefabContents(objeto);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(objeto);
        }

        return prefab;
    }

    private static T ObterOuAdicionar<T>(GameObject objeto) where T : Component
    {
        T componente = objeto.GetComponent<T>();
        return componente != null ? componente : objeto.AddComponent<T>();
    }

    private static Dictionary<int, AnimationClip> CriarClipsInimigo(string prefixo, string pastaDestino, string nomeBase, float sampleRate)
    {
        Dictionary<int, AnimationClip> clips = new Dictionary<int, AnimationClip>();

        foreach ((string sufixo, int direcao, string nome) in Direcoes)
        {
            Sprite idle = CarregarSprite($"{prefixo}_{sufixo}_1");
            List<Sprite> framesAndando = prefixo == "S"
                ? CarregarFramesCientista(sufixo)
                : CarregarFrames(prefixo, sufixo);

            if (idle == null)
            {
                Debug.LogWarning($"Sprite idle nao encontrada: {prefixo}_{sufixo}_1");
                continue;
            }

            if (framesAndando.Count == 0)
            {
                framesAndando.Add(idle);
            }

            clips[direcao] = CriarClip($"{pastaDestino}/{nomeBase}_Idle_{nome}.anim", new[] { idle }, 1f, false);
            clips[direcao + 10] = CriarClip($"{pastaDestino}/{nomeBase}_Andar_{nome}.anim", framesAndando, sampleRate, true);
        }

        return clips;
    }

    private static List<Sprite> CarregarFramesCientista(string sufixo)
    {
        Sprite frame1 = CarregarSprite($"S_{sufixo}_1");
        Sprite frame2 = CarregarSprite($"S_{sufixo}_2");
        Sprite frame3 = CarregarSprite($"S_{sufixo}_3");
        List<Sprite> frames = new List<Sprite>();

        if (frame1 != null)
        {
            frames.Add(frame1);
        }

        if (frame2 != null)
        {
            frames.Add(frame2);
        }

        if (frame1 != null)
        {
            frames.Add(frame1);
        }

        if (frame3 != null)
        {
            frames.Add(frame3);
        }

        return frames;
    }

    private static void ConfigurarCena(GameObject cobraPrefab, GameObject cientistaPrefab, AnimatorController controllerJogador)
    {
        if (!File.Exists(CaminhoCena))
        {
            return;
        }

        UnityEngine.SceneManagement.Scene cena = EditorSceneManager.OpenScene(CaminhoCena);

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");

        if (jogador != null)
        {
            ConfigurarObjetoJogador(jogador, controllerJogador);
            ConfigurarCamera(jogador);
        }

        if (UnityEngine.Object.FindFirstObjectByType<GerenciadorGameOver>() == null)
        {
            new GameObject("GerenciadorGameOver").AddComponent<GerenciadorGameOver>();
        }

        InstanciarInimigoSePreciso(cobraPrefab, "Cobra", new Vector3(-5.5f, -3.2f, 0f), Vector2.right);
        InstanciarInimigoSePreciso(cientistaPrefab, "Cientista", new Vector3(5.6f, 3.1f, 0f), Vector2.left);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    private static void ConfigurarCamera(GameObject jogador)
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();

        if (camera == null)
        {
            return;
        }

        CameraSeguirJogador seguirJogador = camera.GetComponent<CameraSeguirJogador>();

        if (seguirJogador == null)
        {
            seguirJogador = camera.gameObject.AddComponent<CameraSeguirJogador>();
        }

        seguirJogador.alvo = jogador.transform;
        seguirJogador.deslocamento = new Vector3(0f, 0f, camera.transform.position.z);
        seguirJogador.suavizacao = 4f;
        camera.transform.position = jogador.transform.position + seguirJogador.deslocamento;
        EditorUtility.SetDirty(camera.gameObject);
    }

    private static void InstanciarInimigoSePreciso(GameObject prefab, string nome, Vector3 posicao, Vector2 direcaoInicial)
    {
        if (prefab == null || GameObject.Find(nome) != null)
        {
            return;
        }

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instancia.name = nome;
        instancia.transform.position = posicao;

        Inimigo inimigo = instancia.GetComponent<Inimigo>();

        if (inimigo != null)
        {
            inimigo.direcaoInicial = direcaoInicial;
        }
    }
}
