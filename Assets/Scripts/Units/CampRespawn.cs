using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Тренировочный враг: после смерти через несколько секунд появляется снова на своём месте.
[RequireComponent(typeof(Health))]
public class CampRespawn : MonoBehaviour
{
    public float respawnTime = 15f;

    Vector3 home;
    Health health;
    NavMeshAgent agent;

    void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
        health.OnDeath += _ => StartCoroutine(Respawn());
    }

    public void SetHome(Vector3 p) => home = p;

    IEnumerator Respawn()
    {
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        yield return new WaitForSeconds(0.3f);
        SetVisible(false);

        yield return new WaitForSeconds(respawnTime);

        if (agent != null) agent.Warp(home); else transform.position = home;
        var attack = GetComponent<AutoAttack>();
        if (attack != null) attack.ClearTarget();
        var status = GetComponent<StatusEffects>();
        if (status != null) status.ClearAll();
        health.Revive();
        SetVisible(true);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    void SetVisible(bool v)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = v;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = v;
    }
}
