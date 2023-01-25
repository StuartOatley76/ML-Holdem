using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Unity.MLAgents;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PayoutStructure), typeof(PlayerManager), typeof(TableManager))]
public class Tournament : UnitySingleton<Tournament> {

    [Range(400, 600)]
    [SerializeField] private int startingNumberOfPlayersInTournament;
    public int StartingNumberOfPlayersInTournament { get { return startingNumberOfPlayersInTournament; } }

    public EventHandler<NewTournamentEventArgs> NewTournamentEvent;

    private int nextPrizePosition;
    private PayoutStructure payoutStructure;

    [SerializeField] private float entryFee;
    private float totalEntryCost;
    [SerializeField] private int startingStack;
    public int StartingStack { get { return startingStack; } }
    private float entryTostartingRatio;

    private int secondsBetweenBlindIncreasesWithoutHumanPlayer = 60 * 2;

    public EventHandler<BlindIncreaseEventArgs> BlindIncrease;
    [SerializeField] private bool includeHumanPlayer;
    public bool HasHumanPlayer { get { return includeHumanPlayer; } set { includeHumanPlayer = value; } }

    /// <summary>
    /// Used https://pokersoup.com/tool/blindStructureCalculator to calculate these
    /// </summary>
    [SerializeField] private List<int> smallBlindLevels;
    private int currentSmallBlindLevel = 0;
    private bool goSlow = false;
    private float pauseBetweenTourneys = 10f;

    private void Start() {
        entryTostartingRatio = startingStack / entryFee;
        totalEntryCost = startingNumberOfPlayersInTournament * entryFee;
        Academy.Instance.AutomaticSteppingEnabled = false;
        payoutStructure = GetComponent<PayoutStructure>();
        nextPrizePosition = payoutStructure.GetNextPrizePosition(startingNumberOfPlayersInTournament);
        StartCoroutine(WaitBeforeConnectingEvents());
        StartCoroutine(WaitBeforeTriggeringEvent());
        if (!HasHumanPlayer) {
            GameObject HumanControlsUI = GameObject.FindGameObjectWithTag("PlayerControls");
            if(HumanControlsUI != null) {
                HumanControlsUI.SetActive(false);
            }
        }
        InvokeRepeating(nameof(IncreaseBlinds), secondsBetweenBlindIncreasesWithoutHumanPlayer, secondsBetweenBlindIncreasesWithoutHumanPlayer);
    }

    private IEnumerator WaitBeforeConnectingEvents() {
        yield return new WaitUntil(() => PlayerManager.instance != null);
        PlayerManager.instance.TournamentFinished -= TournamentFinished;
        PlayerManager.instance.TournamentFinished += TournamentFinished;
        

    }

    private void TournamentFinished(object sender, TournamentFinishedEventArgs e) {
        Debug.Log("Tournament over, player " + e.Winner.PlayerID + " wins");

        StartCoroutine(StartNextTourney());
        

        //#if UNITY_EDITOR
        //        if (EditorApplication.isPlaying) {
        //            UnityEditor.EditorApplication.isPlaying = false;
        //        }
        //#endif
        //        Application.Quit();
    }

    private IEnumerator StartNextTourney() {
        yield return new WaitForSeconds(pauseBetweenTourneys);
        currentSmallBlindLevel = 0;
        NewTournamentEvent?.Invoke(this, new NewTournamentEventArgs(startingNumberOfPlayersInTournament, startingStack, smallBlindLevels[currentSmallBlindLevel]));
    }

    private IEnumerator WaitBeforeTriggeringEvent() {
        yield return new WaitUntil(() => NewTournamentEvent != null);
        NewTournamentEvent?.Invoke(this, new NewTournamentEventArgs(startingNumberOfPlayersInTournament, startingStack, smallBlindLevels[currentSmallBlindLevel]));
    }

    private void IncreaseBlinds() {
        BlindIncrease?.Invoke(this, new BlindIncreaseEventArgs(smallBlindLevels[currentSmallBlindLevel]));
        currentSmallBlindLevel = (currentSmallBlindLevel + 1 < smallBlindLevels.Count) ? currentSmallBlindLevel + 1 : currentSmallBlindLevel;
    }

    public int GetNextPrizePosition() {
        if(nextPrizePosition >= PlayerManager.instance.NumberOfPlayersLeftIn) {
            nextPrizePosition = payoutStructure.GetNextPrizePosition(PlayerManager.instance.NumberOfPlayersLeftIn);
        }
        return nextPrizePosition;
    }

    public int GetNextPrizeAmountInChips() {
        return (int)(GetNextPrizeAmountInCash() * entryTostartingRatio);
    }

    public int GetCurrentPrizeAmountInChips() {
        return (int)(GetCurrentPrizeAmountInCash() * entryTostartingRatio);
    }

    public float GetCurrentPrizeAmountInCash() {
        float percentage = payoutStructure.GetPayoutPercentage(PlayerManager.instance.NumberOfPlayersLeftIn);
        return totalEntryCost * 0.01f * percentage;
    }

    public float GetNextPrizeAmountInCash() {
        float percentage = payoutStructure.GetPayoutPercentage(GetNextPrizePosition());
        return totalEntryCost * 0.01f * percentage;
    }

    public bool GoSlow() {
        if (HasHumanPlayer || goSlow) {
            return true;
        }
        return false;
    }

    public void ToggleSpeed() {
        goSlow = !goSlow;
    }
}