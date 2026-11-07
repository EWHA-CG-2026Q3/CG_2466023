Shader "CG/RS_Grass"
{
    Properties
    {
        _RootColor ("Root Color", Color) = (0.08, 0.20, 0.04, 1)
        _TipColor ("Tip Color", Color) = (0.52, 0.78, 0.24, 1)
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.35
        _WindDirection ("Wind Direction (x, z)", Vector) = (1, 0.4, 0, 0)
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.12
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.6
        _WindWaveScale ("Wind Wave Scale (1/m)", Range(0.01, 2)) = 0.35
        _Ambient ("Ambient", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        // 두 Pass가 함께 쓰는 코드 (값, 자료형, vert)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RootColor;
            float4 _TipColor;
            float _ColorVariation;
            float4 _WindDirection;
            float _WindStrength;
            float _WindSpeed;
            float _WindWaveScale;
            float _Ambient;
        CBUFFER_END

        struct Attributes                       // 메시에서 vert로 들어오는 값
        {
            float3 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;      // uv.y = 잎에서의 높이 (0 ~ 1)
            float4 color      : COLOR;          // r = 흔들림 어긋남, g = 색 차이
        };

        struct Varyings                         // vert에서 frag로 넘기는 값
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS   : TEXCOORD0;
            float2 uv         : TEXCOORD1;
            float  tint       : TEXCOORD2;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;

            // 1. 물체 좌표 → 월드 좌표 (M)
            float3 positionWS = TransformObjectToWorld(input.positionOS);

            // 2. 바람: 방향 dir으로, 시간에 따라 물결치며 밀어냄
            float h = input.uv.y;                                         // 뿌리 0, 끝 1
            float phase = input.color.r * 6.2831853;                      // 잎마다 다른 어긋남
            float2 dir = normalize(_WindDirection.xy + float2(1e-4, 0));  // (x, z) 방향
            float travel = dot(positionWS.xz, dir) * _WindWaveScale * 6.2831853;   // 바람이 지나가는 물결
            float wave = sin(_Time.y * _WindSpeed - travel + phase * 0.3);          // 큰 물결 (−1 ~ 1)
            float flutter = sin(_Time.y * _WindSpeed * 3.1 + phase) * 0.15;         // 잎마다 잔떨림
            float sway = (0.6 + 0.4 * wave + flutter) * _WindStrength * h * h;      // 끝일수록 크게 (h²)

            positionWS.xz += dir * sway;               // 바람 방향으로 밀기
            positionWS.y  -= abs(sway) * 0.35 * h;     // 눕는 만큼 살짝 낮아지게 (잎이 늘어나 보이지 않게)

            // 3. 월드 좌표 → Clip Space (P × V)
            output.positionCS = TransformWorldToHClip(positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = input.uv;
            output.tint = input.color.g;
            return output;
        }
        ENDHLSL

        // Pass 1: 화면에 색을 그림
        Pass
        {
            Name "GrassForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off   // 풀잎은 얇은 판이라 뒷면도 그려야 함

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 frag(Varyings input) : SV_Target
            {
                // 뿌리는 어둡게, 끝은 밝게
                float3 albedo = lerp(_RootColor.rgb, _TipColor.rgb, input.uv.y);
                albedo *= 1 + (input.tint - 0.5) * _ColorVariation;   // 잎마다 조금씩 다른 색

                // 간단한 조명: 확산광 dot(N, L)을 0 ~ 1로 부드럽게 (half Lambert)
                Light mainLight = GetMainLight();
                float3 N = normalize(input.normalWS);
                float diffuse = dot(N, mainLight.direction) * 0.5 + 0.5;
                float3 color = albedo * (_Ambient + diffuse * mainLight.color);
                return float4(color, 1);
            }
            ENDHLSL
        }

        // Pass 2: 깊이만 기록 (URP가 깊이를 먼저 그리는 설정일 때도 풀이 사라지지 않게)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth

            half fragDepth(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}