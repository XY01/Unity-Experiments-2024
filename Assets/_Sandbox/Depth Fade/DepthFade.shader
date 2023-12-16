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
                float3 positionVS  : TEXCOORD0;
            };

            // To make the Unity shader SRP Batcher compatible, declare all
            // properties related to a Material in a a single CBUFFER block with
            // the name UnityPerMaterial.
            CBUFFER_START(UnityPerMaterial)
            // The following line declares the _BaseColor variable, so that you
            // can use it in the fragment shader.
            half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
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
                
                //fragDepthFrac = pow(fragDepthFrac,2.2);
                //float4 col = float4(sceneDepthFrac, sceneWorldPosDepthFrac, fragDepthFrac, 1);
                float4 col = float4(depthDiff, depthDiff, depthDiff, 1);
                
                // Returning the _BaseColor value.
                return col;//_BaseColor;
            }
            ENDHLSL
        }
    }
}