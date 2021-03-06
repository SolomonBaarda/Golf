﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{
    [Min(1)]
    public const float ChunkSizeWorldUnits = 150;

    [Header("References")]
    public Grid ChunkGrid;
    public Transform ChunkParent;

    private Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    private void Awake()
    {
        UpdateGrid();
    }





    private void UpdateGrid()
    {
        ChunkGrid.cellSize = new Vector3(ChunkSizeWorldUnits, ChunkSizeWorldUnits);
        ChunkGrid.cellSwizzle = GridLayout.CellSwizzle.XZY;
        // Move the Grid so that chunk 0,0 is centered on the origin
        ChunkGrid.transform.position = -new Vector3(ChunkGrid.cellSize.x / 2, 0, ChunkGrid.cellSize.y / 2);
    }




    public TerrainChunk TryAddChunk(TerrainChunkData data, Material material, PhysicMaterial physics, int terrainLayer)
    {
        Vector2Int position = new Vector2Int(data.X, data.Y);
        TerrainChunk chunk;

        // Need to create new chunk
        if (!TerrainChunkExists(position))
        {
            Bounds bounds = new Bounds(data.Centre, data.BoundsSize);

            // Create the chunk
            chunk = new GameObject().AddComponent<TerrainChunk>();

            chunk.Initialise(position, bounds, data, material, physics, ChunkParent, terrainLayer);

            TerrainChunks.Add(position, chunk);
        }
        // Just need to update some values
        else
        {
            if(TerrainChunks.TryGetValue(position, out TerrainChunk c))
            {
                // Update the chunk data 
                c.UpdateChunkData(data);
            }
            chunk = c;
        }

        return chunk;
    }



    private Vector3[,] CalculateWorldPoints(int width, int height, Bounds bounds, float[,] heights)
    {
        Vector3[,] points = new Vector3[width, height];

        Vector3 distanceBetween = TerrainGenerator.CalculateDistanceBetweenVertices(bounds, (width + height) / 2);

        for (int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                points[x, y] = bounds.min + new Vector3(x * distanceBetween.x, heights[x,y], y * distanceBetween.z);
            }
        }

        return points;
    }



    public bool TryGetChunk(Vector2Int pos, out TerrainChunk chunk)
    {
        return TerrainChunks.TryGetValue(pos, out chunk);
    }



    public IEnumerable<TerrainChunk> GetAllChunks()
    {
        return TerrainChunks.Values;
    }


    public List<TerrainChunk> GetChunks(List<Vector2Int> chunks)
    {
        List<TerrainChunk> values = new List<TerrainChunk>();
        foreach (Vector2Int key in chunks)
        {
            if (TerrainChunks.TryGetValue(key, out TerrainChunk val))
            {
                values.Add(val);
            }
        }

        return values;
    }


    public void SetVisibleChunks(List<Vector2Int> visible)
    {
        // Disabel all chunks first
        foreach (TerrainChunk c in TerrainChunks.Values)
        {
            c.SetVisible(false);
        }

        // Then enable the ones we want
        foreach (Vector2Int key in visible)
        {
            if (TerrainChunks.TryGetValue(key, out TerrainChunk chunk))
            {
                chunk.SetVisible(true);
            }
        }
    }


    public bool TerrainChunkIsVisible(Vector2Int chunk)
    {
        if (TerrainChunkExists(chunk))
        {
            TerrainChunks.TryGetValue(chunk, out TerrainChunk c);
            return c.IsVisible;
        }
        return false;
    }


    public bool TerrainChunkExists(Vector2Int chunk)
    {
        return TerrainChunks.ContainsKey(chunk);
    }


    public Vector3 CalculateTerrainChunkCentreWorld(Vector2Int chunk)
    {
        return ChunkGrid.GetCellCenterWorld(new Vector3Int(chunk.x, chunk.y, 0));
    }


    public Bounds CalculateTerrainChunkBounds(Vector2Int chunk)
    {
        return ChunkGrid.GetBoundsLocal(new Vector3Int(chunk.x, chunk.y, 0));
    }


    public Vector3 LocalChunkPosToWorld(Vector2Int chunk, Vector3 localPos)
    {
        Vector3 min = ChunkGrid.CellToWorld(new Vector3Int(chunk.x, chunk.y, 0));

        return min + localPos;
    }


    public Vector2Int WorldToChunk(Vector3 worldPos)
    {
        Vector3Int chunk = ChunkGrid.WorldToCell(worldPos);
        return new Vector2Int(chunk.x, chunk.y);
    }


    public void Clear()
    {
        // Clear all the chunks
        Utils.DestroyAllChildren(ChunkParent);

        TerrainChunks.Clear();
        UpdateGrid();
    }




}
