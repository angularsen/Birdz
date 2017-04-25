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
    public float DragFactor = 0.001f; 
    public float SlipDragFactor = 0.1f;
    public float SlipFactor = 0.1f; 
    public float MassKg = 1.0f;
    public float MaxThrustN = 10;
    public float MaxYawRateDps = 90;
    public float MaxPitchRateDps = 90;
    public float MaxRollRateDps = 180;

    private Animator _animator;
    private float _forwardSpeed;
    private float _slipSpeed;
//    private Vector3 _gravitySpeed;
    private Transform _prevTransform;
    private const int LabelHeight = 20;
    private static readonly string[] _debugLabels = new string[10];

    private static readonly Rect[] _debugLabelRects = _debugLabels
        .Select((str, i) => new Rect(10, 10 + i * 20, 300, LabelHeight))
        .ToArray();

    public void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _prevTransform = transform;
    }

    public void Update()
    {
        // TODO Move me to a game manager object instead
        if (Input.GetButtonDown("Reset"))
            SceneManager.LoadScene(0);

        float dt = Time.fixedDeltaTime;
        float thrustForce = (Input.GetAxis("Thrust") - Input.GetAxis("Brake")) * MaxThrustN;
        float rollDelta = -Input.GetAxis("Roll") * MaxRollRateDps;
        float pitchDelta = Input.GetAxis("Pitch") * MaxPitchRateDps;
        float yawDelta = Input.GetAxis("Yaw") * MaxYawRateDps;
        Vector3 localUp = transform.localRotation * Vector3.up;
        float slipForce = SlipFactor * localUp.x;

        _forwardSpeed += thrustForce / MassKg * dt - _forwardSpeed * DragFactor;
        _slipSpeed += slipForce / MassKg * dt - _slipSpeed * SlipDragFactor;

        Vector3 velocity = transform.forward * _forwardSpeed + transform.right * _slipSpeed;
//        Vector3 dragForce = -velocity * DragFactor;

        transform.position += velocity * dt;
        transform.Rotate(new Vector3(pitchDelta, yawDelta, rollDelta) * dt, Space.Self);

        // Yaw right and pitch down when slipping down to the right when doing a right-roll swing
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity, transform.up), 0.5f * dt);

        _animator.SetFloat(AnimatorParams.Speed, _forwardSpeed);
        _prevTransform = transform;

        int i = 0;
        _debugLabels[i++] = string.Format("Speed[{0}={1}]", _forwardSpeed, velocity.magnitude);
        _debugLabels[i++] = string.Format("Thrust[{0}={1}]", thrustForce, thrustForce);
//        _debugLabels[i++] = string.Format("Drag[{0}={1}]", dragForce, dragForce.magnitude);
        _debugLabels[i++] = string.Format("Slip[{0}={1}]", slipForce, slipForce);
        _debugLabels[i++] = string.Format("Forward{0}, Up{1}, Right{2}", transform.forward, transform.up, transform.right);
        _debugLabels[i++] = string.Format("Thrust axis: {0}", Input.GetAxis("Thrust"));
    }

    void OnGUI()
    {
        for (var i = 0; i < _debugLabels.Length; i++)
        {
            string label = _debugLabels[i];
            if (label == null) continue;

            Rect rect = _debugLabelRects[i];
            GUI.Label(rect, label);
        }
    }
}