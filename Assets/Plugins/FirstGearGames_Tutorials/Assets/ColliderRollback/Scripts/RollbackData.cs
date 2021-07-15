using UnityEngine;

namespace FirstGearGames.Mirror.ColliderRollbacks
{

    /// <summary>
    /// Data used to determine if a collider should roll back, and how far in time. Customize this to your liking.
    /// </summary>
    public struct RollbackData
    {
        public RollbackData(float time, LayerMask targetLayer)
        {
            Time = time;
            TargetLayer = targetLayer;
        }

        #region Public.
        /// <summary>
        /// How far to rollback.
        /// </summary>
        public readonly float Time;
        /// <summary>
        /// ColliderRollback objects must be on this layer to rollback.
        /// </summary>
        public readonly LayerMask TargetLayer;
        #endregion
    }


}