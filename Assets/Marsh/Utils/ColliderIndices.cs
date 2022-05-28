using UnityEngine;

namespace Marsh
{
    public class ColliderIndices
    {
        public static readonly int[] Values;

        static ColliderIndices()
        {
            int maxCubeCount = (TerrainSlice.Width - 1) * (TerrainSlice.Height - 1) * (TerrainSlice.Width - 1);
            int maxTriangleCount = maxCubeCount * 5;
            Values = new int[maxTriangleCount * 3];
            for (var i = 0; i < Values.Length; i++)
            {
                Values[i] = i;
            }
        }
    }
}