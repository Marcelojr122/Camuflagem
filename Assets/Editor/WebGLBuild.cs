using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuild
{
    private const string CenaPrincipal = "Assets/ProceduralMapScene.unity";
    private const string PastaBuildPadrao = "Builds/WebGL_Final";

    [MenuItem("Camuflagem/Build WebGL para Itch.io")]
    public static void BuildWebGLParaItchio()
    {
        string pastaBuild = ObterPastaBuild();
        Directory.CreateDirectory(pastaBuild);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        BuildPlayerOptions opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CenaPrincipal },
            locationPathName = pastaBuild,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport relatorio = BuildPipeline.BuildPlayer(opcoes);

        if (relatorio.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Build WebGL falhou: {relatorio.summary.result}");
        }

        UnityEngine.Debug.Log($"BUILD_WEBGL_OK: {Path.GetFullPath(pastaBuild)}");
    }

    private static string ObterPastaBuild()
    {
        string[] argumentos = Environment.GetCommandLineArgs();

        for (int i = 0; i < argumentos.Length - 1; i++)
        {
            if (argumentos[i] == "-buildOutput")
            {
                return argumentos[i + 1];
            }
        }

        return PastaBuildPadrao;
    }
}
