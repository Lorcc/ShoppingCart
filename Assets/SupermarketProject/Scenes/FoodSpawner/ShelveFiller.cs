using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelveFiller : MonoBehaviour
{
    enum Section { Fruit, Durable, Drinks }

    private GameObject generalizer;
    private GameObject random_item;

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

    public void spawn_random_items(int section, int shelve_type)
    {
        if ((Section)section == Section.Durable)
        {
            foreach (Transform child in random_item.transform)
            {
                FoodSpawner random_food_spawner_script = child.GetComponent<FoodSpawner>();
                if (random_food_spawner_script != null)
                {
                    random_food_spawner_script.spawn_random_durable_item();
                }
            }
        }
        else if ((Section)section == Section.Fruit)
        {
            foreach (Transform child in random_item.transform)
            {
                FoodSpawner random_food_spawner_script = child.GetComponent<FoodSpawner>();
                if (random_food_spawner_script != null)
                {
                    random_food_spawner_script.spawn_random_fruits_item(shelve_type);
                }
            }
        }
        else if ((Section)section == Section.Drinks)
        {
            foreach (Transform child in random_item.transform)
            {
                FoodSpawner random_food_spawner_script = child.GetComponent<FoodSpawner>();
                if (random_food_spawner_script != null)
                {
                    random_food_spawner_script.spawn_random_drinks_item();
                }
            }
        }
    }

    private void Awake()
    {
        // Gets purchable_food group from the scene
        generalizer = this.transform.GetChild(0).gameObject;
        random_item = this.transform.GetChild(1).gameObject;
    }
}
