using System;
using System.Collections;
using UnityEngine;
using Utils;

/// <summary>
/// Class to hanlde main camera behaviour
/// </summary>
public class MainCamera : MonoBehaviour
{
    private Camera mainCamera;  //The camera

    //Floats to handle min and max positions for the camera
    private float minXPos;
    private float maxXPos;
    private float minZPos;
    private float maxZPos;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float xBuffer;
    [SerializeField] private float zBuffer;

    /// <summary>
    /// Connects the set bounds function to the game manager's TableAreaEvent
    /// </summary>
    private void Awake() {
        StartCoroutine(WaitBeforeConnectingEvents());
        
    }

    private IEnumerator WaitBeforeConnectingEvents() {
        yield return new WaitUntil(() => TableManager.Instance != null);
        TableManager.Instance.SetTableArea += SetBounds;
    }

    /// <summary>
    /// Sets the camera bounds based on the number of tables being created and calls CentreCamera()
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetBounds(object sender, TableAreaEventArgs e) {
        minXPos = xBuffer;
        maxXPos = e.NumberOfColumns * e.SpaceBetweenTables - xBuffer;
        minZPos = zBuffer;
        maxZPos = e.NumberOfRows * e.SpaceBetweenTables - zBuffer;
        CentreCamera();
    }

    /// <summary>
    /// Centres the camera in the middle of the play area
    /// </summary>
    private void CentreCamera() {
        Vector3 centre = new Vector3(
                    (maxXPos + minXPos) / 2,
                    transform.position.y,
                    (maxZPos + minZPos) / 2
                    );
        transform.position = centre;
    }

    /// <summary>
    /// Sets the main camera
    /// </summary>
    private void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Sets the camera to move if active
    /// </summary>
    private void Update() {
        if(mainCamera.enabled && Tournament.Instance.HasHumanPlayer && Table.HumanPlayerTable != null) {
            TableCamera camera = Table.HumanPlayerTable.gameObject.GetComponentInChildren<TableCamera>();
            if(camera != null) {
                camera.ChangeToThisCamera();
                return;
            }
        }
        if(mainCamera.enabled == false) {
            return;
        }
        MoveCamera();
    }

    /// <summary>
    /// Moves the camera if necessary
    /// </summary>
    private void MoveCamera() {
        Vector3 directionToMove = MovementController2D.Instance.CheckForMovement();
        if(directionToMove == Vector3.zero) {
            return;
        }
        Vector3 nextPosition = transform.position + directionToMove;
        RestrictToBounds(ref nextPosition);
        transform.position = Vector3.Lerp(transform.position, nextPosition, Time.deltaTime * moveSpeed);
    }

    /// <summary>
    /// ensures the position to move to is within the camera bounds
    /// </summary>
    /// <param name="nextPosition"></param>
    private void RestrictToBounds(ref Vector3 nextPosition) {
        nextPosition.x = (nextPosition.x < minXPos) ? minXPos : nextPosition.x;
        nextPosition.x = (nextPosition.x > maxXPos) ? maxXPos : nextPosition.x;
        nextPosition.z = (nextPosition.z < minZPos) ? minZPos : nextPosition.z;
        nextPosition.z = (nextPosition.z > maxZPos) ? maxZPos : nextPosition.z;
    }
}
