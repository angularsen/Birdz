using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

enum Layers
{
    Player = 8,
    PlayerRagdoll = 9
}

static class InputNames
{
    public const string Reset = "Reset";
    public const string Thrust = "Thrust";
    public const string Brake = "Brake";
    public const string Pitch = "Pitch";
    public const string Roll = "Roll";
    public const string Yaw = "Yaw";
    public const string Menu = "Menu";
    public const string FollowCam = "FollowCam";
    public const string FixedCam = "FixedCam";
    public const string FreeCam = "FreeCam";
    public const string NextCam = "NextCam";
}

static class AnimatorParams
{
    internal const string FlapSpeedMultiplier = "FlapSpeedMultiplier";
    internal const string Speed = "Speed m:s";
    internal const string ThrustInput = "ThrustInput";
}

static class Tags
{
    internal const string Untagged = "Untagged";
    internal const string MainCamera = "MainCamera";
}

// Place the script in the Camera-Control group in the component menu
[AddComponentMenu("Birds/Parrot/Script")]
class Parrot : MonoBehaviour
{
    private const int LabelHeight = 20;
    private static readonly string[] DebugLabels = new string[10];

    private static readonly Rect[] DebugLabelRects = DebugLabels
        .Select((str, i) => new Rect(10, 10 + i * 20, 300, LabelHeight))
        .ToArray();

    private Animator _animator;

    /// <summary>
    ///     Velocity in Local coordinates (body frame).
    /// </summary>
    private Vector3 _localVelocity;

    private Transform _prevTransform;

    // Forward velocity has little drag, but sideways or up/downwards velocity has much higher drag coefficients.
    // Forward body drag of 0.1 found on page 51 in 'Modelling Bird Flight': https://books.google.no/books?id=KG86AgWwFEUC&pg=PA73&lpg=PA73&dq=bird+drag+coefficient&source=bl&ots=RuK6WpSQWJ&sig=S3HbzUEQVtMxQ69gZyKGqvXzAO0&hl=en&sa=X&ved=0ahUKEwjU7KnZrsrTAhWCbZoKHcdJDTcQ6AEIkQEwFg#v=onepage&q=bird%20drag%20coefficient&f=false
    public Vector3 BodyDragFactors = new Vector3(0.1f, 1f, 0.001f);

    public float DragCoefficient = 0.001f;

    /// <summary>
    ///     True if currently in ragdoll/physics state, such as after colliding.
    ///     All direct manipulations of <see cref="GameObject.transform" /> will be skipped during this state.
    /// </summary>
    public bool IsKinematic;

    public float LiftCoefficient = 0.1f;

    public float MassKg = 1.0f;
    public float MaxPitchRateDps = 90;
    public float MaxRollRateDps = 180;
    public float MaxThrustN = 10;

    public float MaxYawRateDps = 30;

//    public float LiftFactor = 10f;
    public float RotateIntoWindLerpFactor = .7f;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _prevTransform = transform;

        // Start with a forward velocity
        _localVelocity = Vector3.forward * 10;

        // Ignore collisions between player bounding box (for collision detection) and its ragdoll colliders (for ragdoll effect)
        Physics.IgnoreLayerCollision((int) Layers.Player, (int) Layers.PlayerRagdoll);
        SetRagdollEnabled(false);
    }

    void Update()
    {
        // TODO Move me to a game manager object instead
        if (Input.GetButtonDown(InputNames.Reset))
        {
            Debug.Log("Reset game.");
            IsKinematic = true;
            SceneManager.LoadScene(0);
        }
        else if (Input.GetButtonDown(InputNames.Menu))
        {
            // TODO Menu with exit option
            Debug.Log("Quitting application.");
            Application.Quit();
        }
    }

    void FixedUpdate()
    {
        if (!IsKinematic) return;

        float dt = Time.fixedDeltaTime;
        float thrustInput = Input.GetAxis(InputNames.Thrust);
        float thrustForce = (thrustInput - Input.GetAxis(InputNames.Brake)) * MaxThrustN;
        float rollDelta = -Input.GetAxis(InputNames.Roll) * MaxRollRateDps * dt;
        float pitchDelta = Input.GetAxis(InputNames.Pitch) * MaxPitchRateDps * dt;
        float yawDelta = Input.GetAxis(InputNames.Yaw) * MaxYawRateDps * dt;

        Transform prevTransform = transform;
        Vector3 prevLocalVelocity = _localVelocity;
        Vector3 prevLocalVelocity2 = new Vector3(
            Mathf.Abs(prevLocalVelocity.x) * prevLocalVelocity.x,
            Mathf.Abs(prevLocalVelocity.y) * prevLocalVelocity.y,
            Mathf.Abs(prevLocalVelocity.z) * prevLocalVelocity.z);

        // Rotate into wind
        Vector3 prevWorldVelocity = prevTransform.TransformVector(prevLocalVelocity);
        Quaternion worldRotationIntoWind = Quaternion.LookRotation(prevWorldVelocity, prevTransform.up);
        prevTransform.rotation = Quaternion.Lerp(prevTransform.rotation, worldRotationIntoWind,
            RotateIntoWindLerpFactor * prevLocalVelocity2.magnitude * dt);

//        Transform prevTransform = transform;
        float prevFwdSpeed = prevLocalVelocity.z;
        float prevFwdSpeed2 = prevFwdSpeed * prevFwdSpeed;
        int angleOfAttackSign = prevLocalVelocity.y < 0 ? +1 : -1;
        float prevAngleOfAttackDeg = prevLocalVelocity.magnitude < 0.1
            ? 0
            : angleOfAttackSign * Vector3.Angle(Vector3.forward,
                  new Vector3(0, prevLocalVelocity.y, prevLocalVelocity.z));

        Vector3 localThrust = Vector3.forward * thrustForce;
        // Lift maxing out at 1G, to simulate bird controlling its own lift
        Vector3 localLift = Vector3.up * Mathf.Min(9.81f,
                                prevFwdSpeed2 *
                                GetLiftCoefficient(prevAngleOfAttackDeg));
        Vector3 localGravitationalForce = transform.InverseTransformVector(Vector3.down * 9.81f * MassKg);
        Vector3 localBodyDrag = -Vector3.Scale(prevLocalVelocity2, BodyDragFactors);
        Vector3 localLiftInducedDrag = Vector3.back * prevFwdSpeed2 * GetDragCoefficient(prevAngleOfAttackDeg);
        Vector3 localDrag = localBodyDrag + localLiftInducedDrag;
        Vector3 totalLocalForce = localThrust + localDrag + localLift + localGravitationalForce;
        Vector3 totalLocalAccel = totalLocalForce / MassKg;

        Vector3 newLocalVelocity = prevLocalVelocity + totalLocalAccel * dt;
        Vector3 rotationInputEuler = new Vector3(pitchDelta, yawDelta, rollDelta);
        Vector3 localDisplacement = newLocalVelocity * dt;
        Vector3 worldDisplacement = transform.TransformVector(localDisplacement);

        transform.position += worldDisplacement;
        transform.Rotate(rotationInputEuler, Space.Self);

        // Update animator parameters
        float newFwdSpeed = newLocalVelocity.z;
        _animator.SetFloat(AnimatorParams.Speed, newFwdSpeed);
        _animator.SetFloat(AnimatorParams.ThrustInput, thrustInput);
        _animator.SetFloat(AnimatorParams.FlapSpeedMultiplier, 1 + thrustInput * 2);

        _prevTransform = prevTransform;
        _localVelocity = newLocalVelocity;

        Vector3 localVelocityKmh = _localVelocity * 3.6f;
        float forwardSpeedKmh = newFwdSpeed * 3.6f;
        var i = 0;
        DebugLabels[i++] = string.Format("Accel[{0}={1} m/s²]", totalLocalAccel, totalLocalAccel.magnitude);
        DebugLabels[i++] = string.Format("Speed[{0}={1} km/h], F.Speed[{2} km/h]", localVelocityKmh,
            localVelocityKmh.magnitude,
            forwardSpeedKmh);
        DebugLabels[i++] = string.Format("Angle of attack [{0}]", prevAngleOfAttackDeg);
//        _debugLabels[i++] = string.Format("Drag[{0}={1}]", dragForce, dragForce.magnitude);
//        DebugLabels[i++] = string.Format("Slip[{0}={1}]", slipForce, slipForce);
        DebugLabels[i++] = string.Format("Pitch[{0:0}], Roll[{1:0}], Heading[{2:0}]",
            Angle360ToPlusMinus180(transform.eulerAngles.x),
            Angle360ToPlusMinus180(transform.eulerAngles.z),
            Angle360ToPlusMinus180(transform.eulerAngles.y));

//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(localBodyDrag), Color.green);
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(localLiftInducedDrag), Color.gray);
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(localDrag), Color.yellow);
//
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(localLift), Color.blue);
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(localThrust), Color.cyan);
//
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(totalLocalAccel), Color.magenta);
//        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(newLocalVelocity), Color.red);
    }

    float Angle360ToPlusMinus180(float angleDeg)
    {
        return angleDeg > 180 ? angleDeg - 360 : angleDeg;
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

    void OnTriggerEnter(Collider col)
    {
//        transform.position += 10 * Vector3.up;

//        _localVelocity = Vector3.zero;
        if (col.name.StartsWith("Terrain "))
        {
            Debug.Log("Hit ground: " + col.name);
//            transform.position -= _localVelocity.normalized;
            SetRagdollEnabled(true);
        }
        else
        {
            Debug.Log("HIT " + col.name);
            SetRagdollEnabled(true);
        }
    }

    private static bool InRagdollLayer(Component body)
    {
        return body.gameObject.layer == (int) Layers.PlayerRagdoll;
    }

    private float GetLiftCoefficient(float angleOfAttackDeg)
    {
        return LiftCoefficient;
//        float x = angleOfAttackDeg;
//        float x2 = x * x;
//        float x3 = x * x2;
//        /*
//        Coefficient found by regression of graph samples:
//        http://www.xuru.org/rt/PR.asp#Manually
//-4 0.15
//0 0.9
//4 1.3
//8 1.48
//9 1.49
//10 1.48
//14 1.3
//*/
//        return 8.887064785e-5f * x3 - 9.398102638e-3f * x2 + 1.439482893e-1f * x + 8.857717866e-1f;
    }

    private float GetDragCoefficient(float angleOfAttackDeg)
    {
        return DragCoefficient;
//        float x = angleOfAttackDeg;
//        float x2 = x * x;
//        float x3 = x * x2;
//        /*
//        Coefficient found by regression of graph samples:
//        http://www.xuru.org/rt/PR.asp#Manually
//-2 .18
//0 .19
//4 .25
//8 .35
//10 .42
//12 .6
//14 1.1
//*/
//        return 7.279089406e-4f * x3 - 7.467534083e-3f * x2 + 2.119913811e-2f * x + 2.330367864e-1f;
    }

    private void SetRagdollEnabled(bool isEnabled)
    {
        List<Collider> ragdollColliders = GetComponentsInChildren<Collider>()
            .Where(InRagdollLayer)
            .ToList();
        List<Rigidbody> ragdollBodies = ragdollColliders
            .Select(col => col.gameObject.GetComponent<Rigidbody>())
            .Where(body => body != null)
            .ToList();

        Collider mainCollider = GetComponentsInChildren<Collider>().First(col => !InRagdollLayer(col));
        mainCollider.enabled = !isEnabled;
        Rigidbody mainBody = GetComponent<Rigidbody>();
        mainBody.detectCollisions = !isEnabled;

        foreach (Collider col in ragdollColliders)
        {
            col.enabled = isEnabled;
            col.isTrigger = !isEnabled;
        }
        foreach (Rigidbody body in ragdollBodies)
        {
            body.isKinematic = !isEnabled;
            body.detectCollisions = isEnabled;
            body.velocity = transform.TransformVector(_localVelocity);
            body.angularVelocity = Vector3.zero;
        }

        // Disable kinematic scripts (let physics take control)
        IsKinematic = !isEnabled;

        // Disable animation to enable ragdoll control of limbs
        _animator.enabled = !isEnabled;
    }

}