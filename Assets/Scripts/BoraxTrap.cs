using UnityEngine;
using System.Collections;

public class BoraxTrap : MonoBehaviour
{
    private float radius;
    private float killChance;
    private float delay;
    private LayerMask antMask;

    public void Init(float radius, float killChance, float delay, LayerMask antMask)
    {
        this.radius = radius;
        this.killChance = killChance;
        this.delay = delay;
        this.antMask = antMask;

        StartCoroutine(TriggerAfterDelay());
    }

    private IEnumerator TriggerAfterDelay()
    {
        yield return new WaitForSeconds(delay);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, antMask);
        foreach (var c in hits)
        {
            if (Random.value <= killChance)
                c.GetComponentInParent<IKillable>()?.GetCaught();
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}