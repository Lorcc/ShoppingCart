using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSupermarketInterior : MonoBehaviour
{
    [SerializeField] private bool random_shelve_orientation = true;
    [SerializeField] private bool horizontal_shelve_orientation_durablefood_area = true;
    [SerializeField] private bool horizontal_shelve_orientation_fruits_area = true;
    [SerializeField] private bool horizontal_shelve_orientation_beverages_area = true;

    [SerializeField] private int number_of_items_to_purchase = 1;
    [SerializeField] private GameObject[] shelve_wall_tile;
    [SerializeField] private GameObject[] available_shelves;

    [SerializeField] private int number_of_static_obstacles = 1;
    [SerializeField] private GameObject[] available_static_obstacles;

    private List<GameObject> shelve_tiles = new List<GameObject>();
    private List<GameObject> static_obstacles = new List<GameObject>();
    private List<GameObject> waypoint_objects = new List<GameObject>();

    enum Section { Fruit, Durable, Drinks }

    private bool[,] horizontal_shelve = new bool[3, 3]{
                                        {false, false, false},
                                        {true, false, false},
                                        {true, false, false}
                                        };
    private bool[,] vertical_shelve = new bool[3, 3]{
                                        {false,true,true},
                                        {false,false,false},
                                        {false,false,false}
                                        };

    public class Area
    {
        public Vector3Int area_size;
        public string orientation;
        public List<GameObject> ground_tiles = new List<GameObject>();

        public Area(Vector3Int size)
        {
            area_size = size;
            orientation = calculate_shelve_orientation(area_size[0], area_size[2]);
        }
        public Area(Vector3Int size, string orientation)
        {
            area_size = size;
            this.orientation = orientation;
        }

        public string calculate_shelve_orientation(int area_horizontal_length, int area_vertical_length)
        {
            List<string> possible_orientations = new List<string>();

            if (area_horizontal_length % 3 == 1)
            {
                possible_orientations.Add("horizontal");
            }
            if (area_vertical_length % 3 == 1)
            {
                possible_orientations.Add("vertical");
            }
            if (possible_orientations.Count == 0)
            {
                int temp_random = Random.Range(0, 2);
                if (temp_random == 0)
                {
                    possible_orientations.Add("vertical");
                }
                else
                {
                    possible_orientations.Add("horizontal");
                }

            }
            int random_index = Random.Range(0, possible_orientations.Count);
            return possible_orientations[random_index];
        }
    }

    public void setup_Supermarket_Interior( int grid_size_x, 
                                            int grid_size_z, 
                                            Vector3 entrance_size, 
                                            Vector3 durablefoods_size, 
                                            Vector3 fruits_size, 
                                            Vector3 beverages_size)
    {
        //////////Shelve Orientation//////////
        Area durablefood_area;
        Area fruit_area;
        Area beverage_area;

        if (random_shelve_orientation)
        {
            durablefood_area = new Area(new Vector3Int((int)durablefoods_size[0],(int)durablefoods_size[1], (int)durablefoods_size[2]));
            fruit_area = new Area(new Vector3Int((int)fruits_size[0], (int)fruits_size[1], (int)fruits_size[2]));
            beverage_area = new Area(new Vector3Int((int)beverages_size[0], (int)beverages_size[1], (int)beverages_size[2]));
        }
        else
        {
            if(horizontal_shelve_orientation_durablefood_area) 
                durablefood_area = new Area(new Vector3Int((int)durablefoods_size[0], (int)durablefoods_size[1], (int)durablefoods_size[2]),"horizontal");
            else
                durablefood_area = new Area(new Vector3Int((int)durablefoods_size[0], (int)durablefoods_size[1], (int)durablefoods_size[2]), "vertical");
            if(horizontal_shelve_orientation_fruits_area)
                fruit_area = new Area(new Vector3Int((int)fruits_size[0], (int)fruits_size[1], (int)fruits_size[2]), "horizontal");
            else
                fruit_area = new Area(new Vector3Int((int)fruits_size[0], (int)fruits_size[1], (int)fruits_size[2]), "vertical");
            if(horizontal_shelve_orientation_beverages_area)
                beverage_area = new Area(new Vector3Int((int)beverages_size[0], (int)beverages_size[1], (int)beverages_size[2]),"horizontal");
            else
                beverage_area = new Area(new Vector3Int((int)beverages_size[0], (int)beverages_size[1], (int)beverages_size[2]), "vertical");
        }


        //////////2D Grid Calculation For Shelves//////////
        bool[,] occupied_grids = new bool[grid_size_z, grid_size_x];

        //Grid which needs to be blocked and checked if it's free
        bool[,] to_filled_grid = new bool[horizontal_shelve.GetLength(0), horizontal_shelve.GetLength(1)];

        //Entrance grid this is false but we just check later for false to save a conversion to true
        bool[,] occupied_entrance = new bool[(int)entrance_size[0], (int)entrance_size[2]];


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
