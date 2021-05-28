﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;

    public bool HasTerrain { get; private set; }

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

    [Space]
    public TerrainData CurrentLoadedTerrain;

    private Transform Player;
    private bool HideChunks = true;
    private float ViewDistance = 0;


    public bool IsLoading { get; private set; } = false;

    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        TerrainChunkManager.Clear();

        HasTerrain = false;

        CurrentLoadedTerrain = null;
    }

    public void Set(bool hideChunks, Transform player, float viewDistance)
    {
        HideChunks = hideChunks;
        Player = player;
        ViewDistance = viewDistance;
    }



    private void LateUpdate()
    {
        if (HideChunks && Player != null && ViewDistance > 0)
        {
            foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
            {
                // Only set the chunks within render distance to be visible
                chunk.SetVisible((chunk.Bounds.center - Player.position).sqrMagnitude <= ViewDistance * ViewDistance);
            }
        }
    }




    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data)
    {
        StartCoroutine(LoadTerrainAsync(data));
    }



    private IEnumerator LoadTerrainAsync(TerrainData data)
    {
        DateTime before = DateTime.Now;
        Clear();
        IsLoading = true;

        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            DateTime a = DateTime.Now;

            // Instantiate the terrain
            TerrainChunk c = TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
            // And instantiate all objects

            //Debug.Log($"Time for chunk: {(DateTime.Now - a).TotalSeconds.ToString("0.00")}");
            //a = DateTime.Now;

            foreach (WorldObjectData worldObjectData in chunk.WorldObjects)
            {
                foreach ((Vector3,Vector3) worldPosition in worldObjectData.WorldPositions)
                {
                    Instantiate(worldObjectData.Prefab, worldPosition.Item1, Quaternion.Euler(worldPosition.Item2), c.transform);
                }
            }

            //Debug.Log($"Time objects: {(DateTime.Now - a).TotalSeconds.ToString("0.00")}");

            // Wait for next frame
            yield return null;
        }

        // Assign the terrain at the end
        HasTerrain = true;
        CurrentLoadedTerrain = data;
        IsLoading = false;


        // Debug
        string message = "* Loaded terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0")
            + " seconds with " + data.Chunks.Count + " chunks and " + data.GolfHoles.Count + " holes.";

        Debug.Log(message);

    }



    public static Vector3 CalculateSpawnPoint(float sphereRadius, Vector3 pointOnMesh)
    {
        return pointOnMesh + (UP * sphereRadius);
    }





    private void OnDrawGizmosSelected()
    {
        if(CurrentLoadedTerrain != null)
        {
            System.Random r = new System.Random(0);
            // Calculate the holes
            foreach (CourseData h in CurrentLoadedTerrain.GolfHoles)
            {
                Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
                Gizmos.color = c;
                Gizmos.DrawLine(h.Start, h.Start + Vector3.up * 100);
                Gizmos.DrawLine(h.Hole, h.Hole + Vector3.up * 500);
            }
        }

    }

}
