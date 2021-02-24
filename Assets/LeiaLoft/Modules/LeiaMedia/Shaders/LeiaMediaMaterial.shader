Shader "LeiaLoft/LeiaMaterial"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;     

            float _ColCount;
            float _RowCount;   
            float _LeiaViewID;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            float2 remap(float2 orig_uv) {
                float cam_ind = _ColCount * _RowCount - 1 - _LeiaViewID;

                float xoffset = _ColCount - 1.0 - fmod(cam_ind, _ColCount);
                float yoffset = floor(cam_ind / _ColCount);
                
                return((orig_uv + float2(xoffset, yoffset)) / float2(_ColCount, _RowCount));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 nview_uvs = remap(i.uv);
                fixed4 col = tex2D(_MainTex, nview_uvs);

                // addresses a reported issue with pixel hue in LeiaMediaViewer being warmer than in VLC.
                // per-pixel differences between LeiaMediaViewer and VLC were computed in ImageJ and 
                // a low-error gamma function was calculated that transforms LeiaMediaViewer output into 
                // VLC output

                col = pow(col, 1.0 / 0.9953616);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            
        ENDCG
        }
    }
    // FallBack "Diffuse"
}