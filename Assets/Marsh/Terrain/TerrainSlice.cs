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

        [SerializeField] private ComputeShader _meshGenerator;
        [SerializeField] private ComputeShader _meshSizeCalculator;
        [SerializeField] private ComputeShader _voxelGenerator;
        [SerializeField] private ComputeShader _voxelManipulator;
        [SerializeField] private Material _sourceMaterial;

        public Bounds Bounds { get; private set; }
        public bool Dirty { get; private set; }

        private Transform _transform;
        private MeshCollider _collider;
        private Material _material;
        private ComputeBuffer _voxels;
        private TerrainSliceMesh _currentMesh;
        private TerrainSliceMesh _updatedMesh;

        public void Modify(Vector3 position, float radius, int modification)
        {
            _voxelManipulator.SetFloat3("_slicePosition", _transform.position);
            _voxelManipulator.SetFloat3("_location", position);
            _voxelManipulator.SetFloat("_radius", radius);
            _voxelManipulator.SetInt("_modification", modification);
            _voxelManipulator.SetBuffer(0, "_voxels", _voxels);
            _voxelManipulator.DispatchDivByThreadGroupSize(Width, Height, Width);
            Dirty = true;
        }

        public void GenerateVoxels()
        {
            _voxelGenerator.SetBuffer(0, "_voxels", _voxels);
            _voxelGenerator.SetFloat3("_worldPosition", _transform.position);
            _voxelGenerator.DispatchDivByThreadGroupSize(Width, Height, Width);
            Dirty = true;
        }

        public void GenerateMesh()
        {
            _updatedMesh = new TerrainSliceMesh(_meshSizeCalculator, _meshGenerator, _voxels);
        }

        private void OnEnable()
        {
            _transform = transform;
            _collider = GetComponent<MeshCollider>();
            _material = new Material(_sourceMaterial);
            _voxels = new ComputeBuffer(VoxelCount, sizeof(int), ComputeBufferType.Structured | ComputeBufferType.Counter);
            var sizeVector = new Vector3(Width, Height, Width);
            Bounds = new Bounds(_transform.position + sizeVector * 0.5f, sizeVector);
        }

        private void OnDisable()
        {
            _voxels?.Dispose();
            _currentMesh?.Destroy();
            _updatedMesh?.Destroy();
        }

        private void Update()
        {
            if (_updatedMesh != null && _updatedMesh.ColliderIsReady)
            {
                if (_currentMesh != null)
                    _currentMesh.Destroy();

                _currentMesh = _updatedMesh;
                _updatedMesh = null;
                _collider.sharedMesh = _currentMesh.CollisionMesh;
                Dirty = false;
            }

            if (_currentMesh != null)
            {
                _material.SetMatrix("_objToWorld", _transform.localToWorldMatrix);
                _material.SetBuffer("_meshTriangles", _currentMesh.Triangles);
                Graphics.DrawProcedural(_material, Bounds, MeshTopology.Triangles, _currentMesh.TriangleCount * 3);
            }
        }
    }
}
