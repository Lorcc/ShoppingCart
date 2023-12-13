using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelveFiller : MonoBehaviour
{
    enum Section { Fruit, Durable, Drinks }

    private GameObject generalizer;

    int counter;
    public Vector3 spawn_purchable_item(int section)
    {
        Vector3 p_item_position = new Vector3();
        int counter = generalizer.transform.childCount;
        if ((Section)section == Section.Durable)
        {
            foreach(Transform child in generalizer.transform)
            {

                float random_number = Random.Range(0.0f, 1.0f);

                if (random_number < (1.0f / (float)counter))
                {
                    PurchableFoodSpawner purchable_food_script = child.GetComponent<PurchableFoodSpawner>();
                    if (purchable_food_script != null)
                    {
                        purchable_food_script.spawn_durable_item();
                        p_item_position = purchable_food_script.transform.localPosition;
                        break;
                    }
                }
                counter--;
            }
        }
        else if ((Section)section == Section.Fruit)
        {
            foreach (Transform child in generalizer.transform)
            {

                float random_number = Random.Range(0.0f, 1.0f);

                if (random_number < (1.0f / (float)counter))
                {
                    PurchableFoodSpawner purchable_food_script = child.GetComponent<PurchableFoodSpawner>();
                    if (purchable_food_script != null)
                    {
                        purchable_food_script.spawn_fruit_item();
                        p_item_position = purchable_food_script.transform.localPosition;
                        break;
                    }
                }
                counter--;
            }
        }
        else if ((Section)section == Section.Drinks)
        {
            foreach (Transform child in generalizer.transform)
            {
                float random_number = Random.Range(0.0f, 1.0f);

                if (random_number < (1.0f / (float)counter))
                {
                    PurchableFoodSpawner purchable_food_script = child.GetComponent<PurchableFoodSpawner>();
                    if (purchable_food_script != null)
                    {
                        purchable_food_script.spawn_drinks_item();
                        p_item_position = purchable_food_script.transform.localPosition;
                        break;
                    }
                }
                counter--;
            }
        }
        return p_item_position;
    }

    private void Awake()
    {
        // Gets purchable_food group from the scene
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
