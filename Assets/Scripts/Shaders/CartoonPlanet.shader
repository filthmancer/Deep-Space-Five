Shader "Custom/CartoonPlanetShader" {
    Properties
    {
        _Color("Color", Color) = (0.5, 0.65, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}	
        _MainTextScrollSpeedX("Main Texture Scroll Speed (X)", float) = 0
        _MainTextScrollSpeedY("Main Texture Scroll Speed(Y)", float) = 0
        [HDR]
        _AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
        _ToonShadingThreshold("Toon Smoothing", Range(0,1))= 0.1
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _SpecularSmoothing("Specular Smoothing", Range(0,1))=0.05
        _Glossiness("Glossiness", Float) = 32

        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        _RimSmoothing("Rim Smoothing", Range(0,1))=0.05
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
                "PassFlags" = "OnlyDirectional"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;				
                float4 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                o.viewDir = WorldSpaceViewDir(v.vertex);

                return o;
            }
            
            float4 _Color;
            float4 _AmbientColor;
            float _Glossiness;
            float4 _SpecularColor;
            float _SpecularSmoothing;
            float4 _RimColor;
            float _RimAmount;
            float _RimThreshold;
            float _ToonShadingThreshold;
            float _RimSmoothing;
            float _MainTextScrollSpeedX;
            float _MainTextScrollSpeedY;


            float4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);

                float NdotL = dot(_WorldSpaceLightPos0, normal);
                float lightIntensity = smoothstep(0, _ToonShadingThreshold, NdotL);
                float4 light = lightIntensity * _LightColor0;

                float3 viewDir = normalize(i.viewDir);
                float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
                float NdotH = dot(normal, halfVector);
                
                float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, _SpecularSmoothing, specularIntensity);
                float4 specular = specularIntensitySmooth * _SpecularColor;

                float4 rimDot = 1 - dot(viewDir, normal);
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - _RimSmoothing, _RimAmount + _RimSmoothing, rimIntensity);
                float4 rim = rimIntensity * _RimColor;

                fixed2 scrolledUV = i.uv;
                fixed scrollX = _MainTextScrollSpeedX * _Time;
                fixed scrollY = _MainTextScrollSpeedY * _Time;

                scrolledUV += fixed2(scrollX, scrollY);

                float4 sample = tex2D(_MainTex, scrolledUV);

                return _Color * sample * (_AmbientColor + light + specular + rim);
            }
            ENDCG
        }
    }
}
