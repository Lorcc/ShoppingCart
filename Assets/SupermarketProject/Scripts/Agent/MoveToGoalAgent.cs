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
    private float agent_cameraspeed = 50f;
    private float agent_movespeed_force = 750f; 
    //private float agent_movespeed_velocity = 150f;
    private float ground_drag = 5f;

    Vector3 movement_direction;

    Rigidbody agent_rigidbody;

    private float collision_reward = 0f;

    private bool is_collided = false;

    private void Start()
    {
        agent_rigidbody = GetComponent<Rigidbody>();
        agent_rigidbody.freezeRotation = true;
    }

    public override void OnEpisodeBegin()
    {
        collision_reward = 0f;
        this.GetComponentInParent<SetupSupermarket>().setup_Supermarket();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        var local_velocity = transform.InverseTransformDirection(agent_rigidbody.velocity);
        var vector_distance = targetTransform.localPosition - transform.localPosition;
        sensor.AddObservation(local_velocity.x); // plus 1 float
        sensor.AddObservation(local_velocity.z); // plus 1 float
        sensor.AddObservation(transform.localPosition); // plus 3 Vector3
        sensor.AddObservation(targetTransform.localPosition); // plus 3 Vector3
        sensor.AddObservation(transform.localRotation); // plus 4 Quaternion
        sensor.AddObservation(vector_distance.magnitude); // plus 3 float
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 vector_distance = targetTransform.localPosition - transform.localPosition;
        float movement_x = actions.ContinuousActions[1];
        float rotation_y = actions.ContinuousActions[0];

        //Set very slow movement to zero
        if(movement_x < 0.05 && movement_x > -0.05) movement_x = 0f;

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

        //TODO change to be more consistant
        //right now if size of the map increases the negative reward will also increase
        //so get the current gridsize
        AddReward(-0.005f * vector_distance.magnitude * 0.1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
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
            //collision_reward -= 0.5f;
            //Debug.Log(collision_reward);
            AddReward(-0.5f);
            is_collided = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent<Goal>(out Goal component))
        {

        }
        else
        {
            is_collided = false;
        }
    }

    //gets called every 0.02 sec so 50 times per second
    private void FixedUpdate()
    {
        if (is_collided)
        {
            collision_reward -= 0.005f;
            AddReward(-0.005f);
            //Debug.Log(collision_reward);
        }
    }
}
