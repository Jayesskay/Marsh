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

        [SerializeField] private ComputeShader _meshGenerator;
        [SerializeField] private ComputeShader _meshSizeCalculator;
        [SerializeField] private ComputeShader _voxelGenerator;
        [SerializeField] private ComputeShader _voxelManipulator;

        private Transform _transform;
        private Mesh _collisionMesh;
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
            _voxelManipulator.SetBuffer(0, "_voxels", _voxels);
            _voxelManipulator.DispatchDivByThreadGroupSize(Width, Height, Width);
            GenerateMesh();
        }

        private void OnEnable()
        {
            _pendingBakeJobMesh = null;
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
                _material.SetBuffer("_meshTriangles", _meshTriangles);
                Graphics.DrawProcedural(_material, Bounds, MeshTopology.Triangles, _meshTriangleCount * 3);
            }
        }

        private void GenerateMesh()
        {
            _voxels.SetCounterValue(0);
            _meshSizeCalculator.SetBuffer(0, "_voxels", _voxels);
            _meshSizeCalculator.DispatchDivByThreadGroupSize(Width, Height, Width);
            ComputeBuffer.CopyCount(_voxels, _meshTriangleCountReceiver, 0);
            AsyncGPUReadback.Request(_meshTriangleCountReceiver, (triangleCountRequest) =>
            {
                var triangleCount = triangleCountRequest.GetData<int>()[0];
                var triangles = new ComputeBuffer(triangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Structured);
                _voxels.SetCounterValue(0);
                _meshGenerator.SetBuffer(0, "_voxels", _voxels);
                _meshGenerator.SetBuffer(0, "_meshTriangles", triangles);
                _meshGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
                AsyncGPUReadback.Request(triangles, (trianglesRequest) =>
                {
                    var mesh = new Mesh();
                    mesh.SetVertices(trianglesRequest.GetData<Vector3>());
                    mesh.SetIndices(ColliderIndices.Values, 0, triangleCount * 3, MeshTopology.Triangles, 0, false, 0);
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
            _voxelGenerator.SetBuffer(0, "_voxels", _voxels);
            _voxelGenerator.SetFloat3("_worldPosition", _transform.position);
            _voxelGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
        }
    }
}
