using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

/// <summary>
/// Class to handle a tournament's payout structure. Create the structure in unity inspector
/// </summary>
public class PayoutStructure : MonoBehaviour
{
    [SerializeField] private List<Payout> payouts; //List of the payouts for the tournament

    /// <summary>
    /// Gets the percentage payout of the funds for the given position
    /// </summary>
    /// <param name="position">The position in the tournament</param>
    /// <returns>payout percentage</returns>
    public float GetPayoutPercentage(int position) {
        Payout payout = null;
        try {
            payout = payouts.First(inRange => inRange.Range.IsInRange(position)); //This may not exist so we need the try catch
        } catch {
            return 0;
        }
        if(payout == null) {
            return 0;
        }
        return payout.Percentage;
    }

    /// <summary>
    /// Finds the finishing position for the next prize tier
    /// </summary>
    /// <param name="playersLeftIn">How many players are currently in the tournament</param>
    /// <returns>Position for the next prize tier</returns>
    public int GetNextPrizePosition(int playersLeftIn) {
        float currentPrize = GetPayoutPercentage(playersLeftIn);
        while(playersLeftIn > 0) {
            playersLeftIn--;
            float nextPrize = GetPayoutPercentage(playersLeftIn);
            if(nextPrize > currentPrize) {
                return playersLeftIn;
            }
        }
        return -1;
    }
}
