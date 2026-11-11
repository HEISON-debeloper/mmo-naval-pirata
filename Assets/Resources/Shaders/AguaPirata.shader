// Agua del mar (URP): el mismo aspecto que el material Lit de antes (color, brillo, sombras, niebla),
// y además NO se dibuja dentro del casco de los barcos cercanos. Sin esto, el mar se veía adentro
// (por las portas, bajo cubierta, en la bodega) como si el barco se estuviera inundando.
// Las olas se mueven en la tarjeta de video (vértice a vértice, con la misma fórmula que Olas.OlaBase) y las
// caras planas salen de las derivadas de la posición: el procesador ya no recalcula 60 000 vértices por cuadro.
Shader "Pirata/Agua"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.18, 0.54, 0.6, 1)
        _Smoothness("Brillo", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
        CBUFFER_END

        // Hasta 8 cascos (los pone AguaOlas cada cuadro)
        float4x4 _CascoALocal[8];   // del mundo al espacio del barco
        float4 _CascoForma[8];      // x = medio ancho, y = alto del casco, z = popa (z), w = punta de la proa (z)
        float4 _CascoForma2[8];     // x = donde empieza a cerrarse la proa (z), y = margen
        int _CascosN;

        // Olas (las pone AguaOlas): tiempo, fuerza y la calma junto a las islas guardada en una textura
        float _OlasT, _OlasInten, _OlasGPU;
        float4 _OlasDesp;      // lo que corrió el origen flotante
        float4 _OlasRejilla;   // x = medio tamaño, y = 1 / celda, z = 1 / lado
        TEXTURE2D(_OlasCalma); SAMPLER(sampler_OlasCalma);

        float OlaBase(float x, float z, float t)
        {
            return 0.6 * sin(0.08 * x + t * 0.8) + 0.4 * sin(0.11 * z + t * 1.1) + 0.25 * sin(0.06 * (x + z) + t * 0.6);
        }

        // Posición en el mundo del vértice ya movido por la ola
        float3 PosConOla(float3 local)
        {
            float3 w = TransformObjectToWorld(local);
            if (_OlasGPU > 0.5)
            {
                float2 celda = floor((local.xz + _OlasRejilla.x) * _OlasRejilla.y + 0.5);
                float2 uv = (celda + 0.5) * _OlasRejilla.z;
                float calma = SAMPLE_TEXTURE2D_LOD(_OlasCalma, sampler_OlasCalma, uv, 0).r;
                w.y += OlaBase(w.x + _OlasDesp.x, w.z + _OlasDesp.z, _OlasT) * _OlasInten * calma;   // olas del mundo "de verdad" (origen flotante)
            }
            return w;
        }

        // Normal de la cara (aspecto facetado), hacia arriba
        float3 NormalCara(float3 posMundo, float3 deMalla)
        {
            if (_OlasGPU < 0.5) return normalize(deMalla);
            float3 n = normalize(cross(ddy(posMundo), ddx(posMundo)));
            return n.y < 0 ? -n : n;
        }

        // Medio ancho del casco a la altura y en el punto z (la misma fórmula que CascoProcedural.Costado)
        float AnchoCasco(float y, float z, float4 f, float4 g)
        {
            float hw = f.x, h = f.y, zp = f.z, zq = f.w, z0 = g.x;
            float s = saturate((z - zp) / max(zq - zp, 0.01));
            float a = 1.0;
            float arriba = h * 0.5, abajo = -h * 0.5;   // la quilla sube en la proa y un poco en la popa
            if (s < 0.08) a = lerp(0.82, 1.0, s / 0.08);
            if (s < 0.1) abajo += h * 0.12 * (1.0 - s / 0.1);
            if (z > z0) { float t = saturate((z - z0) / max(zq - z0, 0.01)); a *= pow(max(0.0, 1.0 - pow(t, 1.8)), 0.7); abajo += h * 0.6 * t * t; }
            if (y >= arriba) return hw * a;
            if (y <= abajo) return 0.0;
            const float n = 2.6;
            float sy = (arriba - y) / (arriba - abajo);
            float sen = pow(sy, n * 0.5);
            float co = sqrt(max(0.0, 1.0 - sen * sen));
            return hw * a * pow(co, 2.0 / n);
        }

        void RecortarCascos(float3 posMundo)
        {
            for (int k = 0; k < _CascosN; k++)
            {
                float3 l = mul(_CascoALocal[k], float4(posMundo, 1.0)).xyz;
                float4 f = _CascoForma[k], g = _CascoForma2[k];
                if (l.z <= f.z + g.y || l.z >= f.w - g.y || l.y >= f.y * 0.5) continue;
                if (abs(l.x) < AnchoCasco(l.y, l.z, f, g) - g.y) discard;
            }
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Entrada { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Salida { float4 pos : SV_POSITION; float3 posMundo : TEXCOORD0; float3 normal : TEXCOORD1; float niebla : TEXCOORD2; };

            Salida vert(Entrada i)
            {
                Salida o;
                o.posMundo = PosConOla(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.posMundo);
                o.normal = TransformObjectToWorldNormal(i.normal);
                o.niebla = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                RecortarCascos(i.posMundo);
                float3 n = NormalCara(i.posMundo, i.normal);
                InputData d = (InputData)0;
                d.positionWS = i.posMundo;
                d.positionCS = i.pos;
                d.normalWS = n;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.posMundo);
                d.shadowCoord = TransformWorldToShadowCoord(i.posMundo);
                d.fogCoord = i.niebla;
                d.bakedGI = SampleSH(n);
                d.shadowMask = half4(1, 1, 1, 1);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                SurfaceData s = (SurfaceData)0;
                s.albedo = _BaseColor.rgb;
                s.alpha = 1;
                s.smoothness = _Smoothness;
                s.occlusion = 1;
                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, i.niebla);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Entrada { float4 pos : POSITION; };
            struct Salida { float4 pos : SV_POSITION; float3 posMundo : TEXCOORD0; };
            Salida vert(Entrada i) { Salida o; o.posMundo = PosConOla(i.pos.xyz); o.pos = TransformWorldToHClip(o.posMundo); return o; }
            half frag(Salida i) : SV_Target { RecortarCascos(i.posMundo); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Entrada { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Salida { float4 pos : SV_POSITION; float3 posMundo : TEXCOORD0; float3 normal : TEXCOORD1; };
            Salida vert(Entrada i) { Salida o; o.posMundo = PosConOla(i.pos.xyz); o.pos = TransformWorldToHClip(o.posMundo); o.normal = TransformObjectToWorldNormal(i.normal); return o; }
            half4 frag(Salida i) : SV_Target { RecortarCascos(i.posMundo); return half4(NormalCara(i.posMundo, i.normal), 0); }
            ENDHLSL
        }
    }
}
