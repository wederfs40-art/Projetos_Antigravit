Shader "Custom/BodycamLensEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Distortion ("Lens Fisheye Distortion", Float) = 0.18
        _CubicDistortion ("Cubic Distortion", Float) = 0.08
        _ChromAberration ("Chromatic Aberration", Float) = 0.006
        _VignetteRoundness ("Vignette Roundness", Float) = 1.15
        _VignettePower ("Vignette Power", Float) = 1.6
        _Exposure ("Camera Exposure", Float) = 1.12
        _Contrast ("Tactical Contrast", Float) = 1.18
        _Saturation ("Color Saturation", Float) = 0.88
        _FilmGrain ("Sensor Noise Grain", Float) = 0.022
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distortion;
            float _CubicDistortion;
            float _ChromAberration;
            float _VignetteRoundness;
            float _VignettePower;
            float _Exposure;
            float _Contrast;
            float _Saturation;
            float _FilmGrain;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // ACES Film Tone Mapping
            float3 ACESFilm(float3 x)
            {
                float a = 2.51f;
                float b = 0.03f;
                float c = 2.43f;
                float d = 0.59f;
                float e = 0.14f;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            float2 DistortUV(float2 uv)
            {
                float2 coord = uv - 0.5;
                float r2 = coord.x * coord.x + coord.y * coord.y;
                float f = 1.0 + r2 * (_Distortion + _CubicDistortion * sqrt(r2));
                return coord * f + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centerUV = DistortUV(i.uv);

                // Fora dos limites da lente de grande angular
                if (centerUV.x < 0.0 || centerUV.x > 1.0 || centerUV.y < 0.0 || centerUV.y > 1.0)
                {
                    return fixed4(0.01, 0.01, 0.01, 1.0);
                }

                // Aberração Cromática radial da lente grande angular
                float2 uvDist = centerUV - 0.5;
                float2 uvR = centerUV + uvDist * _ChromAberration;
                float2 uvB = centerUV - uvDist * _ChromAberration;

                fixed r = tex2D(_MainTex, uvR).r;
                fixed g = tex2D(_MainTex, centerUV).g;
                fixed b = tex2D(_MainTex, uvB).b;
                float3 col = float3(r, g, b);

                // Exposição e Contraste Tático
                col *= _Exposure;
                col = ((col - 0.5f) * _Contrast) + 0.5f;

                // Dessaturação leve estilo Bodycam policial
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(luma, luma, luma), col, _Saturation);

                // Curva ACES
                col = ACESFilm(col);

                // Vinheta óptica esférica de câmera no colete
                float2 vCoord = (i.uv - 0.5) * float2(_VignetteRoundness, 1.0);
                float vDist = length(vCoord) * 2.0;
                float vignette = saturate(1.0 - pow(vDist * 0.55, _VignettePower));
                col *= vignette;

                // Ruído de sensor CMOS (Bodycam Grain)
                float noise = (frac(sin(dot(i.uv + frac(_Time.y * 12.0), float2(12.9898, 78.233))) * 43758.5453) - 0.5) * _FilmGrain;
                col += noise;

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
}
