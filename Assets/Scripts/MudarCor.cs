using System.Collections.Generic;
using UnityEngine;

public class MudarCor : MonoBehaviour
{
    private const string PropriedadeHue = "_Hue";
    private const float VelocidadeHueGrausPorSegundo = 420f;

    private Renderer meuRender;
    private Material materialDoJogador;
    private Camuflar camuflar;
    private readonly List<TapeteHue> tapetesEmContato = new List<TapeteHue>();
    private TapeteHue tapeteAtual;
    private float hueOriginal;

    public TapeteHue TapeteAtual => tapeteAtual;

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

        float hueAlvo = camuflar.EstaCamuflado && tapeteAtual != null
            ? tapeteAtual.Hue
            : hueOriginal;

        AproximarHue(materialDoJogador, hueAlvo, VelocidadeHueGrausPorSegundo * Time.deltaTime);
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

        TapeteHue hueDoTapete = collision.GetComponent<TapeteHue>();

        if (hueDoTapete == null)
        {
            return;
        }

        tapetesEmContato.Remove(hueDoTapete);
        tapeteAtual = tapetesEmContato.Count > 0 ? tapetesEmContato[tapetesEmContato.Count - 1] : null;
        camuflar.DefinirNoTapete(tapeteAtual != null);
    }

    public bool EstaComHueDoTapete(float tolerancia)
    {
        if (materialDoJogador == null || tapeteAtual == null || !TemHue(materialDoJogador))
        {
            return false;
        }

        float diferenca = Mathf.Abs(Mathf.DeltaAngle(materialDoJogador.GetFloat(PropriedadeHue), tapeteAtual.Hue));
        return diferenca <= tolerancia;
    }

    private void RegistrarTapete(Collider2D collision)
    {
        if (camuflar == null || !collision.CompareTag("Tapete"))
        {
            return;
        }

        TapeteHue hueDoTapete = collision.GetComponent<TapeteHue>();

        if (hueDoTapete == null)
        {
            return;
        }

        if (!tapetesEmContato.Contains(hueDoTapete))
        {
            tapetesEmContato.Add(hueDoTapete);
        }

        tapeteAtual = hueDoTapete;
        camuflar.DefinirNoTapete(true);
    }

    private static bool TemHue(Material material)
    {
        return material != null && material.HasProperty(PropriedadeHue);
    }

    private static void AproximarHue(Material material, float hueAlvo, float passo)
    {
        float hueAtual = material.GetFloat(PropriedadeHue);
        float proximoHue = Mathf.MoveTowardsAngle(hueAtual, hueAlvo, passo);
        material.SetFloat(PropriedadeHue, Mathf.Repeat(proximoHue, 360f));
    }
}
