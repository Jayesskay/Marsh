using System.Collections.Generic;
using UnityEngine;

namespace Marsh
{
    public class TerrainManager : MonoBehaviour
    {
        private Transform _transform;
        [SerializeField] private int _size;
        [SerializeField] private TerrainSlice _slicePrefab;
        private Dictionary<Vector3Int, TerrainSlice> _slices = new();

        public void Modify(Vector3 position, float radius, int modification)
        {
            foreach (var (worldIndex, slice) in _slices)
            {
                if (!slice.Dirty && slice.Bounds.Contains(position))
                {
                    slice.Modify(position, radius, modification);
                    slice.GenerateMesh();

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(0, 0, 1), out var sliceFront))
                    {
                        if (!sliceFront.Dirty)
                        {
                            sliceFront.Modify(position, radius, modification);
                            sliceFront.GenerateMesh();
                        }
                    }

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(0, 0, -1), out var sliceBack))
                    {
                        if (!sliceBack.Dirty)
                        {
                            sliceBack.Modify(position, radius, modification);
                            sliceBack.GenerateMesh();
                        }
                    }

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(1, 0, 0), out var sliceRight))
                    {
                        if (!sliceRight.Dirty)
                        {
                            sliceRight.Modify(position, radius, modification);
                            sliceRight.GenerateMesh();
                        }
                    }

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(-1, 0, 0), out var sliceLeft))
                    {
                        if (!sliceLeft.Dirty)
                        {
                            sliceLeft.Modify(position, radius, modification);
                            sliceLeft.GenerateMesh();
                        }
                    }

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(0, 1, 0), out var sliceUp))
                    {
                        if (!sliceUp.Dirty)
                        {
                            sliceUp.Modify(position, radius, modification);
                            sliceUp.GenerateMesh();
                        }
                    }

                    if (_slices.TryGetValue(worldIndex + new Vector3Int(0, -1, 0), out var sliceDown))
                    {
                        if (!sliceDown.Dirty)
                        {
                            sliceDown.Modify(position, radius, modification);
                            sliceDown.GenerateMesh();
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            _transform = transform;
            for (var z = 0; z < _size; z++)
            {
                for (var x = 0; x < _size; x++)
                {
                    var worldPosition = new Vector3(
                        (TerrainSlice.Width - 1) * x, 0.0f, (TerrainSlice.Width - 1) * z
                    );

                    var slice = Instantiate(_slicePrefab, worldPosition, Quaternion.identity, _transform);
                    slice.GenerateVoxels();
                    slice.GenerateMesh();

                    var index = new Vector3Int(x, 0, z);
                    _slices.Add(index, slice);
                }
            }
        }
    }
}
