using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchableFoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject durableable_food_items;
    [SerializeField] private GameObject beverages_items;
    [SerializeField] private GameObject fruit_items;

    private List<GameObject> all_items = new List<GameObject>();

    public void spawn_durable_item()
    {
        GameObject new_object = Instantiate(durableable_food_items, this.transform);
        all_items.Add(new_object);
    }
    public void spawn_drinks_item()
    {
        GameObject new_object = Instantiate(beverages_items, this.transform);
        all_items.Add(new_object);
    }
    public void spawn_fruit_item()
    {
        GameObject new_object = Instantiate(fruit_items, this.transform);
        all_items.Add(new_object);
    }
}
