using System;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace UnityStandardAssets.Cameras
{
    [ExecuteInEditMode]
    public class AutoCam : PivotBasedCameraRig
    {
        [SerializeField] private float _moveSpeed = 3; // How fast the rig will move to keep up with target's position
        [SerializeField] private float _turnSpeed = 1; // How fast the rig will turn to keep up with target's rotation
        [SerializeField] private float _rollSpeed = 0.2f;// How fast the rig will roll (around Z axis) to match target's roll.
        [SerializeField] private bool _followVelocity = false;// Whether the rig will rotate in the direction of the target's velocity.
        [SerializeField] private bool _followTilt = true; // Whether the rig will tilt (around X axis) with the target.
        [SerializeField] private float _spinTurnLimit = 90;// The threshold beyond which the camera stops following the target's rotation. (used in situations where a car spins out, for example)
        [SerializeField] private float _targetVelocityLowerLimit = 4f;// the minimum velocity above which the camera turns towards the object's velocity. Below this we use the object's forward direction.
        [SerializeField] private float _smoothTurnTime = 0.2f; // the smoothing for the camera's rotation

        private float _lastFlatAngle; // The relative angle of the target and the rig from the previous frame.
        private float _currentTurnAmount; // How much to turn the camera
        private float _turnSpeedVelocityChange; // The change in the turn speed velocity
        private Vector3 _rollUp = Vector3.up;// The roll of the camera around the z axis ( generally this will always just be up )


        protected override void FollowTarget(float deltaTime)
        {
            // if no target, or no time passed then we quit early, as there is nothing to do
            if (!(deltaTime > 0) || _target == null)
            {
                return;
            }

            // initialise some vars, we'll be modifying these in a moment
            var targetForward = _target.forward;
            var targetUp = _target.up;

            if (_followVelocity && Application.isPlaying)
            {
                // in follow velocity mode, the camera's rotation is aligned towards the object's velocity direction
                // but only if the object is traveling faster than a given threshold.

                if (TargetRigidbody.velocity.magnitude > _targetVelocityLowerLimit)
                {
                    // velocity is high enough, so we'll use the target's velocty
                    targetForward = TargetRigidbody.velocity.normalized;
                    targetUp = Vector3.up;
                }
                else
                {
                    targetUp = Vector3.up;
                }
                _currentTurnAmount = Mathf.SmoothDamp(_currentTurnAmount, 1, ref _turnSpeedVelocityChange, _smoothTurnTime);
            }
            else
            {
                // we're in 'follow rotation' mode, where the camera rig's rotation follows the object's rotation.

                // This section allows the camera to stop following the target's rotation when the target is spinning too fast.
                // eg when a car has been knocked into a spin. The camera will resume following the rotation
                // of the target when the target's angular velocity slows below the threshold.
                var currentFlatAngle = Mathf.Atan2(targetForward.x, targetForward.z)*Mathf.Rad2Deg;
                if (_spinTurnLimit > 0)
                {
                    var targetSpinSpeed = Mathf.Abs(Mathf.DeltaAngle(_lastFlatAngle, currentFlatAngle))/deltaTime;
                    var desiredTurnAmount = Mathf.InverseLerp(_spinTurnLimit, _spinTurnLimit*0.75f, targetSpinSpeed);
                    var turnReactSpeed = (_currentTurnAmount > desiredTurnAmount ? .1f : 1f);
                    if (Application.isPlaying)
                    {
                        _currentTurnAmount = Mathf.SmoothDamp(_currentTurnAmount, desiredTurnAmount,
                                                             ref _turnSpeedVelocityChange, turnReactSpeed);
                    }
                    else
                    {
                        // for editor mode, smoothdamp won't work because it uses deltaTime internally
                        _currentTurnAmount = desiredTurnAmount;
                    }
                }
                else
                {
                    _currentTurnAmount = 1;
                }
                _lastFlatAngle = currentFlatAngle;
            }

            // camera position moves towards target position:
            transform.position = Vector3.Lerp(transform.position, _target.position, deltaTime*_moveSpeed);

            // camera's rotation is split into two parts, which can have independend speed settings:
            // rotating towards the target's forward direction (which encompasses its 'yaw' and 'pitch')
            if (!_followTilt)
            {
                targetForward.y = 0;
                if (targetForward.sqrMagnitude < float.Epsilon)
                {
                    targetForward = transform.forward;
                }
            }
            var rollRotation = Quaternion.LookRotation(targetForward, _rollUp);

            // and aligning with the target object's up direction (i.e. its 'roll')
            _rollUp = _rollSpeed > 0 ? Vector3.Slerp(_rollUp, targetUp, _rollSpeed*deltaTime) : Vector3.up;
            transform.rotation = Quaternion.Lerp(transform.rotation, rollRotation, _turnSpeed*_currentTurnAmount*deltaTime);
        }
    }
}
