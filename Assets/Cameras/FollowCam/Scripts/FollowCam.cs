using System;
using UnityEngine;
// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

// Place the script in the Camera-Control group in the component menu
[AddComponentMenu("Camera-Control/FollowCam")]
class FollowCam : MonoBehaviour
{
    // Distance behind the target
    public float Distance = 1.0f;

    // Height above the target
    public float Height = 0.25f;

    public LookAheadDirectionType LookAheadDirection = LookAheadDirectionType.TargetVelocity;

    // True to enable springs to smoothly move camera into place, and create a natural 'lag' for rapid change in motion
    public bool Smooth = true;

    // Seconds to look ahead of target in target's velocity vector
    public float LookAheadTime = 1;

    public float MaxLookAheadDistance = 3;

    public float SpringFactor = 20;

    /// <summary>Target to follow and look in relation to.</summary>
    public Transform Target;

    private Vector3 _prevTargetPosition;
    private float _prevTargetTime;
    private Vector3 _prevLookAheadOffset;

    void Start()
    {
        _prevTargetPosition = Target.position;
        _prevLookAheadOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        if (!Target)
            return;

        float dt = Time.deltaTime;
        Vector3 lookAheadOffset = GetLookAheadOffset();
//        Vector3 wantedPosition = Target.position + (Target.up * Height - lookAheadOffset.normalized * Distance);
        Vector3 wantedPosition = Target.position + (Target.up * Height - Target.forward * Distance);

        if (Smooth)
        {
            Vector3 springAcceleration = (wantedPosition - transform.position) * SpringFactor * SpringFactor;
            Vector3 totalAcceleration = springAcceleration;
            transform.position += 0.5f * totalAcceleration * dt * dt;
        }
        else
        {
            transform.position = wantedPosition;
        }

        // Ease into changes of look at rotation by .5 seconds
        //        transform.rotation = Quaternion.Lerp(transform.rotation,
        //            Quaternion.LookRotation(Target.position + lookAheadOffset, Target.up), 0.5f * dt);
        transform.LookAt(Target.position + lookAheadOffset, Target.up);
    }

    Vector3 GetLookAheadOffset()
    {
        float targetDisplacementTime = Time.time - _prevTargetTime;
        if (targetDisplacementTime < 0.02f)
        {
            return _prevLookAheadOffset;
        }
        Vector3 targetDisplacement = Target.position - _prevTargetPosition;
        Vector3 targetVelocity = targetDisplacement / targetDisplacementTime;
        if (targetVelocity.magnitude < 0.1f)
        {
            return Vector3.zero;
        }
        _prevTargetPosition = Target.position;
        _prevTargetTime = Time.time;

        float lookAheadDistance = Mathf.Min(MaxLookAheadDistance, targetVelocity.magnitude * LookAheadTime);
        Vector3 lookAheadDirection;
        switch (LookAheadDirection)
        {
            case LookAheadDirectionType.TargetVelocity:
                lookAheadDirection = targetDisplacement.normalized;
                break;
            case LookAheadDirectionType.TargetForward:
                lookAheadDirection = Target.forward;
                break;
            default:
                throw new NotImplementedException("LookAheadDirectionType: " + LookAheadDirection);
        }

        // Always look at the target
        Vector3 lookAheadOffset = lookAheadDirection * lookAheadDistance;
        _prevLookAheadOffset = lookAheadOffset;
        return lookAheadOffset;
    }
}

enum FollowType
{
    FixedRelativeToLocal,
    FollowTrail,
}

enum LookAheadDirectionType
{
    TargetVelocity,
    TargetForward
}