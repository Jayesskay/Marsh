using UnityEngine;

namespace Marsh
{
    public class TransformController : MonoBehaviour
    {
        private Transform _transform;
        private Vector3 _rotation = new(0, 0, 0);
        [SerializeField] private float _speed;
        [SerializeField, Range(0.01f, 2.0f)] private float _lookSensitivity;
        private PlayerActions _playerActions;

        private void OnEnable()
        {
            _transform = transform;
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
            _rotation.y += lookDelta.x;
            _transform.localRotation = Quaternion.Euler(_rotation);

            var toMove = _playerActions.Fly.Move.ReadValue<Vector3>();
            _transform.Translate(toMove * _speed * Time.deltaTime);

            if (_playerActions.Fly.Build.inProgress)
            {
                var origin = _transform.position;
                var direction = _transform.rotation * Vector3.forward;
                if (Physics.Raycast(origin, direction, out var hitInfo))
                {
                    var hitTransform = hitInfo.transform;
                    if (hitTransform.gameObject.layer == LayerMask.NameToLayer("Terrain"))
                    {
                        var terrain = hitTransform.GetComponentInParent<TerrainManager>();
                        terrain.Modify(hitInfo.point, 2.0f, 1);
                    }
                }
            }
        }
    }
}
