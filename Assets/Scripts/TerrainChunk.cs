﻿using UnityEngine;
using UnityEngine.UI;

public class TerrainChunk
{
    private Vector2Int position;
    public Bounds Bounds { get; }

    private GameObject meshObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public Mesh Visual => meshFilter.mesh;
    public Mesh Collider => meshCollider.sharedMesh;

    public bool IsVisible => meshObject.activeSelf;


    public MeshGenerator.MeshData MeshData;

    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, Texture2D texture, PhysicMaterial physics, Transform parent, int terrainLayer, MeshGenerator.MeshData data)
    {
        this.position = position;
        Bounds = bounds;


        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        meshRenderer.material = material;
        meshRenderer.material.SetTexture("_BaseMap", texture);

        meshCollider.material = physics;

        meshObject.layer = terrainLayer;

        meshObject.transform.position = Bounds.center;
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the height map
        MeshData = data;
    }


    public void UpdateVisualMesh(MeshSettings visual)
    {
        meshFilter.mesh = MeshData.GenerateMesh(visual, out int width, out int height);
        Texture t = meshRenderer.material.GetTexture("_BaseMap");
        Vector2 scale = new Vector2((float)(width / t.width), (float)(height / t.height));
        meshRenderer.material.SetTextureScale("_BaseMap", scale);
    }


    public void UpdateColliderMesh(MeshSettings collider, bool useSameMesh)
    {
        Mesh m = meshFilter.mesh;
        if (!useSameMesh)
        {
            m = MeshData.GenerateMesh(collider, out _, out _);
        }

        meshCollider.sharedMesh = m;
    }


    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}
