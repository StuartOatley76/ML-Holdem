using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utils.ExtentionMethods;

[RequireComponent(typeof(ScoredPlayerController))]
public class ScoredPlayer : Player
{

    public override void RequestAction(ActionRequestEventArgs actionRequest, int roundCounter) {
        lastStageStarted = roundCounter;
        state = PlayerState.EnteredRequest;
        AddActionArgsInfo(actionRequest);
        if(actionRequest.Stage == RoundStage.PreFlop) {
            ScorePreflopHand(ref actionRequest);
            base.RequestAction(actionRequest, roundCounter);
            return;
        }
        RequestHands?.Invoke(this, new CardsEventArgs(pocket, board));
    }

    protected void ScorePreflopHand(ref ActionRequestEventArgs actionRequest) {
         ScoredActionRequestEventArgs scoredActionRequest = new ScoredActionRequestEventArgs(actionRequest);
        int score = 0;
        pocket.Cards.SortDescending();
        if (pocket.Card1.Rank == pocket.Card2.Rank) {
            score = (int)HandRank.Pair << pocket.Cards.Length * 4;
        }
        for(int i = pocket.Cards.Length - 1; i >= 0; i--) {
            score |= (int)pocket.Cards[i].Rank << (i * 4);
        }
        scoredActionRequest.Score = score;
        actionRequest = scoredActionRequest;
    }

    protected override void ReceiveAnalysedHands(object obj, HandsEventArgs h) {
        //if (gettingScore) {
        //    Debug.LogWarning("Player " + PlayerID + " Received analysed hands");
        //}
        state = PlayerState.ReceivedAnalysis;
        hands = new List<Hand>(h.Hands);
        hands = hands.OrderByDescending(hand => hand.Score).ToList();
        if (hands == null || hands.Count == 0) {
            Debug.LogError("Hands null or empty");
        }
        if (gettingScore) {

            //foreach (Hand hand in hands) {
            //    foreach (Card card in hand.Cards) {
            //        Debug.LogWarning(card.ToString());
            //    }
            //    Debug.LogWarning(Enum.GetName(typeof(HandRank), hand.handRank));
            //    Debug.LogWarning("Ace low = " + hand.AceLowStraight);
            //    Debug.LogWarning("Score = " + Score);
            //}

            return;
        }
        actionArgs = new ScoredActionRequestEventArgs(actionArgs) {
            Score = Score
        };
        StartCoroutine(GetAction());
    }

}
