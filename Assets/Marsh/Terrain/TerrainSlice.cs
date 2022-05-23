using UnityEngine;
using UnityEngine.Rendering;

namespace Marsh
{
    public class TerrainSlice : MonoBehaviour
    {
        public const int Width = 32;
        public const int Height = 32;
        public const int VoxelCount = Width * Height * Width;

        private static readonly int[] _indicesForColliders;
        private static readonly Bounds _maxMeshBounds;

        static TerrainSlice()
        {
            int maxCubeCount = (Width - 1) * (Height - 1) * (Width - 1);
            int maxTriangleCount = maxCubeCount * 5;
            _indicesForColliders = new int[maxTriangleCount * 3];
            for (var i = 0; i < _indicesForColliders.Length; i++)
            {
                _indicesForColliders[i] = i;
            }

            var maxMeshSize = new Vector3(Width, Height, Width);
            _maxMeshBounds = new Bounds(maxMeshSize * 0.5f, maxMeshSize);
        }

        [SerializeField] private ComputeShader _voxelGenerationShader;
        private ComputeKernel _voxelGenerator;

        [SerializeField] private ComputeShader _meshSizeCalculationShader;
        private ComputeKernel _meshSizeCalculator;

        [SerializeField] private ComputeShader _meshGenerationShader;
        private ComputeKernel _meshGenerator;

        private Transform _transform;
        private MeshCollider _collider;
        [SerializeField] private Material _sourceMaterial;
        private Material _material;
        private ComputeBuffer _voxels;
        private ComputeBuffer _meshTriangles;
        private ComputeBuffer _meshTriangleCountReceiver;
        private int _meshTriangleCount;

        private void OnEnable()
        {
            CreateComputeKernels();
            _transform = transform;
            _collider = GetComponent<MeshCollider>();
            _material = new Material(_sourceMaterial);
            _voxels = new ComputeBuffer(VoxelCount, sizeof(int), ComputeBufferType.Structured | ComputeBufferType.Counter);
            _meshTriangleCountReceiver = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            _meshTriangleCount = 0;
            GenerateVoxels();
            GenerateMesh();
        }

        private void OnDisable()
        {
            _voxels?.Dispose();
            _meshTriangles?.Dispose();
            _meshTriangleCountReceiver?.Dispose();
        }

        private void Update()
        {
            _material.SetMatrix("_objToWorld", _transform.localToWorldMatrix);
            _material.SetBuffer("_meshPositions", _meshTriangles);
            Graphics.DrawProcedural(_material, _maxMeshBounds, MeshTopology.Triangles, _meshTriangleCount * 3);
        }

        private void CreateCollisionMesh()
        {
            AsyncGPUReadback.Request(_meshTriangles, (request) =>
            {
                var mesh = new Mesh();
                mesh.SetVertices(request.GetData<Vector3>());
                mesh.SetIndices(_indicesForColliders, 0, _meshTriangleCount * 3, MeshTopology.Triangles, 0, false, 0);
                _collider.sharedMesh = mesh;
            });
        }

        private void CreateComputeKernels()
        {
            _voxelGenerator = new ComputeKernel(_voxelGenerationShader, "CSMain");
            _meshSizeCalculator = new ComputeKernel(_meshSizeCalculationShader, "CSMain");
            _meshGenerator = new ComputeKernel(_meshGenerationShader, "CSMain");
        }

        private void GenerateMesh()
        {
            _voxels.SetCounterValue(0);
            _meshSizeCalculator.SetBuffer("_voxels", _voxels);
            _meshSizeCalculator.DispatchDivByThreadGroupSize(Width, Height, Width);
            ComputeBuffer.CopyCount(_voxels, _meshTriangleCountReceiver, 0);
            AsyncGPUReadback.Request(_meshTriangleCountReceiver, (triangleCountRequest) =>
            {
                _meshTriangleCount = triangleCountRequest.GetData<int>()[0];
                _meshTriangles = new ComputeBuffer(_meshTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Structured);
                _voxels.SetCounterValue(0);
                _meshGenerator.SetBuffer("_voxels", _voxels);
                _meshGenerator.SetBuffer("_meshTriangles", _meshTriangles);
                _meshGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
                CreateCollisionMesh();
            });
        }

        private void GenerateVoxels()
        {
            _voxelGenerator.SetBuffer("_voxels", _voxels);
            _voxelGenerator.SetFloat3("_worldPosition", _transform.position);
            _voxelGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
        }
    }
}
