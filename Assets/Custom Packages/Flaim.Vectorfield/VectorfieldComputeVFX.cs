using System;
using System.Collections;
using Flaim.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Flaim.Compute
{
    public class VectorfieldComputeVFX : MonoBehaviour
    {
        [SerializeField] private ComputeShader VectorFieldShader;
        [SerializeField] private VisualEffect VisualEffect;
        [SerializeField] private VisualEffect VisualEffectParticles;
      
        [Header("World Dimensions")]
        [SerializeField] private Vector3 BoundsSize = new Vector3(4,2, 4);
        [SerializeField] private float CellSize = .15f;

        [Header("Influence")]
        [SerializeField] private CameraRaycastPosition InfluenceTransform;
        [SerializeField] private float InfluenceRadius = 1f;
        
        [Header("Diffusion")]
        [SerializeField] private float DiffusionRate = 10f;
        [SerializeField] private float AdvectionTimestep = 15f;
        
        private int _width = 4;
        private int _height = 4;
        private int _depth = 4;
        
        private GraphicsBuffer _vectorFieldBuffer;
        private int _updateHandle;
        private int3 _threadCount;
        
        [Header("Debug")]
        [SerializeField] private bool DrawVectorPositions = true;

        [SerializeField] private Vector3 debugGridPos;

        void Start()
        {
            Initialize();
        }

        private void OnValidate()
        {
            // Vector field dimensions
            _width = BoundsSize.x > 0 ? Mathf.FloorToInt(BoundsSize.x / CellSize) : 1;
            _height = BoundsSize.y > 0 ? Mathf.FloorToInt(BoundsSize.y / CellSize) : 1;
            _depth = BoundsSize.z > 0 ? Mathf.FloorToInt(BoundsSize.z / CellSize) : 1;
        }

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            // Find the kernel
            _updateHandle = VectorFieldShader.FindKernel("Update");

            // ----------------- GraphicsBuffer -----------------
            //
            // Create a GraphicsBuffer
            int totalSize = _width * _height * _depth;
            _vectorFieldBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSize, sizeof(float) * 3);
            // Init data
            Vector3[] vectorFieldData = new Vector3[totalSize];
            for (int i = 0; i < totalSize; i++)
                vectorFieldData[i] = new Vector3(0, 0, 0);
            _vectorFieldBuffer.SetData(vectorFieldData);
            
            // ----------------- Compute Shader -----------------
            //
            // Set the buffer and dimensions in the compute shader
            VectorFieldShader.SetBuffer(_updateHandle, "VectorfieldBuffer", _vectorFieldBuffer);
          
            // Set constants
            VectorFieldShader.SetInts("GridDimensions", new int[] { _width, _height, _depth });
            VectorFieldShader.SetVector("BoundsMin", transform.position - BoundsSize * .5f);
            VectorFieldShader.SetVector("BoundsSize", BoundsSize);
            
            
            VectorFieldShader.SetFloat("CellSize", BoundsSize.x / (float)_width);
            //vectorFieldShader.SetVector("Dimensions", new Vector3( width, height, depth ));
            // Set thread counts
            _threadCount = new int3(
                Mathf.CeilToInt(_width / 8f),
                Mathf.CeilToInt(_height / 8f),
                Mathf.CeilToInt(_depth / 8f));

            // ----------------- Visual Effect -----------------
            //
            VisualEffect.SetVector3("GridDimensions", new Vector3(_width, _height, _depth));
            VisualEffect.SetVector3("BoundsSize", BoundsSize);
            VisualEffectParticles.SetVector3("GridDimensions", new Vector3(_width, _height, _depth));
            VisualEffectParticles.SetVector3("BoundsSize", BoundsSize);
            
            // Set the buffer for the visual effect
            VisualEffect.SetGraphicsBuffer("VectorfieldBuffer", _vectorFieldBuffer);
            VisualEffectParticles.SetGraphicsBuffer("VectorfieldBuffer", _vectorFieldBuffer);
            VisualEffect.Stop();
            VisualEffect.Reinit();
            VisualEffect.Play();
            
            Debug.Log($"Vectorfield intiailized. Thread count: {_threadCount}  Cell Count: {_width}-{_height}-{_depth}");
        }
       

        void Update()
        {
            VectorFieldShader.SetFloat("DeltaTime", Time.deltaTime);
            // Diffusion
            VectorFieldShader.SetFloat("DiffusionRate", DiffusionRate);
            VectorFieldShader.SetFloat("AdvectionTimestep", AdvectionTimestep);
            // Influence
            Vector3 normalizedInfluencePos = WorldToLocalPosition(InfluenceTransform.transform.position);
            VectorFieldShader.SetFloat("InfluenceRadius", InfluenceRadius);
            VectorFieldShader.SetVector("InfluenceNormalizedPosition", normalizedInfluencePos);
            VectorFieldShader.SetVector("InfluenceVelocity", InfluenceTransform.Velocity);

            // Dispatch the compute shader
            VectorFieldShader.Dispatch(_updateHandle, _threadCount.x, _threadCount.y, _threadCount.z);
            
            //Debug.Log("normalizedInfluencePos: " + normalizedInfluencePos);
        }
        
        uint GetIndex(Vector3 gridPos)
        {
            return (uint)(gridPos.x + gridPos.y * _width + gridPos.z * _width * _height);
        }
        
        Vector3 WorldToLocalPosition(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(InfluenceTransform.transform.position);
            local += BoundsSize * .5f;
            Vector3 normalized = new Vector3(
                local.x / BoundsSize.x,
                local.y / BoundsSize.y,
                local.z / BoundsSize.z);

            return normalized;
        }
        
        Vector3 WorldToGridPos(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(InfluenceTransform.transform.position);
            local += BoundsSize * .5f;
            local.x = Mathf.Floor(local.x);
            local.y = Mathf.Floor(local.y);
            local.z = Mathf.Floor(local.z);
            return local;
        }
        
        

        void OnDestroy()
        {
            // Release the buffer when the object is destroyed
            if (_vectorFieldBuffer != null)
            {
                _vectorFieldBuffer.Release();
            }
        }

        private void OnDrawGizmos()
        {
            // Draw bounds
            Gizmos.DrawWireCube(transform.position, BoundsSize);
            
            // Draw cell centers
            if(!DrawVectorPositions)
                return;
            
            Gizmos.color = Color.white;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        Vector3 pos = transform.position + new Vector3(x, y, z) * CellSize - (BoundsSize * .5f) + (Vector3.one * CellSize * .5f);
                        Gizmos.DrawLine(pos, pos + new Vector3(0, .2f, 0) * CellSize);
                    }
                }
            }
        }
    }
}