﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldObjectGenerator : MonoBehaviour
{
    public float Radius = 20;
    [Range(1, 10)]
    public int Iterations = 5;


    [Space]
    public List<WorldObjectPreset> WorldObjectPrefabs;
    private Dictionary<Biome.Decoration, List<GameObject>> Prefabs = new Dictionary<Biome.Decoration, List<GameObject>>();



    private void Awake()
    {
        // Add all the objects to the dictionary
        foreach (WorldObjectPreset p in WorldObjectPrefabs)
        {
            // Add a new entry if not there
            if (!Prefabs.TryGetValue(p.Type, out List<GameObject> prefabList))
            {
                prefabList = new List<GameObject>();
                Prefabs.Add(p.Type, prefabList);
            }

            // Add all prefabs to the list
            prefabList.AddRange(p.Prefabs);
        }
    }



    private float GetLowestYPosFromNeighbours(TerrainMap.Point point)
    {
        float lowest = (point.LocalVertexPosition + point.Offset).y;

        foreach (TerrainMap.Point neighbour in point.Neighbours)
        {
            float y = (neighbour.LocalVertexPosition + neighbour.Offset).y;
            if (y < lowest)
            {
                lowest = y;
            }
        }

        return lowest;
    }

    public List<WorldObjectData> CalculateDataForChunk(TerrainMap m)
    {
        Dictionary<GameObject, WorldObjectData> prefabsInChunk = new Dictionary<GameObject, WorldObjectData>();

        int seed = Noise.Seed(m.Chunk.ToString());

        // Get the local position
        List<Vector2> localPosition2D = PoissonDiscSampling.GenerateLocalPoints(Radius, new Vector2(m.Bounds.size.x, m.Bounds.size.z), seed, Iterations);

        System.Random r = new System.Random(seed);

        // Add the offset to it
        Vector3 offset = new Vector3(m.Bounds.min.x, 0, m.Bounds.min.z);
        // Loop through each position
        foreach (Vector2 pos in localPosition2D)
        {
            // Get the correct world pos
            Vector3 worldPos = new Vector3(pos.x, 0, pos.y) + offset;
            TerrainMap.Point p = Utils.GetClosestTo(worldPos, m.Bounds.min, m.Bounds.max, in m.Points);
            worldPos.y = GetLowestYPosFromNeighbours(p);

            // Find its type
            List<Biome.Decoration> types = p.ValidDecoration;
            if (types.Count > 0)
            {
                // Randomly choose the object type to go here
                int typeIndex = r.Next(0, types.Count);
                Biome.Decoration type = types[typeIndex];

                if (Prefabs.TryGetValue(type, out List<GameObject> prefabs))
                {
                    if (prefabs.Count > 0)
                    {
                        // Randomly choose the prefab to go here
                        int prefabIndex = r.Next(0, prefabs.Count);
                        GameObject prefab = prefabs[prefabIndex];

                        // Create it if we need to
                        if (!prefabsInChunk.TryGetValue(prefab, out WorldObjectData d))
                        {
                            d = new WorldObjectData(prefab, new List<Vector3>());
                            prefabsInChunk.Add(prefab, d);
                        }

                        // Add it
                        d.WorldPositions.Add(worldPos);
                    }
                }
            }
        }


        return prefabsInChunk.Values.ToList();
    }





    public void CheckWorldObjectDistancesBetweenChunks(List<WorldObjectData> chunk, List<WorldObjectData> neighbour)
    {
        foreach (WorldObjectData neighbourObject in neighbour)
        {
            // Loop through each position in the neighbour chunk
            foreach (Vector3 neighbourPosition in neighbourObject.WorldPositions)
            {
                // Check each object for 
                foreach (WorldObjectData thisChunkObject in chunk)
                {
                    // Remove all that are too close
                    thisChunkObject.WorldPositions.RemoveAll(x => (x - neighbourPosition).sqrMagnitude < Radius * Radius);
                }
            }
        }
    }










    [Serializable]
    public class WorldObjectPreset
    {
        public Biome.Decoration Type;
        public List<GameObject> Prefabs;
    }
}
