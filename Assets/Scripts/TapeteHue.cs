using UnityEngine;

[DisallowMultipleComponent]
public class TapeteHue : MonoBehaviour
{
    [Range(0f, 360f)]
    [SerializeField] private float hue;

    public float Hue => hue;

    public void Configurar(float novoHue)
    {
        hue = Mathf.Repeat(novoHue, 360f);
    }
}
