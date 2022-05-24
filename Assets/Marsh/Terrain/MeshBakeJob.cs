using Unity.Jobs;
using UnityEngine;

namespace Marsh
{
    public struct MeshBakeJob : IJob
    {
        private int _meshId;

        public MeshBakeJob(int meshId)
        {
            _meshId = meshId;
        }

        public void Execute()
        {
            Physics.BakeMesh(_meshId, false);
        }
    }
}