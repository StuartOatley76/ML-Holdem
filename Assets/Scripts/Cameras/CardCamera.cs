using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardCamera : MonoBehaviour
{
    [SerializeField] private Collider cardCollider;
    [SerializeField] private Camera cardCamera;
    
    private void Start()
    {
        cardCamera.enabled = false;
        FindObjectOfType<CardUI>().OnCardColliderClicked += OnCardColliderClicked;
    }

    private void OnCardColliderClicked(object sender, CardCameraEventArgs e) {
        if(e.CardCollider == cardCollider) {
            e.CardCamera = cardCamera;
        }
    }
}
