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
        //Spawn entrance fences
        for (int x_entr = (int)grid_size_x - (int)entrance_size.x; x_entr < grid_size_x; x_entr++)
        {
            float offset_x = 0.5f;
            float offset_y = 0.05f;
            float fence_position_z = (-grid_size_y / 2.0f) + entrance_size.z - offset_y;
            Vector3 fence_position = this.transform.position + new Vector3((x_entr - (grid_size_x / 2.0f) + offset_x), 0.8f, fence_position_z);
            Quaternion shelve_rotation = Quaternion.Euler(0, 0, 0);
            GameObject fence = Instantiate(fence_tile, fence_position, shelve_rotation, this.transform);
            fence_tiles.Add(fence);
            //agent_pos.x = entrance_position.x + entrance_scale.x / 2.0f - 1.5f;
            //agent_pos.y = entrance_position.z + entrance_scale.z / 2.0f + 0.5f;
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
