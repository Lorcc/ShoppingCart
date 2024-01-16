using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupEntrance : MonoBehaviour
{
    [SerializeField] private GameObject fence_tile;
    [SerializeField] private GameObject fence_doubleentrance_tile;

    private List<GameObject> fence_tiles = new List<GameObject>();

    public void setup_entrance(int grid_size_x, int grid_size_y, Vector3 entrance_size)
    {
        //Clear ground tiles
        foreach (GameObject fence_tile in fence_tiles)
        {
            Destroy(fence_tile);
        }
        //Spawn entrance fences
        int x_entr_start = (int)grid_size_x - (int)entrance_size.x;
        int x_entr = x_entr_start;
        while(x_entr < grid_size_x)
        {
            if (x_entr < x_entr_start + (int)entrance_size.x / 2)
            {
                float offset_x = 0.5f;
                float offset_y = 0.1f;
                float fence_position_z = (-grid_size_y / 2.0f) + entrance_size.z - offset_y;
                Vector3 fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + offset_x), 0.8f, fence_position_z);
                Quaternion fence_rotation = Quaternion.Euler(0, 0, 0);
                GameObject fence = Instantiate(fence_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
            }
            else if (x_entr == x_entr_start + (int)entrance_size.x / 2)
            {
                float offset_x = 1.0f;
                float offset_y = 0.1f;
                float fence_position_z = (-grid_size_y / 2.0f) + entrance_size.z - offset_y;
                Vector3 fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + offset_x), 0.8f, fence_position_z);
                Quaternion fence_rotation = Quaternion.Euler(0, 0, 0);
                GameObject fence = Instantiate(fence_doubleentrance_tile, fence_position, fence_rotation, this.transform);
                fence_tiles.Add(fence);
                x_entr++;
                x_entr++;
            }
            else if(x_entr > x_entr_start + (int)entrance_size.x / 2 + 1)
            {
                float offset_x = 0.5f;
                float offset_y = 0.1f;
                float fence_position_z = (-grid_size_y / 2.0f) + entrance_size.z - offset_y;
                Vector3 fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + offset_x), 0.8f, fence_position_z);
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
    }

}
