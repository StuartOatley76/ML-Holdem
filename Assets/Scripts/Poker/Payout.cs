using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

[Serializable]
public class Payout
{
    [SerializeField] private Range range;
    [SerializeField] private float percentage;
    public Range Range { get { return range; } }
    public float Percentage { get { return percentage; } } 
    public Payout(int min, int max, float percentOfTotal) {
        range = new Range(min, max);
        percentage = percentOfTotal;
    }
}
