using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<AgentReposition>(out AgentReposition component))
        {
            Debug.Log("moin");
            //Destroy(this.gameObject);
        }
    }
}
