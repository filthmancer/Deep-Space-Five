Shader "Custom/CartoonRings"
{
    //https://www.shadertoy.com/view/wsfXDl
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GapRatio ("Gap Ratio", float) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
            float _GapRatio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv -= 0.5F;
                uv *= 2.0F;

                float r = length(uv);
                const float center = _GapRatio;
                float angle = atan2(uv.x, uv.y) + _Time.x;

                float vlength = (r-center)/(1.0F-center);

                float2 anim = float2((sin(angle)+1.0), vlength*4);

                // sample the texture
                fixed4 col = tex2D(_MainTex, anim);
                col *= step(0.01, vlength) * step(vlength, 0.99);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
