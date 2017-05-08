using UnityEngine;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

public class Bobbing : MonoBehaviour
{
    private Vector3 _startPos;

    // Use this for initialization
    void Start()
    {
        _startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = _startPos + new Vector3(0, .5f * Mathf.Sin(Time.time * 6.28f), 0);
    }
}