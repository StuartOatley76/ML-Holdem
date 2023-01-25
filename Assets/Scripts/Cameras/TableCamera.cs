using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
/// <summary>
/// Class to handle a table camera's behaviour
/// </summary>
public class TableCamera : MonoBehaviour
{
    [SerializeField] private Camera tableCamera;
    [SerializeField] private Collider tableCollider;
    private static Camera MainCamera;
    private int tableLayerMask;
    public static int ActiveTable { get; private set; } = -1;
    private void Start() {
        MainCamera = Camera.main;
        tableLayerMask = LayerMask.GetMask("Table");
    }
    private void Update() {

        if (!TableManager.Instance.IsHumanPlayerPlaying && Input.GetKeyDown(KeyCode.Escape)) {
            CancelCamera();
        }

        if (Input.GetMouseButtonDown(0)) {

            //Don't perform raycast if mouse is over UI element
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            RaycastHit hit;
            
            Ray ray = CameraTracker.Instance.CurrentCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, tableLayerMask)) {
                if (!(hit.collider == tableCollider)) {
                    return;
                }
                ChangeToThisCamera();
            }

        }
    }
    private void OnDisable() {
        CancelCamera();
    }

    public void ChangeToThisCamera() {
        CameraTracker.Instance.ChangeCamera(tableCamera);
        Table table = transform.root.gameObject.GetComponent<Table>();
        if (table != null) {
            ActiveTable = table.TableID;
        } else {
            ActiveTable = -1;
        }
    }

    public static void CancelCamera() {
        CameraTracker.Instance.ChangeCamera(MainCamera);
        ActiveTable = -1;
    }
}
