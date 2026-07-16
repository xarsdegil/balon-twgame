using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    public class BalloonController : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;
        public RectTransform balloonAssembly;
        public RectTransform visualRoot;
        public CanvasGroup balloonCanvasGroup;
        public Graphic balloonBody;
        public Graphic balloonHighlight;
        public BalloonExplosionEffect explosionEffect;
        public RectTransform shakeTarget;

        [Header("Runtime (read only)")]
        [SerializeField] private float currentSize = 1f;
        [SerializeField, Range(0f, 1f)] private float dangerLevel;

        private Vector2 baseAssemblyPosition;
        private Quaternion baseAssemblyRotation;
        private Coroutine idleMotion;

        private void Awake()
        {
            if (balloonAssembly != null)
            {
                baseAssemblyPosition = balloonAssembly.anchoredPosition;
                baseAssemblyRotation = balloonAssembly.localRotation;
            }
        }

        private void OnEnable()
        {
            idleMotion = StartCoroutine(IdleMotion());
        }

        private void OnDisable()
        {
            if (idleMotion != null)
                StopCoroutine(idleMotion);
        }

        public void ResetBalloon()
        {
            currentSize = 1f;
            dangerLevel = 0f;
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one;
            if (balloonCanvasGroup != null)
                balloonCanvasGroup.alpha = 1f;
            ApplyColors();
        }

        public IEnumerator Inflate(float growth, float normalizedDanger)
        {
            dangerLevel = Mathf.Clamp01(normalizedDanger);
            float targetSize = Mathf.Min(currentSize + growth, settings.maximumVisualScale);
            Vector3 start = Vector3.one * currentSize;
            float elapsed = 0f;

            while (elapsed < settings.inflationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settings.inflationDuration);
                float eased = settings.inflationEase.Evaluate(t);
                float size = Mathf.LerpUnclamped(currentSize, targetSize, eased);
                float pulse = Mathf.Sin(t * Mathf.PI) * settings.squashStretchAmount;
                visualRoot.localScale = new Vector3(size * (1f + pulse), size * (1f - pulse * 0.55f), 1f);
                yield return null;
            }

            currentSize = targetSize;
            visualRoot.localScale = Vector3.one * currentSize;
            ApplyColors();
        }

        public IEnumerator Explode()
        {
            Vector3 startScale = visualRoot.localScale;
            float elapsed = 0f;
            while (elapsed < settings.preExplosionHold)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / settings.preExplosionHold;
                float tenseScale = 1f + Mathf.Sin(t * Mathf.PI * 7f) * 0.018f + t * 0.08f;
                visualRoot.localScale = startScale * tenseScale;
                yield return null;
            }

            balloonCanvasGroup.alpha = 0f;
            Coroutine shake = StartCoroutine(ScreenShake(settings.explosionDuration * 0.7f));
            if (explosionEffect != null)
                yield return explosionEffect.Play(settings.balloonColor, settings.explosionDuration);
            if (shake != null)
                yield return shake;
        }

        private IEnumerator IdleMotion()
        {
            while (true)
            {
                if (settings != null && balloonAssembly != null)
                {
                    float time = Time.unscaledTime * settings.idleSwaySpeed;
                    float rotation = Mathf.Sin(time) * settings.idleSwayDegrees;
                    float shake = dangerLevel * settings.dangerShakePixels;
                    Vector2 jitter = Random.insideUnitCircle * shake * 0.16f;
                    balloonAssembly.anchoredPosition = baseAssemblyPosition + jitter + Vector2.up * Mathf.Sin(time * 0.72f) * 4f;
                    balloonAssembly.localRotation = baseAssemblyRotation * Quaternion.Euler(0f, 0f, rotation);
                }
                yield return null;
            }
        }

        private IEnumerator ScreenShake(float duration)
        {
            if (shakeTarget == null)
                yield break;

            Vector2 origin = shakeTarget.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = (1f - elapsed / duration) * settings.screenShakePixels;
                shakeTarget.anchoredPosition = origin + Random.insideUnitCircle * strength;
                yield return null;
            }
            shakeTarget.anchoredPosition = origin;
        }

        private void ApplyColors()
        {
            if (settings == null)
                return;
            if (balloonBody != null)
                balloonBody.color = Color.Lerp(settings.balloonColor, Color.white, dangerLevel * 0.14f);
            if (balloonHighlight != null)
                balloonHighlight.color = settings.balloonHighlightColor;
        }

        public void RefreshColors()
        {
            ApplyColors();
        }
    }
}
