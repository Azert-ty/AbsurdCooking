using System.Collections.Generic;
using UnityEngine;

public static class EnemyChaseCoordinator
{
    private static readonly List<EnemyMovement> chasingEnemies =
        new List<EnemyMovement>();

    private static Transform currentTarget;

    public static void Register(EnemyMovement enemy, Transform target)
    {
        if (enemy == null || target == null)
            return;

        currentTarget = target;

        if (!chasingEnemies.Contains(enemy))
        {
            chasingEnemies.Add(enemy);
        }

        RecalculateRanks();
    }

    public static void Unregister(EnemyMovement enemy)
    {
        if (enemy == null)
            return;

        if (chasingEnemies.Contains(enemy))
        {
            chasingEnemies.Remove(enemy);
        }

        RecalculateRanks();
    }

    public static void RecalculateRanks()
    {
        if (currentTarget == null)
            return;

        chasingEnemies.RemoveAll(enemy => enemy == null);

        chasingEnemies.Sort((a, b) =>
        {
            float distanceA =
                (a.transform.position - currentTarget.position).sqrMagnitude;

            float distanceB =
                (b.transform.position - currentTarget.position).sqrMagnitude;

            return distanceA.CompareTo(distanceB);
        });

        for (int i = 0; i < chasingEnemies.Count; i++)
        {
            chasingEnemies[i].SetChaseRank(i + 1);
        }
    }
}