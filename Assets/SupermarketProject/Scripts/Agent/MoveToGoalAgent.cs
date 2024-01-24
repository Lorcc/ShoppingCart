using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private GameObject agent_camera;
    private float agent_cameraspeed = 100f; //100f
    private float agent_movespeed_force = 1500f; // 1500f
    //private float agent_movespeed_velocity = 150f;
    private float ground_drag = 5f;

    Vector3 movement_direction;

    Rigidbody agent_rigidbody;

    private bool is_heuristic = false;

    //testing
    private float reward_count = 0f;
    private void Start()
    {
        agent_rigidbody = GetComponent<Rigidbody>();
        agent_rigidbody.freezeRotation = true;

        switch (Unity.MLAgents.Policies.BehaviorType.HeuristicOnly.ToString())
        {
            case "HeuristicOnly":
                is_heuristic = true;
                break;
            case "InferenceOnly":
                break;
            default:
                break;
                

        }
    }

    public override void OnEpisodeBegin()
    {
        reward_count = 0f;
        this.GetComponentInParent<SetupSupermarket>().setup_Supermarket();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        // old movement
        /*float moveX = actions.ContinuousActions[0]; 
        float moveZ = actions.ContinuousActions[1];

        float moveSpeed = 5f;
        transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        */

        float movement_x = actions.ContinuousActions[1];
        float rotation_y = actions.ContinuousActions[0];

        // to get values between -0,5 until 0,5
        if (is_heuristic != true)
        {
            movement_x -= 0.5f;
            rotation_y -= 0.5f;
        }

        Vector3 rotation_direction = new Vector3(0, rotation_y, 0);
        Quaternion delta_rotation = Quaternion.Euler(rotation_direction * Time.fixedDeltaTime * agent_cameraspeed);
        agent_rigidbody.MoveRotation(agent_rigidbody.rotation * delta_rotation);
        //agent_rigidbody.angularDrag = ground_angular_drag;

        Vector3 cam_f = agent_camera.transform.forward;
        cam_f.y = 0;
        cam_f = cam_f.normalized;

        movement_direction = cam_f * movement_x;
        //agent_rigidbody.velocity = movement_direction * Time.fixedDeltaTime * agent_movespeed_velocity;
        agent_rigidbody.AddForce(movement_direction * Time.fixedDeltaTime * agent_movespeed_force, ForceMode.Force);

        agent_rigidbody.drag = ground_drag;  
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal") * 0.5f;
        continuousActions[1] = Input.GetAxisRaw("Vertical") * 0.5f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Goal>(out Goal component))
        {
            SetReward(1f);
            EndEpisode();
        }
        else
        {
            reward_count -= 0.5f;
            Debug.Log(reward_count);
            AddReward(-0.5f);
        }
    }


}
