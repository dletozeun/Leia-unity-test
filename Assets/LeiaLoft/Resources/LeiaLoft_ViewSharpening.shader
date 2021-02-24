/****************************************************************
*
* Copyright 2019 © Leia Inc.  All rights reserved.
*
* NOTICE:  All information contained herein is, and remains
* the property of Leia Inc. and its suppliers, if any.  The
* intellectual and technical concepts contained herein are
* proprietary to Leia Inc. and its suppliers and may be covered
* by U.S. and Foreign Patents, patents in process, and are
* protected by trade secret or copyright law.  Dissemination of
* this information or reproduction of this materials strictly
* forbidden unless prior written permission is obtained from
* Leia Inc.
*
****************************************************************
*/
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "LeiaLoft/ViewSharpening"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        enable ("IsEnabled", float) = 1.0
        _Color ("Color", Color) = (1,1,1,1)
        sharpening_x ("SharpeningX", Vector) = (0.0, 0.0, 0.0, 0.0)
        sharpening_y ("SharpeningY", Vector) = (0.0, 0.0, 0.0, 0.0)
        sharpening_center("SharpeningCenter", float) = 1.0
        sharpening_x_size ("SharpeningXSize", float) = 0.0
        sharpening_y_size ("SharpeningYSize", float) = 2.0
        gamma ("Gamma", float) = 2.0
    }

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "ViewSharpening"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Hydrogen4View is currently expected to do normalization in its shader.
            // So add additional ops inside of compile guards.
            #pragma multi_compile __ Hydrogen4View

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            uniform float enable;
            uniform float4 _MainTex_TexelSize;
            uniform float4 _Color;

            float4 sharpening_x;
            float4 sharpening_y;
            float sharpening_center;
            float sharpening_x_size;
            float sharpening_y_size;
            float gamma;

            float GetTextureWidth()
            {
                return _MainTex_TexelSize.z;
            }

            float GetTextureHeight()
            {
                return _MainTex_TexelSize.w;
            }

            half3 GammaToLinear(half3 col)
            {
                return pow(col, gamma);
            }

            half3 LinearToGamma(half3 col)
            {
                return pow(col, 1.0 / gamma);
            }

            half3 texture_offset(float2 uv, int2 offset) {
                float2 uv_offset = uv + (float2(offset) / float2(GetTextureWidth(), GetTextureHeight()));
                return tex2D(_MainTex, uv_offset).rgb;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 final_color = fixed4(0, 1, 0, 1);
                final_color.rgb = sharpening_center * GammaToLinear(texture_offset(i.uv, int2(0, 0)));
                float normalizer = 1.0;

                for (int j = 0; j < sharpening_x_size; ++j)
                {
                    final_color.rgb -= sharpening_x[j] * GammaToLinear(texture_offset(i.uv, int2(j + 1, 0)));
                    final_color.rgb -= sharpening_x[j] * GammaToLinear(texture_offset(i.uv, int2(-j - 1, 0)));
// in some cases, normalization occurs in C#
#ifdef Hydrogen4View
                    normalizer -= 2.0 * sharpening_x[j];
#endif
                }
                for (int k = 0; k < sharpening_y_size; ++k)
                {
                    final_color.rgb -= sharpening_y[k] * GammaToLinear(texture_offset(i.uv, int2(0, k + 1)));
                    final_color.rgb -= sharpening_y[k] * GammaToLinear(texture_offset(i.uv, int2(0, -k - 1)));
#ifdef Hydrogen4View
                    normalizer -= 2.0 * sharpening_y[k];
#endif
                }

#ifdef Hydrogen4View
                final_color.rgb /= normalizer;
#endif

                final_color.rgb = clamp(final_color.rgb, 0.0, 1.0);
                final_color.rgb = LinearToGamma(final_color.rgb);

                return final_color;
            }
            
            ENDCG
        }
    }
}