using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class OutsPlayerController : ScoredPlayerController
{
    private int numberOfOuts;

    protected override void CollectAdditionalObservations(VectorSensor sensor) {
        base.CollectAdditionalObservations(sensor);
        if(board == null) {
            return;
        }
        int numberOfCardsLeftInDeck = 52 - (numberOfPlayersAtTable * 2 + board.Length);
        float percentageChanceOfHittingAnOut = numberOfOuts / numberOfCardsLeftInDeck * 100;
        sensor.AddObservation(percentageChanceOfHittingAnOut);
    }

    protected override void SetAdditionalInformation(ActionRequestEventArgs e) {
        base.SetAdditionalInformation(e);
        if(!(e is OutsActionEventArgs)) {
            numberOfOuts = -1;
            return;
        }
        OutsActionEventArgs outs = (OutsActionEventArgs)e;
        numberOfOuts = outs.Outs;
    }
}
