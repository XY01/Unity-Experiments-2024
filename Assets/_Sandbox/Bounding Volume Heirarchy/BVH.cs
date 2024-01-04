using System.Collections.Generic;
using UnityEngine;

public class BVHNode
{
    public Bounds bounds; // Bounding volume for this node
    public BVHNode leftChild;
    public BVHNode rightChild;
    public List<Renderer> objects; // Objects contained in this node (for leaf nodes)

    public BVHNode(List<Renderer> objs)
    {
        objects = objs;
        bounds = new Bounds();

        for (int i = 0; i < objs.Count; i++)
        {
            if(i == 0)
                bounds = objs[i].bounds;
            else
                bounds.Encapsulate(objs[i].bounds);
        }
    }
    
    public void DrawBounds()
    {
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        if(leftChild != null)
            leftChild.DrawBounds();
        if(rightChild != null)
            rightChild.DrawBounds();
    }
}

public class BVH
{
    public BVHNode root;

    public BVH(List<Renderer> objects)
    {
        root = BuildBVH(objects);
    }

    private BVHNode BuildBVH(List<Renderer> objects)
    {
        if (objects.Count == 0)
            return null;

        if (objects.Count == 1)
            return new BVHNode(objects);

        // Split objects into two groups based on a criterion, e.g., median point along an axis
        List<Renderer> leftObjects = new List<Renderer>();
        List<Renderer> rightObjects = new List<Renderer>();
        SplitObjects(objects, ref leftObjects, ref rightObjects);

        
        Debug.Log("Adding new node. Left: " + leftObjects.Count + " Right: " + rightObjects.Count + " Total: " + objects.Count);
        BVHNode node = new BVHNode(objects);
        node.leftChild = BuildBVH(leftObjects);
        node.rightChild = BuildBVH(rightObjects);

        return node;
    }

    private void SplitObjects(List<Renderer> objects, ref List<Renderer> leftObjects, ref List<Renderer> rightObjects)
    {
        // Implement a splitting criterion, e.g., median split
        // This is a simplified example
        Vector3 centroid = Vector3.zero;
        foreach (var obj in objects)
        {
            centroid += obj.GetComponent<Renderer>().bounds.center;
        }
        centroid /= objects.Count;

        foreach (var obj in objects)
        {
            if (obj.GetComponent<Renderer>().bounds.center.x < centroid.x)
                leftObjects.Add(obj);
            else
                rightObjects.Add(obj);
        }
    }
}