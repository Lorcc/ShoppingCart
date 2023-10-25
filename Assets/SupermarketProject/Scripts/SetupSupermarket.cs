using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSupermarket : MonoBehaviour
{
    [SerializeField] private int min_ground_size = 3;
    [SerializeField] private int max_ground_size = 10;
    private bool[,] test;

    public void calculate_Grid()
    {
        int grid_size_x = Random.Range(min_ground_size, max_ground_size);
        int grid_size_y = Random.Range(min_ground_size, max_ground_size);
        GameObject ground = this.transform.Find("Ground").gameObject;
        ground.transform.localScale = new Vector3(grid_size_x, 0.5f, grid_size_y);
        Debug.Log(grid_size_x);
        Debug.Log(grid_size_y);
    }


    // Start is called before the first frame update
    void Start()
    {
        calculate_Grid();
        Debug.Log(test);
    } 


    // Update is called once per frame
    void Update()
    {
        
    }
}
