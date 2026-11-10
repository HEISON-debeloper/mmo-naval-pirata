// Gotas de lluvia: sin luz, mezcla con transparencia y usa el color de cada partícula
Shader "Pirata/Lluvia"
{
    Properties { _Color ("Color", Color) = (1, 1, 1, 0.55) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                // se desvanece hacia los bordes y las puntas de la raya
                float bu = 1.0 - abs(i.uv.x * 2.0 - 1.0), bv = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                fixed4 c = i.color; c.a *= saturate(min(bu, bv) * 2.0);
                return c;
            }
            ENDCG
        }
    }
}
