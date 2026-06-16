using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(NavMeshAgent))]
public class AntAI : Agent, IKillable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float turnSpeed = 180f;

    [Header("Vision")]
    [SerializeField] private float visionRange = 6f;
    [SerializeField] private float visionAngle = 120f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask foodMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Eating")]
    [SerializeField] private float eatRange = 0.5f;

    [Header("Rewards")]
    [SerializeField] private float rewardEat = 10f;
    [SerializeField] private float rewardLosPerSecond = 0.1f;
    [SerializeField] private float rewardApproach = 0.05f;
    [SerializeField] private float penaltyCaught = -5f;

    private NavMeshAgent nav;
    private FoodSpawning foodManager;

    private enum AntState { Wander, SeekFood, EvadePlayer }
    private AntState state = AntState.Wander;

    private Transform targetFood;
    private Transform lastKnownPlayer;
    private bool playerIsVisible;
    private float distToFoodPrevFrame = float.MaxValue;
    private Vector3 wanderDestination;

    public override void Initialize()
    {
        nav = GetComponent<NavMeshAgent>();
        nav.speed = moveSpeed;
        nav.angularSpeed = turnSpeed;
        foodManager = FindObjectOfType<FoodSpawning>();
    }

    public override void OnEpisodeBegin()
    {
        targetFood = null;
        lastKnownPlayer = null;
        playerIsVisible = false;
        distToFoodPrevFrame = float.MaxValue;
        state = AntState.Wander;

        if (nav.isOnNavMesh)
            nav.ResetPath();
    }

    

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(nav.velocity.normalized);

        if (targetFood != null)
        {
            Vector3 toFood = targetFood.position - transform.position;
            sensor.AddObservation(toFood.normalized);
            sensor.AddObservation(Mathf.Clamp01(toFood.magnitude / visionRange));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(1f);
        }

        sensor.AddObservation(playerIsVisible ? 1f : 0f);

        if (lastKnownPlayer != null)
        {
            Vector3 toPlayer = lastKnownPlayer.position - transform.position;
            sensor.AddObservation(toPlayer.normalized);
            sensor.AddObservation(Mathf.Clamp01(toPlayer.magnitude / visionRange));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(1f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        TickFSM();
        GivePerFrameRewards();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
    }

    private void TickFSM()
    {
        RefreshFoodTarget();
        RefreshPlayerVisibility();

        switch (state)
        {
            case AntState.Wander:
                DoWander();
                if (playerIsVisible) ChangeState(AntState.EvadePlayer);
                else if (targetFood != null) ChangeState(AntState.SeekFood);
                break;

            case AntState.SeekFood:
                DoSeekFood();
                if (playerIsVisible) ChangeState(AntState.EvadePlayer);
                else if (targetFood == null) ChangeState(AntState.Wander);
                break;

            case AntState.EvadePlayer:
                DoEvadePlayer();
                if (!playerIsVisible)
                    ChangeState(targetFood != null ? AntState.SeekFood : AntState.Wander);
                break;
        }
    }

    private void ChangeState(AntState next)
    {
        state = next;
    }

    private void DoWander()
    {
        if (!nav.hasPath || nav.remainingDistance < 0.4f)
        {
            wanderDestination = RandomNavMeshPoint(7f);
            nav.SetDestination(wanderDestination);
        }
    }

    private void DoSeekFood()
    {
        if (targetFood == null) return;

        nav.SetDestination(targetFood.position);

        if (Vector3.Distance(transform.position, targetFood.position) <= eatRange)
            ConsumeFood();
    }

    private void DoEvadePlayer()
    {
        if (lastKnownPlayer == null) return;

        Vector3 fleeDir = (transform.position - lastKnownPlayer.position).normalized;
        Vector3 fleeGoal = transform.position + fleeDir * 9f;

        if (NavMesh.SamplePosition(fleeGoal, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            nav.SetDestination(hit.position);
    }

    private void RefreshFoodTarget()
    {
        if (targetFood != null && targetFood.gameObject.activeInHierarchy)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, foodMask);
        float best = float.MaxValue;
        targetFood = null;

        foreach (var col in hits)
        {
            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < best && HasLineOfSight(col.transform.position))
            {
                best = d;
                targetFood = col.transform;
            }
        }
    }

    private void RefreshPlayerVisibility()
    {
        playerIsVisible = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, playerMask);
        foreach (var col in hits)
        {
            Vector3 toPlayer = col.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (angle < visionAngle * 0.5f && HasLineOfSight(col.transform.position))
            {
                playerIsVisible = true;
                lastKnownPlayer = col.transform;
                return;
            }
        }
    }

    private bool HasLineOfSight(Vector3 worldPoint)
    {
        Vector3 eyePos = transform.position + Vector3.up * 0.25f;
        Vector3 dir = worldPoint - eyePos;
        return !Physics.Raycast(eyePos, dir.normalized, dir.magnitude, obstacleMask);
    }

    private void GivePerFrameRewards()
    {
        if (targetFood == null) return;

        if (HasLineOfSight(targetFood.position))
            AddReward(rewardLosPerSecond * Time.fixedDeltaTime);

        float distNow = Vector3.Distance(transform.position, targetFood.position);
        if (distNow < distToFoodPrevFrame)
            AddReward(rewardApproach);

        distToFoodPrevFrame = distNow;
    }

    private void ConsumeFood()
    {
        AddReward(rewardEat);
        foodManager?.OnFoodEaten(targetFood.gameObject);

        targetFood = null;
        distToFoodPrevFrame = float.MaxValue;
        ChangeState(AntState.Wander);
    }

    public void GetCaught()
    {
        Debug.Log("ANT KILLED");

        AddReward(penaltyCaught);
        EndEpisode();
        gameObject.SetActive(false);
        GameWorld.Instance?.OnAntKilled();
    }

    private Vector3 RandomNavMeshPoint(float radius)
    {
        Vector3 candidate = transform.position + Random.insideUnitSphere * radius;
        candidate.y = transform.position.y;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidArc(
            transform.position, Vector3.up,
            Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward,
            visionAngle, visionRange);
 
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, eatRange);
 
        if (targetFood != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetFood.position);
        }
    }
#endif
}