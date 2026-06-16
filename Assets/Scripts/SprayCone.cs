using UnityEngine;

public class SprayCone : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        AntAI ant = other.GetComponentInParent<AntAI>();

        if (ant != null)
        {
            ant.GetCaught();
        }
    }
}