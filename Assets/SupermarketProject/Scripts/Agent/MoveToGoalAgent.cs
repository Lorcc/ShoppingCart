using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using System.IO;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private GameObject agent_camera;
    private float agent_cameraspeed = 50f;
    private float agent_movespeed_force = 750f; 
    //private float agent_movespeed_velocity = 150f;
    private float ground_drag = 1f;
    private float m_ForwardSpeed = 8f;
    Vector3 movement_direction;


    public List<Vector3> shortest_path = new List<Vector3>();
    Vector3 current_waypoint;

    Rigidbody agent_rigidbody;

    //Reward
    float m_Existential;
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
        this.GetComponentInParent<SetupSupermarketRepaired>().setup_Supermarket();
        current_waypoint = shortest_path.Last();
        m_Existential = 5f / MaxStep;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        var local_velocity = transform.InverseTransformDirection(agent_rigidbody.velocity);
        var vector_distance = targetTransform.localPosition - transform.localPosition;
        var vector_distance_waypoint = current_waypoint - transform.localPosition;
        sensor.AddObservation(local_velocity.x); // plus 1 float
        sensor.AddObservation(local_velocity.z); // plus 1 float
        sensor.AddObservation(transform.localPosition); // plus 3 Vector3
        sensor.AddObservation(targetTransform.localPosition); // plus 3 Vector3
        //sensor.AddObservation(transform.localRotation); // plus 4 Quaternion
        sensor.AddObservation(vector_distance.magnitude); // plus 1 float
        sensor.AddObservation(vector_distance_waypoint.magnitude);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        //Existential penalty for the agent
        collision_reward -= m_Existential;
        //Debug.Log(collision_reward);
        AddReward(-m_Existential);
        move_Agent_Discrete(actions.DiscreteActions);
    }

    /*public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }*/

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }
    }

    public void move_Action_Continuous(ActionSegment<float> act)
    {
        Vector3 vector_distance = targetTransform.localPosition - transform.localPosition;
        float movement_x = act[1];
        float rotation_y = act[0];

        //Set very slow movement to zero
        if (movement_x < 0.05 && movement_x > -0.05) movement_x = 0f;

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
        //collision_reward -= 0.005f * vector_distance.magnitude * 0.1f;
        //sAddReward(-0.005f * vector_distance.magnitude * 0.1f);
    }

    public void move_Agent_Discrete(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = act[0];
        var rotateAxis = act[1];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed * 0.8f;
                break;
        }
        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 40f);
        agent_rigidbody.AddForce(dirToGo, ForceMode.Force);
        agent_rigidbody.drag = ground_drag;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Goal>(out Goal component))
        {
            SetReward(1f);
            EndEpisode();
        }
        else if(other.TryGetComponent<Waypoint>(out Waypoint waypoint))
        {
            current_waypoint = get_next_waypoint(shortest_path, waypoint.transform.localPosition);
        }
        else
        {
            collision_reward -= 0.2f;
            AddReward(-0.2f);
            is_collided = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent<Goal>(out Goal component))
        {

        }
        else if(other.TryGetComponent<Waypoint>(out Waypoint waypoint))
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

    public Vector3 get_next_waypoint(List<Vector3> shortest_path, Vector3 old_waypoint)
    {
        Vector3 next_waypoint;
        bool waypoint_ahead = false;
        for (int i = 0; i < shortest_path.Count; i++)
        {
            if(shortest_path[i] == old_waypoint)
            {
                shortest_path.RemoveRange(i, shortest_path.Count - i);
                waypoint_ahead = true;
            }
        }
        if(shortest_path.Count == 0)
        {
            next_waypoint = targetTransform.localPosition;
        }
        else
        {
            next_waypoint = shortest_path.Last();
        }

        if (waypoint_ahead)
        {
            collision_reward += 0.2f;
            //Debug.Log(collision_reward);
            AddReward(0.2f);
        }
        return next_waypoint;
    }

    
}
