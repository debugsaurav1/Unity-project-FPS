using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damagetest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FirstPersonController.OnTakeDamage(15);
        }
    }
}
