using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BalloonGame
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class HsvColorWheel : MaskableGraphic, IPointerDownHandler, IDragHandler
    {
        [Range(12, 128)] public int segments = 72;
        [Range(2, 24)] public int saturationSteps = 12;
        public RectTransform selectionMarker;

        public event Action<Color> ColorChanged;
        public Color SelectedColor => Color.HSVToRGB(hue, saturation, value);

        private float hue = 0.36f;
        private float saturation = 1f;
        private float value = 0.78f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            UpdateMarker();
        }

        public void SetColor(Color selectedColor, bool notify = false)
        {
            Color.RGBToHSV(selectedColor, out hue, out saturation, out value);
            value = Mathf.Max(0.01f, value);
            SetVerticesDirty();
            UpdateMarker();
            if (notify)
                ColorChanged?.Invoke(SelectedColor);
        }

        public void OnPointerDown(PointerEventData eventData) => SelectAt(eventData);

        public void OnDrag(PointerEventData eventData) => SelectAt(eventData);

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Vector2 center = rect.center;
            int ringCount = Mathf.Max(2, saturationSteps);
            int segmentCount = Mathf.Max(12, segments);

            for (int ring = 0; ring <= ringCount; ring++)
            {
                float normalizedRadius = (float)ring / ringCount;
                for (int segment = 0; segment <= segmentCount; segment++)
                {
                    float normalizedAngle = (float)segment / segmentCount;
                    float angle = normalizedAngle * Mathf.PI * 2f;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 point = center + direction * radius * normalizedRadius;
                    Color vertexColor = Color.HSVToRGB(normalizedAngle, normalizedRadius, value);
                    helper.AddVert(point, vertexColor, Vector2.zero);
                }
            }

            int row = segmentCount + 1;
            for (int ring = 0; ring < ringCount; ring++)
            {
                for (int segment = 0; segment < segmentCount; segment++)
                {
                    int current = ring * row + segment;
                    int nextRing = current + row;
                    helper.AddTriangle(current, nextRing, nextRing + 1);
                    helper.AddTriangle(current, nextRing + 1, current + 1);
                }
            }
        }

        private void SelectAt(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
                return;

            Rect rect = rectTransform.rect;
            Vector2 fromCenter = localPoint - rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            saturation = Mathf.Clamp01(fromCenter.magnitude / radius);
            hue = Mathf.Repeat(Mathf.Atan2(fromCenter.y, fromCenter.x) / (Mathf.PI * 2f), 1f);
            UpdateMarker();
            ColorChanged?.Invoke(SelectedColor);
        }

        private void UpdateMarker()
        {
            if (selectionMarker == null)
                return;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            float angle = hue * Mathf.PI * 2f;
            selectionMarker.anchoredPosition =
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * saturation;
        }
    }
}
