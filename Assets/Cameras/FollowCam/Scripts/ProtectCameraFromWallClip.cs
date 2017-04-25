using System;
using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Cameras
{
    public class ProtectCameraFromWallClip : MonoBehaviour
    {
        public float ClipMoveTime = 0.05f;              // time taken to move when avoiding cliping (low value = fast, which it should be)
        public float ReturnTime = 0.4f;                 // time taken to move back towards desired position, when not clipping (typically should be a higher value than clipMoveTime)
        public float SphereCastRadius = 0.1f;           // the radius of the sphere used to test for object between camera and target
        public bool VisualiseInEditor;                  // toggle for visualising the algorithm through lines for the raycast in the editor
        public float ClosestDistance = 0.5f;            // the closest distance the camera can be from the target
        public bool Protecting { get; private set; }    // used for determining if there is an object between the target and the camera
        public string DontClipTag = "Player";           // don't clip against objects with this tag (useful for not clipping against the targeted object)

        private Transform _cam;                  // the transform of the camera
        private Transform _pivot;                // the point at which the camera pivots around
        private float _originalDist;             // the original distance to the camera before any modification are made
        private float _moveVelocity;             // the velocity at which the camera moved
        private float _currentDist;              // the current distance from the camera to the target
        private Ray _ray = new Ray();                        // the ray used in the lateupdate for casting between the camera and the target
        private RaycastHit[] _hits;              // the hits between the camera and the target
        private RayHitComparer _rayHitComparer;  // variable to compare raycast hit distances


        private void Start()
        {
            // find the camera in the object hierarchy
            _cam = GetComponentInChildren<Camera>().transform;
            _pivot = _cam.parent;
            _originalDist = _cam.localPosition.magnitude;
            _currentDist = _originalDist;

            // create a new RayHitComparer
            _rayHitComparer = new RayHitComparer();
        }


        private void LateUpdate()
        {
            // initially set the target distance
            float targetDist = _originalDist;

            _ray.origin = _pivot.position + _pivot.forward*SphereCastRadius;
            _ray.direction = -_pivot.forward;

            // initial check to see if start of spherecast intersects anything
            var cols = Physics.OverlapSphere(_ray.origin, SphereCastRadius);

            bool initialIntersect = false;
            bool hitSomething = false;

            // loop through all the collisions to check if something we care about
            for (int i = 0; i < cols.Length; i++)
            {
                if ((!cols[i].isTrigger) &&
                    !(cols[i].attachedRigidbody != null && cols[i].attachedRigidbody.CompareTag(DontClipTag)))
                {
                    initialIntersect = true;
                    break;
                }
            }

            // if there is a collision
            if (initialIntersect)
            {
                _ray.origin += _pivot.forward*SphereCastRadius;

                // do a raycast and gather all the intersections
                _hits = Physics.RaycastAll(_ray, _originalDist - SphereCastRadius);
            }
            else
            {
                // if there was no collision do a sphere cast to see if there were any other collisions
                _hits = Physics.SphereCastAll(_ray, SphereCastRadius, _originalDist + SphereCastRadius);
            }

            // sort the collisions by distance
            Array.Sort(_hits, _rayHitComparer);

            // set the variable used for storing the closest to be as far as possible
            float nearest = Mathf.Infinity;

            // loop through all the collisions
            for (int i = 0; i < _hits.Length; i++)
            {
                // only deal with the collision if it was closer than the previous one, not a trigger, and not attached to a rigidbody tagged with the dontClipTag
                if (_hits[i].distance < nearest && (!_hits[i].collider.isTrigger) &&
                    !(_hits[i].collider.attachedRigidbody != null &&
                      _hits[i].collider.attachedRigidbody.CompareTag(DontClipTag)))
                {
                    // change the nearest collision to latest
                    nearest = _hits[i].distance;
                    targetDist = -_pivot.InverseTransformPoint(_hits[i].point).z;
                    hitSomething = true;
                }
            }

            // visualise the cam clip effect in the editor
            if (hitSomething)
            {
                Debug.DrawRay(_ray.origin, -_pivot.forward*(targetDist + SphereCastRadius), Color.red);
            }

            // hit something so move the camera to a better position
            Protecting = hitSomething;
            _currentDist = Mathf.SmoothDamp(_currentDist, targetDist, ref _moveVelocity,
                                           _currentDist > targetDist ? ClipMoveTime : ReturnTime);
            _currentDist = Mathf.Clamp(_currentDist, ClosestDistance, _originalDist);
            _cam.localPosition = -Vector3.forward*_currentDist;
        }


        // comparer for check distances in ray cast hits
        public class RayHitComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((RaycastHit) x).distance.CompareTo(((RaycastHit) y).distance);
            }
        }
    }
}
