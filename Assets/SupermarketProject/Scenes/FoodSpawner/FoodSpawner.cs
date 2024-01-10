using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns greyed out items with random scale in shelves
public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] durableable_food_items;
    [SerializeField] private GameObject[] fruit_items;
    [SerializeField] private GameObject[] beverages_items;

    private float[] scale_value = { 0.75f, 1.25f };
    //TODO delete all_items
    private List<GameObject> all_items = new List<GameObject>();
    private int number_of_items_per_row = 3;

    public void spawn_random_durable_item()
    {
        Vector3 item_scale = new Vector3(Random.Range(scale_value[0], scale_value[1]), Random.Range(scale_value[0], scale_value[1]), 1);
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);

        int random_item = Random.Range(0, durableable_food_items.Length);

        for (int i = 0; i < number_of_items_per_row; i++)
        {
            GameObject new_object = Instantiate(durableable_food_items[random_item], this.transform);
            new_object.transform.localPosition = new_object.transform.localPosition + new Vector3(i * 0.1f, 0f, 0f);
            new_object.transform.localScale = item_scale;
            all_items.Add(new_object);
        }
    }
    
    public void spawn_random_fruits_item()
    {
        Vector3 item_scale = new Vector3(Random.Range(scale_value[0], scale_value[1]), Random.Range(scale_value[0], scale_value[1]), 1);
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);

        int random_item = Random.Range(0, fruit_items.Length);

        for (int i = 0; i < number_of_items_per_row; i++)
        {
            GameObject new_object = Instantiate(fruit_items[random_item], this.transform);
            new_object.transform.localPosition = new_object.transform.localPosition + new Vector3(i * 0.1f, 0f, 0f);
            new_object.transform.localScale = item_scale;
            all_items.Add(new_object);
        }
    }

    public void spawn_random_drinks_item()
    {
        int random_item = Random.Range(0, beverages_items.Length);

        for (int i = 0; i < number_of_items_per_row; i++)
        {
            GameObject new_object = Instantiate(beverages_items[random_item], this.transform);
            new_object.transform.localPosition = new_object.transform.localPosition + new Vector3(i * 0.1f, 0f, 0f);
            all_items.Add(new_object);
        }
    }
}
