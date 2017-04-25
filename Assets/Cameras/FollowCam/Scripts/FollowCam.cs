using System;
using UnityEngine;
// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

// Place the script in the Camera-Control group in the component menu
[AddComponentMenu("Camera-Control/FollowCam")]
class FollowCam : MonoBehaviour
{
    // Distance behind the target
    public float Distance = 5.0f;

    // Height above the target
    public float Height = 1.0f;

    public LookAheadDirectionType LookAheadDirection = LookAheadDirectionType.TargetVelocity;

    // True to enable springs to smoothly move camera into place, and create a natural 'lag' for rapid change in motion
    public bool Smooth = true;

    // Seconds to look ahead of target in target's velocity vector
    public float LookAheadTime = 1;

    public float MaxLookAheadDistance = 3;

    public float SpringFactor = 20;

    /// <summary>Target to follow and look in relation to.</summary>
    public Transform Target;

    private Transform _prevTargetTransform;
    private Transform _prevTransform;

    void Start()
    {
        _prevTransform = transform;
        _prevTargetTransform = Target;
    }

    void LateUpdate()
    {
        if (!Target)
            return;

        float dt = Time.deltaTime;

        Vector3 wantedPosition = Target.position + (Target.up * Height - Target.forward * Distance);
        Vector3 springAcceleration = (wantedPosition - transform.position) * SpringFactor * SpringFactor;
        Vector3 totalAcceleration = springAcceleration;

        if (Smooth)
        {
            transform.position += 0.5f * totalAcceleration * dt * dt;
        }
        else
        {
            transform.position = wantedPosition;
        }

        Vector3 lookAheadOffset = GetLookAheadOffset(dt);
        transform.LookAt(Target.position + lookAheadOffset, Target.up);
        _prevTargetTransform = Target;
        _prevTransform = transform;
    }

    Vector3 GetLookAheadOffset(float dt)
    {
        Vector3 targetVelocity = _prevTargetTransform == null
            ? Vector3.zero
            : (Target.position - _prevTargetTransform.position) / dt;

        float lookAheadDistance = Mathf.Min(MaxLookAheadDistance, targetVelocity.magnitude * LookAheadTime);
        Vector3 lookAheadDirection;
        switch (LookAheadDirection)
        {
            case LookAheadDirectionType.TargetVelocity:
                lookAheadDirection = targetVelocity;
                break;
            case LookAheadDirectionType.TargetForward:
                lookAheadDirection = Target.rotation * Vector3.forward;
                break;
            default:
                throw new NotImplementedException("LookAheadDirectionType: " + LookAheadDirection);
        }

        // Always look at the target
        Vector3 lookAheadOffset = lookAheadDirection * lookAheadDistance;
        return lookAheadOffset;
    }
}

enum LookAheadDirectionType
{
    TargetVelocity,
    TargetForward
}