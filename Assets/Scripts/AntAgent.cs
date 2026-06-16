using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class AntAgent : Agent, IKillable
{
    [SerializeField] private FruitSpawner fruitSpawner;
    [Header("Settings")]
    public Transform[] fruits;
    public float moveSpeed = 3f;
    public float rotateSpeed = 180f;
    public float boundaryLimit = 23f;

    [HideInInspector] public int fruitsCollected = 0;
    [HideInInspector] public float totalReward = 0f;
    [HideInInspector] public int episodeCount = 0;

    private Transform targetFruit;
    private int currentFruitIndex = 0;

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.rotation = Quaternion.identity;

        fruitsCollected = 0;
        totalReward = 0f;
        episodeCount++;

        if (fruitSpawner != null)
            fruitSpawner.SpawnFruits();

        currentFruitIndex = 0;

        if (fruits != null && fruits.Length > 0)
            targetFruit = fruits[currentFruitIndex];
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);

        if (targetFruit != null)
        {
            Vector3 dirToFruit = (targetFruit.position - transform.position).normalized;
            sensor.AddObservation(dirToFruit.x);
            sensor.AddObservation(dirToFruit.z);
            sensor.AddObservation(Vector3.Distance(transform.position, targetFruit.position));
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

        float distanceFromCenter = Vector3.Distance(
            new Vector3(transform.localPosition.x, 0, transform.localPosition.z),
            Vector3.zero
        );

        if (distanceFromCenter > boundaryLimit || transform.localPosition.y < -1f)
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
        if (other.gameObject.CompareTag("Fruit"))
        {
            Destroy(other.gameObject);

            AddReward(1.0f);
            totalReward += 1.0f;
            fruitsCollected++;

            currentFruitIndex++;
            if (currentFruitIndex >= fruits.Length)
            {
                AddReward(2.0f);
                totalReward += 2.0f;
                EndEpisode();
            }
            else
            {
                targetFruit = fruits[currentFruitIndex];
            }
        }
    }

    public void GetCaught()
    {
        AddReward(-1f);
        EndEpisode();
        gameObject.SetActive(false);
        GameWorld.Instance?.OnAntKilled();
    }
}