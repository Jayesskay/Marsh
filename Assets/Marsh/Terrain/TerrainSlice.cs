using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using UnityEngine;

namespace Marsh
{
    public class TerrainSlice : MonoBehaviour
    {
        public const int Width = 32;
        public const int Height = 32;
        public const int VoxelCount = Width * Height * Width;
        public Bounds Bounds { get; private set; }
        public bool Dirty { get { return _pendingBakeJobMesh != null; } }

        private static readonly int[] _indicesForColliders;

        [SerializeField] private ComputeShader _voxelGenerationShader;
        private ComputeKernel _voxelGenerator;

        [SerializeField] private ComputeShader _voxelManipulationShader;
        private ComputeKernel _voxelManipulator;

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

        private JobHandle _pendingBakeJob;
        private Mesh _pendingBakeJobMesh;

        public void Modify(Vector3 position, float radius, int modification)
        {
            _voxelManipulator.SetFloat3("_slicePosition", _transform.position);
            _voxelManipulator.SetFloat3("_location", position);
            _voxelManipulator.SetFloat("_radius", radius);
            _voxelManipulator.SetInt("_modification", modification);
            _voxelManipulator.SetBuffer("_voxels", _voxels);
            _voxelManipulator.DispatchDivByThreadGroupSize(Width, Height, Width);
            GenerateMesh();
        }

        static TerrainSlice()
        {
            int maxCubeCount = (Width - 1) * (Height - 1) * (Width - 1);
            int maxTriangleCount = maxCubeCount * 5;
            _indicesForColliders = new int[maxTriangleCount * 3];
            for (var i = 0; i < _indicesForColliders.Length; i++)
            {
                _indicesForColliders[i] = i;
            }
        }

        private void OnEnable()
        {
            _pendingBakeJobMesh = null;
            CreateComputeKernels();
            _transform = transform;
            _collider = GetComponent<MeshCollider>();
            _material = new Material(_sourceMaterial);
            _voxels = new ComputeBuffer(VoxelCount, sizeof(int), ComputeBufferType.Structured | ComputeBufferType.Counter);
            _meshTriangleCountReceiver = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            _meshTriangleCount = 0;
            var sizeVector = new Vector3(Width, Height, Width);
            Bounds = new Bounds(_transform.position + sizeVector * 0.5f, sizeVector);
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
            if (ColliderFinishedBaking())
            {
                UpdateCollider();
            }

            DrawIfMeshExists();
        }

        private void CreateComputeKernels()
        {
            _voxelGenerator = new ComputeKernel(_voxelGenerationShader, "CSMain");
            _voxelManipulator = new ComputeKernel(_voxelManipulationShader, "CSMain");
            _meshSizeCalculator = new ComputeKernel(_meshSizeCalculationShader, "CSMain");
            _meshGenerator = new ComputeKernel(_meshGenerationShader, "CSMain");
        }

        private bool ColliderFinishedBaking()
        {
            return _pendingBakeJobMesh != null && _pendingBakeJob.IsCompleted;
        }

        private void UpdateCollider()
        {
            _collider.sharedMesh = _pendingBakeJobMesh;
            _pendingBakeJobMesh = null;
        }

        private void DrawIfMeshExists()
        {
            if (_meshTriangleCount != 0)
            {
                _material.SetMatrix("_objToWorld", _transform.localToWorldMatrix);
                _material.SetBuffer("_meshPositions", _meshTriangles);
                Graphics.DrawProcedural(_material, Bounds, MeshTopology.Triangles, _meshTriangleCount * 3);
            }
        }

        private void GenerateMesh()
        {
            _voxels.SetCounterValue(0);
            _meshSizeCalculator.SetBuffer("_voxels", _voxels);
            _meshSizeCalculator.DispatchDivByThreadGroupSize(Width, Height, Width);
            ComputeBuffer.CopyCount(_voxels, _meshTriangleCountReceiver, 0);
            AsyncGPUReadback.Request(_meshTriangleCountReceiver, (request) =>
            {
                var triangleCount = request.GetData<int>()[0];
                var triangles = new ComputeBuffer(triangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Structured);
                _voxels.SetCounterValue(0);
                _meshGenerator.SetBuffer("_voxels", _voxels);
                _meshGenerator.SetBuffer("_meshTriangles", triangles);
                _meshGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
                AsyncGPUReadback.Request(triangles, (req) =>
                {
                    var mesh = new Mesh();
                    mesh.SetVertices(req.GetData<Vector3>());
                    mesh.SetIndices(_indicesForColliders, 0, triangleCount * 3, MeshTopology.Triangles, 0, false, 0);
                    _pendingBakeJob = new MeshBakeJob(mesh.GetInstanceID()).Schedule();
                    _pendingBakeJobMesh = mesh;
                    _meshTriangles?.Dispose();
                    _meshTriangles = triangles;
                    _meshTriangleCount = triangleCount;
                });
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
