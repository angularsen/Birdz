using System;
using System.Linq;
using UnityEngine;

// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

static class CameraNames
{
    internal const string FollowCam = "FollowCam";
    internal const string FixedCam = "FixedCam";
    internal const string FreeCam = "FreeCam";
}

[AddComponentMenu("Camera-Control/CameraSwitcher")]
public class CameraSwitcher : MonoBehaviour
{
    private Camera[] _cameras;
    private Camera _followCam;
    private Camera _fixedCam;
    private Camera _freeCam;

    // ReSharper restore FieldCanBeMadeReadOnly.Local

    // Use this for initialization
    void Start()
    {
        Camera[] cameras = Camera.allCameras;
        _cameras = cameras;
        _followCam = cameras.First(x => x.name == CameraNames.FollowCam);
        _fixedCam = cameras.First(x => x.name == CameraNames.FixedCam);
        _freeCam = cameras.First(x => x.name == CameraNames.FreeCam);
    }

    // Update is called once per frame
    void Update()
    {
        // Optimization
        if (!Input.anyKey) return;

        Camera nextCam = null;
        if (Input.GetButtonDown(InputNames.FollowCam))
        {
            nextCam = _followCam;
        }
        else if (Input.GetButtonDown(InputNames.FixedCam))
        {
            nextCam = _fixedCam;
        }
        else if (Input.GetButtonDown(InputNames.FreeCam))
        {
            nextCam = _freeCam;
        }
        else if (Input.GetButtonDown(InputNames.NextCam))
        {
            nextCam = GetNextCam();
        }

        if (nextCam == null)
            return;
        SwitchToCamera(nextCam);
    }

    private Camera GetNextCam()
    {
        int currIdx = Array.IndexOf(_cameras, Camera.main);
        return _cameras[(currIdx + 1) % _cameras.Length];
    }

    private void SwitchToCamera(Camera switchToCam)
    {
        if (switchToCam == _freeCam || switchToCam == _fixedCam)
        {
            // Move free cam to match current cam for a natural transition
            Transform mainCamTrans = Camera.main.transform;
            switchToCam.transform.SetPositionAndRotation(mainCamTrans.position, mainCamTrans.rotation);
        }

        Debug.Log("Switching to camera: " + switchToCam);
        foreach (Camera cam in _cameras)
            // Enable camera and disable all others
            cam.enabled = cam == switchToCam;
    }
}