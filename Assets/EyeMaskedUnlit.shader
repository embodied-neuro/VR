// Shader "Custom/EyeMaskedUnlit"
// {
//     Properties
//     {
//         _MainTex ("Texture", 2D) = "white" {}
//         _TargetEye ("Target Eye (0=Left, 1=Right)", Float) = 0
//         _Contrast("Contrast", Range(0,1)) = 1 
//     }
//     SubShader
//     {
//         Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
//         Pass
//         {
//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #pragma multi_compile_instancing
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//                 UNITY_VERTEX_INPUT_INSTANCE_ID
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 vertex : SV_POSITION;
//                 UNITY_VERTEX_OUTPUT_STEREO
//             };
//             TEXTURE2D(_MainTex);
//             SAMPLER(sampler_MainTex);

//             // float4 _MainTex_ST;
            
//             // sampler2D _MainTex;
//             // float4 _MainTex_ST;
//             float _TargetEye;
//             float _Contrast; 

        
//             v2f vert (appdata v)
//             {
//                 v2f o = (v2f)0;
//                 UNITY_SETUP_INSTANCE_ID(v);
//                 UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//                 o.vertex = TransformObjectToHClip(v.vertex.xyz);
//                 o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                 return o;
//             }

//             half4 frag (v2f i) : SV_Target
//             {
//                 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
//                 if (unity_StereoEyeIndex != _TargetEye)
//                 {
//                     discard;
//                 }
//                 //half4 color = tex2d(_MainTex, i.uv);
//                 half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
//                 color.rgb = (color.rgb - 0.5) * _Contrast + 0.5;
//                 return color; 
//             }
//             ENDHLSL
//         }
//     }
// }

Shader "Custom/EyeMaskedUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetEye ("Target Eye (0=Left, 1=Right)", Float) = 0
        _Contrast ("Contrast", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;
                float _TargetEye;
                float _Contrast;

            CBUFFER_END


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };


            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }


            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


                // Show stimulus only in assigned eye
                if (unity_StereoEyeIndex != _TargetEye)
                {
                    discard;
                }


                half4 color = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    i.uv
                );


                // Contrast manipulation
                color.rgb = (color.rgb - 0.5) * _Contrast + 0.5;


                return color;
            }


            ENDHLSL
        }
    }
}