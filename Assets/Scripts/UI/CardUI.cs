using System;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{

    [SerializeField] private RawImage cardDisplay;

    private Camera currentCardCamera;

    public event EventHandler<CardCameraEventArgs> OnCardColliderClicked;
    int holeCardLayerMask;
    private void Start() {
        holeCardLayerMask = LayerMask.GetMask("HoleCards");
        cardDisplay.enabled = false;
        CameraTracker.Instance.MainCameraActive += Disable;
    }

    private void Update()
    {
        if (CameraTracker.Instance.IsCurrentCameraMainCamera) {
            return;
        }
        if (Input.GetMouseButtonDown(0)) {

            if (MousePosRaycaster.Instance != null) {
                RaycastHit hit = MousePosRaycaster.Instance.Hit;
                if(1 << hit.collider.gameObject.layer != holeCardLayerMask) {
                    return;
                }
                CardCameraEventArgs newCamera = new CardCameraEventArgs(hit.collider);
                OnCardColliderClicked?.Invoke(this, newCamera);
                if(newCamera.CardCamera == null) {
                    return;
                }

                if(currentCardCamera != null) {
                    currentCardCamera.enabled = false;
                }
                currentCardCamera = newCamera.CardCamera;
                currentCardCamera.enabled = true;
                cardDisplay.enabled = true;
            }
        }
    }

    private void Disable(object o, EventArgs e) {
        if (currentCardCamera != null) {
            currentCardCamera.enabled = false;
            currentCardCamera = null;
            cardDisplay.enabled = false;
        }
    }

}
