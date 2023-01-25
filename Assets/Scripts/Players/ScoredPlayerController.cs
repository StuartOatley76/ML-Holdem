using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ScoredPlayerController : PlayerController
{
    private int handScore = 0;
    private int winningScore = 0;
    protected override void CollectAdditionalObservations(VectorSensor sensor) {
        sensor.AddObservation(handScore);
    }

    protected override void SetAdditionalInformation(ActionRequestEventArgs e) {
        if(!(e is ScoredActionRequestEventArgs)) {
            handScore = -1;
            return;
        }
        ScoredActionRequestEventArgs scored = (ScoredActionRequestEventArgs)e;
        handScore = scored.Score;
    }

    protected override void AddWinningHandInformation(IWinningHandEventArgs actionArgs) {
        base.AddWinningHandInformation(actionArgs);
        winningScore = actionArgs.WinningScore;
    }

    protected override void SetWinningPocketInformation(VectorSensor sensor) {
        base.SetWinningPocketInformation(sensor);
        sensor.AddObservation(winningScore);
    }
}
