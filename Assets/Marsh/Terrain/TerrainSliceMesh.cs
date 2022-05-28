using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Marsh
{
    public class TerrainSliceMesh
    {
        public ComputeBuffer Triangles { get; private set; }
        public int TriangleCount { get; private set; }
        public Mesh CollisionMesh { get; private set; }
        private ComputeBuffer _triangleCountReceiver;
        private JobHandle _collisionMeshBakeJob;

        public TerrainSliceMesh(ComputeShader sizeCalculator, ComputeShader generator, ComputeBuffer voxels)
        {
            _triangleCountReceiver = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            voxels.SetCounterValue(0);
            sizeCalculator.SetBuffer(0, "_voxels", voxels);
            sizeCalculator.DispatchDivByThreadGroupSize(TerrainSlice.Width, TerrainSlice.Height, TerrainSlice.Width);
            ComputeBuffer.CopyCount(voxels, _triangleCountReceiver, 0);

            AsyncGPUReadback.Request(_triangleCountReceiver, (triangleCountRequest) =>
            {
                TriangleCount = triangleCountRequest.GetData<int>()[0];
                Triangles = new ComputeBuffer(TriangleCount, sizeof(float) * 9, ComputeBufferType.Structured);
                voxels.SetCounterValue(0);
                generator.SetBuffer(0, "_voxels", voxels);
                generator.SetBuffer(0, "_meshTriangles", Triangles);
                generator.DispatchDivByThreadGroupSize(TerrainSlice.Width, TerrainSlice.Height, TerrainSlice.Width);

                AsyncGPUReadback.Request(Triangles, (trianglesRequest) =>
                {
                    CollisionMesh = new Mesh();
                    CollisionMesh.SetVertices(trianglesRequest.GetData<Vector3>());
                    CollisionMesh.SetIndices(ColliderIndices.Values, 0, TriangleCount * 3, MeshTopology.Triangles, 0, false, 0);
                    _collisionMeshBakeJob = new MeshBakeJob(CollisionMesh.GetInstanceID()).Schedule();
                });
            });
        }

        public void Destroy()
        {
            Triangles?.Dispose();
            TriangleCount = 0;
            Object.Destroy(CollisionMesh);
            _triangleCountReceiver?.Dispose();
        }

        public bool ColliderIsReady
        {
            get { return CollisionMesh != null && _collisionMeshBakeJob.IsCompleted; }
        }
    }
}