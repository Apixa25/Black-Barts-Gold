// ============================================================================
// BBGDivider.cs
// Black Bart's Gold — Brass Horizontal Divider
// Path: Assets/Scripts/UI/Components/BBGDivider.cs
// ============================================================================
// Horizontal brass divider bar. Drop between sections for visual separation.
//
// Creation:
//   BBGDivider.Create(parent);
//   BBGDivider.Create(parent, width: 350, height: 6);
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace BlackBartsGold.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class BBGDivider : MonoBehaviour
    {
        private Image _image;
        private bool _built;

        private void Start()
        {
            if (!_built) Build();
        }

        /// <summary>Create a brass divider bar.</summary>
        public static BBGDivider Create(Transform parent, float width = 300, float height = 10)
        {
            var go = new GameObject("BBGDivider", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);

            var divider = go.AddComponent<BBGDivider>();
            divider.Build();
            return divider;
        }

        /// <summary>Create a divider that stretches to fill parent width with margins.</summary>
        public static BBGDivider CreateStretched(Transform parent, float height = 10, float horizontalMargin = 24)
        {
            var go = new GameObject("BBGDivider", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(horizontalMargin, -height * 0.5f);
            rect.offsetMax = new Vector2(-horizontalMargin, height * 0.5f);

            var divider = go.AddComponent<BBGDivider>();
            divider.Build();
            return divider;
        }

        public void Build()
        {
            if (_built) return;

            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();

            _image = GetComponent<Image>();
            if (_image == null)
                _image = gameObject.AddComponent<Image>();

            _image.sprite = BBGSprites.DividerBrass;
            _image.type = Image.Type.Simple;
            _image.preserveAspect = false;
            _image.raycastTarget = false;
            _image.color = Color.white;

            _built = true;
        }
    }
}
