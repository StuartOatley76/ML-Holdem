using System;
using System.Collections.Generic;
using UnityEngine;

public class SliderChangedEventArgs : EventArgs
{
    private float percentage;
    public float Percentage {
        get {
            return percentage;
        }
        private set {
            percentage = Mathf.Clamp(value, 0, 100);
        } 
    }
    public int Chips { get; set; }

    public SliderChangedEventArgs(float percentage) {
        Percentage = percentage;
    }
}
