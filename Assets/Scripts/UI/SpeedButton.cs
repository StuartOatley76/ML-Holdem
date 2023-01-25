using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedButton : MonoBehaviour
{
    [SerializeField] private Button speedButton;
    [SerializeField] private string goSlowText = "Slow down";
    [SerializeField] private string goFastText = "Speed up";
    private TextMeshProUGUI speedButtonText;
    private void Start() {
        speedButtonText = speedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (Tournament.Instance.HasHumanPlayer) {
            speedButton.gameObject.SetActive(false);
            return;
        }
        SetText();
    }

    public void SetText() {
        if (!speedButtonText) {
            return;
        }
        StartCoroutine(ChangeText());
    }

    private IEnumerator ChangeText() {
        yield return null;
        speedButtonText.text = Tournament.Instance.GoSlow() ? goFastText : goSlowText;
    }
}
