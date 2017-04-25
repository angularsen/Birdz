using UnityEngine;

namespace UnityStandardAssets.Cameras
{
    public abstract class PivotBasedCameraRig : AbstractTargetFollower
    {
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 	Camera Rig
        // 		Pivot
        // 			Camera

        protected Transform Cam; // the transform of the camera
        protected Vector3 LastTargetPosition;
        protected Transform Pivot; // the point at which the camera pivots around


        protected virtual void Awake()
        {
            // find the camera in the object hierarchy
            Cam = GetComponentInChildren<Camera>().transform;
            Pivot = Cam.parent;
        }
    }
}