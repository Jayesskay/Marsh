using System.Collections.Generic;
using UnityEngine;

namespace Marsh
{
    public class TerrainManager : MonoBehaviour
    {
        private Transform _transform;
        [SerializeField] private int _size;
        [SerializeField] private TerrainSlice _slicePrefab;
        private List<TerrainSlice> _slices = new();

        public void Modify(Vector3 position, float radius, int modification)
        {
            foreach (var slice in _slices)
            {
                var closestPoint = slice.Bounds.ClosestPoint(position);
                if (!slice.Dirty && Vector3.Distance(closestPoint, position) < radius)
                {
                    slice.Modify(position, radius, modification);
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
                    _slices.Add(slice);
                }
            }
        }
    }
}
