using UnityEngine;
using Utils;
using UnityEngine.EventSystems;
/// <summary>
/// Singleton to allow only one raycast from the mouse position to be needed each frame
/// </summary>
public class MousePosRaycaster : UnitySingleton<MousePosRaycaster> {

    private RaycastHit hit;
    public RaycastHit Hit { get { return hit; } }

    /// <summary>
    /// 
    /// </summary>
    void Update() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        Ray ray = CameraTracker.Instance.CurrentCamera.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out hit);
    }
}
