using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

internal class PotInformation : MonoBehaviour {

    [SerializeField] private GameObject informationHolder;
    [SerializeField] private TextMeshProUGUI potTotal;
    private bool hasBeenUsedThisFrame = false;
    public void ShowInformation(int potValue) {
        potTotal.text = potValue.ToString();
        hasBeenUsedThisFrame = true;
        informationHolder.SetActive(true);
    }

    private void LateUpdate() {
        if (!hasBeenUsedThisFrame) {
            potTotal.text = string.Empty;
            informationHolder.SetActive(false);
        }
        hasBeenUsedThisFrame = false;
    }
}