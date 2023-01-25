using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(Collider))]
public class Chips : MonoBehaviour
{
    public Player Player { private get; set; }
    private int tableID = -1;
    private static PlayerInformation playerInformation;

    private void OnEnable() {
        Table table = transform.root.gameObject.GetComponent<Table>();
        if (table != null) {
            tableID = table.TableID;
        }
        if(playerInformation == null) {
            playerInformation = FindObjectOfType<PlayerInformation>();
        }
    }

    private void Update() {
        if(CameraTracker.Instance == null || CameraTracker.Instance.IsCurrentCameraMainCamera || playerInformation == null
            || TableCamera.ActiveTable != tableID || Player == null) {
            return;
        }

        if(MousePosRaycaster.Instance != null) {
            RaycastHit hit = MousePosRaycaster.Instance.Hit;
            if(hit.collider.gameObject.GetComponent<Chips>() == this) {
                playerInformation.ShowInformation(Player);
            }
        } 
    }
}
