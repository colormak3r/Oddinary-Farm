using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LassoRenderer : MonoBehaviour
{
    [Header("Lasso Settings")]
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private Transform startTransform;
    [SerializeField]
    private Transform targetTransform;
    [SerializeField]
    private int segmentCount = 35;
    [SerializeField]
    private float segmentLength = 0.25f;

    [Header("Debugs")]
    [SerializeField]
    private bool renderLine = true;

    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    // Credit:
    // https://www.youtube.com/watch?v=FcnvwtyxLds
    // https://github.com/dci05049/Verlet-Rope-Unity/blob/master/Tutorial%20Verlet%20Rope/Assets/Rope.cs

    private void Start()
    {
        Vector3 ropeStartPoint = Vector3.zero;

        for (int i = 0; i < segmentCount; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= segmentLength;
        }
    }

    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        if (!renderLine) return;
        Simulate();
    }

    private void Simulate()
    {
        // SIMULATION
        Vector2 forceGravity = new Vector2(0f, -1.5f);

        for (int i = 1; i < segmentCount; i++)
        {
            RopeSegment firstSegment = ropeSegments[i];
            Vector2 velocity = firstSegment.posNow - firstSegment.posOld;
            firstSegment.posOld = firstSegment.posNow;
            firstSegment.posNow += velocity;
            firstSegment.posNow += forceGravity * Time.fixedDeltaTime;
            ropeSegments[i] = firstSegment;
        }

        //CONSTRAINTS
        for (int i = 0; i < 50; i++)
        {
            ApplyConstraint();
        }
    }

    private void ApplyConstraint()
    {
        RopeSegment firstSegment = ropeSegments[0];
        firstSegment.posNow = startTransform.position;
        ropeSegments[0] = firstSegment;

        RopeSegment lastSegment = ropeSegments[segmentCount - 1];
        lastSegment.posNow = targetTransform ? targetTransform.position : startTransform.position;
        ropeSegments[segmentCount - 1] = lastSegment;

        for (int i = 0; i < segmentCount - 1; i++)
        {
            RopeSegment firstSeg = ropeSegments[i];
            RopeSegment secondSeg = ropeSegments[i + 1];

            float dist = (firstSeg.posNow - secondSeg.posNow).magnitude;
            float error = Mathf.Abs(dist - segmentLength);
            Vector2 changeDir = Vector2.zero;

            if (dist > segmentLength)
            {
                changeDir = (firstSeg.posNow - secondSeg.posNow).normalized;
            }
            else if (dist < segmentLength)
            {
                changeDir = (secondSeg.posNow - firstSeg.posNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                firstSeg.posNow -= changeAmount * 0.5f;
                ropeSegments[i] = firstSeg;
                secondSeg.posNow += changeAmount * 0.5f;
                ropeSegments[i + 1] = secondSeg;
            }
            else
            {
                secondSeg.posNow += changeAmount;
                ropeSegments[i + 1] = secondSeg;
            }
        }
    }

    private void DrawRope()
    {
        if (targetTransform == null || !renderLine)
        {
            lineRenderer.enabled = false;
            return;
        }
        else
        {
            lineRenderer.enabled = true;
        }

        Vector3[] ropePositions = new Vector3[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            if (i == segmentCount - 1)
                ropePositions[i] = targetTransform.position;

            else
                ropePositions[i] = ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;

        public RopeSegment(Vector2 pos)
        {
            posNow = pos;
            posOld = pos;
        }
    }

    public void SetRenderLine(bool render)
    {
        bool previousRenderLine = renderLine;
        renderLine = render;
        if (renderLine && !previousRenderLine)
        {
            Simulate(); // Force simulate immidiately when enabling rendering
        }
    }
}
