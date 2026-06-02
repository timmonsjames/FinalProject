using UnityEngine;
using System.Collections;

public class GelTrap : MonoBehaviour
{
    [SerializeField] private LayerMask antMask;

    private float freezeDuration;
    private bool triggered = false;

    public void Init(float duration)
    {
        freezeDuration = duration;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if ((antMask.value & (1 << other.gameObject.layer)) == 0) return;

        AntAI ant = other.GetComponent<AntAI>();
        if (ant != null)
        {
            triggered = true;
            StartCoroutine(FreezeAnt(ant));
        }
    }

    private IEnumerator FreezeAnt(AntAI ant)
    {
        var nav = ant.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null) nav.isStopped = true;

        yield return new WaitForSeconds(freezeDuration);

        if (ant != null && ant.gameObject.activeInHierarchy)
            if (nav != null) nav.isStopped = false;

        Destroy(gameObject);
    }
}