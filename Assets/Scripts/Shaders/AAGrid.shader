Shader "Custom/AAGrid"
{
    Properties
    {
        _GridColour ("Grid Colour", color) = (1, 1, 1, 1)
        _BaseColour ("Base Colour", color) = (1, 1, 1, 0)
        _GridSpacing ("Grid Spacing", float) = 0.1
        _LineThickness ("Line Thickness", float) = 1        
        _SpokeAmount ("Spokes", float) = 10  

        _FadeMultipler("Fade Multiplier", float) = 1     
        _FadeMaximum("Fade Max", float)=15
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            fixed4 _GridColour;
            fixed4 _BaseColour;
            float _GridSpacing;
            float _LineThickness;
            float _SpokeAmount;
            float _FadeMaximum;
            float _FadeMultipler;

            float Pingpong(float speed)
            {
                int remainder = fmod(floor(_Time.y*speed), 2);
                return remainder==1?1-frac(_Time.y*speed):frac(_Time.y*speed);
            }
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.uv = mul(unity_ObjectToWorld, v.vertex).xz;

                float4 objectOrigin = unity_ObjectToWorld[3];
                float4 dist = length(objectOrigin-v.vertex);

                o.worldPos = mul(unity_ObjectToWorld,objectOrigin - v.vertex) / _GridSpacing;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {        
                // Pick a coordinate to visualize in a grid
                float pi = 3.141592653589793;
                float scale = _SpokeAmount;
                //float4 worldPos = float4(0,0,0,1) + i.localPos;
                float2 coord = float2(length(i.worldPos.xz), atan2(i.worldPos.x, i.worldPos.z) * scale / pi);

                //coord = mul(unity_ObjectToWorld, float2(0,0));

                // Handling the wrap-around is tricky in this case. The function atan()
                // is not continuous and jumps when it wraps from -pi to pi. The screen-
                // space partial derivative will be huge along that boundary. To avoid
                // this, compute another coordinate that places the jump at a different
                // place, then use the coordinate where the jump is farther away.
                //
                // When doing this, make sure to always evaluate both fwidth() calls even
                // though we only use one. All fragment shader threads in the thread group
                // actually share a single instruction pointer, so threads that diverge
                // down different conditional branches actually cause both branches to be
                // serialized one after the other. Calling fwidth() from a thread next to
                // an inactive thread ends up reading inactive registers with old values
                // in them and you get an undefined value.
                // 
                // The conditional uses +/-scale/2 since coord.y has a range of +/-scale.
                // The jump is at +/-scale for coord and at 0 for wrapped.
                float2 wrapped = float2(coord.x, frac(coord.y / (2.0 * scale)) * (2.0 * scale));
                float2 coordWidth = fwidth(coord);
                float2 wrappedWidth = fwidth(wrapped);
                float2 width = coord.y < -scale * 0.5 || coord.y > scale * 0.5 ? wrappedWidth : coordWidth;

                float2 range = abs(frac(coord-0.5F)-0.5F);

                float2 speeds = fwidth(coord);
                speeds = coord.y < -scale * 0.5 || coord.y > scale * 0.5 ? wrappedWidth : coordWidth;
                /* // Euclidean norm gives slightly more even thickness on diagonals
                float4 deltas = float4(ddx(i.uv), ddy(i.uv));
                speeds = sqrt(float2(
                dot(deltas.xz, deltas.xz),
                dot(deltas.yw, deltas.yw)
                ));
                */  // Cheaper Manhattan norm in fwidth slightly exaggerates thickness of diagonals

                float2 pixelRange = range/speeds;
                float lineWeight = saturate(min(pixelRange.x, pixelRange.y) - _LineThickness);
                
                float4 gridCol = _GridColour;
                float falloff = smoothstep(0.0, _FadeMaximum,length(i.worldPos)* _FadeMultipler);
                gridCol -= (falloff);
                return lerp(gridCol, _BaseColour, lineWeight);
            }

            
            ENDCG
        }
    }
}
