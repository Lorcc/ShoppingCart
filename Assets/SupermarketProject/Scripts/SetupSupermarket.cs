using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSupermarket : MonoBehaviour
{
    [SerializeField] private int min_ground_size = 20;
    [SerializeField] private int max_ground_size = 30;

    [SerializeField] private int min_entrance_size = 6;
    [SerializeField] private int max_entrance_size = 7;

    [SerializeField] private GameObject shelf_pref;
    [SerializeField] private GameObject entrance_pref;
    [SerializeField] private GameObject fruits_pref;
    [SerializeField] private GameObject durablefood_pref;
    [SerializeField] private GameObject alcohol_pref;



    private List<GameObject> entrance_tiles = new List<GameObject>();
    private List<GameObject> fruits_tiles = new List<GameObject>();
    private List<GameObject> durablefood_tiles = new List<GameObject>();
    private List<GameObject> alcohol_tiles = new List<GameObject>();
    private List<GameObject> shelve_tiles = new List<GameObject>();


    public void calculate_Grid()
    {
        // Generate ground
        int grid_size_x = Random.Range(min_ground_size, max_ground_size);
        int grid_size_y = Random.Range(min_ground_size, max_ground_size);
        GameObject ground = this.transform.Find("Ground").gameObject;
        ground.transform.localScale = new Vector3(grid_size_x, 0.5f, grid_size_y);
        Debug.Log("Grid in x: " + grid_size_x);
        Debug.Log("Grid in y: " + grid_size_y);

        // Generate entrance
        Quaternion entranceRotation = Quaternion.Euler(0, 0, 0);
        entrance_pref.transform.localScale = new Vector3(Random.Range(min_entrance_size, max_entrance_size), 0.5f, Random.Range(min_entrance_size, max_entrance_size));
        Vector3 entrance_size = entrance_pref.transform.localScale;
        Vector3 entrance_position = this.transform.localPosition + new Vector3((grid_size_x / 2.0f - entrance_size[0] / 2.0f), 0.5f, (-grid_size_y / 2.0f + entrance_size[2] / 2.0f));
        GameObject entrance = Instantiate(entrance_pref, entrance_position, entranceRotation, this.transform);
        entrance_tiles.Add(entrance);

        // Generate fruits vegetable 
        Quaternion fruitsRotation = Quaternion.Euler(0, 0, 0);
        fruits_pref.transform.localScale = new Vector3(entrance_size[0], 0.5f, grid_size_y - entrance_size[2]);
        Vector3 fruits_position = this.transform.localPosition + new Vector3((grid_size_x / 2.0f - fruits_pref.transform.localScale[0] / 2.0f), 0.5f, (grid_size_y / 2.0f - fruits_pref.transform.localScale[2] / 2.0f));
        GameObject fruits = Instantiate(fruits_pref, fruits_position, fruitsRotation, this.transform);
        fruits_tiles.Add(fruits);

        // Generate durablefood
        Quaternion durablefoodRotation = Quaternion.Euler(0, 0, 0);
        durablefood_pref.transform.localScale = new Vector3(grid_size_x - entrance_size[0], 0.5f, grid_size_y - entrance_size[2]);
        Vector3 durablefood_position = this.transform.localPosition + new Vector3(( durablefood_pref.transform.localScale[0]/2.0f - grid_size_x / 2.0f), 0.5f, (grid_size_y / 2.0f - durablefood_pref.transform.localScale[2] / 2.0f));
        GameObject durablefood = Instantiate(durablefood_pref, durablefood_position, durablefoodRotation, this.transform);
        durablefood_tiles.Add(durablefood);

        // Generate fruits vegetable 
        Quaternion alcoholRotation = Quaternion.Euler(0, 0, 0);
        alcohol_pref.transform.localScale = new Vector3(grid_size_x - entrance_size[0], 0.5f, entrance_size[2]);
        Vector3 alcohol_position = this.transform.localPosition + new Vector3((alcohol_pref.transform.localScale[0] / 2.0f - grid_size_x / 2.0f), 0.5f, (-grid_size_y / 2.0f + alcohol_pref.transform.localScale[2] / 2.0f));
        GameObject alcohol = Instantiate(alcohol_pref, alcohol_position, alcoholRotation, this.transform);
        alcohol_tiles.Add(alcohol);

        // Generate north outershelves
        //bool[,] number_of_tiles = new bool[grid_size_x, grid_size_y];
        /*
        for (int x = 1; x < grid_size_x; x++)
        {
            float offset = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3((x - (grid_size_x / 2.0f)), 0, (grid_size_y / 2.0f) - offset);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelf_pref, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate south outershelves
        for (int x_entr = 1; x_entr < grid_size_x - entrance_pref.transform.localScale[0]; x_entr++)
        {
            float offset = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f)), 0, (-grid_size_y / 2.0f) + offset);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelf_pref, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate west outershelves
        for (int y = 1; y < grid_size_y; y++)
        {
            float offset = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((-grid_size_x / 2.0f) + offset), 0, y - (grid_size_y / 2.0f));
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelf_pref, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate east outershelves
        for (int y_entr = (int)entrance_pref.transform.localScale[2]; y_entr < grid_size_y; y_entr++)
        {
            float offset = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((grid_size_x / 2.0f) - offset), 0, y_entr - (grid_size_y / 2.0f));
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelf_pref, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }*/
    }


    // Start is called before the first frame update
    void Start()
    {
        calculate_Grid();
    }


    // Update is called once per frame
    void Update()
    {

    }
}
