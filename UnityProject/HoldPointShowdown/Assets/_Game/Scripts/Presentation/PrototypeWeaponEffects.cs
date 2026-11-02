using System.Collections;
using HoldPointShowdown.Combat;
using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWeaponEffects : MonoBehaviour
    {
        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private LineRenderer _tracer;
        [SerializeField] private ParticleSystem _muzzleParticles;
        [SerializeField] private ParticleSystem _hitParticles;
        [SerializeField] private Light _muzzleLight;
        private Coroutine _tracerRoutine;
        private bool _isSubscribed;

        public void Configure(PrototypeHitscanWeapon weapon, Transform muzzle)
        {
            Unsubscribe();
            _weapon = weapon;
            _muzzle = muzzle;
            Subscribe();
            EnsureEffects();
        }

        public void PlayExternalShot(Vector3 origin, Vector3 endPoint, bool hit)
        {
            EnsureEffects();
            PlayShot(origin, endPoint, hit);
        }

        private void OnEnable()
        {
            Subscribe();
            EnsureEffects();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleFired(HitscanShot shot)
        {
            Vector3 origin = _muzzle != null ? _muzzle.position : shot.Origin;
            PlayShot(origin, shot.EndPoint, shot.Hit);
        }

        private void PlayShot(Vector3 origin, Vector3 endPoint, bool hit)
        {
            _tracer.SetPosition(0, origin);
            _tracer.SetPosition(1, endPoint);
            _tracer.enabled = true;
            if (_tracerRoutine != null)
            {
                StopCoroutine(_tracerRoutine);
            }

            _tracerRoutine = StartCoroutine(HideTracer());
            _muzzleParticles.transform.position = origin;
            _muzzleParticles.transform.forward = (endPoint - origin).normalized;
            _muzzleParticles.Play(true);
            _muzzleLight.transform.position = origin;
            _muzzleLight.enabled = true;
            StartCoroutine(HideMuzzleLight());

            if (hit)
            {
                _hitParticles.transform.position = endPoint;
                _hitParticles.Play(true);
            }
        }

        private void EnsureEffects()
        {
            if (_tracer != null)
            {
                return;
            }

            GameObject tracerObject = new("曳光弹");
            tracerObject.transform.SetParent(transform, false);
            _tracer = tracerObject.AddComponent<LineRenderer>();
            _tracer.positionCount = 2;
            _tracer.startWidth = 0.055f;
            _tracer.endWidth = 0.012f;
            _tracer.startColor = new Color(1f, 0.88f, 0.25f, 1f);
            _tracer.endColor = new Color(1f, 0.35f, 0.1f, 0.1f);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _tracer.material = new Material(shader);
            }
            _tracer.enabled = false;

            _muzzleParticles = CreateBurst("枪口火焰", new Color(1f, 0.65f, 0.08f), 12, 0.08f, 0.12f, 2.2f);
            _hitParticles = CreateBurst("命中特效", new Color(0.3f, 0.9f, 1f), 16, 0.16f, 0.09f, 2.8f);

            GameObject lightObject = new("枪口闪光");
            lightObject.transform.SetParent(transform, false);
            _muzzleLight = lightObject.AddComponent<Light>();
            _muzzleLight.type = LightType.Point;
            _muzzleLight.color = new Color(1f, 0.55f, 0.12f);
            _muzzleLight.range = 4f;
            _muzzleLight.intensity = 5f;
            _muzzleLight.enabled = false;
        }

        private ParticleSystem CreateBurst(string name, Color color, short count, float lifetime, float size, float speed)
        {
            GameObject effect = new(name);
            effect.transform.SetParent(transform, false);
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.03f;
            return particles;
        }

        private IEnumerator HideTracer()
        {
            yield return new WaitForSeconds(0.065f);
            _tracer.enabled = false;
            _tracerRoutine = null;
        }

        private IEnumerator HideMuzzleLight()
        {
            yield return new WaitForSeconds(0.045f);
            _muzzleLight.enabled = false;
        }

        private void Subscribe()
        {
            if (_weapon != null && !_isSubscribed)
            {
                _weapon.Fired += HandleFired;
                _isSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_weapon != null && _isSubscribed)
            {
                _weapon.Fired -= HandleFired;
            }

            _isSubscribed = false;
        }
    }
}
