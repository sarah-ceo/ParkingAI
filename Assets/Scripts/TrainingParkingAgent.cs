using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;
//using System.Numerics;

public class TrainingParkingAgent : Agent
{
    Rigidbody rBody;
    public float motorForce;
    public float steerForce;
    public Transform Target;
    public WheelCollider FrontLeft;
    public WheelCollider FrontRight;
    public WheelCollider BackLeft;
    public WheelCollider BackRight;

    private bool has_collided;

    public TrainingLevelManager levelManager;

    void Start()
    {
        BackLeft.brakeTorque = 0;
        BackRight.brakeTorque = 0;
        rBody = GetComponent<Rigidbody>();
        has_collided = false;
        levelManager.BuildLevel();
        this.maxStep = 2000;
    }


    public override void OnEpisodeBegin()
    {
        float parkingFill = Random.Range(0.0f, 0.5f);
        levelManager.EndLevel();
        levelManager.InitLevel(this.gameObject, parkingFill);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target perspective position
        sensor.AddObservation(this.transform.InverseTransformPoint(Target.transform.position));

        // Target perspective direction
        Vector3 TargetDir = (Target.transform.position - this.transform.position).normalized;
        sensor.AddObservation(this.transform.InverseTransformDirection(TargetDir));

        // Agent perspective velocity
        sensor.AddObservation(this.transform.InverseTransformVector(rBody.velocity));

        // Angle with parking spot
        float angle = System.Math.Min(Vector3.Angle(this.transform.forward, Target.transform.forward), Vector3.Angle(-this.transform.forward, Target.transform.forward));
        sensor.AddObservation(angle);

    }
    public bool IsParked()
    {
        Vector3 diffToSpot = this.transform.position - Target.transform.position;
        diffToSpot.y = 0;
        float distanceToSpot = diffToSpot.magnitude;
        float angle = System.Math.Min(Vector3.Angle(this.transform.forward, Target.transform.forward), Vector3.Angle(-this.transform.forward, Target.transform.forward));
        return distanceToSpot < .3f && angle < 15f; // values to reach are .3 and 15
        //Gonna start with 1.0 and 45, .75 and 30, .5 and 20, .3 and 15
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        float avancerReculer = vectorAction[0];
        float gaucheDroite = vectorAction[1];

        BackLeft.motorTorque = Mathf.Clamp(avancerReculer, -1, 1) * motorForce;
        BackRight.motorTorque = Mathf.Clamp(avancerReculer, -1, 1) * motorForce;

        FrontLeft.steerAngle = Mathf.Clamp(gaucheDroite, -1, 1) * steerForce;
        FrontRight.steerAngle = Mathf.Clamp(gaucheDroite, -1, 1) * steerForce;

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Reached target
        if (IsParked())
        {
            SetReward(1.0f);
            EndEpisode();
        }
        // Collided with obstacle
        else if (has_collided)
        {
            has_collided = false;
            SetReward(-1.0f);
            EndEpisode();
        }
        // Fell
        else if (this.transform.localPosition.y < -1 || this.transform.rotation.z > 60 || this.transform.rotation.z < -60)
        {
            EndEpisode();
        }
    }

    public override float[] Heuristic()
    {
        var action = new float[2];
        action[0] = Input.GetAxis("Vertical");
        action[1] = Input.GetAxis("Horizontal");

        return action;
    }

    public void OnCollisionEnter(Collision collision)
    {

        if (collision.rigidbody != null && collision.rigidbody.transform.CompareTag("obstacle")) // si l'objet est un obstacle, alors pénalité.
                                                                                                 // Pour accéder au tag, on passe par le rigidbody puisque le tag est associé au rigidbody, pas forcément au collider.
        {
            has_collided = true;
        }
    }
}
