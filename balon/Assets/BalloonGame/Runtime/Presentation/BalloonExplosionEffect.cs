using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    public class BalloonExplosionEffect : MonoBehaviour
    {
        [Tooltip("Add or remove child shards freely. The effect animates every child RectTransform.")]
        public List<RectTransform> shards = new List<RectTransform>();
        public float minimumDistance = 180f;
        public float maximumDistance = 420f;
        public float minimumSpin = 120f;
        public float maximumSpin = 520f;

        public IEnumerator Play(Color color, float duration)
        {
            if (shards.Count == 0)
                CollectChildShards();

            Vector2[] targets = new Vector2[shards.Count];
            float[] spins = new float[shards.Count];
            for (int i = 0; i < shards.Count; i++)
            {
                RectTransform shard = shards[i];
                if (shard.GetComponent<CanvasRenderer>() == null)
                    shard.gameObject.AddComponent<CanvasRenderer>();

                float angle = (360f / Mathf.Max(1, shards.Count)) * i + Random.Range(-16f, 16f);
                targets[i] = Quaternion.Euler(0f, 0f, angle) * Vector2.up * Random.Range(minimumDistance, maximumDistance);
                spins[i] = Random.Range(minimumSpin, maximumSpin) * (i % 2 == 0 ? 1f : -1f);
                shard.anchoredPosition = Vector2.zero;
                shard.localRotation = Quaternion.Euler(0f, 0f, angle);
                shard.localScale = Vector3.one;
                shard.gameObject.SetActive(true);
                Graphic graphic = shard.GetComponent<Graphic>();
                if (graphic != null)
                {
                    Color shardColor = color;
                    shardColor.a = 1f;
                    graphic.color = shardColor;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float travel = 1f - Mathf.Pow(1f - t, 3f);
                for (int i = 0; i < shards.Count; i++)
                {
                    RectTransform shard = shards[i];
                    shard.anchoredPosition = targets[i] * travel + Vector2.down * (170f * t * t);
                    shard.localRotation *= Quaternion.Euler(0f, 0f, spins[i] * Time.unscaledDeltaTime);
                    shard.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, t);
                    Graphic graphic = shard.GetComponent<Graphic>();
                    if (graphic != null)
                    {
                        Color fadedColor = graphic.color;
                        fadedColor.a = 1f - t;
                        graphic.color = fadedColor;
                    }
                }
                yield return null;
            }

            foreach (RectTransform shard in shards)
                shard.gameObject.SetActive(false);
        }

        [ContextMenu("Collect Child Shards")]
        public void CollectChildShards()
        {
            shards.Clear();
            foreach (RectTransform child in transform)
                shards.Add(child);
        }
    }
}
