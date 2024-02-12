using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupEntrance : MonoBehaviour
{
    [SerializeField] private GameObject fence_tile;
    [SerializeField] private GameObject fence_doubleentrance_tile;

    private List<GameObject> fence_tiles = new List<GameObject>();
    private const float OFFSET_X = 0.5f;
    private const float OFFSET_Z = 0.0f;
    private const int CHECKOUT_SIZE = 6;

    public void setup_entrance(int grid_size_x, int grid_size_z, Vector3 entrance_size)
    {
        float object_position_y = 0.25f;
        //Clear ground tiles
        foreach (GameObject fence_tile in fence_tiles)
        {
            Destroy(fence_tile);
        }
        //Spawn entrance fences horizontal(from left to right or west to east)
        int x_entr_start = (int)grid_size_x - (int)entrance_size.x;
        int x_entr = x_entr_start;
        float fence_position_z = (-grid_size_z / 2.0f) + entrance_size.z - OFFSET_Z;
        
        while (x_entr < grid_size_x)
        {
            Vector3 fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + OFFSET_X), object_position_y, fence_position_z);
            if (x_entr == x_entr_start)
            {
                Quaternion fence_rotation = Quaternion.Euler(0, 180, 0);
                GameObject fence = Instantiate(fence_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
            }
            else if (x_entr < x_entr_start + (int)entrance_size.x / 2)
            {
                Quaternion fence_rotation = Quaternion.Euler(0, 0, 0);
                GameObject fence = Instantiate(fence_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
            }
            else if (x_entr == x_entr_start + (int)entrance_size.x / 2)
            {
                x_entr++; //to get the center of the double fence, already add one here to x_entry
                fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f)), object_position_y, fence_position_z);
                Quaternion fence_rotation = Quaternion.Euler(0, 0, 0);
                GameObject fence = Instantiate(fence_doubleentrance_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
            }
            else if(x_entr > x_entr_start + (int)entrance_size.x / 2 + 1)
            {
                Quaternion fence_rotation = Quaternion.Euler(0, 180, 0);
                GameObject fence = Instantiate(fence_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
            }
            else
            {
                Debug.LogError("Out of bounds entrance spawn");
                x_entr++;
            }
        }
        //Spawn entrance fences vertical(from bottom up or south to north)
        //first six steps are always the same because of the checkout 
        int z_entr_end = (int)grid_size_z - (int)entrance_size.z;
        int z_entr = (int)grid_size_z - CHECKOUT_SIZE;
        float fence_position_x = (grid_size_x / 2.0f) - entrance_size.x;
        float fence_offset_z = 0.5f;
        while (z_entr > z_entr_end)
        {
            Quaternion fence_rotation = Quaternion.Euler(0, 90, 0);
            Vector3 fence_position = this.transform.position + new Vector3(fence_position_x, object_position_y, (grid_size_z / 2.0f) - z_entr + fence_offset_z);
            GameObject fence = Instantiate(fence_tile, fence_position, fence_rotation, this.transform);
            fence_tiles.Add(fence);
            z_entr--;
        }
    }

}
