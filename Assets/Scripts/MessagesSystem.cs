using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessagesSystem : MonoBehaviour
{
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject messagePrefab;

    [Header("Behavior")]
    [SerializeField, Min(0f)] private float messageDuration = 3f;
    [SerializeField, Min(0f)] private float spacing = 4f;

    // Newest first (index 0 is the one at the bottom-left)
    private readonly List<RectTransform> _messages = new();

    public static MessagesSystem Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowMessage(string message, Color color)
    {
        if (canvasTransform == null || messagePrefab == null) return;

        var go = Instantiate(messagePrefab, canvasTransform);
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();

        // Try to set text on TMP_Text or legacy Text
        bool textSet = false;
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = message; tmp.color = color; textSet = true; }
        var uiText = textSet ? null : go.GetComponentInChildren<Text>(true);
        if (uiText != null) { uiText.text = message; uiText.color = color; textSet = true; }

        // Anchor to bottom-left so (0,0) sits at screen's bottom-left in this canvas
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;

        // Rebuild to get correct size after setting text
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        float h = Mathf.Max(0f, rect.rect.height);

        // Push existing messages up by the new one's height + spacing
        float delta = h + spacing;
        for (int i = 0; i < _messages.Count; i++)
        {
            var r = _messages[i];
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, r.anchoredPosition.y + delta);
        }

        // New message at bottom (0,0)
        rect.anchoredPosition = Vector2.zero;

        // Track and schedule removal
        _messages.Insert(0, rect);
        StartCoroutine(AutoRemove(rect));
    }

    private IEnumerator AutoRemove(RectTransform rect)
    {
        yield return new WaitForSeconds(messageDuration);

        if (rect == null) yield break;

        // Rebuild to get current height if content changed (rare but safe)
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        float h = Mathf.Max(0f, rect.rect.height);
        float delta = h + spacing;

        int idx = _messages.IndexOf(rect);
        if (idx >= 0)
        {
            // Messages above this one (older messages, higher Y) go down to close the gap
            for (int i = idx + 1; i < _messages.Count; i++)
            {
                var r = _messages[i];
                if (r != null)
                {
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, r.anchoredPosition.y - delta);
                }
            }

            _messages.RemoveAt(idx);
        }

        if (rect != null) Destroy(rect.gameObject);
    }
}
