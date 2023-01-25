using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils;
public class HumanPlayerController : OutsPlayerController
{
    GameObject HumanControlsUI;
    private Slider slider;
    private float[] vectorAction = new float[2];
    private bool humanControl;
    private bool humanActionMade;
    private TextMeshProUGUI CheckButtonText;
    protected override void OnEnable() {
        base.OnEnable();
        StartCoroutine(WaitBeforeSetup());
    }

    private IEnumerator WaitBeforeSetup() {

        if (Tournament.Instance == null) {
            yield return new WaitUntil(() => Tournament.Instance != null);
        }
        if (Tournament.Instance.HasHumanPlayer) {
            HumanControlsUI = GameObject.FindGameObjectWithTag("PlayerControls");
            if (HumanControlsUI == null) {
                Debug.LogError("No Player Controls UI found");
                yield break;
            }
            SetControlsVisibility(false);
            try {
                Utils.HelperFunctions.Helpers.FindChildWithTag(HumanControlsUI, "FoldButton")
                    .GetComponent<Button>().onClick.AddListener(delegate { OnButtonPress((int)PokerAction.Fold); });
                Button check = Utils.HelperFunctions.Helpers.FindChildWithTag(HumanControlsUI, "CallButton").GetComponent<Button>();
                CheckButtonText = check.GetComponentInChildren<TextMeshProUGUI>();
                CheckButtonText.text = "Check";
                check.onClick.AddListener(delegate { OnButtonPress((int)PokerAction.CheckOrCall); });
                Utils.HelperFunctions.Helpers.FindChildWithTag(HumanControlsUI, "RaiseButton")
                    .GetComponent<Button>().onClick.AddListener(delegate { OnButtonPress((int)PokerAction.Raise); });
                slider = Utils.HelperFunctions.Helpers.FindChildWithTag(HumanControlsUI, "RaiseSlider").GetComponent<Slider>();
                slider.minValue = 0;
                StartCoroutine(SetInitialSliderValues());
            } catch {
                Debug.LogError("Player Controls UI not set up properly");
                HumanControlsUI.SetActive(false);
                yield break;
            }
            humanControl = true;
        } else { 
            humanControl = false; 
        }
    }

    protected override void RequestPlayerAction(object o, ActionRequestEventArgs e) {

        if (humanControl) {
            slider.minValue = e.Bet.MinAmount;
            if (e.Bet.MinAmount == 0) {
                CheckButtonText.text = "Check";
            } else {
                CheckButtonText.text = "Call " + e.Bet.MinAmount;
            }

            slider.value = slider.minValue;
            slider.maxValue = player.Stack;
            StartCoroutine(WaitForPlayerAction(o, e));
        } else {
            base.RequestPlayerAction(o, e);
        }
    }

    private IEnumerator WaitForPlayerAction(object o, ActionRequestEventArgs e) {
        SetControlsVisibility(true);
        yield return new WaitUntil(() => humanActionMade == true);
        SetControlsVisibility(false);
        base.RequestPlayerAction(o, e);
    }

    private IEnumerator SetInitialSliderValues() {
        yield return new WaitUntil(() => player != null);
        slider.maxValue = player.Stack;

    }

    private void SetControlsVisibility(bool visibility) {
        foreach (Transform child in HumanControlsUI.transform) {
            child.gameObject.SetActive(visibility);
        }
    }

    public void OnButtonPress(int action) {
        vectorAction = new float[2];
        vectorAction[0] = action;
        if((PokerAction)action == PokerAction.Raise) {
            vectorAction[1] = slider.value / (player.Stack / 100);
        }
        SetControlsVisibility(false);
        humanActionMade = true;
    }

    public override void Heuristic(float[] actionsOut) {
        actionsOut[0] = vectorAction[0];
        actionsOut[1] = vectorAction[1];
        humanActionMade = false;
    }

}
