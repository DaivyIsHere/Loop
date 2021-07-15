using Mirror;
using System;
using UnityEngine;

namespace FirstGearGames.Mirror.ColliderRollbacks
{

    public class ColliderRollback : NetworkBehaviour
    {
        #region Types.
        protected enum FrameRollbackTypes
        {
            LerpFirst,
            LerpMiddle,
            Exact
        }
        /// <summary>
        /// Used to store where colliders are during the snapshot.
        /// </summary>
        private struct ColliderSnapshot
        {
            public ColliderSnapshot(Transform t)
            {
                WorldPosition = t.position;
                WorldRotation = t.rotation;
            }

            /// <summary>
            /// WorldPosition of transform during snapshot.
            /// </summary>
            public Vector3 WorldPosition;
            /// <summary>
            /// WorldRotation of transform during snapshot.
            /// </summary>
            public Quaternion WorldRotation;

            public void UpdateValues(Transform t)
            {
                WorldPosition = t.position;
                WorldRotation = t.rotation;
            }
        }

        /// <summary>
        /// Used to store where colliders start before rollbacks.
        /// </summary>
        private class RollingCollider
        {
            public RollingCollider(Transform t, ColliderRollback colliderRollback)
            {
                Transform = t;
                LocalPosition = t.localPosition;
                LocalRotation = t.localRotation;
                /* Subscribe to events. No need to unsubscribe since this 
                 * class is intiialized once and lives long as the holding
                 * class does. */
                colliderRollback.OnRollback += ColliderRollback_OnRollback;
                colliderRollback.OnReturnForward += ColliderRollback_OnReturnForward;
            }

            /// <summary>
            /// Received when ReturnForward is called on ColliderRollback.
            /// </summary>
            private void ColliderRollback_OnReturnForward()
            {
                Transform.localPosition = LocalPosition;
                Transform.localRotation = LocalRotation;
            }

            /// <summary>
            /// Received when Rollback is called on ColliderRollback.
            /// </summary>
            private void ColliderRollback_OnRollback(FrameRollbackTypes rollbackType, int endFrame, float percent)
            {
                //Exact frame.
                if (rollbackType == FrameRollbackTypes.Exact)
                {
                    int index = GetSnapshotIndex(endFrame);
                    Transform.position = _snapshots[index].WorldPosition;
                    Transform.rotation = _snapshots[index].WorldRotation;
                }
                //Start frame.
                else if (rollbackType == FrameRollbackTypes.LerpFirst)
                {
                    //Lerp between actual position and the most recent snapshot.
                    int firstFrame = GetSnapshotIndex(0);
                    Transform.position = Vector3.Lerp(Transform.position, _snapshots[firstFrame].WorldPosition, percent);
                    Transform.rotation = Quaternion.Lerp(Transform.rotation, _snapshots[firstFrame].WorldRotation, percent);
                }
                //Middle frame.
                else if (rollbackType == FrameRollbackTypes.LerpMiddle)
                {
                    //Lerp between end frame and the one before it.
                    int firstFrame = GetSnapshotIndex(endFrame - 1);
                    int secondFrame = GetSnapshotIndex(endFrame);
                    Transform.position = Vector3.Lerp(_snapshots[firstFrame].WorldPosition, _snapshots[secondFrame].WorldPosition, percent);
                    Transform.rotation = Quaternion.Lerp(_snapshots[firstFrame].WorldRotation, _snapshots[secondFrame].WorldRotation, percent);
                }

#if UNITY_EDITOR
                //If simulating rollback.
                if (DebugDrawer.RollbackTime > 0f)
                    DebugExtension.DrawLocalCube(Transform, Vector3.one);
#endif
            }

            #region Public.
            /// <summary>
            /// Transform collider is for.
            /// </summary>
            public readonly Transform Transform;
            /// <summary>
            /// LocalPosition of transform at start.
            /// </summary>
            public readonly Vector3 LocalPosition;
            /// <summary>
            /// LocalRotation of transform at start.
            /// </summary>
            public readonly Quaternion LocalRotation;
            #endregion

            #region Private.
            /// <summary>
            /// Current snapshots for this collider.
            /// </summary>
            private ColliderSnapshot[] _snapshots;
            /// <summary>
            /// Index to write a snapshot in.
            /// </summary>
            private int _writeIndex = 0;
            /// <summary>
            /// True if snapshots are being recycled rather than written for the first time.
            /// </summary>
            private bool _recycleSnapshots = false;
            #endregion

            /// <summary>
            /// Fills snapshots with current value.
            /// </summary>
            public void ResetSnapshots(int count)
            {
                if (count <= 0)
                {
                    Debug.LogError("Cannot reset snapshots with count less than 1.");
                    return;
                }

                _snapshots = new ColliderSnapshot[count];
                //Reset data as if new.
                _writeIndex = 0;
                _recycleSnapshots = false;
                //Add first snapshot.
                AddSnapshot();
            }

            /// <summary>
            /// Adds a snapshot for this collider.
            /// </summary>
            public void AddSnapshot()
            {
                //Not yet recycling, make a new snapshot.
                if (!_recycleSnapshots)
                    _snapshots[_writeIndex] = new ColliderSnapshot(Transform);
                //Snapshot array traversed already, start recycling.
                else
                    _snapshots[_writeIndex].UpdateValues(Transform);

                _writeIndex++;
                if (_writeIndex >= _snapshots.Length)
                {
                    _writeIndex = 0;
                    _recycleSnapshots = true;
                }
            }

            /// <summary>
            /// Gets a snapshot on the specified index.
            /// </summary>
            /// <returns></returns>
            private int GetSnapshotIndex(int historyCount)
            {
                /* Since write index is increased after a write
                 * we must reduce it by 1 to get to the last
                 * write index, before removing history count. */
                int index = (_writeIndex - 1) - historyCount;
                //If negative value start taking from the back.
                if (index < 0)
                {
                    /* Cannot take from back, snapshots aren't filled yet.
                     * Instead take the oldest snapshot, which in this case
                     * would be index 0. */
                    if (!_recycleSnapshots)
                        return 0;
                    //Snapshots filled, take from back.
                    else
                        return (_snapshots.Length + index);
                }
                //Not a negative value, return as is.
                else
                {
                    return index;
                }
            }
        }
        #endregion

        #region Protected.
        /// <summary>
        /// Dispatched when a rollback occurs.
        /// </summary>
        protected event Action<FrameRollbackTypes, int, float> OnRollback;
        /// <summary>
        /// Dispatched when return forward is called.
        /// </summary>
        protected event Action OnReturnForward;
        #endregion

        #region Serialized.
        /// <summary>
        /// Objects holding colliders which can rollback.
        /// </summary>
        [Tooltip("Objects holding colliders which can rollback.")]
        [SerializeField]
        private GameObject[] _colliderParents = new GameObject[0];
        #endregion

        #region Private.
        /// <summary>
        /// Rollback data about ColliderParents.
        /// </summary>
        private RollingCollider[] _rollingColliders = new RollingCollider[0];
        /// <summary>
        /// True if rolled back.
        /// </summary>
        private bool _rolledBack = false;
        /// <summary>
        /// Maximum snapshots allowed. Generated at runtime using snapshot interval and max rollback time.
        /// </summary>
        private int _maxSnapshots = 0;
        /// <summary>
        /// True if initialized.
        /// </summary>
        private bool _initialized = false;
        #endregion

        private void OnEnable()
        {
            //Not yet initialized.
            if (!_initialized)
            {
                InitializeRollingColliders();
            }
            //Already initialized, reset snapshots.
            else
            {
                //Not going to make an event for this since it only occurs OnEnable.
                for (int i = 0; i < _rollingColliders.Length; i++)
                    _rollingColliders[i].ResetSnapshots(_maxSnapshots);
            }

            ChangeEventSubscriptions(true);
        }

        private void OnDisable()
        {
            ChangeEventSubscriptions(false);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            //If not acting as a client host then destroy script.
            if (base.isClientOnly)
                DestroyImmediate(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            InitializeRollingColliders();
        }

        /// <summary>
        /// Subscribes or unsubscribes to events needed for rolling back.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeEventSubscriptions(bool subscribe)
        {
            if (subscribe)
            {
                RollbackManager.OnCreateSnapshot += RollbackManager_OnCreateSnapshot;
                RollbackManager.OnReturnForward += RollbackManager_OnReturnForward;
                RollbackManager.OnRollback += RollbackManager_OnRollback;
            }
            else
            {
                RollbackManager.OnCreateSnapshot -= RollbackManager_OnCreateSnapshot;
                RollbackManager.OnReturnForward -= RollbackManager_OnReturnForward;
                RollbackManager.OnRollback -= RollbackManager_OnRollback;
            }
        }

        private void RollbackManager_OnRollback(float rollbackTime)
        {
            Rollback(rollbackTime);
        }

        /// <summary>
        /// Received when ReturnForward is called on RollbackManager.
        /// </summary>
        private void RollbackManager_OnReturnForward()
        {
            ReturnForward();
        }

        /// <summary>
        /// Received when ReturnForward is called on RollbackManager.
        /// </summary>
        private void RollbackManager_OnCreateSnapshot()
        {
            GenerateSnapshot();
        }

        /// <summary>
        /// Creates rolling collider values.
        /// </summary>
        private void InitializeRollingColliders()
        {
            if (_initialized)
                return;

            _maxSnapshots = Mathf.CeilToInt(RollbackManager.MAX_ROLLBACK_TIME / RollbackManager.LERP_SNAPSHOT_INTERVAL);
            _rollingColliders = new RollingCollider[_colliderParents.Length];

            /* Generate a rolling collider for each
             * collider parent. */
            for (int i = 0; i < _colliderParents.Length; i++)
            {
                if (_colliderParents[i].gameObject == null)
                    continue;

                /* Creates a new rolling collider and fills the snapshots with it's current
                 * position. If you were to roll back before all snapshots could fill
                 * with new data an incorrect rollback position/rotation would be returned
                 * but the chances of this happening are slim to none, and impossible after
                 * the MAX_ROLLBACK_TIME duration has passed. */
                _rollingColliders[i] = new RollingCollider(_colliderParents[i].transform, this);
                _rollingColliders[i].ResetSnapshots(_maxSnapshots);
            }

            _initialized = true;
        }

        /// <summary>
        /// Checks if a snapshot should be generated for rolling colliders.
        /// </summary>
        private void GenerateSnapshot()
        {
            //Can't generate a snapshot while rolled back.
            if (_rolledBack)
                return;

            for (int i = 0; i < _rollingColliders.Length; i++)
            {
                if (_rollingColliders[i] == null)
                    continue;
                _rollingColliders[i].AddSnapshot();
            }
        }

        /// <summary>
        /// Rolls back collider snapshots the specified amount of time.
        /// </summary>
        /// <param name="time"></param>
        public void Rollback(float time)
        {
            //Already rolled back.
            if (_rolledBack)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Colliders are already rolled back. Returning colliders forward first.");
                ReturnForward();
            }
            //No rollback.
            if (time <= 0f)
                return;

            FrameRollbackTypes rollbackType;
            int endFrame;
            float percent;

            float decimalFrame = (time / RollbackManager.LERP_SNAPSHOT_INTERVAL);
            //Out of frames.
            if (decimalFrame >= (_maxSnapshots - 1))
            {
                rollbackType = FrameRollbackTypes.LerpMiddle;
                endFrame = (_maxSnapshots - 1);
                percent = 1f;
            }
            //Within frames.
            else
            {
                percent = (decimalFrame % 1);

                /* Rolling back at least 2 frames.
                 * If only rolling back one frame decimalFrame
                 * would be less than 1, since index of 0 would be
                 * the first frame. */
                if (decimalFrame > 1f)
                {
                    rollbackType = FrameRollbackTypes.LerpMiddle;
                    endFrame = Mathf.CeilToInt(decimalFrame);
                }
                //Not rolling back more than 1 frame.
                else
                {
                    endFrame = 0;
                    rollbackType = FrameRollbackTypes.LerpFirst;
                }
            }

            /* Dispatch events rolling back colliders.. This lets rolling colliders work without iterating through
            * each rolling collider, a much faster approach. */
            OnRollback?.Invoke(rollbackType, endFrame, percent);

            _rolledBack = true;
        }

        /// <summary>
        /// Returns hitbox transforms to their starting position.
        /// </summary>
        public void ReturnForward()
        {
            if (!_rolledBack)
                return;

            /* Dispatch events returning back colliders.. This lets rolling colliders work without iterating through
            * each rolling collider, a much faster approach. */
            OnReturnForward?.Invoke();

            _rolledBack = false;
        }

        /// <summary>
        /// Returns if this gameObject contains the specified layer.
        /// </summary>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private bool ContainsLayer(LayerMask layerMask)
        {
            return (layerMask == (layerMask | (1 << gameObject.layer)));
        }
    }

}
