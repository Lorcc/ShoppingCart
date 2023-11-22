using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchableFoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject durableable_food_items;
    [SerializeField] private GameObject alcohol_items;
    [SerializeField] private GameObject fruit_items;

    private List<GameObject> all_items = new List<GameObject>();
    // Start is called before the first frame update

    public void spawn_durable_item()
    {
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
        GameObject new_object = Instantiate(durableable_food_items, this.transform);
        all_items.Add(new_object);
    }
    void spawn_alcohol_item()
    {
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
        GameObject new_object = Instantiate(alcohol_items, this.transform);
        all_items.Add(new_object);
    }
    void spawn_fruit_item()
    {
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
        GameObject new_object = Instantiate(fruit_items, this.transform);
        all_items.Add(new_object);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
