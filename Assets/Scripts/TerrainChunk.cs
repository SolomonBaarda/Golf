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
    public TerrainMap TerrainMap;

    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer,
            MeshGenerator.MeshData data, TerrainMap terrainMap)
    {
        this.position = position;
        Bounds = bounds;

        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        // Material stuff
        meshRenderer.material = material;
        //meshRenderer.material.SetTexture("_BaseMap", texture);

        // Physics material
        meshCollider.material = physics;

        // Set position
        meshObject.layer = terrainLayer;
        meshObject.transform.position = Bounds.center;
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the maps
        MeshData = data;
        TerrainMap = terrainMap;
    }


    public void UpdateVisualMesh(MeshSettings visual)
    {
        meshFilter.mesh = MeshData.GenerateMesh(visual);
        
        
        for (int y = 0; y < TerrainMap.Height; y += visual.SimplificationIncrement)
        {
            for (int x = 0; x < TerrainMap.Width; x += visual.SimplificationIncrement)
            {
                switch (TerrainMap.Map[x, y].Biome)
                {
                    case TerrainSettings.Biome.Grass:
                        break;
                    case TerrainSettings.Biome.Sand:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.yellow, 100);
                        break;
                    case TerrainSettings.Biome.Hole:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.red, 100);
                        break;
                    case TerrainSettings.Biome.Water:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.blue, 100);
                        break;
                    case TerrainSettings.Biome.Ice:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.white, 100);
                        break;
                }
            }
        }
        
        
    }


    public void UpdateColliderMesh(MeshSettings collider, bool useSameMesh)
    {
        Mesh m = meshFilter.mesh;
        if (!useSameMesh)
        {
            m = MeshData.GenerateMesh(collider);
        }

        meshCollider.sharedMesh = m;
    }


    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}
