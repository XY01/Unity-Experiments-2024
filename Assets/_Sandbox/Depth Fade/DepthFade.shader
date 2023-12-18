// This shader fills the mesh shape with a color that a user can change using the
// Inspector window on a Material.
Shader "Example/URPDepthFade"
{
    // The _BaseColor variable is visible in the Material's Inspector, as a field
    // called Base Color. You can use it to select a custom color. This variable
    // has the default value (1, 1, 1, 1).
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _FadeDist("Fade Distance", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                // Homogeneous Clip Space
                float4 positionHCS  : SV_POSITION;
                // View Space
                half3 positionVS  : TEXCOORD0;
            };

            // To make the Unity shader SRP Batcher compatible, declare all
            // properties related to a Material in a a single CBUFFER block with
            // the name UnityPerMaterial.
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _FadeDist;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Method 0 - Use the matracies to transform the obj space > world > view
                // float3 positionWS = mul(UNITY_MATRIX_M, float4(IN.positionOS.xyz, 1.0)).xyz;
                // OUT.positionVS = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;

                // Method 1 - Use the helper functions to transform the obj space > world > view
                float3 world = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionVS = TransformWorldToView(world);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // To calculate the UV coordinates for sampling the depth buffer,
                // divide the pixel location by the render target resolution
                // _ScaledScreenParams.
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                real rawDepth = SampleSceneDepth(UV);
                
                // Eye depth is in world units from the camera position plane (Not near plane)
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // Reconstruct the world space positions from depth.
                float3 worldPos = ComputeWorldSpacePosition(UV, rawDepth, UNITY_MATRIX_I_VP);


                
                float fragDepthFrac = frac(-IN.positionVS.z);
                float sceneDepthFrac = frac(sceneEyeDepth);
                float sceneWorldPosDepthFrac = frac(worldPos.z);

                float depthDiff = saturate(abs(-IN.positionVS.z - sceneEyeDepth));
                float depthIntersect = pow(1 - depthDiff,_FadeDist);
                
                 float4 col;
                // Multi channel depth frac
                //col = float4(sceneDepthFrac, sceneWorldPosDepthFrac, fragDepthFrac, 1);

                // Depth Diff
                //col = float4(depthDiff, depthDiff, depthDiff, 1);

                // Depth Intersect
                col = float4(depthIntersect, depthIntersect, depthIntersect, 1);
                
                // Returning the _BaseColor value.
                return col;//_BaseColor;
            }
            ENDHLSL
        }
    }
}