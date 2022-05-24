using Unity.Mathematics;
using UnityEngine;

namespace Marsh
{
    public class ComputeKernel
    {
        private ComputeShader _shader;
        private int _index;
        private int3 _threadGroupSize;

        public ComputeKernel(ComputeShader shader, string name)
        {
            _shader = shader;
            _index = shader.FindKernel(name);
            shader.GetKernelThreadGroupSizes(_index, out uint x, out uint y, out uint z);
            _threadGroupSize = new((int)x, (int)y, (int)z);
        }

        public void DispatchDivByThreadGroupSize(int x, int y, int z)
        {
            _shader.Dispatch(_index, x / _threadGroupSize.x, y / _threadGroupSize.y, z / _threadGroupSize.z);
        }

        public void SetBuffer(string name, ComputeBuffer buffer)
        {
            _shader.SetBuffer(_index, name, buffer);
        }

        public void SetInt(string name, int v)
        {
            _shader.SetInt(name, v);
        }

        public void SetFloat(string name, float v)
        {
            _shader.SetFloat(name, v);
        }

        public void SetFloat3(string name, float3 v)
        {
            _shader.SetFloats(name, v.x, v.y, v.z);
        }
    }
}
