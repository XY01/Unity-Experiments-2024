using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class VectorfieldComputeVFX : MonoBehaviour
{
    public ComputeShader vectorFieldShader;
    public VisualEffect visualEffect;
    private GraphicsBuffer vectorFieldBuffer;
    private int kernelHandle;

    // Vector field dimensions
    public int width = 256;
    public int height = 256;
    public int depth = 256;

    void Start() 
    {
        SetupShader();
    }

    [ContextMenu("Setup Shader")]
    void SetupShader()
    {
        // Find the kernel
        kernelHandle = vectorFieldShader.FindKernel("CSMain");

        // Create a GraphicsBuffer
        int totalSize = width * height * depth;
        vectorFieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(float) * 4);

        Vector4[] vectorFieldData = new Vector4[totalSize];
        for(int i = 0; i < totalSize; i++)
        {
            vectorFieldData[i] = new Vector4(Mathf.Sin(i * .1f), Mathf.Cos(i * .31f), 0);
        }
        vectorFieldBuffer.SetData(vectorFieldData);
        
        // Set the buffer and dimensions in the compute shader
        vectorFieldShader.SetBuffer(kernelHandle, "VectorfieldBuffer", vectorFieldBuffer);
        vectorFieldShader.SetInts("dimensions", new int[] { width, height, depth });
        //vectorFieldShader.SetVector("Dimensions", new Vector3( width, height, depth ));

        // Set the buffer for the visual effect
        visualEffect.SetGraphicsBuffer("VectorfieldBuffer", vectorFieldBuffer);
        
        Dispatch();
    }

    [ContextMenu("Dispatch")]
    private void Dispatch()
    {
            // Dispatch the compute shader
            vectorFieldShader.Dispatch(kernelHandle,
                Mathf.Max(width / 8, 1),
                Mathf.Max(height / 8, 1),
                Mathf.Max(depth / 8, 1));
        
    }

    void OnDestroy() {
        // Release the buffer when the object is destroyed
        if (vectorFieldBuffer != null) {
            vectorFieldBuffer.Release();
        }
    }
}