﻿using UnityEngine;
using System.Collections.Generic;



public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    /// <summary>
    /// The maximum LOD Terrain data.
    /// </summary>
    public Point[,] Map;

    // Settings
    public TerrainSettings TerrainSettings;

    public List<Point.NeighbourDirection> EdgeNeighboursAdded;

    public TerrainMap(Vector2Int chunk, int width, int height, in Vector3[,] baseVertices, Bounds bounds,
        in float[,] rawHeights, in float[,] bunkersMask, in float[,] holesMask, in TerrainSettings terrainSettings)
    {
        terrainSettings.ValidateValues();
        AnimationCurve copy = new AnimationCurve(terrainSettings.HeightDistribution.keys);

        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;

        TerrainSettings = terrainSettings;
        EdgeNeighboursAdded = new List<Point.NeighbourDirection>();

        // Create the map
        Map = new Point[width, height];

        // Assign all the terrain point vertices
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool atEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                Biome.Type biome = CalculateBiome(terrainSettings, bunkersMask[x, y], holesMask[x, y]);
                float originalHeight = CalculateFinalHeight(terrainSettings, copy, rawHeights[x, y], bunkersMask[x, y]);

                // Assign the terrain point
                Map[x, y] = new Point(baseVertices[x, y], bounds.center, originalHeight, biome, atEdge);
            }
        }

        // Now set each neighbour
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {

                // Add the 3x3 of points as neighbours
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int pointX = x + i, pointY = y + j;

                        // Ensure within the array bounds
                        if (Utils.IsWithinArrayBounds(pointX, pointY, in Map))
                        {
                            // Don't add its self
                            if (pointX != x || pointY != y)
                            {
                                Map[x, y].Neighbours.Add(Map[pointX, pointY]);
                            }
                        }
                    }
                }
            }
        }
    }

    private Point.NeighbourDirection CalculateNeighbourDirection(int dirX, int dirY)
    {
        Point.NeighbourDirection d = Point.NeighbourDirection.Up;

        // Sides
        if (dirX != dirY)
        {
            if (dirX == -1)
            {
                d = Point.NeighbourDirection.Left;
            }
            else if (dirX == 1)
            {
                d = Point.NeighbourDirection.Right;
            }
            else if (dirY == -1)
            {
                d = Point.NeighbourDirection.Up;
            }
            else if (dirY == 1)
            {
                d = Point.NeighbourDirection.Down;
            }
        }
        // Corners
        else
        {
            if (dirX == -1 && dirY == -1)
            {
                d = Point.NeighbourDirection.UpLeft;
            }
            else if (dirX == 1 && dirY == -1)
            {
                d = Point.NeighbourDirection.UpRight;
            }
            else if (dirX == -1 && dirY == 1)
            {
                d = Point.NeighbourDirection.DownLeft;
            }
            else if (dirX == 1 && dirY == 1)
            {
                d = Point.NeighbourDirection.DownRight;
            }
        }

        return d;
    }


    private Biome.Type CalculateBiome(in TerrainSettings settings, float rawBunker, float rawHole)
    {
        Biome.Type b = settings.MainBiome;

        // Do a bunker
        if (settings.DoBunkers && !Mathf.Approximately(rawBunker, Point.Empty))
        {
            b = Biome.Type.Sand;
        }

        // Hole is more important
        if (!Mathf.Approximately(rawHole, Point.Empty))
        {
            b = Biome.Type.Hole;
        }

        return b;
    }


    private float CalculateFinalHeight(in TerrainSettings settings, in AnimationCurve multithreadingSafeCurve, float rawHeight, float rawBunker)
    {
        // Calculate the height to use
        float height = rawHeight;
        if (settings.UseCurve)
        {
            height = multithreadingSafeCurve.Evaluate(rawHeight);
        }

        // And apply the scale
        height *= settings.HeightMultiplier;


        // Add the bunker now
        if (settings.DoBunkers)
        {
            height -= rawBunker * settings.BunkerMultiplier;
        }

        return height;
    }



    public void DebugMinMaxHeight()
    {
        float min = Map[0, 0].Height, max = min;
        foreach (Point p in Map)
        {
            min = p.Height < min ? p.Height : min;
            max = p.Height > max ? p.Height : max;
        }

        Debug.Log("Terrain map " + Chunk.ToString() + "min: " + min + " max: " + max);
    }




    public void AddEdgeNeighbours(int dirX, int dirY, ref TerrainMap map, out bool mapNeedsUpdating)
    {
        dirX = Mathf.Clamp(dirX, -1, 1);
        dirY = Mathf.Clamp(dirY, -1, 1);

        Point.NeighbourDirection direction = CalculateNeighbourDirection(dirX, dirY);

        mapNeedsUpdating = false;

        if (Width == map.Width && Height == map.Height)
        {
            // Only add the neighbours if it has not been done already
            if (!EdgeNeighboursAdded.Contains(direction))
            {
                EdgeNeighboursAdded.Add(direction);

                // Horizontal case
                if (direction == Point.NeighbourDirection.Up || direction == Point.NeighbourDirection.Down)
                {
                    int y = 0, neighbourY = Height - 1;
                    if (direction == Point.NeighbourDirection.Down)
                    {
                        y = Height - 1;
                        neighbourY = 0;
                    }

                    for (int x = 0; x < Width; x++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            if (Utils.IsWithinArrayBounds(x + i, neighbourY, map.Map))
                            {
                                AddNeighbourForEdge(ref Map[x, y], ref map.Map[x + i, neighbourY], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }
                // Vertical case
                else if (direction == Point.NeighbourDirection.Left || direction == Point.NeighbourDirection.Right)
                {
                    int x = 0, neighbourX = Width - 1;
                    if (direction == Point.NeighbourDirection.Right)
                    {
                        x = Width - 1;
                        neighbourX = 0;
                    }

                    for (int y = 0; y < Height; y++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (Utils.IsWithinArrayBounds(neighbourX, y + j, map.Map))
                            {
                                AddNeighbourForEdge(ref Map[x, y], ref map.Map[neighbourX, y + j], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }

                // Diagonal cases - TODO
                /*

                // Neighbour is diagonal up left
                if (direction == Point.NeighbourDirection.UpLeft)
                {
                    AddNeighbourForEdge(ref Map[0, 0], ref map.Map[Width - 1, Height - 1], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal up right
                else if (direction == Point.NeighbourDirection.UpRight)
                {
                    AddNeighbourForEdge(ref Map[Width - 1, 0], ref map.Map[0, Height - 1], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal down left
                else if (direction == Point.NeighbourDirection.DownLeft)
                {
                    AddNeighbourForEdge(ref Map[0, Height - 1], ref map.Map[Width - 1, 0], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal down right
                else if (direction == Point.NeighbourDirection.DownLeft)
                {
                    AddNeighbourForEdge(ref Map[Width - 1, Height - 1], ref map.Map[0, 0], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                */

            }
        }
        else
        {
            Debug.LogError("Trying to add edge neighbours for TerrainMaps of different size.");
        }
    }


    private static void AddNeighbourForEdge(ref Point p, ref Point neighbour, out bool terrainNeedsUpdating)
    {
        terrainNeedsUpdating = false;
        if (p.IsAtEdgeOfMesh && neighbour.IsAtEdgeOfMesh)
        {
            if (!p.Neighbours.Contains(neighbour))
            {
                p.Neighbours.Add(neighbour);

                // These neighbours are the same hole that is split by the chunk border
                if (p.Biome == Biome.Type.Hole && neighbour.Biome == Biome.Type.Hole)
                {
                    if (p.Hole != neighbour.Hole)
                    {
                        terrainNeedsUpdating = true;
                        p.Hole.Merge(ref neighbour.Hole);
                    }
                }
            }
        }
    }




    public Point GetClosestTo(Vector3 worldPos)
    {
        int estimatedX = Width - (int)((Bounds.max.x - worldPos.x) / (Bounds.max.x - Bounds.min.x) * Width);
        int estimatedY = Height - (int)((Bounds.max.z - worldPos.z) / (Bounds.max.z - Bounds.min.z)  * Height);


        // TODO - needs to be fixed
        Point closest = Map[estimatedX, estimatedY];

        int offset = 1;

        // Don't bother checking each vertex
        for (int y = -offset; y <= offset; y++)
        {
            for (int x = -offset; x <= offset; x++)
            {
                int i = estimatedX + x, j = estimatedY + y;

                // Valid index
                if (i >= 0 && j >= 0 && i < Width && j < Height)
                {
                    Point p = Map[i, j];
                    Vector3 pos = p.LocalVertexPosition + p.Offset;
                    if (Vector3.Distance(pos, worldPos) < Vector3.Distance(closest.LocalVertexPosition + closest.Offset, worldPos))
                    {
                        closest = p;
                    }
                }
            }
        }

        //Debug.DrawLine(closest.LocalVertexBasePosition + closest.Offset, closest.LocalVertexBasePosition + closest.Offset + (Vector3.up * 100), Color.red, 100);

        //Debug.DrawLine(worldPos, worldPos + (Vector3.up * 100), Color.green, 100);


        return closest;
    }




    public class Point
    {
        public const float Empty = 0f;

        public Vector3 LocalVertexBasePosition;
        // Calculate the point of the vertex
        public Vector3 LocalVertexPosition => LocalVertexBasePosition + (TerrainGenerator.UP * Height);
        public Vector3 Offset;

        public bool IsAtEdgeOfMesh;

        public Biome.Type Biome;
        public float Height;
        public float OriginalHeight;

        /// <summary>
        /// If this point is part of a Hole.
        /// </summary>
        public Hole Hole;
        public List<Point> Neighbours;


        public Point(Vector3 localVertexPos, Vector3 offset, float height, Biome.Type biome, bool isAtEdgeOfMesh)
        {
            LocalVertexBasePosition = localVertexPos;
            Offset = offset;

            IsAtEdgeOfMesh = isAtEdgeOfMesh;

            Neighbours = new List<Point>();

            Biome = biome;
            Height = height;
            OriginalHeight = Height;
        }


        public enum NeighbourDirection
        {
            Up, Down, Left, Right,
            UpLeft, UpRight, DownLeft, DownRight,
        }

    }






}




