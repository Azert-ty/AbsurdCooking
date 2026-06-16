using System.Collections.Generic;
using UnityEngine;

public class PlayerTrail : MonoBehaviour
{
    private struct TrailPoint
    {
        public float time;
        public Vector3 position;

        public TrailPoint(float time, Vector3 position)
        {
            this.time = time;
            this.position = position;
        }
    }

    [Header("Trail")]
    [SerializeField] private float maxHistoryTime = 3f;
    [SerializeField] private float recordInterval = 0.04f;

    private readonly List<TrailPoint> points = new List<TrailPoint>();

    private float nextRecordTime;
    private Vector3 previousPosition;

    public Vector2 Velocity { get; private set; }
    public Vector2 MoveDirection { get; private set; } = Vector2.down;

    private void Awake()
    {
        previousPosition = transform.position;
        points.Add(new TrailPoint(Time.time, transform.position));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime > 0f)
        {
            Vector3 delta = transform.position - previousPosition;
            Velocity = delta / deltaTime;

            if (Velocity.sqrMagnitude > 0.01f)
                MoveDirection = Velocity.normalized;
        }

        previousPosition = transform.position;

        if (Time.time >= nextRecordTime)
        {
            points.Add(new TrailPoint(Time.time, transform.position));
            nextRecordTime = Time.time + recordInterval;
        }

        float oldestAllowedTime = Time.time - maxHistoryTime;

        while (points.Count > 2 && points[0].time < oldestAllowedTime)
        {
            points.RemoveAt(0);
        }
    }

    public Vector3 GetPositionAgo(float secondsAgo)
    {
        if (points.Count == 0)
            return transform.position;

        float wantedTime = Time.time - secondsAgo;

        if (wantedTime <= points[0].time)
            return points[0].position;

        int lastIndex = points.Count - 1;

        if (wantedTime >= points[lastIndex].time)
            return points[lastIndex].position;

        for (int i = lastIndex; i > 0; i--)
        {
            TrailPoint current = points[i];
            TrailPoint previous = points[i - 1];

            if (previous.time <= wantedTime && current.time >= wantedTime)
            {
                float t = Mathf.InverseLerp(
                    previous.time,
                    current.time,
                    wantedTime
                );

                return Vector3.Lerp(previous.position, current.position, t);
            }
        }

        return points[lastIndex].position;
    }
}