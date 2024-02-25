using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchableFoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] durableable_food_items;
    [SerializeField] private GameObject[] fruit_items;
    [SerializeField] private GameObject[] beverages_items;
    //TODO delete all_items at end
    private List<GameObject> all_items = new List<GameObject>();

    public void spawn_durable_item()
    {
        //Return a random int within [minInclusive..maxExclusive)
        int random_item = Random.Range(0, durableable_food_items.Length);
        GameObject new_object = Instantiate(durableable_food_items[random_item], this.transform);
        all_items.Add(new_object);
    }
    public void spawn_fruit_item()
    {
        int random_item = Random.Range(0, fruit_items.Length);
        GameObject new_object = Instantiate(fruit_items[random_item], this.transform);
        all_items.Add(new_object);
    }
    public void spawn_drinks_item()
    {
        int random_item = Random.Range(0, beverages_items.Length);
        GameObject new_object = Instantiate(beverages_items[random_item], this.transform);
        all_items.Add(new_object);
    }

    public void spawn_fruit_item(Vector3 p_item_pos)
    {
        int random_item = Random.Range(0, fruit_items.Length);
        GameObject new_object = Instantiate(fruit_items[random_item], this.transform);
        all_items.Add(new_object);
    }

}
