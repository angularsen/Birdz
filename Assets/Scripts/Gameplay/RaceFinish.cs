using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

/// <summary>
///     Place on finish object to collide into.
///     Requires:
///     - Collider with 'Is Trigger' disabled and no rigid body attached
/// </summary>
public class RaceFinish : MonoBehaviour
{
    private Text _finishedText;
    private float _startTime;
    private Text _timeText;

    // Use this for initialization
    void Start()
    {
        _finishedText = GameObject.Find("FinishedText").GetComponent<Text>();
        _timeText = GameObject.Find("TimeText").GetComponent<Text>();
        _finishedText.enabled = false;
        _startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_finishedText.enabled)
            _timeText.text = string.Format("Time: {0:0.0} s", Time.time - _startTime);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == Tags.PlayerCollider)
            _finishedText.enabled = true;
    }
}