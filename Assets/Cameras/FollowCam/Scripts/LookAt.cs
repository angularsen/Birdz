using UnityEngine;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

enum UpDirection
{
    World,
    Target
}

[AddComponentMenu("Camera-Control/LookAt")]
class LookAt : MonoBehaviour
{
    /// <summary>Offset relative to <see cref="Target" /> to look at.</summary>
    public Vector3 Offset;

    /// <summary>Target to follow and look in relation to.</summary>
    public Transform Target;

    /// <summary>What up direction to use when looking at target.</summary>
    public UpDirection UpDirection;

    void Start()
    {
    }

    void LateUpdate()
    {
        if (!Target)
            return;

        Vector3 up = UpDirection == UpDirection.World ? Vector3.up : Target.up;
        transform.LookAt(Target.position + Target.TransformVector(Offset), up);
    }
}