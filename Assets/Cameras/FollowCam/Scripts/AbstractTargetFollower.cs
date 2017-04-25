using UnityEngine;

namespace UnityStandardAssets.Cameras
{
    public abstract class AbstractTargetFollower : MonoBehaviour
    {
        public enum UpdateType // The available methods of updating are:
        {
            FixedUpdate, // Update in FixedUpdate (for tracking rigidbodies).
            LateUpdate, // Update in LateUpdate. (for tracking objects that are moved in Update)
            ManualUpdate // user must call to update camera
        }

        // Whether the rig should automatically target the player.
        [SerializeField] private readonly bool _autoTargetPlayer = true;

        [SerializeField] protected Transform _target; // The target object to follow
        [SerializeField] protected Vector3 _targetOffset = Vector3.zero; // The offset relative to target to look at
        [SerializeField] private UpdateType _updateType; // stores the selected update type

        protected Rigidbody TargetRigidbody;

        protected virtual void Start()
        {
            // if auto targeting is used, find the object tagged "Player"
            // any class inheriting from this should call base.Start() to perform this action!
            if (_autoTargetPlayer)
                FindAndTargetPlayer();
            if (_target == null) return;
            TargetRigidbody = _target.GetComponent<Rigidbody>();
        }


        private void FixedUpdate()
        {
            // we update from here if updatetype is set to Fixed, or in auto mode,
            // if the target has a rigidbody, and isn't kinematic.
            if (_autoTargetPlayer && (_target == null || !_target.gameObject.activeSelf))
                FindAndTargetPlayer();
            if (_updateType == UpdateType.FixedUpdate)
                FollowTarget(Time.deltaTime);
        }


        private void LateUpdate()
        {
            // we update from here if updatetype is set to Late, or in auto mode,
            // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
            if (_autoTargetPlayer && (_target == null || !_target.gameObject.activeSelf))
                FindAndTargetPlayer();
            if (_updateType == UpdateType.LateUpdate)
                FollowTarget(Time.deltaTime);
        }


        public void ManualUpdate()
        {
            // we update from here if updatetype is set to Late, or in auto mode,
            // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
            if (_autoTargetPlayer && (_target == null || !_target.gameObject.activeSelf))
                FindAndTargetPlayer();
            if (_updateType == UpdateType.ManualUpdate)
                FollowTarget(Time.deltaTime);
        }

        protected abstract void FollowTarget(float deltaTime);


        private void FindAndTargetPlayer()
        {
            // auto target an object tagged player, if no target has been assigned
            GameObject targetObj = GameObject.FindGameObjectWithTag("Player");
            if (targetObj)
                _target = targetObj.transform;
        }
    }
}