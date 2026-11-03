using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LootCrate : MonoBehaviour
    {
        [SerializeField] private Transform _lidPivot;
        [SerializeField] private WeaponConfig[] _weaponPool;
        [SerializeField, Min(1f)] private float _interactionRange = 3.4f;
        [SerializeField, Min(1f)] private float _refillSeconds = 45f;
        [SerializeField, Range(0f, 1f)] private float _weaponChance = 0.45f;
        [SerializeField] private PrototypeInventory _playerInventory;
        [SerializeField] private Renderer[] _renderers;

        private bool _isOpen;
        private float _refillAt;
        private Quaternion _closedRotation;
        private GUIStyle _promptStyle;

        public bool IsOpen => _isOpen;
        public int SpawnedItemCount { get; private set; }

        public void Configure(Transform lidPivot, WeaponConfig[] weaponPool, float refillSeconds = 45f)
        {
            _lidPivot = lidPivot;
            _weaponPool = weaponPool;
            _refillSeconds = Mathf.Max(1f, refillSeconds);
            _renderers = GetComponentsInChildren<Renderer>();
            _closedRotation = _lidPivot != null ? _lidPivot.localRotation : Quaternion.identity;
        }

        public bool TryOpen(PrototypeInventory inventory)
        {
            if (_isOpen || inventory == null || !inventory.CanInteract)
            {
                return false;
            }

            _isOpen = true;
            _refillAt = Time.time + _refillSeconds;
            SpawnedItemCount = 0;
            SpawnSupply(SupplyKind.AmmoPack, 1 + Random.Range(0, 2), 0);
            SpawnSupply(Random.value < 0.5f ? SupplyKind.Medkit : SupplyKind.ArmorPlate, 1, 1);

            if (_weaponPool != null && _weaponPool.Length > 0 && Random.value <= _weaponChance)
            {
                WeaponConfig config = _weaponPool[Random.Range(0, _weaponPool.Length)];
                if (config != null)
                {
                    Vector3 position = GetDropPosition(2) + (Vector3.up * 0.45f);
                    if (WorldItemPickup.CreateWeaponDrop(config, config.MagazineSize, config.AmmoPerPickup, position) != null)
                    {
                        SpawnedItemCount++;
                    }
                }
            }

            return true;
        }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _closedRotation = _lidPivot != null ? _lidPivot.localRotation : Quaternion.identity;
        }

        private void Start()
        {
            _playerInventory ??= Object.FindAnyObjectByType<PrototypeInventory>();
        }

        private void Update()
        {
            _playerInventory ??= Object.FindAnyObjectByType<PrototypeInventory>();
            if (_isOpen && Time.time >= _refillAt)
            {
                _isOpen = false;
                SpawnedItemCount = 0;
            }

            if (_lidPivot != null)
            {
                Quaternion target = _closedRotation * Quaternion.Euler(_isOpen ? -105f : 0f, 0f, 0f);
                _lidPivot.localRotation = Quaternion.Slerp(
                    _lidPivot.localRotation,
                    target,
                    1f - Mathf.Exp(-10f * Time.deltaTime));
            }

            if (!_isOpen && IsPlayerNearby())
            {
                bool interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    || MobileInputBridge.ConsumeInteract();
                if (interactPressed)
                {
                    TryOpen(_playerInventory);
                }
            }
        }

        private void OnGUI()
        {
            if (!IsPlayerNearby())
            {
                return;
            }

            _promptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            string prompt = _isOpen
                ? $"补给箱已开启 · {Mathf.CeilToInt(Mathf.Max(0f, _refillAt - Time.time))}秒后补充"
                : "[E] 开启补给箱";
            GUI.Box(new Rect((Screen.width * 0.5f) - 130f, Screen.height - 132f, 260f, 38f), prompt, _promptStyle);
        }

        private void SpawnSupply(SupplyKind kind, int amount, int index)
        {
            if (WorldItemPickup.CreateSupplyDrop(kind, amount, GetDropPosition(index)) != null)
            {
                SpawnedItemCount++;
            }
        }

        private Vector3 GetDropPosition(int index)
        {
            float angle = (index * 120f + 25f) * Mathf.Deg2Rad;
            return transform.position + new Vector3(Mathf.Cos(angle) * 1.45f, 0.35f, Mathf.Sin(angle) * 1.45f);
        }

        private bool IsPlayerNearby()
        {
            return _playerInventory != null
                && _playerInventory.CanInteract
                && Vector3.Distance(_playerInventory.transform.position, transform.position) <= _interactionRange;
        }
    }
}
