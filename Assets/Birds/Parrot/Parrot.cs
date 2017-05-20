// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public const string Vertical = "Vertical";
    public const string Horizontal = "Horizontal";
}

static class AnimatorParams
{
    internal const string FlapSpeedMultiplier = "FlapSpeedMultiplier";
    internal const string Speed = "Speed m:s";
    internal const string ThrustInput = "ThrustInput";
    public const string Grounded = "Grounded";
}

static class Tags
{
    internal const string Untagged = "Untagged";
    internal const string MainCamera = "MainCamera";
    internal const string Player = "Player";
    public const string PlayerCollider = "PlayerCollider";
    public const string Finish = "Finish";
    public const string Respawn = "Respawn";
}

enum BirdState
{
    Flying,
    Landing,
    Grounded,
    TakeOff,
    Crashing,
}

// Place the script in the Camera-Control group in the component menu
[AddComponentMenu("Birds/Parrot/Script")]
public class Parrot : MonoBehaviour
{
    private const int LabelHeight = 20;
    private const float FlyingAnimationHeight = 0.765f;
    private static readonly string[] DebugLabels = new string[10];

    private static readonly Rect[] DebugLabelRects = DebugLabels
        .Select((str, i) => new Rect(10, 10 + i * 20, 300, LabelHeight))
        .ToArray();

    private Animator _animator;

    /// <summary>
    ///     Velocity in Local coordinates (body frame).
    /// </summary>
    private Vector3 _localVelocity;

//    private Transform _prevTransform;

    // Forward velocity has little drag, but sideways or up/downwards velocity has much higher drag coefficients.
    // Forward body drag of 0.1 found on page 51 in 'Modelling Bird Flight': https://books.google.no/books?id=KG86AgWwFEUC&pg=PA73&lpg=PA73&dq=bird+drag+coefficient&source=bl&ots=RuK6WpSQWJ&sig=S3HbzUEQVtMxQ69gZyKGqvXzAO0&hl=en&sa=X&ved=0ahUKEwjU7KnZrsrTAhWCbZoKHcdJDTcQ6AEIkQEwFg#v=onepage&q=bird%20drag%20coefficient&f=false
    public Vector3 BodyDragFactors = new Vector3(0.1f, 1f, 0.001f);

    public float DragCoefficient = 0.001f;
    public float LiftCoefficient = 0.1f;

    public float MassKg = 1.0f;
    public float MaxPitchRateDps = 90;
    public float MaxRollRateDps = 180;
    public float MaxThrustN = 10;
    public float MaxBrakeN = 3;

    public float MaxYawRateDps = 30;

//    public float LiftFactor = 10f;
    public float RotateIntoWindLerpFactor = .7f;

    /// <summary>Audio source for the airflow of winds and when moving in air.</summary>
    private AudioSource _audioAirflow;

    /// <summary>Audio source for collisions.</summary>
    private AudioSource _audioCollision;

    /// <summary>Time when the flap animation started last time, used to apply thrust in sync with animation.</summary>
    private float _flapStartTime;

    private BirdState _state;

    // Once on script load, after all game objects are created and can be referenced by .Find()
    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        _audioAirflow = audioSources.First(x => x.name == "AudioSourceAirflow");
        _audioCollision = audioSources.First(x => x.name == "AudioSourceCollision");

        // Ignore collisions between player bounding box (for collision detection) and its ragdoll colliders (for ragdoll effect)
        Physics.IgnoreLayerCollision((int) Layers.Player, (int) Layers.PlayerRagdoll);
    }

    // On start, after Awake()
    void Start()
    {
        OnLevelLoad();
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        switch (_state)
        {
            case BirdState.Grounded:
                HandleGrounded();
                break;
            case BirdState.Flying:
            case BirdState.Landing:
            case BirdState.TakeOff:
                HandleFlying();
                break;
            case BirdState.Crashing:
                // Let physics do its thing
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnLevelLoad()
    {
        SetState(BirdState.Flying);

        // Start with a forward velocity
        _localVelocity = Vector3.forward * 10;

        SetRagdollEnabled(false);
    }

    private void HandleFlying()
    {
        float dt = Time.fixedDeltaTime;
        float timeSinceFlapStart = Time.time - _flapStartTime;

        // Only apply thrust during the downwards flap motion (0.5s time window from start of downwards flap animation)
        float thrustInput = Input.GetAxis(InputNames.Thrust);

        float thrustInputWhenFlapping = thrustInput * Gaussian(timeSinceFlapStart, 0.1f, 0.01f);
        float thrustForce = (thrustInputWhenFlapping) * MaxThrustN;
        float brakeForce = Input.GetAxis(InputNames.Brake) * MaxBrakeN;
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

        Vector3 localThrust = Vector3.forward * (thrustForce - brakeForce);
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

        float newFwdSpeed = newLocalVelocity.z;

        // Update animator parameters
        _animator.SetFloat(AnimatorParams.Speed, newFwdSpeed);
        _animator.SetFloat(AnimatorParams.ThrustInput, thrustInput);
        _animator.SetFloat(AnimatorParams.FlapSpeedMultiplier, 1 + thrustInput * 2);

        // Update audio parameters
        UpdateAirflowAudioByVelocity(newLocalVelocity);
        _localVelocity = newLocalVelocity;

        #region Debug graphics
        Vector3 localVelocityKmh = newLocalVelocity * 3.6f;
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

        #endregion
//        _prevTransform = prevTransform;
    }

    private void HandleGrounded()
    {
        float thrustInput = Input.GetAxis(InputNames.Thrust);
        if (thrustInput > 0.5f)
        {
            SetState(BirdState.TakeOff);
            // Initial boost/jump
            _localVelocity = 5f * Vector3.forward + 5f * Vector3.up;
            transform.position += _localVelocity * 0.2f;
        }
    }

    private void SetState(BirdState state)
    {
        print("Parrot: SetState: " + state);
        _state = state;

        // TODO Use a string to expose all states to animator instead
        _animator.SetBool(AnimatorParams.Grounded, state == BirdState.Grounded);
    }

    private static float Gaussian(float x, float xMax, float stdDev)
    {
        return Mathf.Exp(-(x - xMax) * (x - xMax) / 2 / (stdDev * stdDev));
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

    void OnTriggerExit(Collider col)
    {
        Debug.Log("Trigger exit: " + col.name);
        if (_state == BirdState.TakeOff)
        {
            SetState(BirdState.Flying);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        Debug.Log("Trigger enter: " + col.name);
        if (_state == BirdState.TakeOff)
            return;

        if (col.name.StartsWith("Terrain ") || col.tag == Tags.Finish)
        {
            if (_localVelocity.magnitude < 5)
                Land();
            else
                Crash(col);
        }
        else if (col.name.StartsWith("tree"))
        {
            Crash(col);
        }
    }

    private void Land()
    {
        Debug.Log("Landed.");
        SetState(BirdState.Grounded);
        _localVelocity = Vector3.zero;
        _animator.SetFloat(AnimatorParams.Speed, 0);
        UpdateAirflowAudioByVelocity(Vector3.zero);

        // Rotate back to upright position
        transform.rotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
    }

    private void Crash(Collider col)
    {
        Vector3 velocity = transform.TransformVector(_localVelocity);

        // Increase collision volume with speed, and use a minimum volume of 5%
        _audioCollision.volume = Mathf.Lerp(0.01f, 1, Mathf.InverseLerp(0, 30, velocity.magnitude));
        _audioCollision.Play();

        Debug.Log("Hit ground: " + col.name);

        SetRagdollEnabled(true, velocity);

        _localVelocity = Vector3.zero;
        UpdateAirflowAudioByVelocity(Vector3.zero);
        SetState(BirdState.Crashing);
    }

    // Called by ParrotModel->AnimationSoundPlayer via SendMessageUpwards()
    void OnAnimationFlapStart()
    {
//        Debug.Log("SendMessageUpwards RECEIVED");
        _flapStartTime = Time.time;
    }

    internal BirdState GetState()
    {
        return _state;
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

    private void SetRagdollEnabled(bool isEnabled, Vector3? bodyVelocity = null)
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
            if (isEnabled)
            {
                if (bodyVelocity != null) body.velocity = bodyVelocity.Value;
                body.angularVelocity = Vector3.zero;
            }
        }

        // Disable animation to enable ragdoll control of limbs
        _animator.enabled = !isEnabled;
    }

    private void UpdateAirflowAudioByVelocity(Vector3 newLocalVelocity)
    {
        float volumeLerp = Mathf.InverseLerp(0, 15, newLocalVelocity.magnitude);
        float pitchLerp = Mathf.InverseLerp(0, 25, newLocalVelocity.magnitude);
        _audioAirflow.volume = volumeLerp;
        _audioAirflow.pitch = Mathf.Lerp(0.5f, 3f, pitchLerp);
    }

    private float Angle360ToPlusMinus180(float angleDeg)
    {
        return angleDeg > 180 ? angleDeg - 360 : angleDeg;
    }
}