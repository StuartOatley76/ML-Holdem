using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OutsPlayerController), typeof(OutsFinder))]
public class OutsPlayer : ScoredPlayer
{
    private EventHandler<FindOutsEventArgs> GetOutsEvent;


    OutsFinder outsFinder;
    protected override void Start() {
        base.Start();
        outsFinder = GetComponent<OutsFinder>();
        GetOutsEvent += outsFinder.SetListener(ReceiveOuts);
    }

    private void ReceiveOuts(object obj, OutsEventArgs outs) {
        state = PlayerState.ReceivedOuts;
        actionArgs = new OutsActionEventArgs(actionArgs) {
            Score = Score,
            Outs = outs.Outs
        };
        StartCoroutine(GetAction());
        
    }

    protected override void ReceiveAnalysedHands(object obj, HandsEventArgs h) {
        state = PlayerState.ReceivedAnalysis;
        hands = new List<Hand>(h.Hands);
        hands = hands.OrderByDescending(hand => hand.Score).ToList();
        if(hands == null || hands.Count == 0) {
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
        GetOutsEvent?.Invoke(this, new FindOutsEventArgs(pocket, actionArgs.Board, hands[0]));
    }
}
