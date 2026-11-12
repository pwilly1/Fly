Shader "Fly/Enchanted River"
{
    Properties
    {
        _BaseMap ("Painted water", 2D) = "white" {}
        _Tint ("Water tint", Color) = (1, 1, 1, 1)
        _FlowSpeed ("Flow speed", Range(0, 0.1)) = 0.055
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "River"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 bank : TEXCOORD1;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float bank : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Tint;
                float _FlowSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.bank = input.bank.x;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float ripple = sin(input.uv.y * 9.0 + input.uv.x * 13.0 + _Time.y * 0.9) * 0.012;
                float2 flow = float2(ripple, _Time.y * _FlowSpeed);
                half3 painted = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + flow).rgb;
                half3 detail = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * 1.73 + float2(0.31, -0.17) + flow * 0.55).rgb;
                half3 water = lerp(painted, detail, 0.18) * _Tint.rgb * input.color.rgb;
                float bankDistance = min(input.bank, 1.0 - input.bank);
                float foam = (1.0 - smoothstep(0.07, 0.17, bankDistance)) * 0.18;
                water = lerp(water, half3(0.72, 0.96, 0.88), foam);
                return half4(water, _Tint.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
