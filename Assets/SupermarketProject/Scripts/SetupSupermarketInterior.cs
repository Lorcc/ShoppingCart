using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSupermarketInterior : MonoBehaviour
{
    private const int CHECKOUT_SIZE = 7;

    [SerializeField] private bool random_shelve_orientation = true;
    [SerializeField] private bool horizontal_shelve_orientation_durablefood_area = true;
    [SerializeField] private bool horizontal_shelve_orientation_fruits_area = true;
    [SerializeField] private bool horizontal_shelve_orientation_beverages_area = true;

    [SerializeField] [Tooltip(" ")] [Range(1,15)] private int number_of_items_to_purchase = 1;

    [SerializeField] [Range(0, 15)] private int number_of_static_obstacles = 1;
    [SerializeField] private GameObject[] available_static_obstacles;

    [SerializeField] private GameObject[] shelve_wall_tile;
    [SerializeField] private GameObject[] available_shelves;

    [SerializeField] private GameObject agent;
    [SerializeField] private GameObject goal;
    [SerializeField] private GameObject waypoint;

    SetupEntrance setup_entrance;

    private List<GameObject> shelve_tiles = new List<GameObject>();
    private List<GameObject> static_obstacles = new List<GameObject>();
    private List<GameObject> waypoint_objects = new List<GameObject>();

    // List with the positions for A*
    private List<Vector2> goal_localpositions_2d = new List<Vector2>();
    private List<Vector2Int> goal_map_position_2d = new List<Vector2Int>();
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
                                            Vector3 beverages_size,
                                            Vector3 entrance_position)
    {
        ///// Reset Previous Values /////
        reset_Object_Lists();

        ////////// Shelve Orientation //////////
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
            if (horizontal_shelve_orientation_durablefood_area) 
                durablefood_area = new Area(new Vector3Int((int)durablefoods_size[0], (int)durablefoods_size[1], (int)durablefoods_size[2]),"horizontal");
            else
                durablefood_area = new Area(new Vector3Int((int)durablefoods_size[0], (int)durablefoods_size[1], (int)durablefoods_size[2]), "vertical");
            if (horizontal_shelve_orientation_fruits_area)
                fruit_area = new Area(new Vector3Int((int)fruits_size[0], (int)fruits_size[1], (int)fruits_size[2]), "horizontal");
            else
                fruit_area = new Area(new Vector3Int((int)fruits_size[0], (int)fruits_size[1], (int)fruits_size[2]), "vertical");
            if (horizontal_shelve_orientation_beverages_area)
                beverage_area = new Area(new Vector3Int((int)beverages_size[0], (int)beverages_size[1], (int)beverages_size[2]),"horizontal");
            else
                beverage_area = new Area(new Vector3Int((int)beverages_size[0], (int)beverages_size[1], (int)beverages_size[2]), "vertical");
        }


        ////////// 2D Grid Calculation For Shelves //////////
        float object_position_y = 0.25f;
        //Grid which will have all the inner shelve positions, could be seen as a top view map
        //true means no shelve and false means there should be a shelve
        bool[,] occupied_grid = new bool[grid_size_z, grid_size_x];

        //Grid which needs to be blocked and checked if it's free
        bool[,] to_filled_grid = new bool[horizontal_shelve.GetLength(1), horizontal_shelve.GetLength(0)];

        //Split whole grid up into extra parts and initialize with false
        bool[,] occupied_entrance = new bool[(int)entrance_size[2], (int)entrance_size[0]];
        bool[,] occupied_durablefood_grid = new bool[(int)durablefoods_size[2], (int)durablefoods_size[0]];
        bool[,] occupied_fruits_grid = new bool[(int)fruits_size[2], (int)fruits_size[0]];
        bool[,] occupied_beverages_grid = new bool[(int)beverages_size[2], (int)beverages_size[0]];

        ///// Durablefood Area /////
        // Take out at least one field around the edge of the field and 2 to the north and west
        for (int grid_hor = 0; grid_hor < occupied_durablefood_grid.GetLength(0); grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < occupied_durablefood_grid.GetLength(1); grid_vert++)
            {
                if (grid_hor == 0 || grid_hor == 1 || grid_hor == occupied_durablefood_grid.GetLength(0) - 1)
                    occupied_durablefood_grid[grid_hor, grid_vert] = true;
                if (grid_vert == 0 || grid_vert == 1 || grid_vert == occupied_durablefood_grid.GetLength(1) - 1)
                    occupied_durablefood_grid[grid_hor, grid_vert] = true;
                if (durablefood_area.orientation == "horizontal")
                {
                    to_filled_grid = horizontal_shelve;
                    if (occupied_durablefood_grid[grid_hor, grid_vert] == false)
                    {

                        if ((occupied_durablefood_grid.GetLength(0) % 3 == 1) && (grid_hor == occupied_durablefood_grid.GetLength(0) - 2))
                        {
                            // empty so that there is false in this row without using the horizontal_shelve which would break the thing
                        }
                        else
                        {
                            for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                            {
                                for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                                {
                                    if (to_filled_grid[z_local, x_local] == true)
                                    {
                                        occupied_durablefood_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                    }
                                }
                            }
                        }
                        // create walkthroughs inbetween shelves
                        if (grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 || grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 + 1)
                        {
                            occupied_durablefood_grid[grid_hor, grid_vert] = true;
                        }
                    }
                }
                else if (durablefood_area.orientation == "vertical")
                {
                    to_filled_grid = vertical_shelve;
                    if (occupied_durablefood_grid[grid_hor, grid_vert] == false)
                    {
                        if ((occupied_durablefood_grid.GetLength(1) % 3 == 1) && (grid_vert == occupied_durablefood_grid.GetLength(1) - 2))
                        {
                            //pass
                        }
                        else
                        {
                            for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                            {
                                for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                                {
                                    if (to_filled_grid[z_local, x_local] == true)
                                    {
                                        occupied_durablefood_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                    }
                                }
                            }
                        }
                        // create walkthroughs inbetween shelves
                        if (grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2 || grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2 + 1)
                        {
                            occupied_durablefood_grid[grid_hor, grid_vert] = true;
                        }
                    }
                }
            }
        }
        
        ///// Fruits Area /////
        // Take out at least one field around the edge of the field and 2 to the north, east and south
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
                        occupied_fruits_grid[grid_hor, grid_vert] = true;
                    }
                }
            }
        }

        ///// Beverages Area /////
        // Take out at least one field around the edge of the field and 2 to the north and east
        for (int grid_hor = 0; grid_hor < occupied_beverages_grid.GetLength(0); grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < occupied_beverages_grid.GetLength(1); grid_vert++)
            {
                if (grid_hor == 0 || grid_hor == occupied_beverages_grid.GetLength(0) - 2 || grid_hor == occupied_beverages_grid.GetLength(0) - 1)
                    occupied_beverages_grid[grid_hor, grid_vert] = true;
                if (grid_vert == 0 ||grid_vert == 1|| occupied_beverages_grid.GetLength(1) - CHECKOUT_SIZE <= grid_vert)
                    occupied_beverages_grid[grid_hor, grid_vert] = true;
                if (beverage_area.orientation == "horizontal")
                {
                    if (occupied_beverages_grid[grid_hor, grid_vert] == false)
                    {
                        to_filled_grid = horizontal_shelve;
                        for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                        {
                            for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                            {
                                if (to_filled_grid[z_local, x_local] == true)
                                {
                                    occupied_beverages_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                    // create walkthroughs inbetween shelves at the same place as durablefood one
                    if (grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 || grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 + 1)
                    {
                        occupied_beverages_grid[grid_hor, grid_vert] = true;
                    }
                }

                else if (beverage_area.orientation == "vertical")
                {
                    if (occupied_beverages_grid[grid_hor, grid_vert] == false)
                    {
                        to_filled_grid = vertical_shelve;
                        for (int z_local = 0; z_local < to_filled_grid.GetLength(0); z_local++)
                        {
                            for (int x_local = 0; x_local < to_filled_grid.GetLength(1); x_local++)
                            {
                                if (to_filled_grid[z_local, x_local] == true)
                                {
                                    occupied_beverages_grid[grid_hor + z_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                }
            }
        }


        ////////// Merge Area Grids into Main Occupied Grid //////////
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {

                //take out occupied durablefood fields
                if (grid_hor == 0 && grid_vert == 0)
                {
                    for (int z_local = 0; z_local < occupied_durablefood_grid.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_durablefood_grid.GetLength(1); x_local++)
                        {
                            occupied_grid[grid_hor + z_local, grid_vert + x_local] = occupied_durablefood_grid[z_local, x_local];
                        }
                    }
                }
                //take out occupied fruit fields
                if (grid_hor == 0 && grid_vert == grid_size_x - fruits_size[0])
                {
                    for (int z_local = 0; z_local < occupied_fruits_grid.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_fruits_grid.GetLength(1); x_local++)
                        {
                            occupied_grid[grid_hor + z_local, grid_vert + x_local] = occupied_fruits_grid[z_local, x_local];
                        }
                    }
                }
                //take out occupied beverages fields
                if (grid_hor == grid_size_z - beverages_size[2] && grid_vert == 0)
                {
                    for (int z_local = 0; z_local < occupied_beverages_grid.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_beverages_grid.GetLength(1); x_local++)
                        {
                            occupied_grid[grid_hor + z_local, grid_vert + x_local] = occupied_beverages_grid[z_local, x_local];
                        }
                    }
                }
                //take out entrance fields
                if (grid_hor == grid_size_z - entrance_size[2] && grid_vert == grid_size_x - entrance_size[0])
                {
                    for (int z_local = 0; z_local < occupied_entrance.GetLength(0); z_local++)
                    {
                        for (int x_local = 0; x_local < occupied_entrance.GetLength(1); x_local++)
                        {
                            occupied_grid[grid_hor + z_local, grid_vert + x_local] = true;
                        }
                    }
                }
            }
        }


        ////////// Visualisation Bool Array //////////
        string text = "";
        /*for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                if (occupied_grid[grid_hor, grid_vert] == false)
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
        Debug.Log("Gridsize_X: " + grid_size_x + "Gridsize_Z: " + grid_size_z);*/


        ////////// Spawning Shelves //////////
        ///// Calculate Number of Inner Shelves /////
        int number_of_shelves = 0;
        for (int i = 0; i < grid_size_z; i++)
        {
            for (int k = 0; k < grid_size_x; k++)
            {
                if (occupied_grid[i, k] == false) number_of_shelves++;
            }
        }
        int temp_number_of_shelves = number_of_shelves;

        ///// Spawn Inner Shelves /////
        int temp_number_of_items = number_of_items_to_purchase;
        for(int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                if (occupied_grid[grid_hor, grid_vert] == false)
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

                    //Debug.Log(object_position + " " + grid_hor + " " + grid_vert);
                    Vector3 temp_position;
                    Vector2 temp_goal_position;

                    //Durablefood Area
                    if (grid_hor < durablefoods_size[2] && grid_vert < durablefoods_size[0])
                    {
                        if (durablefood_area.orientation == "horizontal")
                        {
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Horizontal(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                        else
                        {
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Vertical(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                    }
                    //Fruit Area 
                    else if (grid_hor < fruits_size[2] && grid_vert >= grid_size_x - fruits_size[0])
                    {
                        if (fruit_area.orientation == "horizontal")
                        {
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Horizontal(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                        else
                        {
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Vertical(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                    }
                    //Beverages Area 
                    else if (grid_hor >= grid_size_z - beverages_size[2] && grid_vert < beverages_size[0])
                    {
                        if (beverage_area.orientation == "horizontal")
                        {
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Horizontal(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                        else
                        {
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_Goal_Position_Vertical(object_position, temp_position);
                                goal_localpositions_2d.Add(temp_goal_position);
                                goal_map_position_2d.Add(new Vector2Int(grid_hor, grid_vert));
                            }
                        }
                    }
                }
            }
        }

        ///// Spawn Outer Shelves /////
        //TODO add these to the regular spawning so that there is a posibility to spawn purchaseable items there
        //Generate northern outer shelves
        for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
        {
            float offset_x = 0.5f;
            float offset_z = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + offset_x), object_position_y, (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, -90, 0);
            GameObject shelve = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate southern outer shelves
        for (int grid_vert = 0; grid_vert < grid_size_x - entrance_size[0]; grid_vert++)
        {
            float offset_x = 0.5f;
            float offset_z = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + offset_x), object_position_y, (-grid_size_z / 2.0f) - offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 90, 0);
            GameObject shelve = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate western outer shelves
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            float offset_x = 0.25f;
            float offset_z = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3((-(grid_size_x / 2.0f) - offset_x), object_position_y, grid_hor - (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 180, 0);
            GameObject shelve = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate eastern outer shelves
        for (int grid_hor = (int)entrance_size[2]; grid_hor < grid_size_z; grid_hor++)
        {
            float offset_x = 0.25f;
            float offset_z = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((grid_size_x / 2.0f) + offset_x), object_position_y, grid_hor - (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }


        ////////// Spawn Entrance Fence //////////
        setup_entrance.setup_Entrance(grid_size_x, grid_size_z, entrance_size, entrance_position);


        ////////// Spawn Static Obstacles //////////
        //Create a grid where false values are positions where obstacles can spawn
        //for that invert the occupied_grid array and also take out the entrance and checkout fields
        bool[,] occupied_grid_static_obstacles = new bool[grid_size_z, grid_size_x];
        int number_of_occupied_checkout_entrance_fields = 0;
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                if(grid_vert >= grid_size_x - entrance_size[0] - CHECKOUT_SIZE && grid_hor >= grid_size_z - entrance_size[2] - 2)
                {
                    occupied_grid_static_obstacles[grid_hor, grid_vert] = true;
                    number_of_occupied_checkout_entrance_fields++;
                }
                else
                {
                    occupied_grid_static_obstacles[grid_hor, grid_vert] = !occupied_grid[grid_hor, grid_vert];
                }
            }
        }

        int number_of_possible_obstacle_fields = grid_size_x * grid_size_z - number_of_shelves - number_of_occupied_checkout_entrance_fields;
        int temp_number_of_obstacles = number_of_static_obstacles;
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
            {
                //Guard Clause
                if (temp_number_of_obstacles <= 0)
                {
                    break;
                }
                if (occupied_grid_static_obstacles[grid_hor, grid_vert] == false)
                {
                    float random_number = Random.Range(0.0f, 1.0f);
                    if (random_number < ((float)temp_number_of_obstacles / (float)number_of_possible_obstacle_fields))
                    {
                        float object_offset = 0.5f;
                        int random_item = Random.Range(0, available_static_obstacles.Length);
                        int random_rotation = Random.Range(0, 2);

                        Vector3 object_rotation_vertical = new Vector3(0, 0, 0);
                        Vector3 object_rotation_horizontal = new Vector3(0, 90, 0);
                        if (random_rotation == 1)
                        {
                            object_rotation_vertical.y = 180;
                            object_rotation_horizontal.y = 270;
                        }

                        Quaternion object_rotation = Quaternion.Euler(object_rotation_vertical);
                        Vector3 object_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + object_offset), object_position_y, (grid_size_z / 2.0f) - grid_hor - object_offset);

                        //Durablefood Area
                        if (grid_hor < durablefoods_size[2] && grid_vert < durablefoods_size[0])
                        {
                            if (durablefood_area.orientation == "horizontal")
                            {
                                object_rotation = Quaternion.Euler(object_rotation_horizontal);
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                            else
                            {
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                        }
                        //Fruits Area
                        else if (grid_hor < fruits_size[2] && grid_vert >= grid_size_x - fruits_size[0])
                        {
                            if (fruit_area.orientation == "horizontal")
                            {
                                object_rotation = Quaternion.Euler(object_rotation_horizontal);
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                            else
                            {
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                        }
                        //Beverages Area 
                        else if (grid_hor >= grid_size_z - beverages_size[2] && grid_vert < beverages_size[0])
                        {
                            if (beverage_area.orientation == "horizontal")
                            {
                                object_rotation = Quaternion.Euler(object_rotation_horizontal);
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                            else
                            {
                                GameObject new_object = Instantiate(available_static_obstacles[random_item], object_position, object_rotation, this.transform);
                                static_obstacles.Add(new_object);
                            }
                        }
                        temp_number_of_obstacles--;
                    }
                    number_of_possible_obstacle_fields--;
                }
            }
        }

        ////////// Goal Position //////////
        float goal_position_y = 0.75f;
        Vector3 goal_spawn_pos = new Vector3(goal_localpositions_2d[0].x, this.transform.position.y + goal_position_y, goal_localpositions_2d[0].y);
        goal.GetComponent<Goal>().reposition(goal_spawn_pos);
        Debug.Log(goal_spawn_pos);
        Debug.Log(goal_map_position_2d[0]);
    }


    public void reset_Object_Lists()
    {
        //***Empty Goal List***//
        goal_localpositions_2d.Clear();

        //Clear shelve tiles
        foreach (GameObject shelve in shelve_tiles)
        {
            Destroy(shelve);
        }
        //Clear obstacle objects
        foreach (GameObject obstacle in static_obstacles)
        {
            Destroy(obstacle);
        }
        //Clear waypoints
        foreach (GameObject waypoint in waypoint_objects)
        {
            Destroy(waypoint);
        }
    }

    /// <summary>
    /// Calculate position of the goal if shelve is horizontal
    /// </summary>
    /// <param name="shelve_position"></param>
    /// <param name="p_item_position"></param>
    /// <returns></returns>
    public Vector2 calculate_Goal_Position_Horizontal(Vector3 shelve_position, Vector3 p_item_position)
    {
        Vector2 goal_pos = new Vector2();
        goal_pos.x = shelve_position.x;

        if (p_item_position.x < 0)
        {
            goal_pos.y = shelve_position.z + 1;
        }
        else
        {
            goal_pos.y = shelve_position.z - 1;
        }
        return goal_pos;
    }


    /// <summary>
    /// Calculate position of the goal if shelve is vertical
    /// </summary>
    /// <param name="shelve_position"></param>
    /// <param name="p_item_position"></param>
    /// <returns></returns>
    public Vector2 calculate_Goal_Position_Vertical(Vector3 shelve_position, Vector3 p_item_position)
    {
        Vector2 goal_pos = new Vector2();
        goal_pos.y = shelve_position.z;

        if (p_item_position.x < 0)
        {
            goal_pos.x = shelve_position.x - 1;
        }
        else
        {
            goal_pos.x = shelve_position.x + 1;
        }
        return goal_pos;
    }

    private void Awake()
    {
        // Gets entrance script
        setup_entrance = this.transform.GetComponent<SetupEntrance>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }
}
