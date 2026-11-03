using System.Collections;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [SerializeField] private SupplyKind _supplyKind;
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField] private WeaponConfig _weapon;
        [SerializeField, Min(0f)] private float _respawnSeconds = 22f;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private int _weaponMagazine = -1;
        [SerializeField] private int _weaponReserve = -1;

        private SphereCollider _trigger;
        private bool _available = true;
        private float _collectableAt;
        private float _despawnAt;
        private MatchPhaseController _match;

        public bool IsWeapon => _weapon != null;
        public bool IsAvailable => _available;
        public WeaponConfig Weapon => _weapon;
        public SupplyKind SupplyKind => _supplyKind;

        public void ConfigureSupply(SupplyKind kind, int amount, float respawnSeconds, GameObject visualRoot)
        {
            _supplyKind = kind;
            _amount = Mathf.Max(1, amount);
            _weapon = null;
            _weaponMagazine = -1;
            _weaponReserve = -1;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            _collectableAt = 0f;
            _despawnAt = 0f;
            EnsureTrigger();
        }

        public void ConfigureWeapon(WeaponConfig weapon, float respawnSeconds, GameObject visualRoot)
        {
            _weapon = weapon;
            _amount = 1;
            _weaponMagazine = -1;
            _weaponReserve = -1;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            _collectableAt = 0f;
            _despawnAt = 0f;
            EnsureTrigger();
        }

        public void ConfigureDroppedWeapon(
            WeaponConfig weapon,
            int magazineAmmo,
            int reserveAmmo,
            GameObject visualRoot,
            float pickupDelay = 0.8f,
            float despawnSeconds = 60f)
        {
            _weapon = weapon;
            _amount = 1;
            _weaponMagazine = Mathf.Max(0, magazineAmmo);
            _weaponReserve = Mathf.Max(0, reserveAmmo);
            _respawnSeconds = 0f;
            _visualRoot = visualRoot;
            _collectableAt = Time.time + Mathf.Max(0f, pickupDelay);
            _despawnAt = despawnSeconds > 0f ? Time.time + despawnSeconds : 0f;
            EnsureTrigger();
        }

        public void ConfigureDroppedSupply(
            SupplyKind kind,
            int amount,
            GameObject visualRoot,
            float pickupDelay = 0.6f,
            float despawnSeconds = 60f)
        {
            _supplyKind = kind;
            _amount = Mathf.Max(1, amount);
            _weapon = null;
            _weaponMagazine = -1;
            _weaponReserve = -1;
            _respawnSeconds = 0f;
            _visualRoot = visualRoot;
            _collectableAt = Time.time + Mathf.Max(0f, pickupDelay);
            _despawnAt = despawnSeconds > 0f ? Time.time + despawnSeconds : 0f;
            EnsureTrigger();
        }

        public bool TryCollect(PrototypeInventory inventory)
        {
            if (!_available || inventory == null || Time.time < _collectableAt)
            {
                return false;
            }

            if (IsWeapon)
            {
                if (!inventory.TryAddWeapon(_weapon, _weaponMagazine, _weaponReserve))
                {
                    return false;
                }
            }
            else
            {
                int collectedAmount = inventory.TryAddUpTo(_supplyKind, _amount);
                if (collectedAmount <= 0)
                {
                    return false;
                }

                _amount -= collectedAmount;
                if (_amount > 0)
                {
                    return true;
                }
            }

            if (isActiveAndEnabled)
            {
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                SetAvailable(false);
            }

            return true;
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void Start()
        {
            _match = Object.FindAnyObjectByType<MatchPhaseController>();
        }

        private void Update()
        {
            if (_despawnAt > 0f && Time.time >= _despawnAt)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PrototypeInventory inventory = other.GetComponentInParent<PrototypeInventory>();
            TryCollect(inventory);
        }

        private IEnumerator RespawnRoutine()
        {
            SetAvailable(false);
            if (_respawnSeconds <= 0f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(_respawnSeconds);
            if (_match != null && _match.CurrentPhase >= HoldPointShowdown.Core.MatchPhase.FinalBattle)
            {
                yield break;
            }

            _collectableAt = 0f;
            SetAvailable(true);
        }

        public static WorldItemPickup CreateSupplyDrop(SupplyKind kind, int amount, Vector3 position)
        {
            GameObject root = new($"掉落 · {FormatSupplyName(kind)} × {Mathf.Max(1, amount)}");
            root.transform.position = position;

            PrimitiveType shape = kind == SupplyKind.ArmorPlate ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject visual = GameObject.CreatePrimitive(shape);
            visual.name = "掉落物模型";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 0.45f;
            visual.transform.localScale = kind switch
            {
                SupplyKind.AmmoPack => new Vector3(0.65f, 0.42f, 0.48f),
                SupplyKind.Medkit => new Vector3(0.7f, 0.55f, 0.5f),
                _ => new Vector3(0.72f, 0.18f, 0.72f)
            };
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material material = new(shader)
                {
                    color = kind switch
                    {
                        SupplyKind.AmmoPack => new Color(1f, 0.65f, 0.12f),
                        SupplyKind.Medkit => new Color(1f, 0.25f, 0.22f),
                        _ => new Color(0.12f, 0.78f, 0.95f)
                    }
                };
                renderer.material = material;
            }

            WorldItemPickup pickup = root.AddComponent<WorldItemPickup>();
            pickup.ConfigureDroppedSupply(kind, amount, visual);
            return pickup;
        }

        public static WorldItemPickup CreateWeaponDrop(
            WeaponConfig config,
            int magazineAmmo,
            int reserveAmmo,
            Vector3 position,
            float pickupDelay = 0.6f,
            float despawnSeconds = 60f)
        {
            if (config == null)
            {
                return null;
            }

            GameObject root = new($"掉落 · {config.DisplayName}");
            root.transform.position = position;
            GameObject visual;
            if (config.ModelPrefab != null)
            {
                visual = Instantiate(config.ModelPrefab, root.transform);
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
                visual.transform.localScale *= 1.15f;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.28f, 0.18f, 1.1f);
                Collider fallbackCollider = visual.GetComponent<Collider>();
                if (fallbackCollider != null)
                {
                    Destroy(fallbackCollider);
                }
            }

            visual.name = "掉落武器模型";
            visual.AddComponent<ShowcasePropMotion>().Configure(48f, 0.14f, 1.15f);
            WorldItemPickup pickup = root.AddComponent<WorldItemPickup>();
            pickup.ConfigureDroppedWeapon(
                config,
                magazineAmmo >= 0 ? magazineAmmo : config.MagazineSize,
                reserveAmmo >= 0 ? reserveAmmo : config.StartingReserveAmmo,
                visual,
                pickupDelay,
                despawnSeconds);
            return pickup;
        }

        private static string FormatSupplyName(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => "弹药包",
                SupplyKind.Medkit => "医疗包",
                SupplyKind.ArmorPlate => "护甲片",
                _ => "物资"
            };
        }

        private void SetAvailable(bool available)
        {
            _available = available;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(available);
            }

            if (_trigger != null)
            {
                _trigger.enabled = available;
            }
        }

        private void EnsureTrigger()
        {
            _trigger = GetComponent<SphereCollider>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<SphereCollider>();
            }

            _trigger.isTrigger = true;
            _trigger.radius = 1.35f;
        }
    }
}
