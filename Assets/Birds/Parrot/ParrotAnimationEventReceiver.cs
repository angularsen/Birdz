using UnityEngine;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

public class ParrotAnimationEventReceiver : MonoBehaviour
{
    private AudioSource _audioFlap;

    // Use this for initialization
    void Start()
    {
        _audioFlap = GameObject.Find("AudioSourceFlap").GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FlapStart()
    {
        _audioFlap.Play();
        Debug.Log("SendMessageUpwards");
        SendMessageUpwards("OnAnimationFlapStart");
    }
}