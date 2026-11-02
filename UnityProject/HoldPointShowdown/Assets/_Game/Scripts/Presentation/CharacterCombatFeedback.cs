using System.Collections;
using HoldPointShowdown.Combat;
using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterCombatFeedback : MonoBehaviour
    {
        private Damageable _damageable;
        private BlockCharacterVisual _visual;
        private ParticleSystem _deathBurst;
        private Coroutine _flashRoutine;

        private void Start()
        {
            _visual = GetComponent<BlockCharacterVisual>();
            Bind(GetComponentInParent<Damageable>());
            _deathBurst = CreateDeathBurst();
        }

        public void Bind(Damageable damageable)
        {
            if (_damageable == damageable)
            {
                return;
            }

            Unbind();
            _damageable = damageable;
            if (_damageable != null)
            {
                _damageable.Damaged += HandleDamaged;
                _damageable.Died += HandleDied;
                _damageable.ResetCompleted += HandleReset;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleDamaged(Damageable _, float amount, Vector3 hitPoint, GameObject instigator)
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private void HandleDied(Damageable _)
        {
            if (_deathBurst != null)
            {
                _deathBurst.transform.position = transform.position + (Vector3.up * 1.4f);
                _deathBurst.Play(true);
            }

            SetRenderersEnabled(false);
        }

        private void HandleReset(Damageable _)
        {
            SetRenderersEnabled(true);
        }

        private IEnumerator FlashRoutine()
        {
            SetFlashColor(new Color(1f, 0.35f, 0.25f, 1f));
            transform.localScale = new Vector3(1.08f, 0.92f, 1.08f);
            yield return new WaitForSeconds(0.09f);
            ClearFlashColor();
            transform.localScale = Vector3.one;
            _flashRoutine = null;
        }

        private void SetFlashColor(Color color)
        {
            if (_visual == null || _visual.Renderers == null)
            {
                return;
            }

            MaterialPropertyBlock block = new();
            block.SetColor("_BaseColor", color);
            foreach (Renderer renderer in _visual.Renderers)
            {
                renderer?.SetPropertyBlock(block);
            }
        }

        private void ClearFlashColor()
        {
            if (_visual == null || _visual.Renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in _visual.Renderers)
            {
                renderer?.SetPropertyBlock(null);
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_visual == null || _visual.Renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in _visual.Renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        private ParticleSystem CreateDeathBurst()
        {
            GameObject effect = new("淘汰粒子");
            effect.transform.SetParent(transform.parent, false);
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.2f, 0.8f, 1f),
                new Color(1f, 0.35f, 0.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)28) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            return particles;
        }

        private void Unbind()
        {
            if (_damageable == null)
            {
                return;
            }

            _damageable.Damaged -= HandleDamaged;
            _damageable.Died -= HandleDied;
            _damageable.ResetCompleted -= HandleReset;
            _damageable = null;
        }
    }
}
