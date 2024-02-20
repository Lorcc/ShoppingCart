using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSupermarketRepaired : MonoBehaviour
{
    [SerializeField] private bool fixed_ground_size = true;
    [SerializeField] [Range(13, 50)] private int x_ground_size = 20;
    [SerializeField] [Range(13, 50)] private int z_ground_size = 20;
    [SerializeField] [Range(13, 50)] private int min_ground_size = 20;
    [SerializeField] [Range(13, 50)] private int max_ground_size = 30;
    [SerializeField] private bool fixed_entrance_size = true;
    [SerializeField] [Range(6, 20)] private int x_entrance_size = 6;
    [SerializeField] [Range(6, 20)] private int z_entrance_size = 6;
    [SerializeField] [Range(6, 20)] private int min_entrance_size = 6;
    [SerializeField] [Range(6, 20)] private int max_entrance_size = 6;

    private List<GameObject> ground_tiles = new List<GameObject>();


    public void setup_Supermarket()
    {
        ///// Test if Input Values are Viable ///// 
        check_Input();

        int grid_size_x;
        int grid_size_z;
        int entrance_size_x;
        int entrance_size_z;

        if (fixed_ground_size)
        {
            grid_size_x = x_ground_size;
            grid_size_z = z_ground_size;
        }
        else
        {
            grid_size_x = Random.Range(min_ground_size, max_ground_size);
            grid_size_z = Random.Range(min_ground_size, max_ground_size);
        }
        if (fixed_entrance_size)
        {
            entrance_size_x = x_entrance_size;
            entrance_size_z = z_entrance_size;
        }
        else
        {
            entrance_size_x = Random.Range(min_entrance_size, max_entrance_size);
            entrance_size_z = Random.Range(min_entrance_size, max_entrance_size);
        }

        ////////// Ground Area //////////
        float ground_area_position_y = 0f;

        GameObject entrance_area = this.transform.Find("ground_entrance").gameObject;
        Vector3 entrance_size = new Vector3(entrance_size_x, 0.5f, entrance_size_z);
        entrance_area.transform.localScale = entrance_size;
        entrance_area.GetComponent<ground_scaling>().scale_Texture(entrance_size);
        Vector3 entrance_position = this.transform.position + new Vector3((grid_size_x / 2.0f - entrance_size[0] / 2.0f), ground_area_position_y, (-grid_size_z / 2.0f + entrance_size[2] / 2.0f));
        entrance_area.transform.position = entrance_position;

        GameObject durablefoods_area = this.transform.Find("ground_durable_food").gameObject;
        Vector3 durablefoods_size = new Vector3(grid_size_x - entrance_size[0], 0.5f, grid_size_z - entrance_size[2]);
        durablefoods_area.transform.localScale = durablefoods_size;
        durablefoods_area.GetComponent<ground_scaling>().scale_Texture(durablefoods_size);
        durablefoods_area.transform.position = this.transform.position + new Vector3((durablefoods_size[0] / 2.0f - grid_size_x / 2.0f), ground_area_position_y, (grid_size_z / 2.0f - durablefoods_size[2] / 2.0f));

        GameObject fruits_area = this.transform.Find("ground_fruits").gameObject;
        Vector3 fruits_size = new Vector3(entrance_size[0], 0.5f, grid_size_z - entrance_size[2]);
        fruits_area.transform.localScale = fruits_size;
        fruits_area.GetComponent<ground_scaling>().scale_Texture(fruits_size);
        fruits_area.transform.position = this.transform.position + new Vector3((grid_size_x / 2.0f - fruits_size[0] / 2.0f), ground_area_position_y, (grid_size_z / 2.0f - fruits_size[2] / 2.0f));

        GameObject beverages_area = this.transform.Find("ground_beverages").gameObject;
        Vector3 beverages_size = new Vector3(grid_size_x - entrance_size[0], 0.5f, entrance_size[2]);
        beverages_area.transform.localScale = beverages_size;
        beverages_area.GetComponent<ground_scaling>().scale_Texture(beverages_size);
        beverages_area.transform.position = this.transform.position + new Vector3((beverages_size[0] / 2.0f - grid_size_x / 2.0f), ground_area_position_y, (-grid_size_z / 2.0f + beverages_size[2] / 2.0f));

        GetComponent<SetupSupermarketInterior>().setup_Supermarket_Interior(grid_size_x, grid_size_z,entrance_size,durablefoods_size,fruits_size,beverages_size, entrance_position);
    }

    public void check_Input()
    {
        if (fixed_ground_size)
        {
            if(fixed_entrance_size)
            {
                if(x_entrance_size > x_ground_size / 2 || z_entrance_size > z_ground_size / 2)
                {
                    Debug.LogError("Entrance size should not exceed halve the ground size.");
                    Application.Quit();
                }
            }
            else
            {
                if (min_entrance_size > max_entrance_size)
                {
                    Debug.LogError("Maximum entrance size should be bigger or the same as minimum entrance size.");
                    Application.Quit();
                }
                else if (max_entrance_size > x_ground_size / 2 || max_entrance_size > z_entrance_size / 2)
                {
                    Debug.LogError("Maximum entrance size should not exceed halve theground size.");
                    Application.Quit();
                }
            }
        }
        else
        {
            if (min_ground_size > max_ground_size)
            {
                Debug.LogError("Minimum ground size should not be bigger or the same as maximum ground size.");
                Application.Quit();
            }

            if (fixed_entrance_size)
            {
                if (x_entrance_size > min_ground_size / 2 || z_entrance_size > min_ground_size / 2)
                {
                    Debug.LogError("Entrance size should not exceed halve the ground size.");
                    Application.Quit();
                }
            }
            else
            { 
                if (min_entrance_size > max_entrance_size)
                {
                    Debug.LogError("Maximum entrance size should be bigger or the same as minimum entrance size.");
                    Application.Quit();
                }
                else if (max_entrance_size > min_ground_size / 2)
                {
                    Debug.LogError("Entrance size should not exceed halve the minimum ground size.");
                    Application.Quit();
                }
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
        //setup_Supermarket();
    }
}
