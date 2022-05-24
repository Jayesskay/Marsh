using System.Collections;
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

        public void Modify(Vector3 position, float radius, int modification)
        {
            foreach (var slice in _slices)
            {
                if (!slice.Dirty && slice.Bounds.Contains(position))
                {
                    slice.Modify(position, radius, modification);
                }
            }
        }
    }
}
