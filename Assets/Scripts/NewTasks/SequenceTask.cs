using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SequenceTask : BaseTask
{
    [Header("Grid")]
    [SerializeField] private List<Button> gridButtons = new(9);

    [Header("UI Labels")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Sequence Settings")]
    [Tooltip("Base flashes for round 1. Each round adds sequenceIncrement more steps.")]
    [SerializeField] private int baseSequenceLength = 3;
    [SerializeField] private int sequenceIncrement = 2;
    [SerializeField] private int totalRounds = 3;

    [Header("Timing")]
    [SerializeField] private float flashOnDuration = 0.4f;
    [SerializeField] private float flashOffDuration = 0.2f;
    [SerializeField] private float preInputDelay = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(0.10f, 0.13f, 0.20f, 1f);
    [SerializeField] private Color flashColor = new Color(0.40f, 0.85f, 1.00f, 1f);
    [SerializeField] private Color correctColor = new Color(0.20f, 0.90f, 0.45f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.90f, 0.20f, 0.25f, 1f);
    [SerializeField] private Color activeColor = new Color(0.25f, 0.55f, 0.80f, 1f); // player pressed


    private List<int> currentSequence = new();
    private int currentRound = 0;
    private int playerInputStep = 0;
    private bool acceptingInput = false;
    private bool roundFailed = false;   // NEW: indicates current round attempt failed
    private Coroutine activeRoutine;

    protected override void OnOpen()
    {
        // Reset state
        currentRound = 0;
        currentSequence.Clear();
        acceptingInput = false;
        roundFailed = false;

        // Wire buttons
        for (int i = 0; i < gridButtons.Count; i++)
        {
            int captured = i;
            gridButtons[i].onClick.RemoveAllListeners();
            gridButtons[i].onClick.AddListener(() => OnGridButtonPressed(captured));
            SetButtonColor(captured, idleColor);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTask);
        }

        if (titleText) titleText.text = "NAVIGATION CONSOLE";
        if (feedbackText) feedbackText.text = "";

        // Start game
        activeRoutine = StartCoroutine(RunGame());
    }

    protected override void OnClose()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        acceptingInput = false;
        ResetAllButtons();
    }

    private IEnumerator RunGame()
    {
        currentRound = 1;

        while (currentRound <= totalRounds)
        {
            // Reset failure flag at start of each round attempt
            roundFailed = false;

            // Build sequence for this round (adds new steps each round)
            int targetLength = baseSequenceLength + (currentRound - 1) * sequenceIncrement;
            while (currentSequence.Count < targetLength)
                currentSequence.Add(Random.Range(0, gridButtons.Count));

            UpdateRoundText();
            SetStatus("WATCH CAREFULLY…");
            if (feedbackText) feedbackText.text = "";

            yield return new WaitForSeconds(0.8f);

            // Play the full sequence (up to targetLength)
            yield return StartCoroutine(PlaySequence(currentSequence, targetLength));

            yield return new WaitForSeconds(preInputDelay);

            playerInputStep = 0;
            acceptingInput = true;
            SetStatus("REPEAT THE SEQUENCE");

            // Wait until player finishes input (success) or fails (acceptingInput set false)
            yield return new WaitUntil(() => !acceptingInput);

            // If the round failed, retry without advancing to next round
            if (roundFailed)
            {
                SetFeedback("WRONG — RETRYING", wrongColor);
                yield return new WaitForSeconds(1.2f);
                continue; // Stay on same round
            }

            // Round succeeded – show success feedback
            if (currentRound < totalRounds)
            {
                SetFeedback("CORRECT", correctColor);
                yield return new WaitForSeconds(1.0f);
                ResetAllButtons();
            }

            // Advance to next round
            currentRound++;
        }

        // All rounds completed successfully
        SetStatus("ACCESS GRANTED");
        SetFeedback("SABOTAGE SUCCESSFUL", correctColor);
        yield return new WaitForSeconds(1.5f);
        CompleteTask();
    }

    private IEnumerator PlaySequence(List<int> sequence, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int index = sequence[i];
            SetButtonColor(index, flashColor);
            yield return new WaitForSeconds(flashOnDuration);
            SetButtonColor(index, idleColor);
            yield return new WaitForSeconds(flashOffDuration);
        }
    }


    private void OnGridButtonPressed(int index)
    {
        if (!acceptingInput) return;

        int expected = currentSequence[playerInputStep];

        if (index == expected)
        {
            // Correct press
            StartCoroutine(FlashButtonBriefly(index, correctColor));
            playerInputStep++;

            int targetLength = baseSequenceLength + (currentRound - 1) * sequenceIncrement;

            if (playerInputStep >= targetLength)
            {
                // Round complete
                acceptingInput = false;
            }
        }
        else
        {
            // Wrong press — fail this round and restart it
            StartCoroutine(HandleWrongInput(index));
        }
    }

    private IEnumerator HandleWrongInput(int pressedIndex)
    {
        acceptingInput = false;
        roundFailed = true;   // Signal that this round needs to be retried

        // Flash the wrong button red
        SetButtonColor(pressedIndex, wrongColor);
        yield return new WaitForSeconds(0.3f);
        SetButtonColor(pressedIndex, idleColor);
        yield return new WaitForSeconds(0.8f);

        // Clear any temporary feedback; the outer loop will show "WRONG — RETRYING"
        if (feedbackText) feedbackText.text = "";
    }


    private IEnumerator FlashButtonBriefly(int index, Color color)
    {
        SetButtonColor(index, color);
        yield return new WaitForSeconds(0.18f);
        SetButtonColor(index, idleColor);
    }

    private void SetButtonColor(int index, Color color)
    {
        if (index < 0 || index >= gridButtons.Count) return;

        // Use the Image component on the button for direct color control
        var img = gridButtons[index].GetComponent<Image>();
        if (img) img.color = color;
    }

    private void ResetAllButtons()
    {
        for (int i = 0; i < gridButtons.Count; i++)
            SetButtonColor(i, idleColor);
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    private void SetFeedback(string msg, Color color)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        feedbackText.color = color;
    }

    private void UpdateRoundText()
    {
        if (roundText)
            roundText.text = $"ROUND {currentRound} / {totalRounds}";
    }
}