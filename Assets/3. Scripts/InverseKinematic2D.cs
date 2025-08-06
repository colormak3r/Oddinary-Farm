using UnityEngine;

/// <summary>
/// Lightweight FABRIK solver for a single 2-D chain (e.g. robot leg).
/// Assumes every bone’s +X axis points toward its child in the T-pose.
/// </summary>
[ExecuteInEditMode]
public class InverseKinematic2D : MonoBehaviour
{
    [Header("Chain (Root -> Tip)")]
    [Tooltip("Root, mid-bones, then the foot (end)")]
    public Transform[] joints;          // e.g. {Hip, Knee, Ankle, Foot}

    [Header("Effector / Pole")]
    public Transform effector;          // drag the object you move
    public Vector2 effectorOffset;    // local offset from effector to toe tip
    public Transform pole;              // optional knee-aim helper (in front of knee)

    [Header("Solver Settings")]
    [Min(1)] public int maxIterations = 10;
    [Min(0f)] public float tolerance = 0.001f;

    [Header("Foot Rotation Matching")]
    public bool matchEffectorRotation = true;
    [Range(0, 1)] public float rotationWeight = 1f; // 0 = ignore, 1 = copy fully

    // ---------- private cached data ----------
    float[] lengths;             // segment lengths
    float totalLength;
    Vector2[] positions;         // scratch buffer

    void Awake() => Init();

    void LateUpdate() => Solve();

    void Init()
    {
        if (joints == null || joints.Length < 2) return;

        int n = joints.Length;
        lengths = new float[n - 1];
        positions = new Vector2[n];
        totalLength = 0f;

        for (int i = 0; i < n - 1; i++)
        {
            float len = Vector2.Distance(joints[i].position, joints[i + 1].position);
            lengths[i] = len;
            totalLength += len;
        }
    }

    void Solve()
    {
        if (effector == null || joints == null || joints.Length < 2) return;

        // 1. cache current world positions
        for (int i = 0; i < joints.Length; i++)
            positions[i] = joints[i].position;

        Vector2 rootPos = positions[0];
        Vector2 targetPos = (Vector2)effector.position + effectorOffset;

        // 2. out-of-reach? -> straight line toward target
        if ((targetPos - rootPos).sqrMagnitude >= totalLength * totalLength)
        {
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector2 dir = (targetPos - positions[i]).normalized;
                positions[i + 1] = positions[i] + dir * lengths[i];
            }
        }
        else
        {
            // FABRIK iterations
            for (int it = 0; it < maxIterations; it++)
            {
                // --- backward pass ---
                positions[^1] = targetPos;
                for (int i = positions.Length - 2; i >= 0; i--)
                {
                    Vector2 dir = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + dir * lengths[i];
                }

                // optional pole constraint (after backward, before forward)
                ApplyPoleConstraint();

                // --- forward pass ---
                positions[0] = rootPos;
                for (int i = 1; i < positions.Length; i++)
                {
                    Vector2 dir = (positions[i] - positions[i - 1]).normalized;
                    positions[i] = positions[i - 1] + dir * lengths[i - 1];
                }

                // stop early if we’re close enough
                if ((positions[^1] - targetPos).sqrMagnitude < tolerance * tolerance)
                    break;
            }
        }

        // 3. write back positions & bone rotations
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].position = positions[i];

            if (i < joints.Length - 1)
            {
                Vector2 toChild = (positions[i + 1] - positions[i]).normalized;
                float angle = Mathf.Atan2(toChild.y, toChild.x) * Mathf.Rad2Deg;
                joints[i].rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // 4. blend foot toward effector rotation
        if (matchEffectorRotation && rotationWeight > 0f)
        {
            Transform foot = joints[^1];
            foot.rotation = Quaternion.Lerp(
                foot.rotation,
                effector.rotation,
                rotationWeight);
        }
    }

    // -------- helper --------
    void ApplyPoleConstraint()
    {
        if (pole == null || positions.Length < 3) return;

        Vector2 root = positions[0];
        Vector2 ankle = positions[^1];
        Vector2 pole2D = pole.position;

        // project knee onto plane defined by root-ankle and pole
        Vector2 rootToAnkle = ankle - root;
        Vector2 rootToPole = pole2D - root;

        float angle = Vector2.SignedAngle(rootToAnkle, rootToPole);
        Vector2 kneeDir = Quaternion.Euler(0, 0, angle) * rootToAnkle.normalized;
        positions[1] = root + kneeDir * lengths[0];
    }
}
