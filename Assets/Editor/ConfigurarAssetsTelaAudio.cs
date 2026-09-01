using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurarAssetsTelaAudio
{
    private const string CaminhoCenaPrincipal = "Assets/ProceduralMapScene.unity";

    [MenuItem("Camuflagem/Configurar telas e audios")]
    public static void ConfigurarCenaPrincipal()
    {
        ConfigurarSpriteUi("Assets/Sprites/Menu_Bg.png");
        ConfigurarSpriteUi("Assets/Sprites/Game_Over_Bg.png");
        ConfigurarSpriteUi("Assets/Sprites/End_Bg.png");

        UnityEngine.SceneManagement.Scene cena = EditorSceneManager.OpenScene(CaminhoCenaPrincipal);
        GerenciadorTelasJogo telas = Object.FindFirstObjectByType<GerenciadorTelasJogo>();

        if (telas == null)
        {
            telas = new GameObject("GerenciadorTelasJogo").AddComponent<GerenciadorTelasJogo>();
        }

        telas.fundoMenu = CarregarSprite("Assets/Sprites/Menu_Bg.png");
        telas.fundoGameOver = CarregarSprite("Assets/Sprites/Game_Over_Bg.png");
        telas.fundoVitoria = CarregarSprite("Assets/Sprites/End_Bg.png");
        telas.musicaJogo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/Coloro.mp3");
        telas.musicaGameOver = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/Gameover.mp3");
        telas.somPushBox = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/Pushbox.mp3");
        telas.musicaVitoria = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/Victory.mp3");
        telas.mostrarMenuAoIniciar = true;

        EditorUtility.SetDirty(telas);
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("TELAS_E_AUDIOS_CONFIGURADOS");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static Sprite CarregarSprite(string caminho)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

        if (sprite != null)
        {
            return sprite;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(caminho);

        foreach (Object asset in assets)
        {
            if (asset is Sprite spriteInterno)
            {
                return spriteInterno;
            }
        }

        Debug.LogWarning($"Sprite nao encontrada: {caminho}");
        return null;
    }

    private static void ConfigurarSpriteUi(string caminho)
    {
        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(caminho) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
