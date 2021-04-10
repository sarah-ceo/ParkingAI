using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class TrainingLevelManager : MonoBehaviour
{
    public GameObject[] carPrefabs;
    public GameObject targetObject;
    public Material whiteLineMaterial;
    public Material groundMaterial;
    public Material wallMaterial;
    /*[Space(30)]
    public GameObject groundObject;
    public GameObject spawnObject;*/
    [Space(30)]

    public float averageCarWidth;
    public float averageCarLength;
    [RangeAttribute(0.05f, 0.3f)]
    public float lineWidth;
    public float parkingSlotMargin;
    [RangeAttribute(0.0f, 1.0f)]
    public float parkingWidth = 10.0f;
    public float parkingHeight = 10.0f;

    private GameObject linePrefab;
    private List<GameObject> lineInstances;
    private float linePrefabDefaultWidth;
    private float linePrefabDefaultHeight;

    private GameObject ground;

    /***********/
    //Ajout des murs autour du parking
    private GameObject north_wall;
    private GameObject south_wall;
    private GameObject west_wall;
    private GameObject east_wall;
    /***********/

    private List<GameObject> parkedCarInstances;
    List<PositionRotation> slotList;

    private bool isInit = false;
    private bool isBuilt = false;
    public float initDate { get; private set; } = -1;

    public void BuildLevel()
    {
        InitializeLinePrefab();
        SpawnGround();
        /***********/
        SpawnWalls();
        /***********/
        slotList = InitParkingLayout();
        isBuilt = true;
    }

    public bool IsBuilt()
    {
        return isBuilt;
    }

    public Vector2 LevelSize()
    {
        return new Vector2(parkingWidth, parkingHeight);
    }

    public void InitLevel(GameObject carAgent, float parkingFill)
    {
        if (!isInit)
        {
            SpawnCarsAndTarget(carAgent, parkingFill);
        }
        isInit = true;

        initDate = Time.time;
    }

    public void EndLevel()
    {
        if (isInit)
        {
            foreach (GameObject car in parkedCarInstances)
            {
                Destroy(car);
            }
            parkedCarInstances = null;
        }
        isInit = false;
    }

    private void SpawnGround()
    {
        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.parent = this.transform.parent;
        ground.GetComponent<Renderer>().material = groundMaterial;
        ground.name = "ground";

        Vector3 scale = ground.transform.localScale;
        scale.z *= parkingHeight / 10.0f;
        scale.x *= parkingWidth / 10.0f;
        ground.transform.localScale = scale;
        ground.transform.localPosition = Vector3.zero;
    }

    /***********/
    // Spawn des murs autour du parking
    private void SpawnWalls()
    {
        north_wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        north_wall.transform.parent = this.transform.parent;
        north_wall.GetComponent<Renderer>().material = wallMaterial;
        north_wall.name = "north_wall";
        north_wall.transform.localScale = new Vector3(parkingWidth, 0.2f, 0.1f);
        north_wall.transform.localPosition = new Vector3(0, 0.1f, parkingHeight / 2);
        north_wall.transform.tag = "obstacle";
        Rigidbody nwbody = north_wall.AddComponent<Rigidbody>();
        nwbody.isKinematic = true;

        south_wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        south_wall.transform.parent = this.transform.parent;
        south_wall.GetComponent<Renderer>().material = wallMaterial;
        south_wall.name = "south_wall";
        south_wall.transform.localScale = new Vector3(parkingWidth, 0.2f, 0.1f);
        south_wall.transform.localPosition = new Vector3(0, 0.1f, -parkingHeight / 2);
        south_wall.transform.tag = "obstacle";
        Rigidbody swbody = south_wall.AddComponent<Rigidbody>();
        swbody.isKinematic = true;

        west_wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        west_wall.transform.parent = this.transform.parent;
        west_wall.GetComponent<Renderer>().material = wallMaterial;
        west_wall.name = "west_wall";
        west_wall.transform.localScale = new Vector3(0.1f, 0.2f, parkingHeight);
        west_wall.transform.localPosition = new Vector3(-parkingWidth / 2, 0.1f, 0);
        west_wall.transform.tag = "obstacle";
        Rigidbody wwbody = west_wall.AddComponent<Rigidbody>();
        wwbody.isKinematic = true;

        east_wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        east_wall.transform.parent = this.transform.parent;
        east_wall.GetComponent<Renderer>().material = wallMaterial;
        east_wall.name = "east_wall";
        east_wall.transform.localScale = new Vector3(0.1f, 0.2f, parkingHeight);
        east_wall.transform.localPosition = new Vector3(parkingWidth / 2, 0.1f, 0);
        east_wall.transform.tag = "obstacle";
        Rigidbody ewbody = east_wall.AddComponent<Rigidbody>();
        ewbody.isKinematic = true;

    }

    /***********/

    private void SpawnCarsAndTarget(GameObject carAgent, float parkingFill)
    {
        Assert.IsNotNull(slotList, "SpawnCarsAndTarget : slotList est null ! Il faut construire le niveau avant de faire apparaître les voitures.");
        Assert.IsTrue(slotList.Count >= 2, "SpawnCarsAndTarget : il doit y avoir au moins 2 places de parking : une pour la destination, et une pour faire spawn l'agent.");
        Assert.IsNull(parkedCarInstances, "SpawnCarsAndTarget : on essaie de faire apparaître des voitures alors que les instances précédentes n'ont pas été détruites");

        parkedCarInstances = new List<GameObject>();

        // spawn du target, aka parkingSpot
        int targetSlotIdx = -1;
        if (targetObject != null)
        {
            targetSlotIdx = UnityEngine.Random.Range(0, slotList.Count);
            PositionRotation targetTransform = slotList[targetSlotIdx];
            targetObject.transform.parent = this.transform.parent;
            targetObject.transform.localPosition = targetTransform.position + Vector3.up * 0.01f;
            targetObject.transform.rotation = Quaternion.AngleAxis(targetTransform.rotation.y, Vector3.up);
        }

        // spawn des agents
        int carAgentSlotIdx = -1;
        if (carAgent != null)
        {
            do // trouve un slot différent du target
            {
                carAgentSlotIdx = UnityEngine.Random.Range(0, slotList.Count);
            } while (carAgentSlotIdx == targetSlotIdx);

            PositionRotation carAgentTransform = slotList[carAgentSlotIdx];


            /***********/
            /*
            // spawn des agents pour être en dehors du parking
            float[] top_bottom = { 
                UnityEngine.Random.Range(-0.5f*parkingHeight + averageCarLength, -0.25f*parkingHeight - averageCarLength), 
                UnityEngine.Random.Range(0.25f*parkingHeight + averageCarLength, 0.5f*parkingHeight - averageCarLength) 
            };
            float zvalue = top_bottom[UnityEngine.Random.Range(0,2)];
            Vector3 pos = new Vector3(0f, 0f, zvalue);
            //Vector3 rot = new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            Vector3 rot = Vector3.zero;
            if (zvalue > 0) {
                rot = new Vector3(0, 180, 0);
            }
            carAgentTransform = new PositionRotation(pos, rot);
            */
            /***********/

            InitCarPosition(carAgent, carAgentTransform);
        }

        // spawn des voitures fixes qui occupent le parking
        for (int i = 0; i < slotList.Count; i++)
        {
            if (i == targetSlotIdx || i == carAgentSlotIdx) // on ne veut pas remplir la place de parking de l'agent ni de la destination
                continue;

            if (UnityEngine.Random.Range(0.0f, 1.0f) < parkingFill)
            {
                GameObject newCar = Instantiate(carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)]);
                InitCarPosition(newCar, slotList[i]);
                parkedCarInstances.Add(newCar);
            }
        }
    }

    private void InitCarPosition(GameObject carAgent, PositionRotation carAgentTransform)
    {
        float spawnHeight = 1f;
        float waitingTime = .5f;

        carAgent.transform.parent = this.transform.parent;
        carAgent.transform.localPosition = carAgentTransform.position + Vector3.up * spawnHeight;
        carAgent.transform.rotation = Quaternion.AngleAxis(carAgentTransform.rotation.y, Vector3.up);

        Rigidbody rb = carAgent.GetComponent<Rigidbody>();

        IEnumerator coroutine = FreezePosRot(rb, waitingTime);
        StartCoroutine(coroutine);
    }

    IEnumerator FreezePosRot(Rigidbody rb, float waitingTime)
    {
        // freeze le position et la rotation pour une courte durée. Seule la postion sur l'axe Y est laissée pour que la voiture ne flotte pas dans les airs
        // permet notament de corriger un bug : les voitures peuvent avoir un impulsion au moment où elles apparaissent

        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

        yield return new WaitForSeconds(waitingTime);

        rb.constraints = RigidbodyConstraints.None;
    }

    private void InitializeLinePrefab()
    {
        linePrefab = GameObject.CreatePrimitive(PrimitiveType.Plane);
        linePrefab.GetComponent<Collider>().enabled = false;
        linePrefab.GetComponent<Renderer>().material = whiteLineMaterial;
        linePrefab.name = "line";
        linePrefab.SetActive(false);
        Vector3 lineSize = new Vector3(10, 0, 10);  // linePrefab.GetComponent<Collider>().bounds.size; // bounds ne semble pas être à jour avant le premier rendu
        linePrefabDefaultHeight = lineSize.z;
        linePrefabDefaultWidth = lineSize.x;
        Assert.IsTrue(linePrefabDefaultHeight > 0);
        Assert.IsTrue(linePrefabDefaultWidth > 0);
    }

    private List<PositionRotation> InitParkingLayout() // renvoi des transforms de place de parking
    {
        Assert.IsNull(lineInstances, "InitParkingLayout: On veut organiser la parking alors que cette étape a déjà été faite.");

        lineInstances = new List<GameObject>(); // rempli dans DrawLine
        List<PositionRotation> slotList = new List<PositionRotation>();

        int nbDriveways = 1; // nombre d'allées. Au moins 1.
        float totalSlotRowWidth = 0f; // largeur total de l'ensemble des lignes de places de parking
        totalSlotRowWidth += 2 * (lineWidth + averageCarLength + 2 * parkingSlotMargin); // Les 2 rangées de places sur les 2 côtés.
        float totalSlotRowWidthIncrement = 2 * (averageCarLength + 2 * parkingSlotMargin) + lineWidth; // largeur de 2 rangées collées à ajouter

        while ((parkingWidth - (totalSlotRowWidth + totalSlotRowWidthIncrement)) / (nbDriveways + 1) > averageCarWidth + parkingSlotMargin * 2) // tant qu'on a la place pour ajouter des rangées de parking
        {
            totalSlotRowWidth += totalSlotRowWidthIncrement;
            nbDriveways++;
        }
        int nbSlots = (int)(parkingHeight / (averageCarWidth + 2 * parkingSlotMargin + lineWidth));

        /***********/
        /*
        //Divisé le parkingHeight par 2 à la ligne suivante pour remplir seulement la moitié du parking de places
        nbSlots = (int)(parkingHeight/2 / (averageCarWidth + 2 * parkingSlotMargin + lineWidth));
        */
        /***********/

        //Debug.Log("Nb slots : " + nbSlots + " (" + parkingHeight / (averageCarWidth + 2 * parkingSlotMargin + lineWidth) + ")");
        slotList.AddRange(AddLeftSlotRow(Vector3.left * (parkingWidth / 2 - lineWidth / 2), nbSlots));
        slotList.AddRange(AddRightSlotRow(Vector3.right * (parkingWidth / 2 - lineWidth / 2), nbSlots));

        if (nbDriveways > 1)
        {
            float drivewayWidth = (parkingWidth - totalSlotRowWidth) / nbDriveways;
            Vector3 step = Vector3.right * (lineWidth + 2 * (averageCarLength + 2 * parkingSlotMargin) + drivewayWidth);
            Vector3 currentAnchor = Vector3.left * (parkingWidth / 2) // gauche du parking (axe X-)
                + Vector3.right * (lineWidth + averageCarLength + 2 * parkingSlotMargin // décalage de la rangée de gauche
                + drivewayWidth // décalage de la première allée
                + averageCarLength + 2 * parkingSlotMargin + lineWidth / 2); // décalage de la première double rangée
            for (int i = 1; i < nbDriveways; i++)
            {
                slotList.AddRange(AddDoubleSlotRow(currentAnchor, nbSlots));
                currentAnchor += step;
            }
        }

        return slotList;
    }

    private List<PositionRotation> AddLeftSlotRow(Vector3 anchor, int n) // anchor : ancrage au milieu à gauche, n : nombre de places de parking dans la rangée. Renvoi des transforms de places de parking
    {
        List<PositionRotation> slots = new List<PositionRotation>();

        float rowWidth = averageCarLength + 2 * parkingSlotMargin + lineWidth;
        float totalHeight = (n + 1) * lineWidth + n * (averageCarWidth + 2 * parkingSlotMargin);
        DrawLine(anchor + Vector3.forward * totalHeight / 2 + Vector3.right * lineWidth / 2,
            anchor - Vector3.forward * totalHeight / 2 + Vector3.right * lineWidth / 2);
        GameObject[] lines = AddSeparationLines(anchor + Vector3.right * rowWidth / 2, rowWidth, totalHeight, n + 1);

        for (int i = 0; i < n; i++) // rappel : il y a n+1 lignes, donc en itérant jusqu'à n on peut toujours accéder au i+1 ème élément
        {
            slots.Add(new PositionRotation((lines[i].transform.localPosition + lines[i + 1].transform.localPosition) / 2,
                new Vector3(0, Vector3.SignedAngle(Vector3.forward, Vector3.left, Vector3.up), 0)));
        }

        return slots;
    }

    private List<PositionRotation> AddRightSlotRow(Vector3 anchor, int n)
    {
        List<PositionRotation> slots = new List<PositionRotation>();

        float rowWidth = averageCarLength + 2 * parkingSlotMargin + lineWidth;
        float totalHeight = (n + 1) * lineWidth + n * (averageCarWidth + 2 * parkingSlotMargin);
        DrawLine(anchor + Vector3.forward * totalHeight / 2 + Vector3.left * lineWidth / 2,
            anchor - Vector3.forward * totalHeight / 2 + Vector3.left * lineWidth / 2);
        GameObject[] lines = AddSeparationLines(anchor + Vector3.left * rowWidth / 2, rowWidth, totalHeight, n + 1);

        for (int i = 0; i < n; i++) // rappel : il y a n+1 lignes, donc en itérant jusqu'à n on peut toujours accéder au i+1 ème élément
        {
            slots.Add(new PositionRotation((lines[i].transform.localPosition + lines[i + 1].transform.localPosition) / 2,
                new Vector3(0, Vector3.SignedAngle(Vector3.forward, Vector3.right, Vector3.up), 0)));
        }

        return slots;
    }

    private List<PositionRotation> AddDoubleSlotRow(Vector3 anchor, int n)
    {
        List<PositionRotation> slots = new List<PositionRotation>();

        float rowWidth = averageCarLength + 2 * parkingSlotMargin;
        float totalHeight = (n + 1) * lineWidth + n * (averageCarWidth + 2 * parkingSlotMargin);
        DrawLine(anchor + Vector3.forward * totalHeight / 2, anchor - Vector3.forward * totalHeight / 2);
        GameObject[] lines = AddSeparationLines(anchor, 2 * rowWidth + lineWidth, totalHeight, n + 1);

        for (int i = 0; i < n; i++) // rappel : il y a n+1 lignes, donc en itérant jusqu'à n on peut toujours accéder au i+1 ème élément
        {
            // pour chaque paire de lignes, on créé  2 places.
            Vector3 middle = (lines[i].transform.localPosition + lines[i + 1].transform.localPosition) / 2;
            Vector3 offset = Vector3.right * (lineWidth / 2 + parkingSlotMargin + averageCarLength / 2);
            slots.Add(new PositionRotation(middle + offset,
                new Vector3(0, Vector3.SignedAngle(Vector3.forward, Vector3.left, Vector3.up), 0)));
            slots.Add(new PositionRotation(middle - offset,
                new Vector3(0, Vector3.SignedAngle(Vector3.forward, Vector3.right, Vector3.up), 0)));
        }

        return slots;
    }

    private GameObject[] AddSeparationLines(Vector3 anchor, float rowWidth, float totalHeight, int n) // anchor : ancrage au milieu, n : nombre de lignes
    {
        GameObject[] lines = new GameObject[n];
        int idx = 0;

        float step = totalHeight / (n - 1);
        for (int i = 0; i < n; i++)
        {
            GameObject line = DrawLine(anchor + Vector3.forward * totalHeight / 2 - Vector3.forward * i * step + Vector3.right * rowWidth / 2,
                anchor + Vector3.forward * totalHeight / 2 - Vector3.forward * i * step - Vector3.right * rowWidth / 2);
            lines[idx++] = line;
        }

        return lines;
    }

    private GameObject DrawLine(Vector3 start, Vector3 end)
    {
        float height = (start - end).magnitude;
        GameObject line = Instantiate(linePrefab);
        line.transform.parent = this.transform.parent;
        lineInstances.Add(line);
        line.SetActive(true);

        // scaling
        Vector3 scale = line.transform.localScale;
        scale.z *= height / linePrefabDefaultWidth;
        scale.x *= lineWidth / linePrefabDefaultHeight;
        line.transform.localScale = scale;

        // setting position
        line.transform.localPosition = (start + end) / 2 + Vector3.up * 0.001f;

        // rotating
        float angle = Vector3.Angle(Vector3.ProjectOnPlane(end - start, Vector3.up), Vector3.forward);
        line.transform.eulerAngles = new Vector3(0, angle, 0);

        return line;
    }
}
