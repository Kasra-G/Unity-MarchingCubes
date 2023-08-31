using System;
using UnityEngine;
public static class ArrayUtils
{

    public static T[,,] To3DFrom1D<T>(T[] arr, int xMax, int yMax, int zMax)
    {
        T[,,] result = new T[xMax, yMax, zMax];
        int i = 0;
        for (int z = 0; z < zMax; z++)
        {
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    result[x, y, z] = arr[i];
                    i += 1;
                }
            }
        }
        return result;
    }

    public static T[] To1DFrom3D<T>(T[,,] arr, int xMax, int yMax, int zMax)
    {
        T[] result = new T[arr.Length];
        int i = 0;
        for (int z = 0; z < zMax; z++)
        {
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    result[i] = arr[x, y, z];
                    i += 1;
                }
            }
        }
        return result;
    }

    public static int indexFromCoord(Vector3Int coord, Vector3Int dimensions)
    {
        return coord.x + dimensions.x * (coord.y + dimensions.y * coord.z);
    }
}

