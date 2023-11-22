using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelveFiller : MonoBehaviour
{
    enum Section { Fruit, Durable, Drinks }

    private GameObject generalizer;

    public void hello(int section)
    {
        Debug.Log("moin");
        if ((Section)section == Section.Durable)
        {
            foreach(Transform child in generalizer.transform)
            {
                PurchableFoodSpawner purchable_food_script = child.GetComponent<PurchableFoodSpawner>();
                if (purchable_food_script != null)
                {
                    Debug.Log("hello");
                }
            }
        }
    }
    private void Awake()
    {
        generalizer = this.transform.GetChild(0).gameObject;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
