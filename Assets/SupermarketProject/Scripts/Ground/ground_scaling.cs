using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ground_scaling : MonoBehaviour
{
    public GameObject ground;
    public Renderer rend;
    [SerializeField] float scale_divisor;
    public void scale_Texture(Vector3 scale)
    {
        rend.sharedMaterial.mainTextureScale = new Vector2(scale.x/scale_divisor,scale.z/scale_divisor);
    }

    private void Awake()
    {
        if (scale_divisor <= 0)
        {
            Debug.LogError("Scale_divisor must be greater than 0. Set value to default 1.");
            scale_divisor = 1;
        }
        ground = this.transform.GetChild(0).gameObject;
        rend = ground.GetComponent<Renderer>();
    }
}
