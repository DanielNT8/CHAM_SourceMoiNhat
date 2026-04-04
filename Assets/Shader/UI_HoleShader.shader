Shader "UI/UI_HoleShader"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,0.85) // Màu màn đêm (Độ mờ 85%)
        _Center ("Hole Center (Viewport)", Vector) = (0.5, 0.5, 0, 0) // Tâm lỗ
        _Radius ("Hole Radius", Range(0, 1)) = 0.05 // Độ to
        _Softness ("Edge Softness", Range(0, 1)) = 0.05 // Độ mờ viền
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex); 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 viewportPos = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 currentPos = float2(viewportPos.x * aspect, viewportPos.y);
                float2 centerPos = float2(_Center.x * aspect, _Center.y);

                float dist = distance(currentPos, centerPos);
                // Viền mượt mà (Tạo cảm giác sương mù)
                float alpha = smoothstep(_Radius, _Radius + _Softness, dist);
                
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}