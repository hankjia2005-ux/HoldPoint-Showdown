using HoldPointShowdown.Data;
using HoldPointShowdown.Core;
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
        private Quaternion _closedRotation;
        private GUIStyle _promptStyle;
        private LootDirector _director;
        private MatchPhase _openedPhase = (MatchPhase)(-1);

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
            MatchPhase phase = _director != null ? _director.CurrentPhase : MatchPhase.Scavenge;
            if ((_isOpen && _openedPhase == phase) || inventory == null || !inventory.CanInteract)
            {
                return false;
            }

            _isOpen = true;
            _openedPhase = phase;
            SpawnedItemCount = 0;
            if (_director != null)
            {
                var drops = _director.RequestDrops(3);
                if (drops.Count == 0)
                {
                    _isOpen = false;
                    return false;
                }
                for (int index = 0; index < drops.Count; index++)
                {
                    LootDrop drop = drops[index];
                    if (drop.IsWeapon)
                    {
                        Vector3 position = GetDropPosition(index) + (Vector3.up * 0.45f);
                        if (WorldItemPickup.CreateWeaponDrop(
                                drop.Weapon,
                                drop.Weapon.MagazineSize,
                                drop.Weapon.AmmoPerPickup,
                                position) != null)
                        {
                            SpawnedItemCount++;
                        }
                    }
                    else
                    {
                        SpawnSupply(drop.Supply, 1, index);
                    }
                }
                return true;
            }

            SpawnSupply(SupplyKind.AmmoPack, 1 + Random.Range(0, 2), 0);
            SpawnSupply(Random.value < 0.5f ? SupplyKind.Medkit : SupplyKind.ArmorPlate, 1, 1);
            if (_weaponPool != null && _weaponPool.Length > 0 && Random.value <= _weaponChance)
            {
                WeaponConfig config = _weaponPool[Random.Range(0, _weaponPool.Length)];
                if (config != null && WorldItemPickup.CreateWeaponDrop(
                        config,
                        config.MagazineSize,
                        config.AmmoPerPickup,
                        GetDropPosition(2) + (Vector3.up * 0.45f)) != null)
                {
                    SpawnedItemCount++;
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
            _director = Object.FindAnyObjectByType<LootDirector>();
        }

        private void Update()
        {
            _playerInventory ??= Object.FindAnyObjectByType<PrototypeInventory>();
            if (_isOpen && _director != null && _director.CurrentPhase != _openedPhase)
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
                ? "补给箱已搜空 · 下一阶段可能补充"
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
