using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

/// <summary>
///     Place on finish object to collide into.
///     Requires:
///     - Collider with 'Is Trigger' disabled and no rigid body attached
/// </summary>
public class LandOnFinish : MonoBehaviour
{
    private Text _finishedText;
    private bool _isBirdInLandingZone;
    private GameObject _playerBird;
    private Parrot _playerScript;
    private float _startTime;
    private Text _timeText;
    private AudioSource _cheerAudio;

    // Use this for initialization
    void Start()
    {
        _finishedText = GameObject.Find("FinishedText").GetComponent<Text>();
        _timeText = GameObject.Find("TimeText").GetComponent<Text>();
        _finishedText.enabled = false;
        _startTime = Time.time;
        _playerBird = GameObject.FindGameObjectsWithTag("Player").Single();
        _playerScript = _playerBird.GetComponent<Parrot>();
        _cheerAudio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_finishedText.enabled)
            _timeText.text = string.Format("Time: {0:0.0} s", Time.time - _startTime);

        if (_isBirdInLandingZone && _playerScript.GetState() == BirdState.Grounded)
        {
            _finishedText.enabled = true;
            _cheerAudio.Play();
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == Tags.PlayerCollider)
        {
            Debug.Log("Player entered landing zone.");
            _isBirdInLandingZone = true;
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (col.tag == Tags.PlayerCollider)
        {
            Debug.Log("Player left landing zone.");
            _isBirdInLandingZone = false;
        }
    }
}