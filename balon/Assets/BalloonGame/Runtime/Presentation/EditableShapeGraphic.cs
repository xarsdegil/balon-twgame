using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    public enum EditableShape
    {
        Ellipse,
        Triangle,
        Diamond
    }

    [AddComponentMenu("Balloon Game/Editable Shape Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class EditableShapeGraphic : MaskableGraphic
    {
        [Header("Editable Shape")]
        public EditableShape shape = EditableShape.Ellipse;
        [Range(3, 96)] public int segments = 48;
        [Range(-180f, 180f)] public float rotationDegrees;

        protected override void OnEnable()
        {
            EnsureCanvasRenderer();
            base.OnEnable();
        }

        private void EnsureCanvasRenderer()
        {
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();

            if (shape == EditableShape.Triangle)
            {
                AddPolygon(vertexHelper, rect, 3);
                return;
            }

            if (shape == EditableShape.Diamond)
            {
                AddPolygon(vertexHelper, rect, 4);
                return;
            }

            AddEllipse(vertexHelper, rect);
        }

        private void AddEllipse(VertexHelper helper, Rect rect)
        {
            int count = Mathf.Clamp(segments, 8, 96);
            Vector2 center = rect.center;
            float angleOffset = rotationDegrees * Mathf.Deg2Rad;
            helper.AddVert(center, color, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= count; i++)
            {
                float angle = angleOffset + i * Mathf.PI * 2f / count;
                Vector2 normalized = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 point = center + Vector2.Scale(normalized, rect.size * 0.5f);
                helper.AddVert(point, color, normalized * 0.5f + Vector2.one * 0.5f);
            }
            for (int i = 0; i < count; i++)
                helper.AddTriangle(0, i + 1, i + 2);
        }

        private void AddPolygon(VertexHelper helper, Rect rect, int sides)
        {
            Vector2 center = rect.center;
            float angleOffset = (rotationDegrees + 90f) * Mathf.Deg2Rad;
            helper.AddVert(center, color, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= sides; i++)
            {
                float angle = angleOffset + i * Mathf.PI * 2f / sides;
                Vector2 normalized = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 point = center + Vector2.Scale(normalized, rect.size * 0.5f);
                helper.AddVert(point, color, normalized * 0.5f + Vector2.one * 0.5f);
            }
            for (int i = 0; i < sides; i++)
                helper.AddTriangle(0, i + 1, i + 2);
        }
    }
}
