using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BVHBuilder : MonoBehaviour
{
    private BVH _bvh;
    
    [SerializeField] private bool _drawBounds = true;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    [ContextMenu("Create BVH")]
    // Update is called once per frame
    void CreateBVH()
    {
        List<Renderer> renderers = GetComponentsInChildren<Renderer>().ToList();
        _bvh = new BVH(renderers);
    }

    private void OnDrawGizmos()
    {
        if(!_drawBounds || _bvh == null)
           return;
        
        _bvh.root.DrawBounds();
        //Gizmos.DrawWireCube(_bvh.root.bounds.center, _bvh.root.bounds.size);
    }
}
