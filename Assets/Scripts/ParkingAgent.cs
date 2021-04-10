using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents.Sensors;
using System;
using UnityEngine.Assertions;
using System.Net.Http.Headers;
using MLAgents.SideChannels;

[RequireComponent(typeof(Rigidbody))]
public class ParkingAgent : Agent
{
    public GameManager gameManager;
    public float motorForce;
    public float steerForce;
    public WheelCollider FrontLeft;
    public WheelCollider FrontRight;
    public WheelCollider BackLeft;
    public WheelCollider BackRight;

    public Transform parkingSpot;
    public bool showDebug = false;

    private Rigidbody rbody;

    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(parkingSpot);
        Assert.IsNotNull(gameManager);

        BackLeft.brakeTorque = 0;
        BackRight.brakeTorque = 0;
        rbody = GetComponent<Rigidbody>(); // composant requis
    }

    public bool IsParked()
    {
        Vector3 diffToSpot = this.transform.position - parkingSpot.transform.position;
        diffToSpot.y = 0;
        float distanceToSpot = diffToSpot.magnitude;
        float angle = Math.Min(Vector3.Angle(this.transform.forward, parkingSpot.transform.forward), Vector3.Angle(-this.transform.forward, parkingSpot.transform.forward));
        return distanceToSpot < .3f && angle < 15.0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target perspective position
        sensor.AddObservation(this.transform.InverseTransformPoint(parkingSpot.transform.position));

        // Target perspective direction
        Vector3 TargetDir = (parkingSpot.transform.position - this.transform.position).normalized;
        sensor.AddObservation(this.transform.InverseTransformDirection(TargetDir));

        // Agent perspective velocity
        sensor.AddObservation(this.transform.InverseTransformVector(rbody.velocity));

        // Angle with parking spot
        float angle = System.Math.Min(Vector3.Angle(this.transform.forward, parkingSpot.transform.forward), Vector3.Angle(-this.transform.forward, parkingSpot.transform.forward));
        sensor.AddObservation(angle);
    }

    public void OnNextLevel()
    {
        EndEpisode();
    }

    public override void OnActionReceived(float[] vectorAction)
    {

        float avancerReculer = vectorAction[0];
        float gaucheDroite = vectorAction[1];

        BackLeft.motorTorque = Mathf.Clamp(avancerReculer, -1, 1) * motorForce;
        BackRight.motorTorque = Mathf.Clamp(avancerReculer, -1, 1) * motorForce;

        FrontLeft.steerAngle = Mathf.Clamp(gaucheDroite, -1, 1) * steerForce;
        FrontRight.steerAngle = Mathf.Clamp(gaucheDroite, -1, 1) * steerForce;
    }

    public override float[] Heuristic()
    {
        var action = new float[4];
        action[0] = Input.GetAxis("Vertical");
        action[1] = Input.GetAxis("Horizontal");

        return action;
    }
}
