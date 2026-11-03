using HoldPointShowdown.Combat;
using UnityEngine;

namespace HoldPointShowdown.UI
{
    [DisallowMultipleComponent]
    public sealed class DamageIndicator : MonoBehaviour
    {
        public const float IndicatorDuration = 0.6f;

        [SerializeField] private Damageable _player;
        [SerializeField] private Camera _camera;

        private Texture2D _redTexture;
        private float _indicatorUntil;
        private int _activeDirection;

        public int ActiveDirection => Time.unscaledTime < _indicatorUntil ? _activeDirection : -1;

        public void Configure(Damageable player, Camera playerCamera)
        {
            Unbind();
            _player = player;
            _camera = playerCamera;
            Bind();
        }

        public static float CalculateRelativeAngle(float cameraYaw, Vector3 origin, Vector3 source)
        {
            Vector3 direction = Vector3.ProjectOnPlane(source - origin, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            float worldAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(cameraYaw, worldAngle);
        }

        public static int AngleToDirection(float relativeAngle)
        {
            float angle = Mathf.DeltaAngle(0f, relativeAngle);
            if (angle >= -45f && angle < 45f) return 0; // front / top
            if (angle >= 45f && angle < 135f) return 1; // right
            if (angle >= -135f && angle < -45f) return 3; // left
            return 2; // rear / bottom
        }

        private void Awake()
        {
            _redTexture = MakeTexture(new Color(1f, 0.08f, 0.04f, 1f));
        }

        private void Start()
        {
            _camera ??= Camera.main;
            Bind();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void HandleDamaged(Damageable target, float _, Vector3 hitPoint, GameObject instigator)
        {
            Camera sourceCamera = _camera != null ? _camera : Camera.main;
            Vector3 source = instigator != null ? instigator.transform.position : hitPoint;
            float yaw = sourceCamera != null ? sourceCamera.transform.eulerAngles.y : target.transform.eulerAngles.y;
            _activeDirection = AngleToDirection(CalculateRelativeAngle(yaw, target.transform.position, source));
            _indicatorUntil = Time.unscaledTime + IndicatorDuration;
        }

        private void OnGUI()
        {
            if (_redTexture == null)
            {
                return;
            }

            DrawLowHealthVignette();
            if (Time.unscaledTime >= _indicatorUntil)
            {
                return;
            }

            float alpha = Mathf.Clamp01((_indicatorUntil - Time.unscaledTime) / IndicatorDuration);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha * 0.9f);
            Rect rect = _activeDirection switch
            {
                0 => new Rect((Screen.width * 0.5f) - 90f, 26f, 180f, 18f),
                1 => new Rect(Screen.width - 44f, (Screen.height * 0.5f) - 90f, 18f, 180f),
                2 => new Rect((Screen.width * 0.5f) - 90f, Screen.height - 44f, 180f, 18f),
                _ => new Rect(26f, (Screen.height * 0.5f) - 90f, 18f, 180f)
            };
            GUI.DrawTexture(rect, _redTexture);
            GUI.color = previous;
        }

        private void DrawLowHealthVignette()
        {
            if (_player == null || _player.MaxHealth <= 0f)
            {
                return;
            }

            float ratio = _player.CurrentHealth / _player.MaxHealth;
            if (ratio >= 0.4f)
            {
                return;
            }

            float danger = Mathf.InverseLerp(0.4f, 0f, ratio);
            float thickness = Mathf.Lerp(28f, 82f, danger);
            Color previous = GUI.color;
            GUI.color = new Color(0.38f, 0f, 0f, Mathf.Lerp(0.16f, 0.48f, danger));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, thickness), _redTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - thickness, Screen.width, thickness), _redTexture);
            GUI.DrawTexture(new Rect(0f, 0f, thickness, Screen.height), _redTexture);
            GUI.DrawTexture(new Rect(Screen.width - thickness, 0f, thickness, Screen.height), _redTexture);
            GUI.color = previous;
        }

        private void Bind()
        {
            if (_player != null)
            {
                _player.Damaged -= HandleDamaged;
                _player.Damaged += HandleDamaged;
            }
        }

        private void Unbind()
        {
            if (_player != null)
            {
                _player.Damaged -= HandleDamaged;
            }
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
