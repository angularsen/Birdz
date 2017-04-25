using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

public static class AnimatorParams
{
    public const string Speed = "Speed m:s";
}

class Parrot : MonoBehaviour
{
    // Forward velocity has little drag, but sideways or up/downwards velocity has very high drag
    // to help change the velocity vector when pitching to do a climb or a banking turn
    public Vector3 DragFactors = new Vector3(2f, 2f, 0.001f); 
    public float LiftFactor = 10f;
    public float RotateIntoWindLerpFactor = .7f;
    public float MassKg = 1.0f;
    public float MaxThrustN = 10;
    public float MaxYawRateDps = 90;
    public float MaxPitchRateDps = 90;
    public float MaxRollRateDps = 180;

    private Animator _animator;

    private Vector3 _velocity;

    private Transform _prevTransform;
    private const int LabelHeight = 20;
    private static readonly string[] DebugLabels = new string[10];

    private static readonly Rect[] DebugLabelRects = DebugLabels
        .Select((str, i) => new Rect(10, 10 + i * 20, 300, LabelHeight))
        .ToArray();

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _prevTransform = transform;

        // Start with a forward velocity
        _velocity = transform.forward * 10;
    }

    void Update()
    {
        // TODO Move me to a game manager object instead
        if (Input.GetButtonDown("Reset"))
            SceneManager.LoadScene(0);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float thrustForce = (Input.GetAxis("Thrust") - Input.GetAxis("Brake")) * MaxThrustN;
        float rollDelta = -Input.GetAxis("Roll") * MaxRollRateDps * dt;
        float pitchDelta = Input.GetAxis("Pitch") * MaxPitchRateDps * dt;
        float yawDelta = Input.GetAxis("Yaw") * MaxYawRateDps * dt;

        Vector3 prevVelocity = _velocity;
        Vector3 prevForward = transform.forward;
        Transform prevTransform = transform;
        float prevForwardSpeed = Vector3.Project(prevVelocity, prevTransform.forward).magnitude;

        Vector3 thrust = prevTransform.forward * thrustForce;
        Vector3 lift = prevTransform.up * Mathf.Min(9.81f, prevForwardSpeed * LiftFactor); // Lift maxing out at 1G, to simulate bird controlling its own lift
        Vector3 gravity = Vector3.down * 9.81f * MassKg;
        //        Vector3 drag = Vector3.Scale(Vector3.Scale(_velocity, _velocity) , DragFactors);
        Vector3 drag = Vector3.Scale(prevVelocity, DragFactors);
        Vector3 totalForce = thrust - drag + lift + gravity;
        Vector3 totalAccel = totalForce / MassKg;



        var rotationInputEuler = new Vector3(pitchDelta, yawDelta, rollDelta);
//        var rotationByInput = Quaternion.Euler(rotationInputEuler);
        transform.position += prevVelocity * dt;
        transform.Rotate(rotationInputEuler, Space.Self);

        // Rotate into wind
//        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(prevVelocity, transform.up),
//            RotateIntoWindLerpFactor * dt);

        // Change velocity by acceleration.
        // Transfer old forward velocity into new forward direction>
        Vector3 newVelocity = prevVelocity
                              + totalAccel * dt
                              + (transform.forward - prevForward) * prevForwardSpeed
                              ;


        float forwardSpeed = Vector3.Project(newVelocity, transform.forward).magnitude;
        _animator.SetFloat(AnimatorParams.Speed, forwardSpeed);

        _prevTransform = prevTransform;
        _velocity = newVelocity;

        Vector3 velocityKmh = _velocity * 3.6f;
        float forwardSpeedKmh = forwardSpeed * 3.6f;
        int i = 0;
        DebugLabels[i++] = string.Format("Accel[{0}={1} m/s²]", totalAccel, totalAccel.magnitude);
        DebugLabels[i++] = string.Format("Speed[{0}={1} km/h], F.Speed[{2} km/h]", velocityKmh, velocityKmh.magnitude, forwardSpeedKmh);
//        DebugLabels[i++] = string.Format("Thrust[{0}={1}]", thrustForce, thrustForce);
//        _debugLabels[i++] = string.Format("Drag[{0}={1}]", dragForce, dragForce.magnitude);
//        DebugLabels[i++] = string.Format("Slip[{0}={1}]", slipForce, slipForce);
        DebugLabels[i++] = string.Format("Forward{0}, Up{1}, Right{2}", transform.forward, transform.up, transform.right);
//        DebugLabels[i++] = string.Format("Thrust axis: {0}", Input.GetAxis("Thrust"));
    }

    void OnGUI()
    {
        for (var i = 0; i < DebugLabels.Length; i++)
        {
            string label = DebugLabels[i];
            if (label == null) continue;

            Rect rect = DebugLabelRects[i];
            GUI.Label(rect, label);
        }
    }

//    void OnCollisionEnter(Collision collision)
//    {
//        transform.position += 10 * Vector3.up;
//        collision.
//        }

    void OnTriggerEnter(Collider col)
    {
        Debug.LogError("HIT");
        transform.position += 10 * Vector3.up;
    }
}