using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<AgentReposition>(out AgentReposition component))
        {
            this.GetComponentInParent<SetupSupermarketInterior>().calculate_a_star(this.transform.position);
            Destroy(this.gameObject);
        }
    }
}
