using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using System.IO;

public class SetupSupermarket : MonoBehaviour
{
    [SerializeField] private int min_ground_size = 20;
    [SerializeField] private int max_ground_size = 30;

    [SerializeField] private int min_entrance_size = 6;
    [SerializeField] private int max_entrance_size = 7;

    [SerializeField] private int number_of_items_to_purchase = 1;

    [SerializeField] private GameObject shelf_pref;
    [SerializeField] private GameObject entrance_pref;
    [SerializeField] private GameObject fruits_pref;
    [SerializeField] private GameObject durablefood_pref;
    [SerializeField] private GameObject alcohol_pref;

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

    [SerializeField] private GameObject shelve_wall_tile;
    [SerializeField] private GameObject[] available_shelves;


    private List<GameObject> entrance_tiles = new List<GameObject>();
    private List<GameObject> fruits_tiles = new List<GameObject>();
    private List<GameObject> durablefood_tiles = new List<GameObject>();
    private List<GameObject> alcohol_tiles = new List<GameObject>();
    private List<GameObject> shelve_tiles = new List<GameObject>();


    // List with the positions for A*
    private List<Vector2> goal_positions_2d = new List<Vector2>();
    private Vector2 agent_starting_position = new Vector2();

    enum Section { Fruit, Durable, Drinks}

   public class Area
    {
        public Vector2Int area_size;
        public string orientation;
        public List<GameObject> ground_tiles = new List<GameObject>();


        public Area(Vector2Int size)
        {
            area_size = size;
            orientation = calculate_shelve_orientation(area_size[0], area_size[1]);
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
            //TODO random machen
            if(possible_orientations.Count == 0)
            {
                possible_orientations.Add("vertical");
            }
            int random_index = Random.Range(0, possible_orientations.Count);
            return possible_orientations[random_index];
        }

        public void hello()
        {
            Debug.Log(orientation);
        }
    }

    // A* Class
    class GridTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Cost { get; set; }
        public float Distance { get; set; }
        public float CostDistance => Cost + Distance;
        public GridTile Parent { get; set; }
        //The distance is essentially the estimated distance, ignoring walls to our target. 
        //So how many tiles left and right, up and down, ignoring walls, to get there.
        public void set_Distance(int targetX, int targetY)
        {
            this.Distance = Mathf.Abs(targetX - X) + Mathf.Abs(targetY - Y);
        }
    }

    private static List<GridTile> GetWalkableTiles(bool[,] occupiedGrids, GridTile currentTile, GridTile targetTile, int GridSize_x, int GridSize_y)
    {
        var possibleTiles = new List<GridTile>()
        {
            new GridTile { X = currentTile.X, Y = currentTile.Y - 1, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X, Y = currentTile.Y + 1, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X - 1, Y = currentTile.Y, Parent = currentTile, Cost = currentTile.Cost + 1 },
            new GridTile { X = currentTile.X + 1, Y = currentTile.Y, Parent = currentTile, Cost = currentTile.Cost + 1 },
        };
        possibleTiles.ForEach(tile => tile.set_Distance(targetTile.X, targetTile.Y));
        var maxX = GridSize_x - 1;
        var maxY = GridSize_y - 1;
        return possibleTiles
            .Where(tile => tile.X >= 0 && tile.X <= maxX)
            .Where(tile => tile.Y >= 0 && tile.X <= maxY)
            .Where(tile => occupiedGrids[tile.X, tile.Y] == true || targetTile.X == tile.X && targetTile.Y == tile.Y)
            .ToList();
    }
    public void calculate_Grid()
    {
        // Generate ground surface
        int grid_size_x = Random.Range(min_ground_size, max_ground_size);
        int grid_size_y = Random.Range(min_ground_size, max_ground_size);
        GameObject ground = this.transform.Find("Ground").gameObject;
        ground.transform.localScale = new Vector3(grid_size_x, 0.5f, grid_size_y);
        Debug.Log("Grid in x: " + grid_size_x);
        Debug.Log("Grid in y: " + grid_size_y);

        Vector3 playfield_position = this.transform.position;
        if (grid_size_x % 2 == 0)
        {
            transform.position = new Vector3(playfield_position.x + 0.5f, playfield_position.y, playfield_position.z);
            Debug.Log(transform.position);
        }
        if (grid_size_y % 2 == 0)
        {
            transform.position = new Vector3(transform.position.x, playfield_position.y, playfield_position.z + 0.5f);
            Debug.Log(transform.position);
        }
        // Generate entrance area
        Quaternion entranceRotation = Quaternion.Euler(0, 0, 0);
        entrance_pref.transform.localScale = new Vector3(Random.Range(min_entrance_size, max_entrance_size), 0.5f, Random.Range(min_entrance_size, max_entrance_size));
        Vector3 entrance_size = entrance_pref.transform.localScale;
        Vector3 entrance_position = this.transform.localPosition + new Vector3((grid_size_x / 2.0f - entrance_size[0] / 2.0f), 0.5f, (-grid_size_y / 2.0f + entrance_size[2] / 2.0f));
        GameObject entrance = Instantiate(entrance_pref, entrance_position, entranceRotation, this.transform);
        entrance_tiles.Add(entrance);



        // Generate durablefood area
        Quaternion durablefoodRotation = Quaternion.Euler(0, 0, 0);
        durablefood_pref.transform.localScale = new Vector3(grid_size_x - entrance_size[0], 0.5f, grid_size_y - entrance_size[2]);
        Vector3 durablefood_size = durablefood_pref.transform.localScale;
        Vector3 durablefood_position = this.transform.localPosition + new Vector3((durablefood_size[0] / 2.0f - grid_size_x / 2.0f), 0.5f, (grid_size_y / 2.0f - durablefood_size[2] / 2.0f));
        GameObject durablefood = Instantiate(durablefood_pref, durablefood_position, durablefoodRotation, this.transform);
        durablefood_tiles.Add(durablefood);
        Area durablefood_area = new Area(new Vector2Int((int)durablefood_size[0], (int)durablefood_size[2]));

        // Generate beverages area
        Quaternion alcoholRotation = Quaternion.Euler(0, 0, 0);
        alcohol_pref.transform.localScale = new Vector3(grid_size_x - entrance_size[0], 0.5f, entrance_size[2]);
        Vector3 alcohol_size = alcohol_pref.transform.localScale;
        Vector3 alcohol_position = this.transform.localPosition + new Vector3((alcohol_size[0] / 2.0f - grid_size_x / 2.0f), 0.5f, (-grid_size_y / 2.0f + alcohol_size[2] / 2.0f));
        GameObject alcohol = Instantiate(alcohol_pref, alcohol_position, alcoholRotation, this.transform);
        alcohol_tiles.Add(alcohol);
        Area alcohol_area = new Area(new Vector2Int((int)alcohol_size[0], (int)alcohol_size[2]));

        // Generate fruits vegetable area
        Quaternion fruitsRotation = Quaternion.Euler(0, 0, 0);
        fruits_pref.transform.localScale = new Vector3(entrance_size[0], 0.5f, grid_size_y - entrance_size[2]);
        Vector3 fruits_size = fruits_pref.transform.localScale;
        Vector3 fruits_position = this.transform.localPosition + new Vector3((grid_size_x / 2.0f - fruits_size[0] / 2.0f), 0.5f, (grid_size_y / 2.0f - fruits_size[2] / 2.0f));
        GameObject fruits = Instantiate(fruits_pref, fruits_position, fruitsRotation, this.transform);
        fruits_tiles.Add(fruits);
        Area fruits_area = new Area(new Vector2Int((int)fruits_size[0], (int)fruits_size[2]));


        bool[,] occupiedGrids = new bool[grid_size_x, grid_size_y];

        // Grid which needs to be blocked and checked if it's free
        bool[,] toFilledGrid = new bool[horizontal_shelve.GetLength(0), horizontal_shelve.GetLength(1)];

        //Entrance grid this is false but we just check later for false to save a conversion to true
        bool[,] occupied_entrance = new bool[(int)entrance_size[0], (int)entrance_size[2]];
        Debug.Log("Entrance Size in x: " + entrance_size[0]);
        Debug.Log("Entrance Size in y: " + entrance_size[2]);


        // Decide which Orientation the shelves should have in the durablefood department
        //bool horizontal_spawn = true; //(Random.value > 0.5f);



        // Take out at least one field around the edge of the field and 2 to the north and east
        bool[,] occupied_durablefood_grid = new bool[(int)durablefood_size[0], (int)durablefood_size[2]];
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
                    toFilledGrid = horizontal_shelve;
                    if (occupied_durablefood_grid[grid_hor, grid_vert] == false)
                    {

                        if ((occupied_durablefood_grid.GetLength(1) % 3 == 1) && (grid_vert == occupied_durablefood_grid.GetLength(1) - 2))
                        {
                            // empty so that there is false in this row without using the horizontal_shelve which would break the thing
                        }
                        else
                        {
                            for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                            {
                                for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                                {
                                    if (toFilledGrid[x_local, y_local] == true)
                                    {
                                        occupied_durablefood_grid[grid_hor + y_local, grid_vert + x_local] = true;
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
                else if (durablefood_area.orientation == "vertical")
                {
                    toFilledGrid = vertical_shelve;
                    if (occupied_durablefood_grid[grid_hor, grid_vert] == false)
                    {
                        if ((occupied_durablefood_grid.GetLength(0) % 3 == 1) && (grid_hor == occupied_durablefood_grid.GetLength(0) - 2))
                        {

                        }
                        else
                        {
                            for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                            {
                                for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                                {
                                    if (toFilledGrid[x_local, y_local] == true)
                                    {
                                        occupied_durablefood_grid[grid_hor + y_local, grid_vert + x_local] = true;
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
            }
        }
        // Take out at least one field around the edge of the field and 2 to the north and east
        bool[,] occupied_alcohol_grid = new bool[(int)alcohol_size[0], (int)alcohol_size[2]];
        for (int grid_hor = 0; grid_hor < occupied_alcohol_grid.GetLength(0); grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < occupied_alcohol_grid.GetLength(1); grid_vert++)
            {
                if (grid_hor == 0 || grid_hor == 1 || grid_hor == occupied_alcohol_grid.GetLength(0) - 2 || grid_hor == occupied_alcohol_grid.GetLength(0) - 1)
                    occupied_alcohol_grid[grid_hor, grid_vert] = true;
                if (grid_vert == 0 || grid_vert == occupied_alcohol_grid.GetLength(1) - 2 || grid_vert == occupied_alcohol_grid.GetLength(1) - 1)
                    occupied_alcohol_grid[grid_hor, grid_vert] = true;
                if (alcohol_area.orientation == "horizontal")
                {
                    if (occupied_alcohol_grid[grid_hor, grid_vert] == false)
                    {
                        toFilledGrid = horizontal_shelve;
                        for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                        {
                            for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                            {
                                if (toFilledGrid[x_local, y_local] == true)
                                {
                                    occupied_alcohol_grid[grid_hor + y_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                    // create walkthroughs inbetween shelves at the same place as durablefood one
                    if (grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2 || grid_hor == (int)occupied_durablefood_grid.GetLength(0) / 2 + 1)
                    {
                        occupied_alcohol_grid[grid_hor, grid_vert] = true;
                    }
                }

                else if (alcohol_area.orientation == "vertical")
                {
                    if (occupied_alcohol_grid[grid_hor, grid_vert] == false)
                    {
                        toFilledGrid = vertical_shelve;
                        for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                        {
                            for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                            {
                                if (toFilledGrid[x_local, y_local] == true)
                                {
                                    occupied_alcohol_grid[grid_hor + y_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                }

            }
        }

        // Take out at least one field around the edge of the field and 2 to the north and east
        bool[,] occupied_fruits_grid = new bool[(int)fruits_size[0], (int)fruits_size[2]];
        for (int grid_hor = 0; grid_hor < occupied_fruits_grid.GetLength(0); grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < occupied_fruits_grid.GetLength(1); grid_vert++)
            {
                if (grid_hor == 0 || grid_hor == occupied_fruits_grid.GetLength(0) - 2 || grid_hor == occupied_fruits_grid.GetLength(0) - 1)
                    occupied_fruits_grid[grid_hor, grid_vert] = true;
                if (grid_vert == 0 || grid_vert == 1 || grid_vert == occupied_fruits_grid.GetLength(1) - 2 || grid_vert == occupied_fruits_grid.GetLength(1) - 1)
                    occupied_fruits_grid[grid_hor, grid_vert] = true;
                if (fruits_area.orientation == "horizontal")
                {
                    if (occupied_fruits_grid[grid_hor, grid_vert] == false)
                    {
                        toFilledGrid = horizontal_shelve;
                        for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                        {
                            for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                            {
                                if (toFilledGrid[x_local, y_local] == true)
                                {
                                    //inverted because of array notation y --> 0,1,2
                                    //  0 1 2 y
                                    //0 0 0 0
                                    //1 1 0 0
                                    //2 1 0 0
                                    //x
                                    occupied_fruits_grid[grid_hor + y_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                }

                else if (fruits_area.orientation == "vertical")
                {
                    if (occupied_fruits_grid[grid_hor, grid_vert] == false)
                    {
                        toFilledGrid = vertical_shelve;
                        for (int x_local = 0; x_local < toFilledGrid.GetLength(0); x_local++)
                        {
                            for (int y_local = 0; y_local < toFilledGrid.GetLength(1); y_local++)
                            {
                                if (toFilledGrid[x_local, y_local] == true)
                                {
                                    occupied_fruits_grid[grid_hor + y_local, grid_vert + x_local] = true;
                                }
                            }
                        }
                    }
                    // create walkthroughs inbetween shelves at the same place as durablefood one
                    if (grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 || grid_vert == (int)occupied_durablefood_grid.GetLength(1) / 2 + 1)
                    {
                        occupied_fruits_grid[grid_hor, grid_vert] = true;
                    }

                }

            }
        }

        // Take out fields

        for (int grid_hor = 0; grid_hor < grid_size_x; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_y; grid_vert++)
            {

                //take out occupied durablefood fields
                if (grid_hor == 0 && grid_vert == 0)
                {
                    for (int x_local = 0; x_local < occupied_durablefood_grid.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_durablefood_grid.GetLength(1); y_local++)
                        {
                            occupiedGrids[grid_hor + x_local, grid_vert + y_local] = occupied_durablefood_grid[x_local, y_local];
                        }
                    }
                }
                //take out occupied alcohol fields
                if (grid_hor == 0 && grid_vert == grid_size_y - alcohol_size[2])
                {
                    for (int x_local = 0; x_local < occupied_alcohol_grid.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_alcohol_grid.GetLength(1); y_local++)
                        {
                            occupiedGrids[grid_hor + x_local, grid_vert + y_local] = occupied_alcohol_grid[x_local, y_local];
                        }
                    }
                }
                //take out occupied fruit fields
                if (grid_hor == grid_size_x - fruits_size[0] && grid_vert == 0)
                {
                    for (int x_local = 0; x_local < occupied_fruits_grid.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_fruits_grid.GetLength(1); y_local++)
                        {
                            occupiedGrids[grid_hor + x_local, grid_vert + y_local] = occupied_fruits_grid[x_local, y_local];
                        }
                    }
                }

                //take out entrance fields
                if (grid_hor == grid_size_x - entrance_size[0] && grid_vert == grid_size_y - entrance_size[2])
                {
                    for (int x_local = 0; x_local < occupied_entrance.GetLength(0); x_local++)
                    {
                        for (int y_local = 0; y_local < occupied_entrance.GetLength(1); y_local++)
                        {
                            occupiedGrids[grid_hor + x_local, grid_vert + y_local] = true;
                        }
                    }
                }
            }
        }


        //get number of spawned shelves in the inner Part
        int number_of_shelves = 0;
        for (int i = 0; i < grid_size_x; i++)
        {
            for (int k = 0; k < grid_size_y; k++)
            {
                if (occupiedGrids[i, k] == false) number_of_shelves++;
            }
        }

        //***Spawn Shelves***//
        for (int grid_hor = 0; grid_hor < grid_size_x; grid_hor++)
        {
            for (int grid_vert = 0; grid_vert < grid_size_y; grid_vert++)
            {
                if (occupiedGrids[grid_hor, grid_vert] == false)
                {
                    bool spawn_food_to_purchase = false;
                    if (number_of_items_to_purchase > 0)
                    {
                        float random_number = Random.Range(0.0f, 1.0f);

                        if (random_number < ((float)number_of_items_to_purchase / (float)number_of_shelves))
                        {
                            spawn_food_to_purchase = true;
                            number_of_items_to_purchase--;
                        }
                        number_of_shelves--;
                    }

                    float object_offset = 0.5f;
                    Quaternion object_rotation = Quaternion.Euler(0, 0, 0);
                    Vector3 object_position = this.transform.localPosition + new Vector3((grid_hor - (grid_size_x / 2.0f) + object_offset), 0.75f, (grid_size_y / 2.0f) - grid_vert - object_offset);

                    Vector3 temp_position = new Vector3();
                    Vector2 temp_goal_position = new Vector2();

                    //durablefood area
                    if (grid_hor < durablefood_area.area_size[0] && grid_vert < durablefood_area.area_size[1])
                    {

                        if (durablefood_area.orientation == "horizontal")
                        {
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Durable;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Durable;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);

                                //Calculation
                                temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                    }
                    //fruit area
                    else if (grid_hor >= grid_size_x - fruits_area.area_size[0] && grid_vert < fruits_area.area_size[1])
                    {
                        if (fruits_area.orientation == "horizontal")
                        {
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Fruit;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Fruit;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                    }
                    // alcohol area
                    else if (grid_hor < alcohol_area.area_size[0] && grid_vert >= grid_size_y - alcohol_area.area_size[1])
                    {
                        if (alcohol_area.orientation == "horizontal")
                        {
                            object_rotation = Quaternion.Euler(0, 90, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Drinks;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_goal_position_horizontal(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }
                        else
                        {
                            object_rotation = Quaternion.Euler(0, 0, 0);
                            GameObject new_object = Instantiate(available_shelves[0], object_position, object_rotation, this.transform);
                            shelve_tiles.Add(new_object);
                            if (spawn_food_to_purchase == true)
                            {
                                Section obj = Section.Drinks;
                                temp_position = new_object.GetComponent<ShelveFiller>().spawn_purchable_item((int)obj);
                                //Calculation
                                temp_goal_position = calculate_goal_position_vertical(object_position, temp_position);
                                goal_positions_2d.Add(temp_goal_position);
                            }
                        }

                    }
                    /**** spawn red cubes ****
                    else
                    {
                        GameObject new_object = Instantiate(shelf_pref, object_position, object_rotation, this.transform);
                        shelve_tiles.Add(new_object);
                    }
                    **************************/
                }
            }
        }


        // Generate north outershelves
        //bool[,] number_of_tiles = new bool[grid_size_x, grid_size_y];

        for (int x = 0; x < grid_size_x; x++)
        {
            float offset_x = 0.5f;
            float offset_y = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((x - (grid_size_x / 2.0f) + offset_x), 0.8f, (grid_size_y / 2.0f) + offset_y);
            Quaternion shelve_rotation = Quaternion.Euler(0, -90, 0);
            GameObject shelve = Instantiate(shelve_wall_tile, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate south outershelves
        for (int x_entr = 0; x_entr < grid_size_x - entrance_pref.transform.localScale[0]; x_entr++)
        {
            float offset_x = 0.5f;
            float offset_y = 0.25f;
            Vector3 shelve_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + offset_x), 0.8f, (-grid_size_y / 2.0f) - offset_y);
            Quaternion shelve_rotation = Quaternion.Euler(0, 90, 0);
            GameObject shelve = Instantiate(shelve_wall_tile, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate west outershelves
        for (int y = 0; y < grid_size_y; y++)
        {
            float offset_x = 0.25f;
            float offset_y = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((-grid_size_x / 2.0f) - offset_x), 0.8f, y - (grid_size_y / 2.0f) + offset_y);
            Quaternion shelve_rotation = Quaternion.Euler(0, 180, 0);
            GameObject shelve = Instantiate(shelve_wall_tile, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }
        //Generate east outershelves
        for (int y_entr = (int)entrance_pref.transform.localScale[2]; y_entr < grid_size_y; y_entr++)
        {
            float offset_x = 0.25f;
            float offset_y = 0.5f;
            Vector3 shelve_position = this.transform.position + new Vector3(((grid_size_x / 2.0f) + offset_x), 0.8f, y_entr - (grid_size_y / 2.0f) + offset_y);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject shelve = Instantiate(shelve_wall_tile, shelve_position, shelve_rotation, this.transform);
            shelve_tiles.Add(shelve);
        }

        ////////// Agent Position //////////
        agent_starting_position = calculate_agent_starting_position(entrance_position, entrance_size);

        //Debug.Log("Agent starting position: " + agent_starting_position);

        for (int i = 0; i < goal_positions_2d.Count; i++)
        {
            //Debug.Log(goal_positions_2d[i]);
            Debug.Log("Position " + i + ":" + parser_localposition_to_map(goal_positions_2d[i], grid_size_x, grid_size_y));
        }



        GridTile Agent = new GridTile();
        GridTile Goal = new GridTile();
        Agent.X = 21;//(int)agent_starting_position.x;
        Agent.Y = 19; //(int)agent_starting_position.y;

        Debug.Log("Agent starting position: " + Agent.X+ " " + Agent.Y);

        if (goal_positions_2d[0] != null)
        {
            Goal.X = 0; //(int)goal_positions_2d[0].x;
            Goal.Y = 0; //(int)goal_positions_2d[0].y;

            Debug.Log("Goal starting position: " + Goal.X + " " + Goal.Y);
            //A* algorithm to check if both agents can reach each other
            //https://dotnetcoretutorials.com/2020/07/25/a-search-pathfinding-algorithm-in-c/
            Agent.set_Distance(Goal.X, Goal.Y);
            List<GridTile> activeTiles = new List<GridTile>();
            activeTiles.Add(Agent);
            List<GridTile> visitedTiles = new List<GridTile>();

            while (activeTiles.Any())
            {
                var checkTile = activeTiles.OrderBy(x => x.CostDistance).First();

                if (checkTile.X == Goal.X && checkTile.Y == Goal.Y)
                {
                    print("Path found for: " + this.name);
                    var tile = checkTile;
                    while (true)
                    {
                        Debug.Log("Current Tile x: " + tile.X + " y: " + tile.Y);
                        tile = tile.Parent;
                        if (tile == null)
                        {
                            Debug.Log("Whole Map written");
                            return;
                        }
                    }
                    /*for (int i = 0; i < visitedTiles.Count; i++)
                    {
                        Debug.Log("Current Tile " + i + " x: " + visitedTiles[i].X + " y: " + visitedTiles[i].Y);
                    }*/
                    //We found the destination and we can be sure (Because the the OrderBy above)
                    //That it is the most low cost option. 
                    /*if (printMapFile != null)
                    {
                        printMapToFile(occupiedGrids, GridSize, checkTile, Agent1, Agent2);
                    }
                    return;*/
                }

                visitedTiles.Add(checkTile);
                activeTiles.Remove(checkTile);
                var walkableTiles = GetWalkableTiles(occupiedGrids, checkTile, Goal, grid_size_x, grid_size_y);
                foreach (var walkableTile in walkableTiles)
                {
                    //We have already visited this tile so we don't need to do so again!
                    if (visitedTiles.Any(x => x.X == walkableTile.X && x.Y == walkableTile.Y))
                        continue;
                    //It's already in the active list, but that's OK, maybe this new tile has a better value (e.g. We might zigzag earlier but this is now straighter). 
                    if (activeTiles.Any(x => x.X == walkableTile.X && x.Y == walkableTile.Y))
                    {
                        var existingTile = activeTiles.First(x => x.X == walkableTile.X && x.Y == walkableTile.Y);
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
        }
    }
        

    public Vector2 calculate_goal_position_horizontal(Vector3 shelve_position, Vector3 p_item_position)
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

    public Vector2 calculate_goal_position_vertical(Vector3 shelve_position, Vector3 p_item_position)
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

    public Vector2 parser_localposition_to_map(Vector2 local_position, int grid_size_x, int grid_size_y)
    {
        float x_component = (float)(grid_size_x - 1.0f) / 2.0f;
        float y_component = (float)(grid_size_y - 1.0f) / 2.0f;
        Vector2 parsed_value = new Vector2();

        if (local_position.x < 0)
        {
            if(local_position.y < 0)
            {
                
            }
            else
            {
                parsed_value.x = (int)(x_component - Mathf.Abs(local_position.y));
                parsed_value.y = (int)(y_component - Mathf.Abs(local_position.x));
            }
        }
        else if(local_position.x > 0)
        {
            if(local_position.y < 0)
            {
                
            }
            else
            {

            }    
        }

        return parsed_value;
    }

    public Vector2 parser_map_to_localposition(Vector2 map_position, int grid_size_x, int grid_size_y)
    {
        float x_component = (float)(grid_size_x - 1.0f) / 2.0f;
        float y_component = (float)(grid_size_y - 1.0f) / 2.0f;
        Vector2 parsed_value = new Vector2();

        if (map_position.x < y_component)
        {

        }
        return parsed_value;
    }
    /// <summary>
    /// calculate Agent starting position in dependencie of the entrence,
    /// the position is always the third 1x1 square from the right and the first field in the fruits area
    /// </summary>
    /// <param name="entrance_position">Position of the entrance </param>
    /// <param name="entrance_scale">Scale of the entrance </param>
    /// <returns></returns>
    public Vector2 calculate_agent_starting_position(Vector3 entrance_position, Vector3 entrance_scale)
    {
        Vector2 agent_pos = new Vector2();
        agent_pos.x = entrance_position.x + entrance_scale.x / 2.0f - 2.5f;
        agent_pos.y = entrance_position.z + entrance_scale.z / 2.0f + 0.5f;
        return agent_pos;
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
