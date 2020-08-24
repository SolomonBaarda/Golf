﻿using System;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public int X, Y;

    public Vector3 Centre;
    public Vector3 Size;

    [SerializeField] public Biome.Type[,] Biomes;

    [SerializeField] public Texture2D BiomeColourMap;
    [SerializeField] public Mesh MainMesh;



    public TerrainChunkData(int x, int y, Vector3 centre, Vector3 size, Biome.Type[,] biomes, Texture2D colourMap, Mesh main)
    {
        X = x;
        Y = y;
        Centre = centre;
        Size = size;

        Biomes = biomes;

        BiomeColourMap = colourMap;
        MainMesh = main;
    }
}
