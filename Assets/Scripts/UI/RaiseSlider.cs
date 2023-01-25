using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils.HelperFunctions;
public class RaiseSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    private TextMeshProUGUI chipText;
    [SerializeField] private Color lowColour = Color.green;
    [SerializeField] private Color highColour = Color.red;
    private ColorBlock colorBlock;
    public EventHandler<SliderChangedEventArgs> OnValueChanged;
    private void Awake() {
        if (!slider) {
            return;
        }
        colorBlock = slider.colors;
        slider.onValueChanged.AddListener(delegate { ValueChanged(); });
        GameObject playerControls = slider.gameObject.transform.parent.gameObject;
        if (playerControls == null) {
            Debug.LogError("Player Controls UI not found");
            slider.gameObject.SetActive(false);
            return;
        }
        GameObject chipTextGO = Helpers.FindChildWithTag(playerControls, "BetSizeText");
        if(chipTextGO == null || chipTextGO.GetComponent<TextMeshProUGUI>() == null) {
            Debug.LogError("Chip text not found");
            slider.gameObject.SetActive(false);
            return;
        }
        chipText = chipTextGO.GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable() {
        if (!slider) {
            return;
        }
        slider.value = slider.minValue;
        colorBlock.normalColor = lowColour;
    }

    private void ValueChanged() {
        colorBlock.normalColor = Color.Lerp(lowColour, highColour, (slider.value - slider.minValue) / slider.maxValue);
        chipText.text = slider.value.ToString();
    }

}
