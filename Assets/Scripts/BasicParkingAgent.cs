using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;

public class BasicParkingAgent : Agent
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

    private List<GameObject> parkedCarInstances;
    public GameObject[] carPrefabs;

    private bool isInit;

    void Start()
    {
        BackLeft.brakeTorque = 0;
        BackRight.brakeTorque = 0;
        rBody = GetComponent<Rigidbody>();
        has_collided = false;
        isInit = true;
    }

    
    public override void OnEpisodeBegin()
    {
        if (!isInit)
        {
            foreach (GameObject car in parkedCarInstances)
            {
                Destroy(car);
            }
            parkedCarInstances = null;
        }

        isInit = false;

        // If the Agent collided or fell, reset position
        if (has_collided || this.transform.localPosition.y < -1 || this.transform.rotation.z > 60 || this.transform.rotation.z < -60)
        {
            has_collided = false;
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
            this.transform.localRotation = Quaternion.Euler(0.0f, this.transform.eulerAngles.y, 0.0f);
        }

        // Move the target to a new spot
        Target.localPosition = new Vector3(Random.value * 16 - 8, 0.01f, Random.value * 16 - 8);
        Target.transform.localRotation = Quaternion.Euler(new Vector3(0, Random.Range(0f, 360f), 0));

        // Spawn obstacles
        parkedCarInstances = new List<GameObject>();

        int random_nb_obstacles = Random.Range(0, 6);

        for (int i=0; i<random_nb_obstacles; i++)
        {
            //Random position
            Vector3 random_position = new Vector3(Random.value * 14 - 7, 0.2f, Random.value * 14 - 7);
            
            while (Conflicting(random_position))
            {
                random_position = new Vector3(Random.value * 14 - 7, 0.2f, Random.value * 14 - 7);
            }

            // Spawn
            GameObject newCar = Instantiate(carPrefabs[Random.Range(0, carPrefabs.Length)], this.transform, false);
            
            newCar.transform.localPosition = random_position;
            newCar.transform.localRotation = Quaternion.Euler(new Vector3(0, Random.Range(0f, 360f), 0));

            Rigidbody rb = newCar.GetComponent<Rigidbody>();

            IEnumerator coroutine = FreezePosRot(rb, .5f);
            StartCoroutine(coroutine);

            if (CalculateDistance(newCar.transform.position, Target.transform.position) < 3.0f)
            {
                Destroy(newCar);
            } else
            {
                parkedCarInstances.Add(newCar);
            }

        }

    }

IEnumerator FreezePosRot(Rigidbody rb, float waitingTime)
{
    // freeze le position et la rotation pour une courte durée. Seule la postion sur l'axe Y est laissée pour que la voiture ne flotte pas dans les airs
    // permet notament de corriger un bug : les voitures peuvent avoir un impulsion au moment où elles apparaissent

    rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

    yield return new WaitForSeconds(waitingTime);

    if (rb)
            rb.constraints = RigidbodyConstraints.None;
}

private float CalculateDistance(Vector3 pos1, Vector3 pos2)
    {
        Vector3 diffToSpot = pos1 - pos2;
        diffToSpot.y = 0;
        return diffToSpot.magnitude;
    }

    private bool Conflicting(Vector3 pos)
    {
        // Check that is far away from parkingSpot
        if (CalculateDistance(pos, Target.transform.localPosition) < 6.0f)
        {
            return true;
        }

        // from our AI car
        if (CalculateDistance(pos, this.transform.localPosition) < 4.0f)
        {
            return true;
        }

        // and other cars
        foreach (GameObject car in parkedCarInstances)
        {
            if (CalculateDistance(pos, car.transform.localPosition) < 4.0f)
            {
                return true;
            }
        }

        return false;
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
