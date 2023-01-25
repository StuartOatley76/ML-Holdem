using System;
using UnityEngine;

public class CardCameraEventArgs : EventArgs {
    public Collider CardCollider { get; private set; }
    public Camera CardCamera { get; set; }

    public CardCameraEventArgs (Collider collider) {
        CardCollider = collider;
    }
}
