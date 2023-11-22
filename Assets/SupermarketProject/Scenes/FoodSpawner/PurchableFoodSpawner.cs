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
    void Start()
    {
        Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
        GameObject new_object = Instantiate(durableable_food_items, this.transform);
        all_items.Add(new_object);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
