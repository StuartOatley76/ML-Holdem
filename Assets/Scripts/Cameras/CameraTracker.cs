using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class CameraTracker : UnitySingleton<CameraTracker>
{
    public Camera CurrentCamera { get; private set; }
    private Camera mainCamera;
    public EventHandler MainCameraActive;
    public bool IsCurrentCameraMainCamera { 
        get { 
            return CurrentCamera == mainCamera; 
        } 
    }

    private void Start() {
        mainCamera = Camera.main;
        CurrentCamera = mainCamera;
    }
    public void ChangeCamera(Camera camera) {
        if(camera == null) {
            return;
        }
        if (!camera.gameObject.activeInHierarchy) {
            camera.gameObject.SetActive(true);
        }
        CurrentCamera.enabled = false;
        CurrentCamera = camera;
        CurrentCamera.enabled = true;
        if(CurrentCamera == mainCamera) {
            MainCameraActive?.Invoke(this, EventArgs.Empty);
        }
    }   
}
