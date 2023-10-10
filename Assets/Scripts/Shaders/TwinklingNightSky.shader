Shader "Custom/TwinklingNightSky"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.0, 1.0)) = 0.5
        _TwinkleSpeed ("Twinkle Speed", Range(0.1, 10.0)) = 1.0
        _TwinkleAmount ("Twinkle Amount", Range(0.0, 1.0)) = 0.2
        _Seed ("Seed", Range(0.0, 100.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Transparent" }

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
                fixed4 color: COLOR;
                float4 customData : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 customData : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _Brightness;
            float _TwinkleSpeed;
            float _TwinkleAmount;
            float _Seed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.customData = v.customData;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate the random value for this pixel based on its position and the seed
                float2 pixelPos = i.uv * _ScreenParams.xy;
                float randValue = frac(sin(dot(pixelPos, float2(12.9898, 78.233))) * i.customData.x);

                //float speed = _TwinkleSpeed ;
                float amount = _TwinkleAmount;
                float speed = i.customData.y;
                //float amount = i.customData.z;


                // Calculate the brightness of the pixel based on the twinkle speed, amount, and random value
                float twinkleBrightness = _Brightness + (amount * sin(_Time.y * speed + randValue));

                // Sample the texture and multiply by the twinkle brightness
                fixed4 tex = tex2D(_MainTex, i.uv) * twinkleBrightness;

                // Output the final color
                return tex * i.color;
            }
            ENDCG
        }
    }
}