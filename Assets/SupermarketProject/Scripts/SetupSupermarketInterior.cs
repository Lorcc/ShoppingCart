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

    [SerializeField] private GameObject agent;
    [SerializeField] private GameObject goal;
    [SerializeField] private GameObject waypoint;

    private List<GameObject> shelve_tiles = new List<GameObject>();
    private List<GameObject> static_obstacles = new List<GameObject>();
    private List<GameObject> waypoint_objects = new List<GameObject>();

    // List with the positions for A*
    private List<Vector2> goal_positions_2d = new List<Vector2>();
    private Vector2 agent_starting_position = new Vector2();
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
        float object_position_y = 0.25f;
        bool[,] occupied_grids = new bool[grid_size_z, grid_size_x];

        //Grid which needs to be blocked and checked if it's free
        bool[,] to_filled_grid = new bool[horizontal_shelve.GetLength(1), horizontal_shelve.GetLength(0)];

        //Split whole grid up into extra parts and initialize with false
        bool[,] occupied_entrance = new bool[(int)entrance_size[2], (int)entrance_size[0]];
        bool[,] occupied_durablefood_grid = new bool[(int)durablefoods_size[2], (int)durablefoods_size[0]];
        bool[,] occupied_fruits_grid = new bool[(int)fruits_size[2], (int)fruits_size[0]];
        bool[,] occupied_beverages_grid = new bool[(int)beverages_size[2], (int)beverages_size[0]];

        // Take out at least one field around the edge of the field and 2 to the north, east and west
        for (int grid_hor = 0; grid_hor < occupied_fruits_grid.GetLength(0); grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < occupied_fruits_grid.GetLength(1); grid_vert++)
            {
                if (grid_hor == 0 || grid_hor == 1 || grid_hor == occupied_fruits_grid.GetLength(0) - 2 || grid_hor == occupied_fruits_grid.GetLength(0) - 1)
                    occupied_fruits_grid[grid_hor, grid_vert] = true;
                if (grid_vert == 0 || grid_vert == occupied_fruits_grid.GetLength(1) - 2 || grid_vert == occupied_fruits_grid.GetLength(1) - 1)
                    occupied_fruits_grid[grid_hor, grid_vert] = true;
                if (fruit_area.orientation == "horizontal")
                {
                    if (occupied_fruits_grid[grid_hor, grid_vert] == false)
                    {
                        to_filled_grid = horizontal_shelve;
                        for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                        {
                            for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                            {
                                if (to_filled_grid[z_local, x_local] == true)
                                {
                                    //inverted because of array notation y --> 0,1,2
                                    //  0 1 2 y
                                    //0 0 0 0
                                    //1 1 0 0
                                    //2 1 0 0
                                    //x
                                    occupied_fruits_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                }

                else if (fruit_area.orientation == "vertical")
                {
                    if (occupied_fruits_grid[grid_hor, grid_vert] == false)
                    {
                        to_filled_grid = vertical_shelve;
                        for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                        {
                            for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                            {
                                if (to_filled_grid[z_local, x_local] == true)
                                {
                                    occupied_fruits_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                    // create walkthroughs inbetween shelves at the same place as durablefood one
                    if (grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2  || grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2 + 1 )
                    {
                        Debug.Log("Moin");
                        occupied_fruits_grid[grid_hor, grid_vert] = true;
                    }
                }
            }
        }

        // Take out fields
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {

                //take out occupied durablefood fields
                if (grid_hor == 0 && grid_vert == 0)
                {
                    for (int x_local = 0; x_local < occupied_durablefood_grid.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_durablefood_grid.GetLength(1); y_local++)
                        {
                            occupied_grids[grid_hor + x_local, grid_vert + y_local] = occupied_durablefood_grid[x_local, y_local];
                        }
                    }
                }
                //take out occupied beverages fields
                /*if (grid_hor == 0 && grid_vert == grid_size_x - beverages_size[2])
                {
                    for (int x_local = 0; x_local < occupied_beverages_grid.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_beverages_grid.GetLength(1); y_local++)
                        {
                            occupied_grids[grid_hor + x_local, grid_vert + y_local] = occupied_beverages_grid[x_local, y_local];
                        }
                    }
                }*/
                //take out occupied fruit fields
                //this is the beverage position
                /*if (grid_hor == grid_size_z - fruits_size[2] && grid_vert == 0)
                {
                    for (int z_local = 0; z_local < occupied_fruits_grid.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_fruits_grid.GetLength(1); x_local++)
                        {
                            occupied_grids[grid_hor + z_local, grid_vert + x_local] = occupied_fruits_grid[z_local, x_local];
                        }
                    }
                }*/
                //take out occupied fruit fields
                if (grid_hor == 0 && grid_vert == grid_size_x - fruits_size[0])
                {
                    for (int z_local = 0; z_local < occupied_fruits_grid.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_fruits_grid.GetLength(1); x_local++)
                        {
                            occupied_grids[grid_hor + z_local, grid_vert + x_local] = occupied_fruits_grid[z_local, x_local];
                        }
                    }
                }
                //take out entrance fields
                /*if (grid_hor == grid_size_z - entrance_size[0] && grid_vert == grid_size_x - entrance_size[2])
                {
                    for (int x_local = 0; x_local < occupied_entrance.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_entrance.GetLength(1); y_local++)
                        {
                            occupied_grids[grid_hor + x_local, grid_vert + y_local] = true;
                        }
                    }
                }*/
            }
        }

        ////////// Visualisation Bool Array //////////
        string text = "";
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                if (occupied_grids[grid_hor, grid_vert] == false)
                {
                    text += "0 ";
                }
                else
                {
                    text += "1 ";
                }
            }
            Debug.Log(text + "\n");
            text = "";
        }
        Debug.Log("Gridsize_X: " + grid_size_x + "Gridsize_Z: " + grid_size_z);


        //get number of spawned shelves in the inner Part
        int number_of_shelves = 0;
        for (int i = 0; i < grid_size_z; i++)
        {
            for (int k = 0; k < grid_size_x; k++)
            {
                if (occupied_grids[i, k] == false) number_of_shelves++;
            }
        }
        int temp_number_of_shelves = number_of_shelves;

        //***Spawn Shelves***//
        int temp_number_of_items = number_of_items_to_purchase;
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                if (occupied_grids[grid_hor, grid_vert] == false)
                {
                    bool spawn_food_to_purchase = false;
                    if (temp_number_of_items > 0)
                    {
                        float random_number = Random.Range(0.0f, 1.0f);

                        if (random_number < ((float)temp_number_of_items / (float)temp_number_of_shelves))
                        {
                            spawn_food_to_purchase = true;
                            temp_number_of_items--;
                        }
                        temp_number_of_shelves--;
                    }
                    //Debug.Log(grid_hor + " " + grid_vert);
                    float object_offset = 0.5f;
                    Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
                    Vector3 object_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + object_offset), object_position_y, (grid_size_z / 2.0f) - grid_hor - object_offset);
                    //Vector2 real_pos = parse_localposition_to_map(new Vector2(object_position.x, object_position.z), grid_size_x, grid_size_y);

                    //occupied_grids_spawn[(int)real_pos.x, (int)real_pos.y] = false;

                    //Debug.Log(object_position + " " + grid_hor + " " + grid_vert);
                    Vector3 temp_position = new Vector3();
                    Vector2 temp_goal_position = new Vector2();

                    //durablefood area
                    if (grid_hor < durablefood_area.area_size[0] && grid_vert < durablefood_area.area_size[2])
                    {
                        if (durablefood_area.orientation == "horizontal")
                        {
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                //temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                //temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                    }
                    //fruit area now beverage
                    //else if (grid_hor >= grid_size_x - fruit_area.area_size[0] && grid_vert < fruit_area.area_size[2])
                    else if (grid_hor < fruits_size[2] && grid_vert >= grid_size_x - fruits_size[0])
                    {
                        if (fruit_area.orientation == "horizontal")
                        {
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                //temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                //temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                    }
                    // beverages area now Fruits area
                    else if (grid_hor >= grid_size_z - beverages_size[2] && grid_vert < beverages_size[0])
                    {
                        if (beverage_area.orientation == "horizontal")
                        {
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                //temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            //new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                //temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                //temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                //goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                    }
                }
            }
        }
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