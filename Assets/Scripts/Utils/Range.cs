using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils {

    [Serializable]
    public class Range {

        [SerializeField] private int min; //Inclusive
        [SerializeField] private int max; //Inclusive

        public Range(int minimum, int maximum) {
            min = minimum;
            max = maximum;
        }

        public bool IsInRange(int numberToCheck) {
            return numberToCheck >= min && numberToCheck <= max;
        }
    }
}