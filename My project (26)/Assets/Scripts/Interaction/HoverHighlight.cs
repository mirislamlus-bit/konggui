using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChapterGame.Interaction
{
    public sealed class HoverHighlight : MonoBehaviour
    {
        [Header("Target")]
        public Transform targetVisual;
        public GameObject hoverRing;
        public SpriteRenderer targetRenderer;

        [Header("Hover")]
        [Range(1f, 1.3f)] public float hoverScale = 1.1f;
        public float scaleSpeed = 10f;
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(1.18f, 1.08f, 0.82f, 1f);

        [Header("Ring")]
        public bool showIdleRing;
        public float idleRingAlpha = 0.16f;
        public float hoverRingAlpha = 0.78f;
        public float ringRadius = 1.1f;
        public float ringRotateSpeed = 45f;
        public float ringPulseSpeed = 2f;
        public Color ringColor = new Color(1f, 0.68f, 0.24f, 1f);

        [Header("Particles")]
        public bool createGoldParticles;

        private readonly List<Renderer> ringRenderers = new List<Renderer>();
        private Transform ringTransform;
        private ParticleSystem goldParticles;
        private Vector3 baseScale;
        private bool hovering;
        private float ringAlpha;

        public void HoverEnter()
        {
            hovering = true;
            SetRingVisible(true);
            if (goldParticles != null && !goldParticles.isPlaying)
            {
                goldParticles.Play();
            }
        }

        public void HoverExit()
        {
            hovering = false;
            if (goldParticles != null)
            {
                goldParticles.Stop();
            }
        }

        public IEnumerator PlayInteractFeedback()
        {
            EnsureRing();
            SetRingVisible(true);

            var duration = 0.18f;
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                var flash = Mathf.Sin(t * Mathf.PI);
                SetRingAlpha(Mathf.Lerp(hoverRingAlpha, 1f, flash));
                if (ringTransform != null)
                {
                    ringTransform.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.72f, t);
                }

                yield return null;
            }

            if (ringTransform != null)
            {
                ringTransform.localScale = Vector3.one;
            }
        }

        private void Awake()
        {
            if (targetVisual == null)
            {
                targetVisual = transform;
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            baseScale = targetVisual.localScale;
            if (targetRenderer != null)
            {
                normalColor = targetRenderer.color;
            }

            EnsureRing();
            SetRingVisible(showIdleRing);
            SetRingAlpha(showIdleRing ? idleRingAlpha : 0f);
        }

        private void Update()
        {
            var targetScale = hovering ? baseScale * hoverScale : baseScale;
            targetVisual.localScale = Vector3.Lerp(targetVisual.localScale, targetScale, Time.deltaTime * scaleSpeed);

            if (targetRenderer != null)
            {
                targetRenderer.color = Color.Lerp(targetRenderer.color, hovering ? hoverColor : normalColor, Time.deltaTime * scaleSpeed);
            }

            UpdateRing(Time.deltaTime);
        }

        private void UpdateRing(float deltaTime)
        {
            EnsureRing();
            if (ringTransform == null)
            {
                return;
            }

            var targetAlpha = hovering ? hoverRingAlpha : showIdleRing ? idleRingAlpha : 0f;
            ringAlpha = Mathf.Lerp(ringAlpha, targetAlpha, deltaTime * scaleSpeed);
            var pulse = 0.88f + Mathf.Sin(Time.time * ringPulseSpeed) * 0.08f;

            ringTransform.Rotate(0f, 0f, ringRotateSpeed * deltaTime);
            ringTransform.localScale = Vector3.one * pulse;
            SetRingAlpha(ringAlpha);
            SetRingVisible(ringAlpha > 0.02f);
        }

        private void EnsureRing()
        {
            if (ringTransform != null)
            {
                return;
            }

            if (hoverRing != null)
            {
                ringTransform = hoverRing.transform;
                ringRenderers.Clear();
                ringRenderers.AddRange(hoverRing.GetComponentsInChildren<Renderer>(true));
                return;
            }

            hoverRing = new GameObject("HoverRing");
            hoverRing.transform.SetParent(transform, false);
            hoverRing.transform.localPosition = Vector3.zero;
            ringTransform = hoverRing.transform;

            CreateLineRing("Outer", ringRadius, 0.025f, 96, 1f);
            CreateLineRing("Middle", ringRadius * 0.76f, 0.018f, 96, 0.72f);
            CreateLineRing("Inner", ringRadius * 0.48f, 0.012f, 72, 0.42f);

            if (createGoldParticles)
            {
                CreateParticles();
            }
        }

        private void CreateLineRing(string name, float radius, float width, int segments, float alphaMultiplier)
        {
            var ring = new GameObject(name);
            ring.transform.SetParent(hoverRing.transform, false);

            var line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingLayerName = "VFX";
            line.sortingOrder = 50;
            line.material = new Material(Shader.Find("Sprites/Default"));

            for (var i = 0; i < segments; i++)
            {
                var angle = (float)i / segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            var color = ringColor;
            color.a *= alphaMultiplier;
            line.startColor = color;
            line.endColor = color;
            ringRenderers.Add(line);
        }

        private void CreateParticles()
        {
            var particleObject = new GameObject("GoldParticles");
            particleObject.transform.SetParent(hoverRing.transform, false);
            goldParticles = particleObject.AddComponent<ParticleSystem>();

            var main = goldParticles.main;
            main.startLifetime = 0.55f;
            main.startSpeed = 0.18f;
            main.startSize = 0.035f;
            main.startColor = new Color(1f, 0.68f, 0.26f, 0.65f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = goldParticles.emission;
            emission.rateOverTime = 10f;

            var shape = goldParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = ringRadius;

            var renderer = goldParticles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "VFX";
            renderer.sortingOrder = 52;
        }

        private void SetRingVisible(bool visible)
        {
            if (hoverRing != null && hoverRing.activeSelf != visible)
            {
                hoverRing.SetActive(visible);
            }
        }

        private void SetRingAlpha(float alpha)
        {
            for (var i = 0; i < ringRenderers.Count; i++)
            {
                var line = ringRenderers[i] as LineRenderer;
                if (line == null)
                {
                    continue;
                }

                var start = line.startColor;
                var end = line.endColor;
                start.a = alpha;
                end.a = alpha;
                line.startColor = start;
                line.endColor = end;
            }
        }
    }
}
