using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentReposition : MonoBehaviour
{
    public void reposition(Vector3 position)
    {
        this.transform.localPosition = position;
        this.transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
