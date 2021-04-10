using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
    public LevelManager levelManager;
    public GameObject[] cars;
    public float levelTime = 20.0f;

    private Dictionary<GameObject, int> scores;
    private Dictionary<GameObject, ParkingAgent> carAgents;

    public void Start()
    {
        Assert.IsNotNull(levelManager, "Le GameManager doit avoir un LevelManager pour pouvoir jouer !");

        carAgents = new Dictionary<GameObject, ParkingAgent>();
        foreach (GameObject car in cars)
        {
            carAgents[car] = car.GetComponent<ParkingAgent>();
        }

        InitScores();
        InitCarAgents();

        levelManager.BuildLevel();
        levelManager.InitLevel(cars);
    }

    public int PlayersCount()
    {
        return cars.Length;
    }

    public void FixedUpdate()
    {
        List<GameObject> scoringAgents = CheckScoringConditions();

        foreach(GameObject agent in scoringAgents)
        {
            scores[agent]++;
        }

        if(scoringAgents.Count > 0 || RemainingTime() <= 0)
        {
            // arrête le niveau en cours et lance le prochain niveau
            NotifyAgentsNextLevel();
            levelManager.EndLevel();
            levelManager.InitLevel(cars);
        }
    }
    public Vector2 LevelSize()
    {
        return levelManager.LevelSize();
    }

    public void SetParkingFill(float fill)
    {
        levelManager.parkingFill = fill;
    }

    public float RemainingTime()
    {
        Assert.IsTrue(levelManager.initDate >= 0, "Un appel a été fait à gameManager.RemainingTime alors que levelManager.initDate n'a jamais été setté.");

        return levelTime - (Time.time - levelManager.initDate);
    }

    public int PlayerScore(int i)
    {
        return scores[cars[i]];
    }

    public string PlayerName(int i)
    {
        return cars[i].name;
    }

    private void InitCarAgents()
    {
        // on veut que les agents se traversent les uns les autres
        foreach(GameObject car in cars)
        {
            Collider[] colliders = car.GetComponentsInChildren<Collider>();

            foreach(Collider collider in colliders)
            {
                foreach (GameObject othercar in cars)
                {
                    if (car == othercar)
                        continue;

                    Collider[] otherColliders = othercar.GetComponentsInChildren<Collider>();

                    foreach (Collider otherCollider in otherColliders)
                    {
                        Physics.IgnoreCollision(collider, otherCollider);
                    }
                }
            }
        }
    }

    private void NotifyAgentsNextLevel()
    {
        foreach(GameObject car in cars)
        {
            carAgents[car].OnNextLevel();
        }
    }

    private List<GameObject> CheckScoringConditions()
    {
        List<GameObject> scoringAgents = new List<GameObject>();

        foreach(GameObject car in cars)
        {
            if (carAgents[car].IsParked())
                scoringAgents.Add(car);
        }

        return scoringAgents;
    }

    private void InitScores()
    {
        scores = new Dictionary<GameObject, int>();
        foreach (GameObject car in cars)
            scores[car] = 0;
    }
}
