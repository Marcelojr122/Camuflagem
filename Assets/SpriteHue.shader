Shader "Custom/SpriteHue"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Color", Color) = (1,1,1,1)

        [Range(0,360)]
        _Hue ("Hue", Float) = 137.62

        [Range(0,2)]
        _Saturation ("Saturation", Float) = 1

        [Range(0,2)]
        _Brightness ("Brightness", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _Color;
                float _Hue;
                float _Saturation;
                float _Brightness;

            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                OUT.color = IN.color * _Color;

                return OUT;
            }


            // ---------------------------------------------------------
            // RGB → HSV
            // ---------------------------------------------------------

            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);

                float4 p = lerp(
                    float4(c.bg, K.wz),
                    float4(c.gb, K.xy),
                    step(c.b, c.g)
                );

                float4 q = lerp(
                    float4(p.xyw, c.r),
                    float4(c.r, p.yzx),
                    step(p.x, c.r)
                );

                float d = q.x - min(q.w, q.y);

                float e = 1e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }


            // ---------------------------------------------------------
            // HSV → RGB
            // ---------------------------------------------------------

            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(
                    1.0,
                    2.0 / 3.0,
                    1.0 / 3.0,
                    3.0
                );

                float3 p =
                    abs(
                        frac(c.xxx + K.xyz) * 6.0
                        - K.www
                    );

                return c.z *
                    lerp(
                        K.xxx,
                        saturate(p - K.xxx),
                        c.y
                    );
            }


            // ---------------------------------------------------------
            // Fragment
            // ---------------------------------------------------------

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );

                // Mantém transparência
                if (tex.a <= 0.001)
                    discard;


                // Cor original do sprite
                float3 rgb = tex.rgb;


                // -----------------------------------------------------
                // Preserva o preto dos olhos
                // -----------------------------------------------------

                float brightness =
                    max(rgb.r, max(rgb.g, rgb.b));

                if (brightness < 0.05)
                {
                    return half4(
                        0,
                        0,
                        0,
                        tex.a * IN.color.a
                    );
                }


                // -----------------------------------------------------
                // RGB → HSV
                // -----------------------------------------------------

                float3 hsv = RGBToHSV(rgb);


                // -----------------------------------------------------
                // Altera Hue
                //
                // Unity trabalha com Hue entre 0 e 1.
                // O Inspector trabalha entre 0 e 360 graus.
                // -----------------------------------------------------

                hsv.x += _Hue / 360.0;

                hsv.x = frac(hsv.x);


                // -----------------------------------------------------
                // Saturação
                // -----------------------------------------------------

                hsv.y *= _Saturation;

                hsv.y = saturate(hsv.y);


                // -----------------------------------------------------
                // Brilho
                // -----------------------------------------------------

                hsv.z *= _Brightness;


                // -----------------------------------------------------
                // HSV → RGB
                // -----------------------------------------------------

                rgb = HSVToRGB(hsv);


                // Aplica cor do SpriteRenderer
                rgb *= IN.color.rgb;


                return half4(
                    rgb,
                    tex.a * IN.color.a
                );
            }

            ENDHLSL
        }
    }
}