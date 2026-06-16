using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;

public class AntAgent : Agent
{
    [Header("Settings")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 180f;
    public float boundaryLimit = 23f;

    [HideInInspector] public int fruitsCollected = 0;
    [HideInInspector] public float totalReward = 0f;
    [HideInInspector] public int episodeCount = 0;

    private FoodSpawning foodSpawner;
    private Transform targetFood;

    void Start()
    {
        foodSpawner = FindObjectOfType<FoodSpawning>();
        if (foodSpawner == null)
            Debug.LogError("AntAgent: FoodSpawning not found in scene!");
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.rotation = Quaternion.identity;

        fruitsCollected = 0;
        totalReward = 0f;
        episodeCount++;

        if (foodSpawner != null)
            foodSpawner.SpawnFood();

        UpdateTarget();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);

        if (targetFood != null)
        {
            Vector3 dirToFood = (targetFood.position - transform.position).normalized;
            sensor.AddObservation(dirToFood.x);
            sensor.AddObservation(dirToFood.z);
            sensor.AddObservation(Vector3.Distance(transform.position, targetFood.position));
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float move = actions.ContinuousActions[0];
        float rotate = actions.ContinuousActions[1];

        transform.Translate(Vector3.forward * move * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up * rotate * rotateSpeed * Time.deltaTime);

        AddReward(-0.001f);

        if (transform.localPosition.y < -1f)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Vertical");
        actions[1] = Input.GetAxis("Horizontal");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            foodSpawner?.OnFoodEaten(other.gameObject);

            AddReward(1.0f);
            totalReward += 1.0f;
            fruitsCollected++;
            if (foodSpawner != null && foodSpawner.AllEaten)
            {
                AddReward(2.0f);
                totalReward += 2.0f;
                EndEpisode();
            }
            else
            {
                UpdateTarget();
            }
        }
    }

    private void UpdateTarget()
    {
        if (foodSpawner == null) return;

        List<Vector3> foodPositions = foodSpawner.GetActiveFoodPositions();
        if (foodPositions.Count == 0)
        {
            targetFood = null;
            return;
        }

        Vector3 closest = foodPositions[0];
        float minDist = Vector3.Distance(transform.position, closest);

        foreach (Vector3 pos in foodPositions)
        {
            float dist = Vector3.Distance(transform.position, pos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = pos;
            }
        }

        GameObject[] allFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in allFood)
        {
            if (Vector3.Distance(food.transform.position, closest) < 0.01f)
            {
                targetFood = food.transform;
                break;
            }
        }
    }
}