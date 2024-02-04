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

    private float collision_reward = 0f;

    private bool is_heuristic = false;

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
        collision_reward = 0f;
        this.GetComponentInParent<SetupSupermarket>().setup_Supermarket();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        var localVelocity = transform.InverseTransformDirection(agent_rigidbody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        //Debug.Log(localVelocity.magnitude);
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
        Debug.Log(transform.localPosition);
        Debug.Log(targetTransform.localPosition);
        //sensor.AddObservation(transform.localRotation);
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
        if (is_heuristic == false)
        {
            movement_x -= 0.5f;
            rotation_y -= 0.5f;
        }

        //backwards speed is roughly 1/3 the speed the agent is driving forward
        if (movement_x < 0)
        {
            movement_x *= 0.6f;
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

        AddReward(-0.0025f);
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
            collision_reward -= 0.2f;
            //Debug.Log(collision_reward);
            AddReward(-0.2f);
        }
    }
}
