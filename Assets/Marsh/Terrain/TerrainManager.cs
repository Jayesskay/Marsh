using System.Collections.Generic;
using UnityEngine;

namespace Marsh
{
    public class TerrainManager : MonoBehaviour
    {
        [SerializeField] private Vector3Int _size;
        [SerializeField] private TerrainSlice _slicePrefab;

        private Transform _transform;
        private List<TerrainSlice> _slices = new();

        public void Modify(Vector3 position, float radius, int modification)
        {
            foreach (var slice in _slices)
            {
                if (slice.Bounds.SqrDistance(position) < (radius * radius) + 1.0f)
                {
                    slice.Modify(position, radius, modification);
                }
            }
        }

        private void OnEnable()
        {
            _transform = transform;
            for (var z = 0; z < _size.z; z++)
            {
                for (var y = 0; y < _size.y; y++)
                {
                    for (var x = 0; x < _size.x; x++)
                    {
                        var worldPosition = new Vector3(
                            (TerrainSlice.Width - 1) * x,
                            (TerrainSlice.Height - 1) * y,
                            (TerrainSlice.Width - 1) * z
                        );

                        var slice = Instantiate(_slicePrefab, worldPosition, Quaternion.identity, _transform);
                        _slices.Add(slice);
                    }
                }
            }
        }
    }
}
