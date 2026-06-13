using System.Collections.Generic;
using UnityEngine;

public class PatrolPathFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyMovement enemyMovement;

    [Header("Visuals")]
    [SerializeField] private GameObject smallDotPrefab;
    [SerializeField] private GameObject bigDotPrefab;

    [Header("Settings")]
    [SerializeField] private float dotSpacing = 0.75f;
    [SerializeField] private float passDistance = 0.35f;
    [SerializeField] private bool loopPath = true;

    private Transform dotsParent;

    private readonly Dictionary<string, PatrolSegment> segments = new();
    private readonly Dictionary<int, PatrolDot> waypointDots = new();
    private readonly List<PatrolDot> allDots = new();

    private PatrolSegment currentSegment;
    private bool reverseDirection;

    private class PatrolSegment
    {
        public int indexA;
        public int indexB;
        public Vector3 start;
        public Vector3 end;
        public List<PatrolDot> dots = new();
    }

    private void Awake()
    {
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();

        GameObject parentObject = GameObject.Find("PatrolPathFeedbacks");

        if (parentObject == null)
            parentObject = new GameObject("PatrolPathFeedbacks");

        dotsParent = parentObject.transform;
    }

    private void Start()
    {
        GeneratePathFeedback();
        ResetAllDots();
    }

    private void Update()
    {
        UpdateCurrentSegmentProgress();
    }

    private void GeneratePathFeedback()
    {
        if (enemyMovement == null)
            return;

        Transform[] waypoints = enemyMovement.Waypoints;

        if (waypoints == null || waypoints.Length < 2)
            return;

        segments.Clear();
        waypointDots.Clear();
        allDots.Clear();

        int legCount = loopPath ? waypoints.Length : waypoints.Length - 1;

        for (int i = 0; i < legCount; i++)
        {
            int nextIndex = i + 1;

            if (nextIndex >= waypoints.Length)
                nextIndex = 0;

            CreateSegmentIfNeeded(i, nextIndex, waypoints);
        }
    }

    private void CreateSegmentIfNeeded(
        int fromIndex,
        int toIndex,
        Transform[] waypoints)
    {
        int indexA = Mathf.Min(fromIndex, toIndex);
        int indexB = Mathf.Max(fromIndex, toIndex);

        string key = GetSegmentKey(indexA, indexB);

        if (segments.ContainsKey(key))
            return;

        PatrolSegment segment = new PatrolSegment();

        segment.indexA = indexA;
        segment.indexB = indexB;
        segment.start = waypoints[indexA].position;
        segment.end = waypoints[indexB].position;

        PatrolDot startDot =
            CreateOrGetWaypointDot(indexA, waypoints[indexA].position);

        if (startDot != null)
            segment.dots.Add(startDot);

        CreateSmallDotsBetween(segment, segment.start, segment.end);

        PatrolDot endDot =
            CreateOrGetWaypointDot(indexB, waypoints[indexB].position);

        if (endDot != null)
            segment.dots.Add(endDot);

        segments.Add(key, segment);
    }

    private void CreateSmallDotsBetween(
        PatrolSegment segment,
        Vector3 start,
        Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        int dotCount = Mathf.FloorToInt(distance / dotSpacing);

        for (int i = 1; i < dotCount; i++)
        {
            float t = i / (float)dotCount;

            Vector3 position =
                Vector3.Lerp(start, end, t);

            PatrolDot dot =
                CreateDot(smallDotPrefab, position);

            if (dot != null)
                segment.dots.Add(dot);
        }
    }

    private PatrolDot CreateOrGetWaypointDot(int index, Vector3 position)
    {
        if (waypointDots.TryGetValue(index, out PatrolDot existingDot))
            return existingDot;

        PatrolDot newDot =
            CreateDot(bigDotPrefab, position);

        if (newDot != null)
            waypointDots.Add(index, newDot);

        return newDot;
    }

    private PatrolDot CreateDot(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        GameObject dotObject = Instantiate(
            prefab,
            position,
            Quaternion.identity,
            dotsParent);

        PatrolDot dot =
            dotObject.GetComponent<PatrolDot>();

        if (dot == null)
        {
            Debug.LogWarning("Le prefab de point n'a pas de script PatrolDot.");
            return null;
        }

        allDots.Add(dot);
        return dot;
    }

    public void StartSegment(int fromIndex, int toIndex)
    {
        int indexA = Mathf.Min(fromIndex, toIndex);
        int indexB = Mathf.Max(fromIndex, toIndex);

        string key = GetSegmentKey(indexA, indexB);

        if (!segments.TryGetValue(key, out PatrolSegment segment))
            return;

        currentSegment = segment;

        reverseDirection = fromIndex > toIndex;

        ResetSegment(currentSegment);
    }

    private void UpdateCurrentSegmentProgress()
    {
        if (currentSegment == null)
            return;

        if (enemyMovement == null)
            return;

        Vector3 segmentDirection =
            currentSegment.end - currentSegment.start;

        float segmentLengthSqr =
            segmentDirection.sqrMagnitude;

        if (segmentLengthSqr <= 0.001f)
            return;

        Vector3 enemyPosition =
            enemyMovement.transform.position;

        float enemyProgress =
            Vector3.Dot(
                enemyPosition - currentSegment.start,
                segmentDirection) / segmentLengthSqr;

        enemyProgress = Mathf.Clamp01(enemyProgress);

        float segmentLength =
            Mathf.Sqrt(segmentLengthSqr);

        float progressMargin =
            passDistance / segmentLength;

        for (int i = 0; i < currentSegment.dots.Count; i++)
        {
            PatrolDot dot = currentSegment.dots[i];

            if (dot == null)
                continue;

            float dotProgress =
                Vector3.Dot(
                    dot.transform.position - currentSegment.start,
                    segmentDirection) / segmentLengthSqr;

            dotProgress = Mathf.Clamp01(dotProgress);

            bool passed;

            if (!reverseDirection)
            {
                passed = dotProgress <= enemyProgress + progressMargin;
            }
            else
            {
                passed = dotProgress >= enemyProgress - progressMargin;
            }

            dot.SetPassed(passed);
        }
    }

    private void ResetSegment(PatrolSegment segment)
    {
        if (segment == null)
            return;

        for (int i = 0; i < segment.dots.Count; i++)
        {
            if (segment.dots[i] != null)
                segment.dots[i].SetPassed(false);
        }
    }

    public void ResetAllDots()
    {
        for (int i = 0; i < allDots.Count; i++)
        {
            if (allDots[i] != null)
                allDots[i].SetPassed(false);
        }
    }

    private string GetSegmentKey(int indexA, int indexB)
    {
        return indexA + "_" + indexB;
    }

    // Ancienne méthode gardée pour éviter les erreurs si un vieux code l'appelle encore.
    public void SetActiveSegment(int segmentIndex)
    {
    }
}