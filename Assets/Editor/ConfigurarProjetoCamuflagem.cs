using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

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
            4f,
            28f,
            8f,
            materialVisao);

        GameObject cientista = CriarOuAtualizarInimigo(
            "Cientista",
            "S",
            "Assets/Prefabs/Inimigos/Cientista.prefab",
            2.05f,
            3.4f,
            6f,
            22f,
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
            List<Sprite> todosFrames = CarregarFrames(prefixo, sufixo);
            List<Sprite> framesAndando = todosFrames.Where(sprite => ObterNumeroFinal(sprite.name) != 1).ToList();

            if (idle == null)
            {
                Debug.LogWarning($"Sprite idle nao encontrada: {prefixo}_{sufixo}_1");
                continue;
            }

            if (framesAndando.Count == 0)
            {
                framesAndando.Add(idle);
            }
            else if (framesAndando.Count == 1 && todosFrames.Count > 1)
            {
                framesAndando.Insert(0, idle);
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
        float sampleRateAnimacao,
        float distanciaVisao,
        float distanciaPatrulha,
        Material materialVisao)
    {
        Dictionary<int, AnimationClip> clips = CriarClipsInimigo(prefixoSprites, PastaAnimInimigos, nome, sampleRateAnimacao);
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
        inimigo.raioCaptura = 0.65f;
        inimigo.direcaoInicial = Vector2.right;
        inimigo.anguloVisao = 28f;
        inimigo.velocidadeGiroVisao = 140f;
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
        instancia.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

        Inimigo inimigo = instancia.GetComponent<Inimigo>();

        if (inimigo != null)
        {
            inimigo.direcaoInicial = direcaoInicial;
        }
    }

    public static void ValidarIntegracaoProcedural()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObjeto = new GameObject("Main Camera");
            cameraObjeto.tag = "MainCamera";
            Camera camera = cameraObjeto.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject gridObjeto = new GameObject("Grid");
            gridObjeto.AddComponent<Grid>();

            GameObject tilemapObjeto = new GameObject("Tilemap");
            tilemapObjeto.transform.SetParent(gridObjeto.transform, false);
            Tilemap tilemap = tilemapObjeto.AddComponent<Tilemap>();
            tilemapObjeto.AddComponent<TilemapRenderer>();

            GameObject mapaObjeto = new GameObject("ProceduralMap");
            ProceduralMap mapa = mapaObjeto.AddComponent<ProceduralMap>();
            mapa.mapWidth = 40;
            mapa.mapHeight = 30;
            mapa.roomCount = 8;
            mapa.randomSeed = false;
            mapa.seed = 12345;
            mapa.tilemap = tilemap;
            mapa.floorTile = CarregarAsset<TileBase>("4d0e0c38c7aad89488e3deeea4a69a8b");
            mapa.wallTopTile = CarregarAsset<TileBase>("8745aad07f1b21e4c84d596c4dc824e8");
            mapa.wallLeftTile = CarregarAsset<TileBase>("e8b60b4f87305824e96c337653be55ab");
            mapa.wallRightTile = CarregarAsset<TileBase>("732cc574cfeb53445aa0cd540a046c9a");
            mapa.wallBottomTile = CarregarAsset<TileBase>("82b17fc6fa87f394591a6d1115663412");
            mapa.wallTile = CarregarAsset<TileBase>("5b32a97254d088c4483a2d316b222305");
            mapa.wallCornerTopLeftTile = CarregarAsset<TileBase>("9818d8bf7efce2643924c62bd30710bd");
            mapa.wallCornerTopRightTile = CarregarAsset<TileBase>("2384a050e39ebc245a868d51adea4dd9");
            mapa.wallCornerBottomLeftTile = CarregarAsset<TileBase>("b51a1748557e506459197ace75d23a2f");
            mapa.wallCornerBottomRightTile = CarregarAsset<TileBase>("44364633dc4f48544b4749462f83f1b1");
            mapa.carpetTile = CarregarAsset<TileBase>("20d9cb10a1f448d4bbd799d16f96d963");
            mapa.carpetTopTile = CarregarAsset<TileBase>("3d10a0c3ecaf45c0b9a41a37f5d4707b");
            mapa.carpetBottomTile = CarregarAsset<TileBase>("fdc4e8f4a4d94234825b4c8559f89c3d");
            mapa.carpetLeftTile = CarregarAsset<TileBase>("ccd72382202f405b9d23b9074e612631");
            mapa.carpetRightTile = CarregarAsset<TileBase>("a2236382f46943f9bf382f73fd4f209d");
            mapa.carpetTopLeftTile = CarregarAsset<TileBase>("015d74ba39044ac4bed796078924d046");
            mapa.carpetTopRightTile = CarregarAsset<TileBase>("c9784c43fe714cce80cc1199fed1d89c");
            mapa.carpetBottomLeftTile = CarregarAsset<TileBase>("0fd5a2806c8a4ef1b1bcf25076c70401");
            mapa.carpetBottomRightTile = CarregarAsset<TileBase>("7fbb79a1420b4bdfb749b826a803a19c");
            mapa.usarFallbackVisualParaChaoEParede = false;
            mapa.jogadorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabJogador);
            mapa.cobraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inimigos/Cobra.prefab");
            mapa.cientistaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inimigos/Cientista.prefab");
            mapa.cameraPrincipal = camera;
            mapa.caixaSprite = CarregarAsset<Sprite>("72c4ad8987f30ad489746c9e346c528f");
            mapa.botaoSprite = CarregarAsset<Sprite>("aea5b74f94051924793a1e8908bfb376");
            mapa.botaoPressionadoSprite = CarregarAsset<Sprite>("9fbe5c8e6fb6a064d9892258566e7bbc");
            mapa.carpetMaterials = new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TapeteMaterial.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TapeteMaterial 1.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TapeteMaterial 2.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TapeteMaterial 3.mat")
            };

            mapa.GenerateMap();
            ValidarCondicao(mapa.TemMapaGerado, "mapa procedural nao gerou chao valido");
            ValidarCondicao(GameObject.FindGameObjectWithTag("Player") != null, "jogador nao foi criado no mapa procedural");
            ValidarCondicao(UnityEngine.Object.FindObjectsByType<Inimigo>(FindObjectsSortMode.None).Length >= 2, "inimigos nao foram criados no mapa procedural");
            ValidarCondicao(GameObject.FindGameObjectsWithTag("Tapete").Length > 0, "tapetes procedurais nao criaram triggers com tag Tapete");
            ValidarCondicao(UnityEngine.Object.FindFirstObjectByType<SaidaPuzzle>() != null, "saida do puzzle nao foi criada no mapa procedural");
            ValidarCondicao(UnityEngine.Object.FindFirstObjectByType<BotaoPuzzle>() != null, "botao do puzzle nao foi criado no mapa procedural");
            ValidarCondicao(UnityEngine.Object.FindFirstObjectByType<CaixaArrastavel>() != null, "caixa arrastavel nao foi criada no mapa procedural");
            ValidarCondicao(tilemap.GetComponent<TilemapCollider2D>() != null, "tilemap procedural nao recebeu TilemapCollider2D");
            ValidarCondicao(camera.GetComponent<CameraSeguirJogador>() != null, "camera nao recebeu CameraSeguirJogador");
            ValidarCampoDeVisaoEstrito();

            Debug.Log("VALIDACAO_PROCEDURAL_OK");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    private static T CarregarAsset<T>(string guid) where T : UnityEngine.Object
    {
        string caminho = AssetDatabase.GUIDToAssetPath(guid);
        T asset = AssetDatabase.LoadAssetAtPath<T>(caminho);

        if (asset != null)
        {
            return asset;
        }

        return AssetDatabase
            .LoadAllAssetsAtPath(caminho)
            .OfType<T>()
            .FirstOrDefault();
    }

    private static void ValidarCondicao(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new InvalidOperationException(mensagem);
        }
    }

    private static void ValidarCampoDeVisaoEstrito()
    {
        GameObject alvoObjeto = new GameObject("Alvo Teste");
        alvoObjeto.tag = "Player";
        alvoObjeto.AddComponent<BoxCollider2D>();

        GameObject inimigoObjeto = new GameObject("Inimigo Teste");
        Rigidbody2D corpo = inimigoObjeto.AddComponent<Rigidbody2D>();
        inimigoObjeto.AddComponent<BoxCollider2D>();
        inimigoObjeto.AddComponent<SpriteRenderer>();
        Inimigo inimigo = inimigoObjeto.AddComponent<Inimigo>();

        corpo.position = Vector2.zero;
        DefinirCampoPrivado(inimigo, "corpo", corpo);
        DefinirCampoPrivado(inimigo, "alvo", alvoObjeto.transform);
        DefinirCampoPrivado(inimigo, "direcaoVisao", Vector2.right);

        System.Reflection.MethodInfo devePerseguir = typeof(Inimigo).GetMethod(
            "DevePerseguirAlvo",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        alvoObjeto.transform.position = new Vector3(4f, 0f, 0f);
        bool dentro = (bool)devePerseguir.Invoke(inimigo, null);

        alvoObjeto.transform.position = new Vector3(0f, 4f, 0f);
        bool fora = (bool)devePerseguir.Invoke(inimigo, null);

        ValidarCondicao(dentro, "inimigo nao detecta jogador dentro do triangulo");
        ValidarCondicao(!fora, "inimigo detecta jogador fora do triangulo");
    }

    private static void DefinirCampoPrivado(object alvo, string nome, object valor)
    {
        System.Reflection.FieldInfo campo = alvo.GetType().GetField(
            nome,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        campo.SetValue(alvo, valor);
    }
}
