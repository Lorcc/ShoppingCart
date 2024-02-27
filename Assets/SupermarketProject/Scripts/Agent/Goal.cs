using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<AgentReposition>(out AgentReposition component))
        {
            //this.GetComponentInParent<SetupSupermarketInterior>().calculate_a_star();
        }
    }

    public void reposition(Vector3 position)
    {
        this.transform.position = position;
    }
}
