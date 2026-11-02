using UnityEngine;

public sealed class TargetContrastController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string colorProperty = "_BaseColor";

    [Header("Contrast (linear 0-1)")]
    [SerializeField] private float minContrast = 0.02f;
    [SerializeField] private float maxContrast = 1.0f;

    [Header("Ramp rate")]
    [Tooltip("dB change per second. Paper: 0.07 dB/frame at 60 Hz = 4.2 dB/s.")]
    [SerializeField] private float dbPerSecond = 4.2f;

    private enum Direction { Idle, Increasing, Decreasing }
    private Direction direction = Direction.Idle;
    private float currentDb;
    private MaterialPropertyBlock block;

    public float CurrentContrast => Mathf.Pow(10f, currentDb / 20f);

    private float MinDb => 20f * Mathf.Log10(minContrast);
    private float MaxDb => 20f * Mathf.Log10(maxContrast);

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        currentDb = MinDb;
        Apply();
    }

    // Wire these to onBeginIncreasing / onBeginDecreasing.
    public void BeginIncreasing() { direction = Direction.Increasing; }
    public void BeginDecreasing() { direction = Direction.Decreasing; }

    // Optional: call when a pair starts, to set the starting level.
    public void ResetToMin() { currentDb = MinDb; direction = Direction.Idle; Apply(); }

    private void Update()
    {
        if (direction == Direction.Idle) return;

        float step = dbPerSecond * Time.deltaTime;
        currentDb += direction == Direction.Increasing ? step : -step;
        currentDb = Mathf.Clamp(currentDb, MinDb, MaxDb);
        Apply();
    }

    private void Apply()
    {
        // Contrast as deviation from mid-grey: 0 = grey, 1 = full contrast.
        float c = CurrentContrast;
        Color tint = Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 1f), Color.white, c);
        targetRenderer.GetPropertyBlock(block);
        block.SetColor(colorProperty, tint);
        targetRenderer.SetPropertyBlock(block);
    }
}