﻿using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils
{

    /// <summary>
    /// Returns true if the GameObject extends class T. Sets output as reference to that class or Null otherwise.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toCheck"></param>
    /// <param name="extendedClass"></param>
    /// <returns></returns>
    public static bool GameObjectExtendsClass<T>(in GameObject toCheck, out T extendedClass) where T : class
    {
        // Get all monobehaviours 
        MonoBehaviour[] all = toCheck.GetComponents<MonoBehaviour>();

        // Loop through each
        foreach (MonoBehaviour behaviour in all)
        {
            // If the monobehaviour implements the interface
            if (behaviour is T output)
            {
                extendedClass = output;

                // Return it
                return true;
            }
        }

        extendedClass = null;
        return false;
    }




    public static void DestroyAllChildren(Transform t)
    {
        // Destroy all of the world objects
        for (int i = 0; i < t.childCount; i++)
        {
            Object.Destroy(t.GetChild(i).gameObject);
        }
    }






    public static bool IsWithinArrayBounds<T>(int x, int y, in T[,] array)
    {
        if (array != null)
        {
            int width = array.GetLength(0), height = array.GetLength(1);

            return x >= 0 && y >= 0 && x < width && y < height;
        }

        return false;
    }



    public static bool ContainsAll<T>(List<T> a, List<T> b)
    {
        return !b.Except(a).Any();
    }

    public static T GetClosestTo<T>(Vector3 worldPos, Vector3 min, Vector3 max, in T[] array, int width, int height, out int indexX, out int indexY)
    {
        // Get the lower bounds of the closest 4 points to the position
        int estimatedX = width - 1 - Mathf.RoundToInt((max.x - worldPos.x) / (max.x - min.x) * width);
        int estimatedY = height - 1 - Mathf.RoundToInt((max.z - worldPos.z) / (max.z - min.z) * height);

        indexX = Mathf.Clamp(estimatedX, 0, width - 1);
        indexY = Mathf.Clamp(estimatedY, 0, height - 1);

        return array[indexY * width + indexX];
    }

    public static T GetClosestTo<T>(Vector2 worldPos, Vector2 min, Vector2 max, in T[] array, int width, int height, out int indexX, out int indexY)
    {
        // Get the lower bounds of the closest 4 points to the position
        int estimatedX = width - 1 - Mathf.RoundToInt((max.x - worldPos.x) / (max.x - min.x) * width);
        int estimatedY = height - 1 - Mathf.RoundToInt((max.y - worldPos.y) / (max.y - min.y) * height);

        indexX = Mathf.Clamp(estimatedX, 0, width - 1);
        indexY = Mathf.Clamp(estimatedY, 0, height - 1);

        return array[indexY * width + indexX];
    }

    public static bool GetClosestIndex(Vector3 position, Vector3 min, Vector3 max, int arrayWidth, int arrayHeight, out int indexX, out int indexY)
    {
        return GetClosestIndex(position.x, position.z, min.x, min.z, max.x, max.z, arrayWidth, arrayHeight, out indexX, out indexY);
    }

    public static bool GetClosestIndex(Vector2 position, Vector2 min, Vector2 max, int arrayWidth, int arrayHeight, out int indexX, out int indexY)
    {
        return GetClosestIndex(position.x, position.y, min.x, min.y, max.x, max.y, arrayWidth, arrayHeight, out indexX, out indexY);
    }

    private static bool GetClosestIndex(float posX, float posY, float minX, float minY, float maxX, float maxY, int arrayWidth, int arrayHeight, out int indexX, out int indexY)
    {
        indexX = -1;
        indexY = -1;

        // Ensure points are valid
        if (minX < maxX && minY < maxY && posX >= minX && posX <= maxX && posY >= minY && posY <= maxY)
        {
            float percentX = (posX - minX) / (maxX - minX), percentY = (posY - minY) / (maxY - minY);

            indexX = (int)Mathf.Abs(percentX * arrayWidth);
            indexY = (int)Mathf.Abs(percentY * arrayHeight);
        }

        return indexX >= 0 && indexY >= 0 && indexX < arrayWidth && indexY < arrayHeight;
    }


    public static T GetClosestTo<T>(Vector3 worldPos, Vector3 min, Vector3 max, in T[,] array, out int indexX, out int indexY)
    {
        int width = array.GetLength(0), height = array.GetLength(1);

        // Get the lower bounds of the closest 4 points to the position
        int estimatedX = width - 1 - Mathf.RoundToInt((max.x - worldPos.x) / (max.x - min.x) * width);
        int estimatedY = height - 1 - Mathf.RoundToInt((max.z - worldPos.z) / (max.z - min.z) * height);

        indexX = Mathf.Clamp(estimatedX, 0, width - 1);
        indexY = Mathf.Clamp(estimatedY, 0, height - 1);

        return array[indexX, indexY];
    }

    public static T GetClosestTo<T>(Vector2 worldPos, Vector2 min, Vector2 max, in T[,] array, out int indexX, out int indexY)
    {
        int width = array.GetLength(0), height = array.GetLength(1);

        // Get the lower bounds of the closest 4 points to the position
        int estimatedX = width - 1 - Mathf.RoundToInt((max.x - worldPos.x) / (max.x - min.x) * width);
        int estimatedY = height - 1 - Mathf.RoundToInt((max.y - worldPos.y) / (max.y - min.y) * height);

        indexX = Mathf.Clamp(estimatedX, 0, width - 1);
        indexY = Mathf.Clamp(estimatedY, 0, height - 1);

        return array[indexX, indexY];
    }


    public static T[] Flatten<T>(in T[,] array)
    {
        int width = array.GetLength(0), height = array.GetLength(1);
        T[] flattened = new T[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flattened[y * width + x] = array[x, y];
            }
        }

        return flattened;
    }

    public static T[,] UnFlatten<T>(in T[] array, int width, int height)
    {
        T[,] unFlattened = new T[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                unFlattened[x, y] = array[y * width + x];
            }
        }

        return unFlattened;
    }






    public static void EMPTY() { }
    public static void EMPTY<T>(T _) { }





}
