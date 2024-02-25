using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using System.IO;

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
    [HideInInspector] public List<Vector3> current_shortest_path = new List<Vector3>();
    
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

    // A* Class
    class GridTile
    {
        public int X { get; set; }
        public int Z { get; set; }
        public int Cost { get; set; }
        public float Distance { get; set; }
        public float CostDistance => Cost + Distance;
        public GridTile Parent { get; set; }
        //The distance is essentially the estimated distance, ignoring walls to our target. 
        //So how many tiles left and right, up and down, ignoring walls, to get there.
        public void set_Distance(int targetX, int targetZ)
        {
            //this.Distance = Mathf.Abs(targetX - X) + Mathf.Abs(targetZ - Z);
            this.Distance = Mathf.Sqrt(Mathf.Pow(targetX - X, 2) + Mathf.Pow(targetZ - Z, 2));
        }
    }

    private static List<GridTile> GetWalkableTiles(bool[,] occupiedGrids, GridTile currentTile, GridTile targetTile, int grid_size_x, int grid_size_z)
    {
        var possibleTiles = new List<GridTile>()
        {
            new GridTile { X = currentTile.X, Z = currentTile.Z - 1, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X, Z = currentTile.Z + 1, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X - 1, Z = currentTile.Z, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X + 1, Z = currentTile.Z, Parent = currentTile, Cost = currentTile.Cost + 1 },
        };

        possibleTiles.ForEach(tile => tile.set_Distance(targetTile.X, targetTile.Z));
        var maxX = grid_size_x - 1;
        var maxZ = grid_size_z - 1;
        for(int i = 0; i < possibleTiles.Count; i++)
        {
            if (possibleTiles[i].X < 0 || possibleTiles[i].X > maxX || possibleTiles[i].Z < 0 || possibleTiles[i].Z > maxZ)
            {
                possibleTiles.RemoveAt(i);
            }
        }

        return possibleTiles
            .Where(tile => tile.X >= 0 && tile.X <= maxX)
            .Where(tile => tile.Z >= 0 && tile.Z <= maxZ)
            .Where(tile => occupiedGrids[tile.Z, tile.X] == true || targetTile.X == tile.X && targetTile.Z == tile.Z)
            .ToList();
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
                if (grid_hor == 0 || grid_hor == 1 || occupied_fruits_grid.GetLength(0) - 4 <= grid_hor)
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

        int grid_position_x = 0;
        int grid_position_z = 0;
        //take out occupied durablefood fields
        for (int z_local = 0; z_local < occupied_durablefood_grid.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_durablefood_grid.GetLength(1); x_local++)
            {
                occupied_grid[grid_position_z + z_local, grid_position_x + x_local] = occupied_durablefood_grid[z_local, x_local];
            }
        }

        //take out occupied fruit fields
        grid_position_x = grid_size_x - (int)fruits_size[0];
        for (int z_local = 0; z_local < occupied_fruits_grid.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_fruits_grid.GetLength(1); x_local++)
            {
                occupied_grid[grid_position_z + z_local, grid_position_x + x_local] = occupied_fruits_grid[z_local, x_local];
            }
        }

        //take out occupied beverages fields
        grid_position_x = 0;
        grid_position_z = grid_size_z - (int)beverages_size[2];
        for (int z_local = 0; z_local < occupied_beverages_grid.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_beverages_grid.GetLength(1); x_local++)
            {
                occupied_grid[grid_position_z + z_local, grid_position_x + x_local] = occupied_beverages_grid[z_local, x_local];
            }
        }

        //take out entrance fields
        grid_position_x = grid_size_x - (int)entrance_size[0];
        grid_position_z = grid_size_z - (int)entrance_size[2];
        for (int z_local = 0; z_local < occupied_entrance.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_entrance.GetLength(1); x_local++)
            {
                occupied_grid[grid_position_z + z_local, grid_position_x + x_local] = true;
            }
        }


        ////////// Visualisation Bool Array //////////
        /*string text = "";
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
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
        Debug.Log("Gridsize_X: " + grid_size_x + " Gridsize_Z: " + grid_size_z);*/


        ////////// Spawning Shelves //////////
        // Calculate Number of Inner Shelves //
        int number_of_shelves = 0;
        for (int i = 0; i < grid_size_z; i++)
        {
            for (int k = 0; k < grid_size_x; k++)
            {
                if (occupied_grid[i, k] == false) number_of_shelves++;
            }
        }

        // Calculate Number of Outer Shelves //
        int number_of_outer_shelves = grid_size_x + grid_size_x - (int)entrance_size[0] + grid_size_z + grid_size_z - (int)entrance_size[2];
        // Calculate Number of Shelves //
        int temp_number_of_shelves = number_of_shelves + number_of_outer_shelves;


        ///// Spawn Outer Shelves /////
        //TODO add these to the regular spawning so that there is a posibility to spawn purchaseable items there
        //Generate northern outer shelves
        int temp_number_of_items = number_of_items_to_purchase;
        for (int grid_vert = 0; grid_vert < grid_size_x; grid_vert++)
        {
            bool spawn_food_to_purchase = false;
            if(temp_number_of_items > 0)
            {
                float random_number = Random.Range(0.0f, 1.0f);

                if (random_number < ((float)temp_number_of_items / (float)temp_number_of_shelves))
                {
                    spawn_food_to_purchase = true;
                    temp_number_of_items--;
                }
                temp_number_of_shelves--;
            }
            float offset_x = 0.5f;
            float offset_z = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + offset_x), object_position_y, (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, -90, 0);
            Vector3 temp_position;
            Vector3 temp_goal_position;
            Vector2Int temp_goal_map_position;
            int shelve_type = 0;

            Section obj = Section.Durable;
            if (grid_vert > grid_size_x - fruits_size[2] + 1f)
            {
                obj = Section.Fruit;
            }

            GameObject new_object = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
            shelve_tiles.Add(new_object);
            if (spawn_food_to_purchase == true)
            {
                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                //Calculation
                temp_goal_position = new Vector3(shelve_position.x,shelve_position.y, shelve_position.z - 0.75f);
                goal_localpositions_2d.Add(new Vector2(temp_goal_position.x, temp_goal_position.z));
                temp_goal_map_position = parse_Localposition_To_Map(temp_goal_position, grid_size_x, grid_size_z);
                goal_map_position_2d.Add(temp_goal_map_position);
            }
        }
        //Generate southern outer shelves
        for (int grid_vert = 0; grid_vert < grid_size_x - entrance_size[0]; grid_vert++)
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

            float offset_x = 0.5f;
            float offset_z = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + offset_x), object_position_y, (-grid_size_z / 2.0f) - offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 90, 0);
            Vector3 temp_position;
            Vector3 temp_goal_position;
            Vector2Int temp_goal_map_position;
            int shelve_type = 0;
            Section obj = Section.Drinks;
            GameObject new_object = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
            shelve_tiles.Add(new_object);

            if (spawn_food_to_purchase == true)
            {
                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                //Calculation
                temp_goal_position = new Vector3(shelve_position.x, shelve_position.y, shelve_position.z + 0.75f);
                goal_localpositions_2d.Add(new Vector2(temp_goal_position.x, temp_goal_position.z));
                temp_goal_map_position = parse_Localposition_To_Map(temp_goal_position, grid_size_x, grid_size_z);
                goal_map_position_2d.Add(temp_goal_map_position);
            }
        }
        //Generate western outer shelves
        for (int grid_hor = 0; grid_hor < grid_size_z; grid_hor++)
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

            float offset_x = 0.25f;
            float offset_z = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3((-(grid_size_x / 2.0f) - offset_x), object_position_y, grid_hor - (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 180, 0);
            Vector3 temp_position;
            Vector3 temp_goal_position;
            Vector2Int temp_goal_map_position;
            int shelve_type = 0;

            Section obj = Section.Durable;
            if (grid_hor < grid_size_z - beverages_size[2] - 1f)
            {
                obj = Section.Drinks;
            }


            GameObject new_object = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
            shelve_tiles.Add(new_object);

            if (spawn_food_to_purchase == true)
            {
                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                //Calculation
                temp_goal_position = new Vector3(shelve_position.x + 0.75f, shelve_position.y, shelve_position.z);
                goal_localpositions_2d.Add(new Vector2(temp_goal_position.x, temp_goal_position.z));
                temp_goal_map_position = parse_Localposition_To_Map(temp_goal_position, grid_size_x, grid_size_z);
                goal_map_position_2d.Add(temp_goal_map_position);
            }
        }
        //Generate eastern outer shelves
        for (int grid_hor = (int)entrance_size[2]; grid_hor < grid_size_z; grid_hor++)
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

            float offset_x = 0.25f;
            float offset_z = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((grid_size_x / 2.0f) + offset_x), object_position_y, grid_hor - (grid_size_z / 2.0f) + offset_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            Vector3 temp_position;
            Vector3 temp_goal_position;
            Vector2Int temp_goal_map_position;
            int shelve_type = 0;
            Section obj = Section.Fruit;
            GameObject new_object = Instantiate(shelve_wall_tile[0], shelve_position, shelve_rotation, this.transform);
            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
            shelve_tiles.Add(new_object);

            if (spawn_food_to_purchase == true)
            {
                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                //Calculation
                temp_goal_position = new Vector3(shelve_position.x - 0.75f, shelve_position.y, shelve_position.z);
                goal_localpositions_2d.Add(new Vector2(temp_goal_position.x, temp_goal_position.z));
                temp_goal_map_position = parse_Localposition_To_Map(temp_goal_position, grid_size_x, grid_size_z);
                goal_map_position_2d.Add(temp_goal_map_position);
            }
        }

        ///// Spawn Inner Shelves /////
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
                    float object_offset = 0.5f;
                    Quaternion object_rotation; 
                    Vector3 object_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + object_offset), object_position_y, (grid_size_z / 2.0f) - grid_hor - object_offset);
                    Vector3 temp_position;
                    Vector2 temp_goal_position;

                    //Durablefood Area
                    if (grid_hor < durablefoods_size[2] && grid_vert < durablefoods_size[0])
                    {
                        if (durablefood_area.orientation == "horizontal")
                        {
                            int shelve_type = 0;
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                            int shelve_type = 0;
                            Section obj = Section.Durable;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                            int shelve_type = 1;
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[1], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                            int shelve_type = 1;
                            Section obj = Section.Fruit;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[1], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                            int shelve_type = 0;
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                            int shelve_type = 0;
                            Section obj = Section.Drinks;
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            new_object.GetComponent<ShelveFiller>().spawn_random_items((int)obj, shelve_type);
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
                if(grid_vert >= grid_size_x - entrance_size[0] - CHECKOUT_SIZE && grid_hor >= grid_size_z - entrance_size[2] - 3)
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


        ////////// Agent Position //////////
        float agent_position_y = 0.75f;
        Vector3 agent_starting_localposition = new Vector3(entrance_position.x + entrance_size.x / 2.0f - 2.5f, this.transform.position.y + agent_position_y, entrance_position.z + entrance_size.z / 2.0f + 1.5f);
        GridTile Agent = new GridTile();
        Vector2Int agent_map_pos = parse_Localposition_To_Map(agent_starting_localposition, grid_size_x, grid_size_z);
        //Debug.Log("Agent Starting Pos: " + agent_map_pos);
        Agent.X = agent_map_pos.y;
        Agent.Z = agent_map_pos.x;
        agent.GetComponent<AgentReposition>().reposition(agent_starting_localposition);


        ////////// Goal Position //////////
        float goal_position_y = 0.75f;
        Vector3 goal_spawn_pos = new Vector3(goal_localpositions_2d[0].x, this.transform.position.y + goal_position_y, goal_localpositions_2d[0].y);
        //Vector3 goal_spawn_pos = new Vector3(-8.5f,0.75f,14f);
        Vector2Int goal_map_position = parse_Localposition_To_Map(goal_spawn_pos, grid_size_x, grid_size_z);
        GridTile Goal = new GridTile();
        Goal.X = goal_map_position.y;
        Goal.Z = goal_map_position.x;
        goal.GetComponent<Goal>().reposition(goal_spawn_pos);


        ////////// Application A* //////////
        bool[,] occupied_grid_astar = occupied_grid;
        int checkout_size_x = 7;
        int checkout_size_z = 5;
        bool[,] occupied_checkout = new bool[checkout_size_z, checkout_size_x];

        //take out checkout fields
        grid_position_x = grid_size_x - (int)entrance_size[0] - CHECKOUT_SIZE + 2;
        grid_position_z = grid_size_z - CHECKOUT_SIZE + 1;
        for (int z_local = 0; z_local < occupied_checkout.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_checkout.GetLength(1); x_local++)
            {
                occupied_grid_astar[grid_position_z + z_local, grid_position_x + x_local] = false;
            }
        }

        //take out entrance fields
        grid_position_x = grid_size_x - (int)entrance_size[0];
        grid_position_z = grid_size_z - (int)entrance_size[2];
        for (int z_local = 0; z_local < occupied_entrance.GetLength(0); z_local++)
        {
            for (int x_local = 0; x_local < occupied_entrance.GetLength(1); x_local++)
            {
                occupied_grid_astar[grid_position_z + z_local, grid_position_x + x_local] = false;
            }
        }
        
        ///// Using A* /////
        List<Vector2> shortest_path = calculate_a_star(goal_map_position_2d[0], Agent, Goal, grid_size_x, grid_size_z, occupied_grid);

        for (int i = 1; i < shortest_path.Count - 1; i++)
        {
            Vector3 waypoint_pos = new Vector3(shortest_path[i].x, this.transform.position.y + 0.75f, shortest_path[i].y);
            current_shortest_path.Add(waypoint_pos);
            Quaternion waypoint_rotation = Quaternion.Euler(0, 0, 0);
            GameObject waypoint_obj = Instantiate(waypoint, waypoint_pos, waypoint_rotation, this.transform);
            waypoint_objects.Add(waypoint_obj);
        }
        this.GetComponentInChildren<MoveToGoalAgent>().shortest_path_waypoints = waypoint_objects;


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

    public Vector2Int parse_Localposition_To_Map(Vector3 local_position, int grid_size_x, int grid_size_z)
    {
        //Vector3 object_position = this.transform.position + new Vector3((grid_vert - (grid_size_x / 2.0f) + object_offset), object_position_y, (grid_size_z / 2.0f) - grid_hor - object_offset);

        float x_half_map_size = (float)(grid_size_x - 1.0f) / 2.0f;
        float z_half_map_size = (float)(grid_size_z - 1.0f) / 2.0f;
        Vector2Int parsed_value = new Vector2Int();

        if (local_position.x <= 0)
        {
            // for cartesian coordinate system 3.quadrant (-,-)
            if (local_position.z < 0)
            {
                parsed_value.x = (int)(z_half_map_size + Mathf.Abs(local_position.z));
                parsed_value.y = (int)(x_half_map_size - Mathf.Abs(local_position.x));
            }
            // for cartesian coordinate system 2.quadrant (-,+)
            else
            {
                parsed_value.x = (int)(z_half_map_size - Mathf.Abs(local_position.z));
                parsed_value.y = (int)(x_half_map_size - Mathf.Abs(local_position.x));
            }
        }
        else if (local_position.x > 0)
        {
            // for cartesian coordinate system 4.quadrant (+,-)
            if (local_position.z < 0)
            {
                parsed_value.x = (int)(z_half_map_size + Mathf.Abs(local_position.z));
                parsed_value.y = (int)(x_half_map_size + Mathf.Abs(local_position.x));
            }
            // for cartesian coordinate system 1.quadrant (+,+)
            else
            {
                parsed_value.x = (int)(z_half_map_size - Mathf.Abs(local_position.z));
                parsed_value.y = (int)(x_half_map_size + Mathf.Abs(local_position.x));
            }
        }
        return parsed_value;
    }

    public Vector2 parse_Map_To_Localposition(Vector2Int map_position, int grid_size_x, int grid_size_z)
    {
        if (map_position.x < 0 || map_position.y < 0)
        {
            Debug.LogError("Wrong function used. Vector2(0f,0f) is given");
            return new Vector2(0f, 0f);
        }
        float x_half_map_size = (float)(grid_size_x - 1.0f) / 2.0f;
        float z_half_map_size = (float)(grid_size_z - 1.0f) / 2.0f;
        Vector2 parsed_value = new Vector2();

        if (map_position.y <= x_half_map_size)
        {
            // for cartesian coordinate system 2.quadrant (-,+)
            if (map_position.x < z_half_map_size)
            {
                parsed_value.x = -x_half_map_size + map_position.y;
                parsed_value.y = z_half_map_size - map_position.x;
            }
            // for cartesian coordinate system 3.quadrant (-,-)
            else
            {
                parsed_value.x = -x_half_map_size + map_position.y;
                parsed_value.y = z_half_map_size - map_position.x;
            }
        }
        else if (map_position.y > x_half_map_size)
        {
            // for cartesian coordinate system 1.quadrant (+,+)
            if (map_position.x < z_half_map_size)
            {
                parsed_value.x = map_position.y - x_half_map_size;
                parsed_value.y = z_half_map_size - map_position.x;
            }
            // for cartesian coordinate system 4.quadrant (+,-)
            else
            {
                parsed_value.x = map_position.y - x_half_map_size;
                parsed_value.y = z_half_map_size - map_position.x;
            }
        }
        return parsed_value;
    }


    ////////// Function for A* Application //////////
    //Returns localposition not map position
    private List<Vector2> calculate_a_star(Vector2 goal_position, GridTile Agent, GridTile Goal, int grid_size_x, int grid_size_z, bool[,] occupiedGrids)
    {
        List<Vector2> shortest_path = new List<Vector2>();
        if (goal_position != null)
        {
            //Debug.Log("Goal starting position: " + Goal.X + " " + Goal.Y);
            //A* algorithm to check if both agents can reach each other
            //https://dotnetcoretutorials.com/2020/07/25/a-search-pathfinding-algorithm-in-c/
            Agent.set_Distance(Goal.X, Goal.Z);
            List<GridTile> activeTiles = new List<GridTile>();
            activeTiles.Add(Agent);
            List<GridTile> visitedTiles = new List<GridTile>();

            while (activeTiles.Any())
            {
                var checkTile = activeTiles.OrderBy(x => x.CostDistance).First();

                if (checkTile.X == Goal.X && checkTile.Z == Goal.Z)
                {
                    var tile = checkTile;
                    while (true)
                    {
                        var test = new Vector2Int(tile.Z, tile.X);
                        var test_local = parse_Map_To_Localposition(test, grid_size_x, grid_size_z);
                        shortest_path.Add(test_local);

                        tile = tile.Parent;
                        if (tile == null)
                        {
                            return shortest_path;
                        }
                    }
                }

                visitedTiles.Add(checkTile);
                activeTiles.Remove(checkTile);
                var walkableTiles = GetWalkableTiles(occupiedGrids, checkTile, Goal, grid_size_x, grid_size_z);
                foreach (var walkableTile in walkableTiles)
                {
                    //We have already visited this tile so we don't need to do so again!
                    if (visitedTiles.Any(x => x.X == walkableTile.X && x.Z == walkableTile.Z))
                        continue;
                    //It's already in the active list, but that's OK, maybe this new tile has a better value (e.g. We might zigzag earlier but this is now straighter). 
                    if (activeTiles.Any(x => x.X == walkableTile.X && x.Z == walkableTile.Z))
                    {
                        var existingTile = activeTiles.First(x => x.X == walkableTile.X && x.Z == walkableTile.Z);
                        if (existingTile.CostDistance > checkTile.CostDistance)
                        {
                            activeTiles.Remove(existingTile);
                            activeTiles.Add(walkableTile);
                        }
                    }
                    else
                    {
                        //We've never seen this tile before so add it to the list. 
                        activeTiles.Add(walkableTile);
                    }
                }
            }
            //Restart Arena Setup
            print("No Path Found! Recalculate Map: " + this.name);
            //TODO change
            return shortest_path;
        }
        else
        {
            //TODO change
            return shortest_path;
        }
    }


    private void Awake()
    {
        // Gets entrance script
        setup_entrance = this.transform.GetComponent<SetupEntrance>();
    }
}
