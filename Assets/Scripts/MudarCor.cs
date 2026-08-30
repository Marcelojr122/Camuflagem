using System.Collections.Generic;
using UnityEngine;

public class MudarCor : MonoBehaviour
{
    private const string PropriedadeHue = "_Hue";
    private const float PassoHue = 1f;

    private Renderer meuRender;
    private Material materialDoJogador;
    private Camuflar camuflar;
    private readonly List<Renderer> tapetesEmContato = new List<Renderer>();
    private Renderer tapeteAtual;
    private float hueOriginal;
    private float tempo = 0f;

    [Range(0, 1)] public float velocidadeMudaCor = 0.01f;

    public Renderer TapeteAtual => tapeteAtual;

    private void Awake()
    {
        meuRender = GetComponent<Renderer>();
        camuflar = GetComponent<Camuflar>();

        if (camuflar == null || meuRender == null)
        {
            return;
        }

        materialDoJogador = meuRender.material;

        if (TemHue(materialDoJogador))
        {
            hueOriginal = materialDoJogador.GetFloat(PropriedadeHue);
        }
    }

    private void Update()
    {
        if (camuflar == null || materialDoJogador == null || !TemHue(materialDoJogador))
        {
            return;
        }

        tempo += Time.deltaTime;

        if (tempo < velocidadeMudaCor)
        {
            return;
        }

        tempo = 0f;

        float hueAlvo = camuflar.EstaCamuflado && tapeteAtual != null
            ? LerHueDoTapete(tapeteAtual)
            : hueOriginal;

        AproximarHue(materialDoJogador, hueAlvo);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        RegistrarTapete(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        RegistrarTapete(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (camuflar == null || !collision.CompareTag("Tapete"))
        {
            return;
        }

        Renderer renderDoTapete = collision.GetComponent<Renderer>();

        if (renderDoTapete == null)
        {
            return;
        }

        tapetesEmContato.Remove(renderDoTapete);
        tapeteAtual = tapetesEmContato.Count > 0 ? tapetesEmContato[tapetesEmContato.Count - 1] : null;
        camuflar.DefinirNoTapete(tapeteAtual != null);
    }

    public bool EstaComHueDoTapete(float tolerancia)
    {
        if (materialDoJogador == null || tapeteAtual == null || !TemHue(materialDoJogador) || !TemHue(tapeteAtual.sharedMaterial))
        {
            return false;
        }

        float diferenca = Mathf.Abs(Mathf.DeltaAngle(materialDoJogador.GetFloat(PropriedadeHue), LerHueDoTapete(tapeteAtual)));
        return diferenca <= tolerancia;
    }

    private void RegistrarTapete(Collider2D collision)
    {
        if (camuflar == null || !collision.CompareTag("Tapete"))
        {
            return;
        }

        Renderer renderDoTapete = collision.GetComponent<Renderer>();

        if (renderDoTapete == null || !TemHue(renderDoTapete.sharedMaterial))
        {
            return;
        }

        if (!tapetesEmContato.Contains(renderDoTapete))
        {
            tapetesEmContato.Add(renderDoTapete);
        }

        tapeteAtual = renderDoTapete;
        camuflar.DefinirNoTapete(true);
    }

    private static bool TemHue(Material material)
    {
        return material != null && material.HasProperty(PropriedadeHue);
    }

    private static float LerHueDoTapete(Renderer renderDoTapete)
    {
        return renderDoTapete.sharedMaterial.GetFloat(PropriedadeHue);
    }

    private static void AproximarHue(Material material, float hueAlvo)
    {
        float hueAtual = material.GetFloat(PropriedadeHue);
        float proximoHue = Mathf.MoveTowardsAngle(hueAtual, hueAlvo, PassoHue);
        material.SetFloat(PropriedadeHue, Mathf.Repeat(proximoHue, 360f));
    }
}
