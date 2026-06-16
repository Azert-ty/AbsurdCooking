using System.Collections.Generic;
using UnityEngine;

public static class EnemyChaseCoordinator
{
    private static readonly List<EnemyMovement> chasingEnemies =
        new List<EnemyMovement>();

    private static Transform currentTarget;
    private static PlayerTrail currentTrail;

    public static void Register(EnemyMovement enemy, Transform target)
    {
        if (enemy == null || target == null)
            return;

        currentTarget = target;
        currentTrail = target.GetComponent<PlayerTrail>();

        if (!chasingEnemies.Contains(enemy))
            chasingEnemies.Add(enemy);

        RecalculateRanks();
    }

    public static void Unregister(EnemyMovement enemy)
    {
        if (enemy == null)
            return;

        chasingEnemies.Remove(enemy);

        if (chasingEnemies.Count == 0)
        {
            currentTarget = null;
            currentTrail = null;
            return;
        }

        RecalculateRanks();
    }

    public static void RecalculateRanks()
    {
        chasingEnemies.RemoveAll(enemy => enemy == null);

        for (int i = 0; i < chasingEnemies.Count; i++)
        {
            chasingEnemies[i].SetChaseRank(i + 1);
        }
    }

    public static Vector3 GetChaseDestination(
        EnemyMovement enemy,
        Transform target,
        float trailDelayPerRank,
        float leadPredictionTime)
    {
        if (enemy == null || target == null)
            return Vector3.zero;

        int index = chasingEnemies.IndexOf(enemy);

        if (index < 0)
            index = 0;

        PlayerTrail trail = currentTrail;

        if (trail == null)
            trail = target.GetComponent<PlayerTrail>();

        if (trail == null)
            return target.position;

        if (index == 0)
        {
            Vector3 predictedPosition =
                target.position +
                (Vector3)(trail.Velocity * leadPredictionTime);

            return predictedPosition;
        }

        float delay = trailDelayPerRank * index;

        return trail.GetPositionAgo(delay);
    }

    public static void Clear()
    {
        chasingEnemies.Clear();
        currentTarget = null;
        currentTrail = null;
    }
}