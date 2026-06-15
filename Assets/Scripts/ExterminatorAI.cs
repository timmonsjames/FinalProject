using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class ExterminatorAI : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolNodes;

    [Header("Vision")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask antMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Spray")]
    [SerializeField] private float sprayRange = 2f;
    [SerializeField] private float sprayRadius = 0.5f;
    [SerializeField] private float sprayRate = 0.25f;
    [SerializeField] private ParticleSystem sprayVFX;

    [Header("Tracker")]
    [SerializeField] private float trackerTimeout = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float nodeReachDist = 1f;

    private NavMeshAgent nav;
    private int currentNode = 0;
    private float timeSinceKill = 0f;
    private float sprayTimer = 0f;

    private Transform currentTarget;

    private enum State { Search, Spray, Pathfind }
    private State state = State.Search;

    private void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        nav.speed = moveSpeed;
    }

    private void Update()
    {
        timeSinceKill += Time.deltaTime;

        switch (state)
        {
            case State.Search: UpdateSearch(); break;
            case State.Spray: UpdateSpray(); break;
            case State.Pathfind: UpdatePathfind(); break;
        }
    }

    private void UpdateSearch()
    {
        if (patrolNodes.Length > 0)
        {
            nav.SetDestination(patrolNodes[currentNode].position);
            if (nav.remainingDistance < nodeReachDist)
                currentNode = (currentNode + 1) % patrolNodes.Length;
        }

        Transform spotted = ScanForAnt();
        if (spotted != null)
        {
            currentTarget = spotted;
            TransitionTo(State.Spray);
            return;
        }

        if (timeSinceKill >= trackerTimeout)
            ActivateTracker();
    }

    private void UpdateSpray()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            timeSinceKill = 0f;
            TransitionTo(State.Search);
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (dist > sprayRange)
        {
            nav.SetDestination(currentTarget.position);
        }
        else
        {
            nav.ResetPath();
            Vector3 dir = currentTarget.position - transform.position;
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            DoSpray();
        }

        Transform closer = ScanForAnt();
        if (closer != null && closer != currentTarget)
        {
            float d1 = Vector3.Distance(transform.position, currentTarget.position);
            float d2 = Vector3.Distance(transform.position, closer.position);
            if (d2 < d1) currentTarget = closer;
        }
    }

    private void UpdatePathfind()
    {
        currentTarget = FindClosestAnt();

        if (currentTarget == null)
        {
            TransitionTo(State.Search);
            return;
        }

        nav.SetDestination(currentTarget.position);

        if (Vector3.Distance(transform.position, currentTarget.position) <= sprayRange)
        {
            TransitionTo(State.Spray);
            return;
        }

        Transform spotted = ScanForAnt();
        if (spotted != null)
        {
            currentTarget = spotted;
            TransitionTo(State.Spray);
        }
    }

    private void DoSpray()
    {
        sprayTimer -= Time.deltaTime;
        if (sprayTimer > 0f) return;
        sprayTimer = sprayRate;

        if (sprayVFX != null && !sprayVFX.isPlaying) sprayVFX.Play();

        // Hit the locked target if it's inside the spray cone.
        Collider[] hits = Physics.OverlapSphere(currentTarget.position, sprayRadius, antMask);
        foreach (var c in hits)
            c.GetComponentInParent<IKillable>()?.GetCaught();

        // Backup: catch the ant wherever it actually is, not where it was last frame.
        // The target moves every frame (evading), and a 0.5m sphere at last-frame's
        // position misses more often than it hits.
        Collider[] muzzle = Physics.OverlapSphere(transform.position, sprayRange, antMask);
        foreach (var c in muzzle)
            c.GetComponentInParent<IKillable>()?.GetCaught();
    }

    private void ActivateTracker()
    {
        timeSinceKill = 0f;
        TransitionTo(State.Pathfind);
        GameWorld.Instance?.OnTrackerActivated();
    }

    private Transform ScanForAnt()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, antMask);
        foreach (var c in hits)
        {
            Vector3 dir = (c.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle < visionAngle * 0.5f)
            {
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                float dist = Vector3.Distance(origin, c.transform.position);
                if (!Physics.Raycast(origin, dir, dist, obstacleMask))
                    return c.transform;
            }
        }
        return null;
    }

    private Transform FindClosestAnt()
    {
        AntAI[] ants = FindObjectsOfType<AntAI>();
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var a in ants)
        {
            if (!a.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, a.transform.position);
            if (d < bestDist) { bestDist = d; best = a.transform; }
        }
        return best;
    }

    private void TransitionTo(State next)
    {
        if (sprayVFX != null && next != State.Spray) sprayVFX.Stop();
        state = next;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sprayRange);
    }
#endif
}