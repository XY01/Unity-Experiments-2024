using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class VectorfieldComputeVFX : MonoBehaviour
{
    public ComputeShader vectorFieldShader;
    public VisualEffect visualEffect;
    private GraphicsBuffer vectorFieldBuffer;
    private int intiHandle;

    // Vector field dimensions
    public int width = 256;
    public int height = 256;
    public int depth = 256;

    private int3 _threadCount;

    void Start() 
    {
        SetupShader();
    }

    [ContextMenu("Setup Shader")]
    void SetupShader()
    {
        // Find the kernel
        intiHandle = vectorFieldShader.FindKernel("InitVectorfield");

        // Create a GraphicsBuffer
        int totalSize = width * height * depth;
        vectorFieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(float) * 4);

        Vector4[] vectorFieldData = new Vector4[totalSize];
        for(int i = 0; i < totalSize; i++)
        {
            vectorFieldData[i] = new Vector4(0, 1, 0);
        }
        vectorFieldBuffer.SetData(vectorFieldData);
        
        // Set the buffer and dimensions in the compute shader
        vectorFieldShader.SetBuffer(intiHandle, "VectorfieldBuffer", vectorFieldBuffer);
        vectorFieldShader.SetInts("Dimensions", new int[] { width, height, depth });
        //vectorFieldShader.SetVector("Dimensions", new Vector3( width, height, depth ));

        visualEffect.Stop();
        visualEffect.SetVector3("Dimensions", new Vector3( width, height, depth ));
        // Set the buffer for the visual effect
        visualEffect.SetGraphicsBuffer("VectorfieldBuffer", vectorFieldBuffer);
        visualEffect.Play();

        _threadCount = new int3(
            Mathf.CeilToInt(width / 8f),
            Mathf.CeilToInt(height / 8f),
            Mathf.CeilToInt(depth / 8f));
        
        Debug.Log($"Thread count: {_threadCount}");
    }

    [ContextMenu("Dispatch")]
    private void Dispatch()
    {
        // Dispatch the compute shader
        vectorFieldShader.Dispatch(intiHandle,_threadCount.x, _threadCount.y, _threadCount.z);
    }

    void OnDestroy() {
        // Release the buffer when the object is destroyed
        if (vectorFieldBuffer != null) {
            vectorFieldBuffer.Release();
        }
    }
}