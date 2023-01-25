using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotUI : MonoBehaviour
{
    RoundOfPoker round;
    private int tableID = -1;
    private static PotInformation potInformation;
    [SerializeField] private GameObject chips;

    private void OnEnable() {
        Table table = transform.root.gameObject.GetComponent<Table>();
        if (table != null) {
            tableID = table.TableID;
        }
        if (potInformation == null) {
            potInformation = FindObjectOfType<PotInformation>();
        }
        if(round == null) {
            round = gameObject.transform.root.GetComponentInChildren<RoundOfPoker>();
        }
    }


    private void Update() {

        if(round == null) {
            return;
        }
        if(round.PotValue <= 0) {
            chips.SetActive(false);
            return;
        }
        chips.SetActive(true);

        if (CameraTracker.Instance == null || CameraTracker.Instance.IsCurrentCameraMainCamera || potInformation == null
           || TableCamera.ActiveTable != tableID) {
            return;
        }

        if (MousePosRaycaster.Instance != null) {
            RaycastHit hit = MousePosRaycaster.Instance.Hit;
            if (hit.collider.gameObject.GetComponent<PotUI>() == this) {
                potInformation.ShowInformation(round.PotValue);
            }
        }
    }
}
