using Unity.VisualScripting;
using UnityEngine;

public class MudarCor : MonoBehaviour
{
    private Renderer meuRender;

    [Range(0, 360)] private float hue;

    private float tempo = 0;
    private bool estaFora = true;

    [Range(0, 1)] public float velocidadeMudaCor = 0.01f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meuRender = GetComponent<Renderer>();
        hue = meuRender.material.GetFloat("_Hue"); 
    }

    // Update is called once per frame
    void Update()
    {
        if (estaFora && tempo >= velocidadeMudaCor)
        {
            MudandoCor(gameObject);
            tempo = 0;
        }

        tempo += Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        estaFora = false;
        if (tempo >= velocidadeMudaCor && CompareTag("Tapete"))
        {
            Debug.Log("Está no circulo");

            MudandoCor(collision.gameObject);
            
            tempo = 0;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        estaFora = true;
    }
    
    private void MudandoCor(GameObject objeto)
    {
        var jHue = objeto.GetComponent<Renderer>().material.GetFloat("_Hue");
        if (hue > jHue)
        {
            jHue += 1;
        }
        else if (hue < jHue)
        {
            jHue -= 1;
        }

        objeto.GetComponent<Renderer>().material.SetFloat("_Hue", jHue);
    }
}


/*
    (MudarCor)
    private SpriteRenderer meuRender;
    private float red;
    private float blue;
    private float green;  

        (Start)
        meuRender = GetComponent<SpriteRenderer>();
        red = meuRender.color.r * 255;
        green = meuRender.color.g * 255;
        blue = meuRender.color.b * 255;

        
        (MudandoCor)
        float jR = objeto.GetComponent<SpriteRenderer>().color.r * 255;
        float jG = objeto.GetComponent<SpriteRenderer>().color.g * 255;
        float jB = objeto.GetComponent<SpriteRenderer>().color.b * 255;

        if (red > jR)
        {
            jR += 1;
        }
        else if (red < jR)
        {
            jR -= 1;
        }

        if (green > jG)
        {
            jG += 1;
        }
        else if (green < jG)
        {
            jG -= 1;
        }

        if (blue > jB)
        {
            jB += 1;
        }
        else if (blue < jB)
        {
            jB -= 1;
        }

        objeto.GetComponent<SpriteRenderer>().color = new Color(jR / 255f, jG / 255f, jB / 255f);
 */