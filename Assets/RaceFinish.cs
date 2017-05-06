using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

public class RaceFinish : MonoBehaviour
{
    private Text _finishedText;

//    private bool _finished;
    private Text _timeText;

    private float _startTime;

    // Use this for initialization
    void Start()
    {
        _finishedText = GameObject.Find("FinishedText").GetComponent<Text>();
        _timeText = GameObject.Find("TimeText").GetComponent<Text>();
//    private Canvas _hud;
        _finishedText.enabled = false;
        _startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_finishedText.enabled)
        {
            _timeText.text = string.Format("Time: {0:0.0} s", Time.time - _startTime);
        }
    }

//    void OnGUI()
//    {
//        if (_finished)
//            GUI.Label(new Rect(10, 200, 300, 50), "FINISHED!");
//    }

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == Tags.Player)
        {
            _finishedText.enabled = true;
        }
    }
}