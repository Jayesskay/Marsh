using Unity.Mathematics;
using UnityEngine;

namespace Marsh
{
    public static class ComputeShaderEx
    {
        public static void DispatchDivByThreadGroupSize(this ComputeShader shader, int x, int y, int z)
        {
            shader.GetKernelThreadGroupSizes(0, out var sx, out var sy, out var sz);
            shader.Dispatch(0, x / (int)sx, y / (int)sy, z / (int)sz);
        }

        public static void SetFloat3(this ComputeShader shader, string name, float3 v)
        {
            shader.SetFloats(name, v.x, v.y, v.z);
        }
    }
}