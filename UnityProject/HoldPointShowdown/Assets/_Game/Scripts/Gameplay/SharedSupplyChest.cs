using System.Collections;
using HoldPointShowdown.Core;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SharedSupplyChest : MonoBehaviour
    {
        [SerializeField] private TeamId _team;
        [SerializeField, Min(0)] private int _ammoPacks = 4;
        [SerializeField, Min(0)] private int _medkits = 3;
        [SerializeField, Min(0)] private int _armorPlates = 3;
        [SerializeField, Min(1f)] private float _interactionRange = 4.5f;
        [SerializeField, Min(1)] private int _maxPerKind = 20;

        [SerializeField] private Transform _player;
        [SerializeField] private TeamMember _playerTeam;
        [SerializeField] private PrototypeInventory _inventory;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private MatchPhaseController _match;
        private bool _isOpen;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private Texture2D _panelTexture;

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
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                _isOpen = !_isOpen;
            }

            if (!_isOpen)
            {
                return;
            }

            bool depositing = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                Transfer(SupplyKind.AmmoPack, depositing);
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                Transfer(SupplyKind.Medkit, depositing);
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                Transfer(SupplyKind.ArmorPlate, depositing);
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
            Rect panel = new((Screen.width - width) * 0.5f, Screen.height - (_isOpen ? 248f : 92f), width, _isOpen ? 210f : 54f);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            string side = _team == TeamId.Blue ? "蓝方" : "红方";
            bool friendly = IsFriendly();
            if (!_isOpen)
            {
                GUI.Label(panel, $"[E] 打开{side}共享消耗品箱{(friendly ? string.Empty : "（敌方：仅可取出）")}", _titleStyle);
                return;
            }

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 30f), $"{side}共享消耗品箱", _titleStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 48f, panel.width - 48f, 78f),
                $"[F1] 弹药包 {_ammoPacks}/{_maxPerKind}     [F2] 医疗包 {_medkits}/{_maxPerKind}     [F3] 护甲片 {_armorPlates}/{_maxPerKind}\n" +
                $"个人背包：{_inventory.UsedSlots}/{_inventory.Capacity}   弹药 {_inventory.AmmoPacks} / 医疗 {_inventory.Medkits} / 护甲 {_inventory.ArmorPlates}",
                _bodyStyle);

            string operation = friendly
                ? "F1/F2/F3 取出  ·  Shift + F1/F2/F3 存入  ·  E关闭"
                : "F1/F2/F3 抢夺物资  ·  敌方物资不可存入  ·  E关闭";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 142f, panel.width - 40f, 42f), operation, _bodyStyle);
        }

        private void Transfer(SupplyKind kind, bool depositing)
        {
            if (_inventory == null)
            {
                return;
            }

            if (depositing)
            {
                if (!IsFriendly() || GetStock(kind) >= _maxPerKind || !_inventory.TryRemove(kind))
                {
                    return;
                }

                SetStock(kind, GetStock(kind) + 1);
            }
            else
            {
                int stock = GetStock(kind);
                if (stock <= 0 || !_inventory.TryAdd(kind))
                {
                    return;
                }

                SetStock(kind, stock - 1);
                if (!IsFriendly())
                {
                    _match?.RaiseMessage($"{(_team == TeamId.Blue ? "蓝方" : "红方")}基地物资被抢", 3f);
                }
            }

            StartCoroutine(FlashChest(IsFriendly() ? new Color(0.25f, 0.85f, 1f) : new Color(1f, 0.15f, 0.12f)));
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
