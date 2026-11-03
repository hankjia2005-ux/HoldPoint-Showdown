using System.Collections;
using System.Collections.Generic;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SharedSupplyChest : MonoBehaviour
    {
        [System.Serializable]
        private sealed class StoredWeapon
        {
            public WeaponConfig Config;
            public int MagazineAmmo;
            public int ReserveAmmo;
        }

        [SerializeField] private TeamId _team;
        [SerializeField, Min(0)] private int _ammoPacks = 4;
        [SerializeField, Min(0)] private int _medkits = 3;
        [SerializeField, Min(0)] private int _armorPlates = 3;
        [SerializeField, Min(1f)] private float _interactionRange = 4.5f;
        [SerializeField, Min(1)] private int _maxPerKind = 20;
        [SerializeField, Min(1)] private int _maxStoredWeapons = 8;
        [SerializeField] private List<StoredWeapon> _storedWeapons = new();

        [SerializeField] private Transform _player;
        [SerializeField] private TeamMember _playerTeam;
        [SerializeField] private PrototypeInventory _inventory;
        [SerializeField] private WeaponLoadout _loadout;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private MatchPhaseController _match;
        private bool _isOpen;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private Texture2D _panelTexture;

        public int AmmoPacks => _ammoPacks;
        public int Medkits => _medkits;
        public int ArmorPlates => _armorPlates;
        public int StoredWeaponCount => _storedWeapons.Count;
        public TeamId Team => _team;

        public int AddSupply(SupplyKind kind, int amount)
        {
            int before = GetStock(kind);
            int after = Mathf.Clamp(before + Mathf.Max(0, amount), 0, _maxPerKind);
            SetStock(kind, after);
            if (after > before)
            {
                Flash(new Color(1f, 0.78f, 0.18f));
            }
            return after - before;
        }

        public void Configure(
            TeamId team,
            Transform player,
            TeamMember playerTeam,
            PrototypeInventory inventory,
            MatchPhaseController match = null)
        {
            _team = team;
            _player = player;
            _playerTeam = playerTeam;
            _inventory = inventory;
            _loadout = inventory != null ? inventory.Loadout : null;
            _match = match;
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Update()
        {
            if (!IsPlayerNearby() || _inventory == null)
            {
                _isOpen = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            bool togglePressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                || MobileInputBridge.ConsumeInteract();
            if (togglePressed)
            {
                _isOpen = !_isOpen;
            }

            if (!_isOpen)
            {
                return;
            }

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                TryWithdrawSupply(SupplyKind.AmmoPack);
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                TryWithdrawSupply(SupplyKind.Medkit);
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                TryWithdrawSupply(SupplyKind.ArmorPlate);
            }
            else if (keyboard.f4Key.wasPressedThisFrame)
            {
                TryWithdrawWeapon();
            }
            else if (keyboard.f5Key.wasPressedThisFrame)
            {
                TryDepositSupply(SupplyKind.AmmoPack);
            }
            else if (keyboard.f6Key.wasPressedThisFrame)
            {
                TryDepositSupply(SupplyKind.Medkit);
            }
            else if (keyboard.f7Key.wasPressedThisFrame)
            {
                TryDepositSupply(SupplyKind.ArmorPlate);
            }
            else if (keyboard.f8Key.wasPressedThisFrame)
            {
                TryDepositActiveWeapon();
            }
        }

        private void OnGUI()
        {
            if (!IsPlayerNearby())
            {
                return;
            }

            EnsureStyles();
            float width = 430f;
            bool mobile = MobileInputBridge.IsEnabled;
            float panelHeight = _isOpen ? (mobile ? 368f : 264f) : 54f;
            Rect panel = new((Screen.width - width) * 0.5f, Screen.height - panelHeight - 38f, width, panelHeight);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            string side = _team == TeamId.Blue ? "蓝方" : "红方";
            bool friendly = IsFriendly();
            if (!_isOpen)
            {
                GUI.Label(panel, $"[E] 打开{side}共享物资箱{(friendly ? string.Empty : "（敌方：仅可抢夺）")}", _titleStyle);
                return;
            }

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 30f), $"{side}共享物资箱", _titleStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 48f, panel.width - 48f, 112f),
                $"取出：[F1] 弹药包 {_ammoPacks}/{_maxPerKind}   [F2] 医疗包 {_medkits}/{_maxPerKind}   [F3] 护甲片 {_armorPlates}/{_maxPerKind}\n" +
                $"[F4] 武器 {_storedWeapons.Count}/{_maxStoredWeapons}   {FormatFirstStoredWeapon()}\n" +
                $"个人背包：{_inventory.UsedSlots}/{_inventory.Capacity}   弹药 {_inventory.AmmoPacks} / 医疗 {_inventory.Medkits} / 护甲 {_inventory.ArmorPlates}",
                _bodyStyle);

            string operation = friendly
                ? "存入：[F5]弹药 [F6]医疗 [F7]护甲 [F8]当前非初始武器  ·  E关闭"
                : "F1–F4 抢夺敌方物资  ·  敌方仓库不可存入  ·  E关闭";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 180f, panel.width - 40f, 62f), operation, _bodyStyle);
            if (mobile && _isOpen)
            {
                string[] withdrawLabels = { "取弹", "取医", "取甲", "取枪" };
                for (int index = 0; index < withdrawLabels.Length; index++)
                {
                    if (!GUI.Button(new Rect(panel.x + 18f + (index * 99f), panel.y + 244f, 92f, 42f), withdrawLabels[index])) continue;
                    if (index < 3) TryWithdrawSupply((SupplyKind)index); else TryWithdrawWeapon();
                }
                if (friendly)
                {
                    string[] depositLabels = { "存弹", "存医", "存甲", "存枪" };
                    for (int index = 0; index < depositLabels.Length; index++)
                    {
                        if (!GUI.Button(new Rect(panel.x + 18f + (index * 99f), panel.y + 296f, 92f, 42f), depositLabels[index])) continue;
                        if (index < 3) TryDepositSupply((SupplyKind)index); else TryDepositActiveWeapon();
                    }
                }
            }
        }

        public bool TryDepositSupply(SupplyKind kind)
        {
            if (_inventory == null || !IsFriendly() || GetStock(kind) >= _maxPerKind || !_inventory.TryRemove(kind))
            {
                return false;
            }

            SetStock(kind, GetStock(kind) + 1);
            Flash(new Color(0.25f, 0.85f, 1f));
            return true;
        }

        public bool TryWithdrawSupply(SupplyKind kind)
        {
            if (_inventory == null)
            {
                return false;
            }

            int stock = GetStock(kind);
            if (stock <= 0 || !_inventory.TryAdd(kind))
            {
                return false;
            }

            SetStock(kind, stock - 1);
            NotifyTheftIfNeeded();
            Flash(IsFriendly() ? new Color(0.25f, 0.85f, 1f) : new Color(1f, 0.15f, 0.12f));
            return true;
        }

        public bool TryDepositActiveWeapon()
        {
            _loadout ??= _inventory != null ? _inventory.Loadout : null;
            if (!IsFriendly() || _loadout == null || _storedWeapons.Count >= _maxStoredWeapons
                || !_loadout.TryRemoveActiveForStorage(out WeaponConfig config, out int magazine, out int reserve))
            {
                return false;
            }

            _storedWeapons.Add(new StoredWeapon
            {
                Config = config,
                MagazineAmmo = magazine,
                ReserveAmmo = reserve
            });
            Flash(new Color(0.25f, 0.85f, 1f));
            return true;
        }

        public bool TryWithdrawWeapon()
        {
            if (_inventory == null || _storedWeapons.Count <= 0)
            {
                return false;
            }

            StoredWeapon stored = _storedWeapons[0];
            if (stored.Config == null || !_inventory.TryAddWeapon(stored.Config, stored.MagazineAmmo, stored.ReserveAmmo))
            {
                return false;
            }

            _storedWeapons.RemoveAt(0);
            NotifyTheftIfNeeded();
            Flash(IsFriendly() ? new Color(0.25f, 0.85f, 1f) : new Color(1f, 0.15f, 0.12f));
            return true;
        }

        private string FormatFirstStoredWeapon()
        {
            return _storedWeapons.Count > 0 && _storedWeapons[0].Config != null
                ? $"下一把：{_storedWeapons[0].Config.DisplayName}"
                : "暂无武器";
        }

        private void NotifyTheftIfNeeded()
        {
            if (!IsFriendly())
            {
                _match?.RaiseMessage($"{(_team == TeamId.Blue ? "蓝方" : "红方")}基地物资被抢", 3f);
            }
        }

        private void Flash(Color color)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(FlashChest(color));
            }
        }

        private bool IsFriendly()
        {
            return _playerTeam != null && _playerTeam.Team == _team;
        }

        private bool IsPlayerNearby()
        {
            if (_player == null)
            {
                return false;
            }

            Vector3 center = transform.position;
            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null)
            {
                Bounds bounds = _renderers[0].bounds;
                for (int i = 1; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                    {
                        bounds.Encapsulate(_renderers[i].bounds);
                    }
                }

                center = bounds.center;
            }

            return _inventory != null
                && _inventory.CanInteract
                && Vector3.Distance(_player.position, center) <= _interactionRange;
        }

        private int GetStock(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => _ammoPacks,
                SupplyKind.Medkit => _medkits,
                SupplyKind.ArmorPlate => _armorPlates,
                _ => 0
            };
        }

        private void SetStock(SupplyKind kind, int value)
        {
            value = Mathf.Max(0, value);
            switch (kind)
            {
                case SupplyKind.AmmoPack:
                    _ammoPacks = value;
                    break;
                case SupplyKind.Medkit:
                    _medkits = value;
                    break;
                case SupplyKind.ArmorPlate:
                    _armorPlates = value;
                    break;
            }
        }

        private IEnumerator FlashChest(Color color)
        {
            MaterialPropertyBlock block = new();
            block.SetColor("_BaseColor", color);
            foreach (Renderer renderer in _renderers)
            {
                renderer?.SetPropertyBlock(block);
            }

            yield return new WaitForSeconds(0.16f);
            foreach (Renderer renderer in _renderers)
            {
                renderer?.SetPropertyBlock(null);
            }
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _panelTexture.SetPixel(0, 0, new Color(0.025f, 0.05f, 0.08f, 0.95f));
            _panelTexture.Apply();
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = _panelTexture;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(_titleStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 0.9f, 1f) }
            };
        }
    }
}
