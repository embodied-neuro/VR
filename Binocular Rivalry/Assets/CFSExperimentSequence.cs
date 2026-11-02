using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Runs a trigger-controlled CFS sequence:
///
/// Initial state:
///   Prompt shown; all stimulus pairs hidden.
///
/// For each stimulus pair:
///   Press 1: show the pair and begin increasing the LEFT-eye target.
///   Press 2: begin decreasing the LEFT-eye target.
///   Press 3: hide the pair, show the prompt, and prepare the next pair.
///
/// Therefore:
///   Press 1 = Object 1 increase
///   Press 2 = Object 1 decrease
///   Press 3 = break/prompt
///   Press 4 = Object 2 increase
///
/// The right-eye object is only activated/deactivated with its pair.
/// This script never changes its contrast.
///
/// Attach this to ExperimentManager. Disable/remove the older
/// ValveIndexExperimentTimer component so both scripts do not respond
/// to the same trigger.
/// </summary>
public sealed class CFSExperimentSequence : MonoBehaviour
{
    [Serializable]
    public sealed class StimulusPair
    {
        [Tooltip("A readable label written to the CSV, such as Object 1.")]
        public string label = "Object";

        [Tooltip(
            "Parent containing this pair's left-eye and right-eye objects."
        )]
        public GameObject pairRoot;

        [Header("Connect your existing LEFT-eye contrast controller")]
        [Tooltip(
            "Call the public no-argument method that starts increasing " +
            "the left-eye object's contrast."
        )]
        public UnityEvent onBeginIncreasing;

        [Tooltip(
            "Call the public no-argument method that starts decreasing " +
            "the left-eye object's contrast."
        )]
        public UnityEvent onBeginDecreasing;
    }

    private enum Phase
    {
        WaitingToStartPair,
        Increasing,
        Decreasing,
        Complete
    }

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;

    [Header("Prompt")]
    [Tooltip("The GameObject displaying 'click to start'.")]
    [SerializeField] private GameObject startPrompt;

    [Header("Stimulus Order")]
    [Tooltip("Put Object 1 first, Object 2 second, and so forth.")]
    [SerializeField] private StimulusPair[] stimulusPairs;

    [Header("Optional Events")]
    [SerializeField] private UnityEvent onBreakStarted;
    [SerializeField] private UnityEvent onSequenceCompleted;

    [Header("Safety")]
    [Tooltip("Ignores unrealistically fast repeated trigger events.")]
    [SerializeField, Min(0f)] private float minimumTimeBetweenPresses = 0.15f;

    private Phase phase = Phase.WaitingToStartPair;
    private int currentPairIndex;
    private double previousAcceptedPressTime = double.NegativeInfinity;
    private double phaseStartInputTime;
    private string csvPath;

    public int CurrentPairNumber => currentPairIndex + 1;
    public bool IsComplete => phase == Phase.Complete;

    private void Awake()
    {
        csvPath = Path.Combine(
            Application.persistentDataPath,
            "CFSExperimentEvents.csv"
        );

        PrepareInitialState();

        Debug.Log($"CFS event CSV will be saved to:\n{csvPath}");
    }

    private void OnEnable()
    {
        if (triggerAction == null || triggerAction.action == null)
        {
            Debug.LogError(
                "CFSExperimentSequence: Assign the Trigger Action.",
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

    private void PrepareInitialState()
    {
        currentPairIndex = 0;
        phase = Phase.WaitingToStartPair;
        previousAcceptedPressTime = double.NegativeInfinity;
        phaseStartInputTime = 0.0;

        SetAllPairsActive(false);

        if (startPrompt != null)
        {
            startPrompt.SetActive(true);
        }
    }

    private void HandleTriggerPressed(InputAction.CallbackContext context)
    {
        double inputTime = context.time;

        if (inputTime - previousAcceptedPressTime < minimumTimeBetweenPresses)
        {
            return;
        }

        previousAcceptedPressTime = inputTime;

        if (!HasValidCurrentPair() && phase != Phase.Complete)
        {
            Debug.LogError(
                "CFSExperimentSequence: No valid stimulus pair is available.",
                this
            );
            return;
        }

        switch (phase)
        {
            case Phase.WaitingToStartPair:
                BeginIncreasing(inputTime);
                break;

            case Phase.Increasing:
                BeginDecreasing(inputTime);
                break;

            case Phase.Decreasing:
                BeginBreakOrComplete(inputTime);
                break;

            case Phase.Complete:
                Debug.Log("The CFS sequence is already complete.");
                break;
        }
    }

    private void BeginIncreasing(double inputTime)
    {
        StimulusPair pair = stimulusPairs[currentPairIndex];

        SetAllPairsActive(false);

        if (startPrompt != null)
        {
            startPrompt.SetActive(false);
        }

        pair.pairRoot.SetActive(true);
        pair.onBeginIncreasing?.Invoke();

        phase = Phase.Increasing;
        phaseStartInputTime = inputTime;

        WriteEvent(
            inputTime,
            "increase_started",
            0.0
        );

        Debug.Log(
            $"Pair {CurrentPairNumber} ({pair.label}): increasing started."
        );
    }

    private void BeginDecreasing(double inputTime)
    {
        StimulusPair pair = stimulusPairs[currentPairIndex];
        double increasingDuration = inputTime - phaseStartInputTime;

        pair.onBeginDecreasing?.Invoke();

        WriteEvent(
            inputTime,
            "decrease_started",
            increasingDuration
        );

        phase = Phase.Decreasing;
        phaseStartInputTime = inputTime;

        Debug.Log(
            $"Pair {CurrentPairNumber} ({pair.label}): decreasing started. " +
            $"Increasing duration: {increasingDuration:F6} s."
        );
    }

    private void BeginBreakOrComplete(double inputTime)
    {
        StimulusPair finishedPair = stimulusPairs[currentPairIndex];
        double decreasingDuration = inputTime - phaseStartInputTime;

        finishedPair.pairRoot.SetActive(false);

        WriteEvent(
            inputTime,
            "pair_finished",
            decreasingDuration
        );

        currentPairIndex++;

        if (currentPairIndex >= stimulusPairs.Length)
        {
            phase = Phase.Complete;

            if (startPrompt != null)
            {
                startPrompt.SetActive(false);
            }

            onSequenceCompleted?.Invoke();

            Debug.Log(
                "All stimulus pairs are complete.\n" +
                $"Saved to: {csvPath}"
            );

            return;
        }

        phase = Phase.WaitingToStartPair;
        phaseStartInputTime = inputTime;

        if (startPrompt != null)
        {
            startPrompt.SetActive(true);
        }

        onBreakStarted?.Invoke();

        Debug.Log(
            $"Break started. Click to start pair {CurrentPairNumber}."
        );
    }

    private bool HasValidCurrentPair()
    {
        return stimulusPairs != null &&
               currentPairIndex >= 0 &&
               currentPairIndex < stimulusPairs.Length &&
               stimulusPairs[currentPairIndex] != null &&
               stimulusPairs[currentPairIndex].pairRoot != null;
    }

    private void SetAllPairsActive(bool active)
    {
        if (stimulusPairs == null)
        {
            return;
        }

        foreach (StimulusPair pair in stimulusPairs)
        {
            if (pair != null && pair.pairRoot != null)
            {
                pair.pairRoot.SetActive(active);
            }
        }
    }

    private void WriteEvent(
        double inputTime,
        string eventName,
        double phaseDuration
    )
    {
        try
        {
            bool writeHeader = !File.Exists(csvPath);
            StimulusPair pair = stimulusPairs[currentPairIndex];

            using StreamWriter writer = new StreamWriter(csvPath, append: true);

            if (writeHeader)
            {
                writer.WriteLine(
                    "utc_time,input_time_s,pair_number,pair_label," +
                    "event,previous_phase_duration_s"
                );
            }

            writer.WriteLine(string.Join(",",
                CsvEscape(
                    DateTime.UtcNow.ToString(
                        "O",
                        CultureInfo.InvariantCulture
                    )
                ),
                inputTime.ToString("F9", CultureInfo.InvariantCulture),
                CurrentPairNumber.ToString(CultureInfo.InvariantCulture),
                CsvEscape(pair.label),
                CsvEscape(eventName),
                phaseDuration.ToString("F9", CultureInfo.InvariantCulture)
            ));
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Could not save CFS event CSV:\n{exception}",
                this
            );
        }
    }

    private static string CsvEscape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// Returns the experiment to Object 1 with the start prompt visible.
    /// This can be assigned to a Unity UI button if needed.
    /// </summary>
    public void ResetSequence()
    {
        PrepareInitialState();
        Debug.Log("CFS sequence reset.");
    }
}
