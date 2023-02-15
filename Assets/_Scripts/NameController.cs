using System.Collections;
using TMPro;
using UnityEngine;

public class NameController : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float padding;

    public IEnumerator UpdateWidth() {
        if (text.renderedWidth < 0.001) {
            yield return null;
        }

        var width = text.renderedWidth;

        panel.anchorMax = new Vector2(panel.pivot.x + width / 2, panel.anchorMax.y);
        panel.anchorMin = new Vector2(panel.pivot.x - width / 2, panel.anchorMin.y);
    }
}