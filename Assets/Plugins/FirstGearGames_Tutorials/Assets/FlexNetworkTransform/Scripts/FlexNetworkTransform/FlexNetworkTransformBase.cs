using FirstGearGames.Utilities.Editors;
using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{


    public abstract class FlexNetworkTransformBase : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Data used to manage moving towards a target.
        /// </summary>
        private class TargetSyncData
        {
            /// <summary>
            /// Delta time passed on this TargetSyncData.
            /// </summary>
            public float TimePassed = 0f;
            /// <summary>
            /// Transform start data when this update was received.
            /// </summary>
            public TransformSyncData StartData;
            /// <summary>
            /// Transform goal data for this update.
            /// </summary>
            public TransformSyncData GoalData;
        }
        /// <summary>
        /// Ways to synchronize datas.
        /// </summary>
        [System.Serializable]
        private enum SynchronizeTypes
        {
            Normal = 0,
            NoSynchronization = 1
        }
        #endregion

        #region Protected.
        /// <summary>
        /// Transform to monitor and modify.
        /// </summary>
        protected abstract Transform TargetTransform { get; }
        #endregion

        /// <summary>
        /// How often to synchronize this transform.
        /// </summary>
        [Tooltip("How often to synchronize this transform.")]
        [Range(0.01f, 0.5f)]
        [SerializeField]
        private float _synchronizeInterval = 0.1f;
        /// <summary>
        /// True to synchronize data anytime it has changed. False to allow greater differences before synchronizing.
        /// </summary>
        [Tooltip("True to synchronize data anytime it has changed. False to allow greater differences before synchronizing.")]
        [SerializeField]
        private bool _preciseSynchronization = true;
        /// <summary>
        /// True to force transform to results. False to stop checking for synchronization once at results. Enforcing ensures clients cannot move around after transform is set to the proper values at a minor performance cost.
        /// </summary>
        [Tooltip("True to force transform to results. False to stop checking for synchronization once at results. Enforcing ensures clients cannot move around after transform is set to the proper values at a minor performance cost.")]
        [SerializeField]
        private bool _enforceResults = true;
        /// <summary>
        /// True to automatically determine interpolation strength. False to specify your own value.
        /// </summary>
        [Tooltip("True to automatically determine interpolation strength. False to specify your own value.")]
        [SerializeField]
        private bool _automaticInterpolation = true;
        /// <summary>
        /// How strongly to interpolate to server results. Higher values will result in more real-time results but may result in occasional stutter during network instability.
        /// </summary>
        [Tooltip("How strongly to interpolate to server results. Higher values will result in more real-time results but may result in occasional stutter during network instability.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float _interpolationStrength = 0.85f;
        /// <summary>
        /// True if using client authoritative movement.
        /// </summary>
        [Tooltip("True if using client authoritative movement.")]
        [SerializeField]
        private bool _clientAuthoritative = true;
        /// <summary>
        /// True to synchronize server results back to owner.
        /// </summary>
        [Tooltip("True to synchronize server results back to owner.")]
        [SerializeField]
        private bool _synchronizeToOwner = true;
        /// <summary>
        /// Synchronize options for position.
        /// </summary>
        [Tooltip("Synchronize options for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizePosition = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the position to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Axes))]
        private Axes _snapPosition = (Axes)0;
        /// <summary>
        /// Sets SnapPosition value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapPosition(Axes value) { _snapPosition = value; }
        /// <summary>
        /// Synchronize states for rotation.
        /// </summary>
        [Tooltip("Synchronize states for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeRotation = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the rotation to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Axes))]
        private Axes _snapRotation = (Axes)0;
        /// <summary>
        /// Sets SnapRotation value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapRotation(Axes value) { _snapRotation = value; }
        /// <summary>
        /// Synchronize states for scale.
        /// </summary>
        [Tooltip("Synchronize states for scale.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeScale = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the scale to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the scale to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Axes))]
        private Axes _snapScale = (Axes)0;
        /// <summary>
        /// Sets SnapScale value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapScale(Axes value) { _snapScale = value; }

        /// <summary>
        /// Last SyncData sent by client.
        /// </summary>
        private TransformSyncData _clientSyncData = null;
        /// <summary>
        /// Last SyncData sent by server.
        /// </summary>
        private TransformSyncData _serverSyncData = null;
        /// <summary>
        /// Next time client may send data.
        /// </summary>
        private float _nextClientSendTime = 0f;
        /// <summary>
        /// Next time server may send data.
        /// </summary>
        private float _nextServerSendTime = 0f;
        /// <summary>
        /// TargetSyncData to move between.
        /// </summary>
        private TargetSyncData _targetSyncData = null;

        private void Update()
        {
            CheckSendToServer();
            CheckSendToClients();
            MoveTowardsSyncDatas();
        }

        /// <summary>
        /// Checks if client needs to send data to server.
        /// </summary>
        private void CheckSendToServer()
        {
            if (!base.isClient)
                return;
            //Not using client auth movement.
            if (!_clientAuthoritative)
                return;
            //Not authoritative client.
            if (!base.hasAuthority)
                return;
            if (Time.time < _nextClientSendTime)
                return;

            //Nothing has changed.
            DirtyProperties dp = SendData(_clientSyncData);
            if (dp == DirtyProperties.None)
                return;

            _nextClientSendTime = Time.time + _synchronizeInterval;
            _clientSyncData = new TransformSyncData((byte)dp, TargetTransform.localPosition, TargetTransform.localRotation, TargetTransform.localScale);

            //Send to server.
            CmdSendSyncData(_clientSyncData);
        }

        /// <summary>
        /// Checks if server needs to send data to clients.
        /// </summary>
        private void CheckSendToClients()
        {
            if (!base.isServer)
                return;
            if (Time.time < _nextServerSendTime)
                return;

            DirtyProperties dp = DirtyProperties.None;
            /* If server only or has authority then use transforms current position.
             * When server only client values are set immediately, but as client host
             * they are smoothed so transforms do not snap. When smoothed instead of
             * sending the transforms current data we will send the goal data. This prevents
             * clients from receiving slower updates when running as a client host. */
            //Breaking if statements down for easier reading.
            if (base.isServerOnly || base.hasAuthority)
                dp = SendData(_serverSyncData);
            //No authority and not server only.
            else if (!base.hasAuthority && !base.isServerOnly)
                dp = SendData(_serverSyncData, _targetSyncData);

            //Nothing has changed.
            if (dp == DirtyProperties.None)
                return;

            _nextServerSendTime = Time.time + _synchronizeInterval;
            _serverSyncData = new TransformSyncData((byte)dp, TargetTransform.localPosition, TargetTransform.localRotation, TargetTransform.localScale);
            //send to clients.
            RpcSendSyncData(_serverSyncData);
        }

        /// <summary>
        /// Returns which properties need to be sent to maintain synchronization with the transforms current properties.
        /// </summary>
        /// <returns></returns>
        private DirtyProperties SendData(TransformSyncData data)
        {
            return SendData(data, null);
        }
        /// <summary>
        /// Returns which properties need to be sent to maintain synchronization with targetData properties.
        /// </summary>
        /// <returns></returns>
        private DirtyProperties SendData(TransformSyncData data, TargetSyncData targetData)
        {
            //Data is null, so it's definitely not a match.
            if (data == null)
                return (DirtyProperties.Position | DirtyProperties.Rotation | DirtyProperties.Scale);

            DirtyProperties dp = DirtyProperties.None;

            if (_synchronizePosition == SynchronizeTypes.Normal && !PositionMatches(data, targetData, _preciseSynchronization))
                dp |= DirtyProperties.Position;
            if (_synchronizeRotation == SynchronizeTypes.Normal && !RotationMatches(data, targetData, _preciseSynchronization))
                dp |= DirtyProperties.Rotation;
            if (_synchronizeScale == SynchronizeTypes.Normal && !ScaleMatches(data, targetData, _preciseSynchronization))
                dp |= DirtyProperties.Scale;

            return dp;
        }

        /// <summary>
        /// Moves towards most recent sync data values.
        /// </summary>
        private void MoveTowardsSyncDatas()
        {
            //Client authority, don't need to synchronize with self.
            if (base.hasAuthority && _clientAuthoritative)
                return;
            //Not client authority but also not synchronize to owner.
            if (base.hasAuthority && !_clientAuthoritative && !_synchronizeToOwner)
                return;
            //No SyncData to check against.
            if (_targetSyncData == null)
                return;

            //Already at the correct position.
            if (SyncDataMatchesTransform(_targetSyncData.GoalData, true))
            {
                //Not enforcing results, can nullify to avoid extra checks.
                if (!_enforceResults)
                    _targetSyncData = null;

                return;
            }

            float interpolationStrength;
            //Automatically calculate interpolation strength.
            if (_automaticInterpolation)
            {
                /* Lerp using two different sliding values.
                 * Interpolation strength matters both beneath 110ms
                 * sync intervals. */
                if (_synchronizeInterval <= 0.1f)
                {
                    float lerpPercent = Mathf.InverseLerp(0.01f, 0.1f, _synchronizeInterval);
                    interpolationStrength = Mathf.Lerp(0.4f, 0.875f, lerpPercent);
                }
                //Higher than average sync intervals.
                else
                {
                    float lerpPercent = Mathf.InverseLerp(0.1f, 0.5f, _synchronizeInterval);
                    interpolationStrength = Mathf.Lerp(0.875f, 0.975f, lerpPercent);
                }
            }
            //Use configured value.
            else
            {
                interpolationStrength = _interpolationStrength;
            }

            _targetSyncData.TimePassed += Time.deltaTime * interpolationStrength;
            float percent = (_targetSyncData.TimePassed / _synchronizeInterval);

            TargetTransform.localPosition = Vector3.Lerp(_targetSyncData.StartData.LocalPosition, _targetSyncData.GoalData.LocalPosition, percent);
            TargetTransform.localRotation = Quaternion.Lerp(_targetSyncData.StartData.LocalRotation, _targetSyncData.GoalData.LocalRotation, percent);
            TargetTransform.localScale = Vector3.Lerp(_targetSyncData.StartData.LocalScale, _targetSyncData.GoalData.LocalScale, percent);
        }

        /// <summary>
        /// Returns true if the passed in axes contains all axes.
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        private bool SnapAll(Axes axes)
        {
            return (axes == (Axes.X | Axes.Y | Axes.Z));
        }

        /// <summary>
        /// Returns true if the passed in SyncData values match this transforms values.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool SyncDataMatchesTransform(TransformSyncData data, bool precise)
        {
            if (data == null)
                return false;

            return (
                PositionMatches(data, null, precise) &&
                RotationMatches(data, null, precise) &&
                ScaleMatches(data, null, precise)
                );
        }

        /// <summary>
        /// Returns if this transform position matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool PositionMatches(TransformSyncData data, TargetSyncData targetData, bool precise)
        {
            if (data == null)
                return false;

            Vector3 localPosition = (targetData == null) ? TargetTransform.localPosition : targetData.GoalData.LocalPosition;

            if (precise)
            {
                return (localPosition == data.LocalPosition);
            }
            else
            {
                float dist = Vector3.SqrMagnitude(localPosition - data.LocalPosition);
                return (dist < 0.0001f);
            }
        }
        /// <summary>
        /// Returns if this transform rotation matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool RotationMatches(TransformSyncData data, TargetSyncData targetData, bool precise)
        {
            if (data == null)
                return false;

            Quaternion localRotation = (targetData == null) ? TargetTransform.localRotation : targetData.GoalData.LocalRotation;

            if (precise)
            {
                return (localRotation == data.LocalRotation);
            }
            else
            {
                float dist = Quaternion.Angle(localRotation, data.LocalRotation);
                return (dist < 1f);
            }
        }
        /// <summary>
        /// Returns if this transform scale matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ScaleMatches(TransformSyncData data, TargetSyncData targetData, bool precise)
        {
            if (data == null)
                return false;

            Vector3 localScale = (targetData == null) ? TargetTransform.localScale : targetData.GoalData.LocalScale;

            if (precise)
            {
                return (TargetTransform.localScale == data.LocalScale);
            }
            else
            {
                float dist = Vector3.SqrMagnitude(localScale - data.LocalScale);
                return (dist < 0.0001f);
            }
        }

        /// <summary>
        /// Returns if whole(extended enum) has any of the part values.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part">Values to check for within whole.</param>
        /// <returns>Returns true part is within whole.</returns>
        private bool EnumContains(System.Enum whole, System.Enum part)
        {
            //If not the same type of Enum return false.
            /* Commented out for performance. Designer
             * should know better than to compare two different
             * enums. Plus this is internal, so I'm the designer, so if
             * I screw up, it's on me! */
            //if (!SameType(value, target))
            //    return false;

            /* Convert enum values to ulong. With so few
             * values a uint would be safe, but should
             * the options expand ulong is safer. */
            ulong wholeNum = System.Convert.ToUInt64(whole);
            ulong partNum = System.Convert.ToUInt64(part);

            return ((wholeNum & partNum) != 0);
        }

        //TODO rotation and prob scale too don't  move properly with lesser interpolation strengths.
        /// <summary>
        /// Sends SyncData to the server. Only used with client auth.
        /// </summary>
        /// <param name="data"></param>
        [Command]
        private void CmdSendSyncData(TransformSyncData data)
        {
            //Sent to self.
            if (base.hasAuthority)
                return;

            //If server only then skip smoothing and snap to data. 
            if (base.isServerOnly)
            {
                FixTransformDatas(null, data);
                TargetTransform.localPosition = data.LocalPosition;
                TargetTransform.localRotation = data.LocalRotation;
                TargetTransform.localScale = data.LocalScale;
            }
            else
            {
                _targetSyncData = new TargetSyncData();
                FixTransformDatas(_targetSyncData, data);
            }
            /* If acting as a client host do nothing as client is already in
             * the right position. */
        }

        /// <summary>
        /// Sends SyncData to clients.
        /// </summary>
        /// <param name="data"></param>
        [ClientRpc]
        private void RpcSendSyncData(TransformSyncData data)
        {
            //If client host exit method.
            if (base.isServer)
                return;

            //If owner of object.
            if (base.hasAuthority)
            {
                //Client authoritative, already in sync.
                if (_clientAuthoritative)
                    return;
                //Not client authoritative, but also not sync to owner.
                if (!_clientAuthoritative && !_synchronizeToOwner)
                    return;
            }

            _targetSyncData = new TargetSyncData();
            FixTransformDatas(_targetSyncData, data);
        }


        /// <summary>
        /// Sets StartData for the specified TargetSyncData.
        /// </summary>
        /// <param name="tsd"></param>
        private void FixTransformDatas(TargetSyncData tsd, TransformSyncData goalData)
        {
            DirtyProperties dirtyProperties = (DirtyProperties)goalData.DirtyProperties;

            /* Begin by setting goal data using what has been serialized
             * via the writer. */
            //Position wasn't included.
            if (!EnumContains(dirtyProperties, DirtyProperties.Position))
                goalData.LocalPosition = TargetTransform.localPosition;
            //Rotation wasn't included.
            if (!EnumContains(dirtyProperties, DirtyProperties.Rotation))
                goalData.LocalRotation = TargetTransform.localRotation;
            //Scale wasn't included.
            if (!EnumContains(dirtyProperties, DirtyProperties.Scale))
                goalData.LocalScale = TargetTransform.localScale;

            if (tsd != null)
            {

                //Assign goal data to tsd.
                tsd.GoalData = goalData;

                /* Build start data off from goal data. */
                //Begin by setting to current values then override as needed.
                tsd.StartData = new TransformSyncData(0, TargetTransform.localPosition, TargetTransform.localRotation, TargetTransform.localScale);
                /* Position. */
                if (EnumContains(dirtyProperties, DirtyProperties.Position))
                {
                    //If to snap all.
                    if (SnapAll(_snapPosition))
                    {
                        tsd.StartData.LocalPosition = tsd.GoalData.LocalPosition;
                    }
                    //Snap some or none.
                    else
                    {
                        //Snap X.
                        if (EnumContains(_snapPosition, Axes.X))
                            tsd.StartData.LocalPosition.x = tsd.GoalData.LocalPosition.x;
                        //Snap Y.
                        if (EnumContains(_snapPosition, Axes.Y))
                            tsd.StartData.LocalPosition.y = tsd.GoalData.LocalPosition.y;
                        //Snap Z.
                        if (EnumContains(_snapPosition, Axes.Z))
                            tsd.StartData.LocalPosition.z = tsd.GoalData.LocalPosition.z;
                    }
                }

                /* Rotation. */
                if (EnumContains(dirtyProperties, DirtyProperties.Rotation))
                {
                    //If to snap all.
                    if (SnapAll(_snapRotation))
                    {
                        tsd.StartData.LocalRotation = tsd.GoalData.LocalRotation;
                    }
                    //Snap some or none.
                    else
                    {
                        /* Only perform snap checks if snapping at least one
                         * to avoid extra cost of calculations. */
                        if ((int)_snapRotation != 0)
                        {
                            //Convert quaternion to eulers for easy snapping.
                            Vector3 startEuler = tsd.StartData.LocalRotation.eulerAngles;
                            Vector3 targetEuler = tsd.GoalData.LocalRotation.eulerAngles;
                            //Snap X.
                            if (EnumContains(_snapRotation, Axes.X))
                                startEuler.x = targetEuler.x;
                            //Snap Y.
                            if (EnumContains(_snapRotation, Axes.Y))
                                startEuler.y = targetEuler.y;
                            //Snap Z.
                            if (EnumContains(_snapRotation, Axes.Z))
                                startEuler.z = targetEuler.z;
                            //Rebuild into quaternion.
                            tsd.StartData.LocalRotation = Quaternion.Euler(startEuler);
                        }
                    }
                }

                if (EnumContains(dirtyProperties, DirtyProperties.Scale))
                {
                    //If to snap all.
                    if (SnapAll(_snapScale))
                    {
                        tsd.StartData.LocalScale = tsd.GoalData.LocalScale;
                    }
                    //Snap some or none.
                    else
                    {
                        //Snap X.
                        if (EnumContains(_snapScale, Axes.X))
                            tsd.StartData.LocalScale.x = tsd.GoalData.LocalScale.x;
                        //Snap Y.
                        if (EnumContains(_snapScale, Axes.Y))
                            tsd.StartData.LocalScale.y = tsd.GoalData.LocalScale.y;
                        //Snap Z.
                        if (EnumContains(_snapScale, Axes.Z))
                            tsd.StartData.LocalScale.z = tsd.GoalData.LocalScale.z;
                    }
                }

            }

        }

    }
}

