using UnityEngine;

namespace Marsh
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _speed;
        [SerializeField, Range(0.01f, 1.0f)] private float _lookSensitivity;

        private Transform _transform;
        private Vector3 _rotation;
        private PlayerActions _playerActions;

        private void OnEnable()
        {
            _transform = transform;
            _rotation = new Vector3(0.0f, 0.0f, 0.0f);
            _playerActions = new PlayerActions();
            _playerActions.Fly.Enable();

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            _playerActions.Fly.Disable();
        }

        private void Update()
        {
            var lookDelta = _playerActions.Fly.Look.ReadValue<Vector2>() * _lookSensitivity;
            _rotation.x -= lookDelta.y;
            _rotation.x = Mathf.Clamp(_rotation.x, -89.0f, 89.0f);
            _rotation.y += lookDelta.x;
            _transform.localRotation = Quaternion.Euler(_rotation);

            var toMove = _playerActions.Fly.Move.ReadValue<Vector3>();
            _transform.Translate(toMove * _speed * Time.deltaTime);

            if (_playerActions.Fly.Build.WasPerformedThisFrame())
            {
                ShootRayForModification(1);
            }

            if (_playerActions.Fly.RemoveTerrain.WasPerformedThisFrame())
            {
                ShootRayForModification(0);
            }
        }

        private void ShootRayForModification(int modification)
        {
            var origin = _transform.position;
            var direction = _transform.rotation * Vector3.forward;

            if (Physics.Raycast(origin, direction, out var hit))
            {
                var hitTransform = hit.transform;
                if (hitTransform.gameObject.layer == LayerMask.NameToLayer("Terrain"))
                {
                    var terrain = hitTransform.GetComponentInParent<TerrainManager>();
                    terrain.Modify(hit.point, 12.0f, modification);
                }
            }
        }
    }
}
