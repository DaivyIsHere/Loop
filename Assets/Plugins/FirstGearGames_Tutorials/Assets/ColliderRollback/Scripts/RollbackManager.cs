using System;
using UnityEngine;

namespace FirstGearGames.Mirror.ColliderRollbacks
{


    public class RollbackManager : MonoBehaviour
    {

        #region Types.
        public enum PhysicsTypes
        {
            TwoDimensional = 1,
            ThreeDimensional = 2,
            Both = 4
        }
        #endregion

        #region Public.
        /// <summary>
        /// Dispatched when a snapshot should be created.
        /// </summary>
        public static event Action OnCreateSnapshot;
        /// <summary>
        /// Dispatched when colliders should return forward.
        /// </summary>
        public static event Action OnReturnForward;
        /// <summary>
        /// Dispatched when a rollback should occur.
        /// </summary>
        public static event Action<float> OnRollback;
        #endregion

        #region Private.
        /// <summary>
        /// Next unscaled time to generate a snapshot.
        /// </summary>
        private float _nextSnapshotTime = 0f;
        #endregion

        #region Const.
        /// <summary>
        /// Maximum amount of time colliders can roll back.
        /// </summary>
        public const float MAX_ROLLBACK_TIME = 0.5f;
        /// <summary>
        /// How frequently to take a snapshot when LerpSnapshots is true. Mind server frequency when setting this value.
        /// </summary>
        public const float LERP_SNAPSHOT_INTERVAL = 0.032f;
        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FirstInitialize()
        {
            GameObject go = new GameObject();
            go.name = "RollbackManager";
            go.AddComponent<RollbackManager>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSnapshotTime)
                return;

            _nextSnapshotTime = Time.unscaledTime + LERP_SNAPSHOT_INTERVAL;
            OnCreateSnapshot?.Invoke();
        }

        /// <summary>
        /// Returns all ColliderRollback objects back to their original position.
        /// </summary>
        public static void ReturnForward()
        {
            OnReturnForward?.Invoke();
        }


        /// <summary>
        /// Rolls back colliders based on entered parameters.
        /// </summary>
        /// <param name="data">Data used in the rollback.</param>
        /// <param name="physicsType">Type of cast you plan on using. EG: if your cast during this rollback are only going to be Physic2D, choose PhysicsTypes.TwoDimensional.</param>
        public static void Rollback(float rollbackTime, PhysicsTypes physicsType)
        {
            OnRollback?.Invoke(rollbackTime);

            if (physicsType == PhysicsTypes.ThreeDimensional)
            {
                Physics.SyncTransforms();
            }
            else if (physicsType == PhysicsTypes.TwoDimensional)
            {
                Physics2D.SyncTransforms();
            }
            else
            {
                Physics.SyncTransforms();
                Physics2D.SyncTransforms();
            }
        }
    }


}