using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float repathRate = 0.1f; // how often it updates path

    private NavMeshAgent agent;
    private float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (player != null)
        {
            agent.isStopped = false;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (!agent.isOnNavMesh) return;

        // continuously chase player
        repathTimer += Time.deltaTime;

        if (repathTimer >= repathRate)
        {
            agent.SetDestination(player.position);
            repathTimer = 0f;
        }

        // smooth rotation toward movement direction
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
        }
    }

    public void SetPlayer(Transform t)
    {
        player = t;
    }
}