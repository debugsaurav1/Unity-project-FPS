using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doors : Interactables
{
    private bool isOpen = false;
    private bool canBeInteractedWith = true;
    private Animator doorAnimator;

    private void Start()
    {
        doorAnimator = GetComponent<Animator>();
    }
    public override void OnFocus()
    {
        return;
    }

    public override void OnInteract()
    {
        if (canBeInteractedWith)
        {
            isOpen = !isOpen;
            Vector3 doorTransformDirection = transform.TransformDirection(Vector3.right);
            Vector3 playerTransformDirection = FirstPersonController.instance.transform.position - transform.position;
            float dotproduct = Vector3.Dot(doorTransformDirection, playerTransformDirection);

            doorAnimator.SetFloat("dotproduct", dotproduct);
            doorAnimator.SetBool("isOpen", isOpen);
        }
    }

    public override void OnLoseFocus()
    {
        return;
    }
}
