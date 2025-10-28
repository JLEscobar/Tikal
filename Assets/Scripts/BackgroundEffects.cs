using UnityEngine;

public class BackgroundEffects : MonoBehaviour
{
    [Header("Layers (UI RectTransforms)")]
    public RectTransform nearStars;   // Bigger/closer stars
    public RectTransform farStars;    // Smaller/farther stars

    [Header("Parallax")]
    [Tooltip("Pixels of max offset at screen edges (before intensity).")]
    public float maxOffsetPixels = 40f;
    [Range(0f, 2f)] public float nearIntensity = 1.0f;
    [Range(0f, 2f)] public float farIntensity = 0.4f;

    [Header("Smoothing")]
    [Tooltip("Time to reach target (lower = snappier).")]
    public float smoothTime = 0.08f;

    [Header("Optional Tilt")]
    [Range(0f, 15f)] public float maxTiltDegrees = 4f; // small z-tilt

    // Internals
    Vector2 _nearVel, _farVel;
    Vector2 _nearOrigin, _farOrigin;

    void Awake()
    {
        if (nearStars) _nearOrigin = nearStars.anchoredPosition;
        if (farStars) _farOrigin = farStars.anchoredPosition;
    }

    void LateUpdate()
    {
        // Normalize mouse relative to screen center: (-1,-1) .. (1,1)
        Vector2 mouse = Input.mousePosition;
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        Vector2 norm = new Vector2(
            Mathf.Approximately(cx, 0f) ? 0f : (mouse.x - cx) / cx,
            Mathf.Approximately(cy, 0f) ? 0f : (mouse.y - cy) / cy
        );
        norm = Vector2.ClampMagnitude(norm, 1f);

        // Parallax target offsets (move opposite to cursor for depth)
        Vector2 targetNear = _nearOrigin - norm * maxOffsetPixels * nearIntensity;
        Vector2 targetFar = _farOrigin - norm * maxOffsetPixels * farIntensity;

        // Smooth movement
        if (nearStars)
        {
            nearStars.anchoredPosition = Vector2.SmoothDamp(
                nearStars.anchoredPosition, targetNear, ref _nearVel, smoothTime);
        }
        if (farStars)
        {
            farStars.anchoredPosition = Vector2.SmoothDamp(
                farStars.anchoredPosition, targetFar, ref _farVel, smoothTime);
        }

        // Optional subtle tilt (same for both; keep it tiny)
        if (maxTiltDegrees > 0f)
        {
            float tiltZ = -norm.x * maxTiltDegrees; // opposite to mouse X
            if (nearStars) nearStars.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            if (farStars) farStars.localRotation = Quaternion.Euler(0f, 0f, tiltZ * 0.5f);
        }
    }

    // Call if you need to re-center after layout changes.
    public void RecalibrateOrigins()
    {
        if (nearStars) _nearOrigin = nearStars.anchoredPosition;
        if (farStars) _farOrigin = farStars.anchoredPosition;
    }
}
