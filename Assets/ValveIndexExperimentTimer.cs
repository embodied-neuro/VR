using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// First trigger press:
///   - Activates the experiment content
///   - Starts a high-precision timer
///
/// Second trigger press:
///   - Stops the timer
///   - Deactivates the experiment content
///   - Appends the timing result to a CSV file
///
/// Attach this component to a separate ExperimentManager GameObject.
/// Do not put it inside experimentRoot, because experimentRoot is disabled while idle.
/// </summary>
public sealed class ValveIndexExperimentTimer : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("A Button action bound to the Valve Index triggerPressed control.")]
    [SerializeField] private InputActionReference triggerAction;

    [Header("Experiment")]
    [Tooltip("The parent GameObject containing the experimental stimuli.")]
    [SerializeField] private GameObject experimentRoot;

    [Tooltip("Prevents an accidental immediate second trigger press.")]
    [SerializeField, Min(0f)] private float minimumRunTimeSeconds = 0.15f;

    [Tooltip("Hide the experimental stimuli before the first press and after the second press.")]
    [SerializeField] private bool hideExperimentWhenIdle = true;

    [Header("Optional Events")]
    [SerializeField] private UnityEvent onExperimentStarted;
    [SerializeField] private UnityEvent onExperimentEnded;

    private bool isRunning;
    private double startInputTime;
    private string startUtc;
    private int trialNumber;
    private string csvPath;

    public bool IsRunning => isRunning;

    public double ElapsedSeconds =>
        isRunning ? Time.realtimeSinceStartupAsDouble - startInputTime : 0.0;

    private void Awake()
    {
        csvPath = Path.Combine(
            Application.persistentDataPath,
            "ValveIndexExperimentTiming.csv"
        );

        if (hideExperimentWhenIdle && experimentRoot != null)
        {
            experimentRoot.SetActive(false);
        }

        Debug.Log($"Experiment timing CSV will be saved to:\n{csvPath}");
    }

    private void OnEnable()
    {
        if (triggerAction == null || triggerAction.action == null)
        {
            Debug.LogError(
                "ValveIndexExperimentTimer: Assign a Trigger Input Action in the Inspector.",
                this
            );
            return;
        }

        triggerAction.action.performed += HandleTriggerPressed;
        triggerAction.action.Enable();
    }

    private void OnDisable()
    {
        if (triggerAction == null || triggerAction.action == null)
        {
            return;
        }

        triggerAction.action.performed -= HandleTriggerPressed;
        triggerAction.action.Disable();
    }

    private void HandleTriggerPressed(InputAction.CallbackContext context)
    {
        // context.time is the timestamp of the input event.
        if (!isRunning)
        {
            StartExperiment(context.time);
            return;
        }

        double duration = context.time - startInputTime;

        if (duration < minimumRunTimeSeconds)
        {
            return;
        }

        EndExperiment(context.time);
    }

    private void StartExperiment(double inputTime)
    {
        isRunning = true;
        trialNumber++;
        startInputTime = inputTime;
        startUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        if (experimentRoot != null)
        {
            experimentRoot.SetActive(true);
        }

        onExperimentStarted?.Invoke();

        Debug.Log($"Trial {trialNumber} started.");
    }

    private void EndExperiment(double inputTime)
    {
        double duration = inputTime - startInputTime;
        string endUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        isRunning = false;

        if (hideExperimentWhenIdle && experimentRoot != null)
        {
            experimentRoot.SetActive(false);
        }

        onExperimentEnded?.Invoke();
        SaveTimingRow(inputTime, duration, endUtc);

        Debug.Log(
            $"Trial {trialNumber} ended. Duration: {duration:F6} seconds.\n" +
            $"Saved to: {csvPath}"
        );
    }

    private void SaveTimingRow(double endInputTime, double duration, string endUtc)
    {
        try
        {
            bool writeHeader = !File.Exists(csvPath);

            using StreamWriter writer = new StreamWriter(csvPath, append: true);

            if (writeHeader)
            {
                writer.WriteLine(
                    "trial,start_utc,end_utc,start_input_time_s,end_input_time_s,duration_s"
                );
            }

            writer.WriteLine(string.Join(",",
                trialNumber.ToString(CultureInfo.InvariantCulture),
                CsvEscape(startUtc),
                CsvEscape(endUtc),
                startInputTime.ToString("F9", CultureInfo.InvariantCulture),
                endInputTime.ToString("F9", CultureInfo.InvariantCulture),
                duration.ToString("F9", CultureInfo.InvariantCulture)
            ));
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Could not save experiment timing CSV:\n{exception}",
                this
            );
        }
    }

    private static string CsvEscape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
