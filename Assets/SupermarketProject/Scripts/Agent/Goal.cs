using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public void reposition(Vector3 position)
    {
        this.transform.localPosition = position;
    }
}
