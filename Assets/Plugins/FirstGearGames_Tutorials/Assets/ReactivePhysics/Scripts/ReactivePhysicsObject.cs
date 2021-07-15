using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirror.ReactivePhyics
{


    public class ReactivePhysicsObject : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Type of object this is being used on.
        /// </summary>
        private enum ObjectTypes
        {
            Reactive = 0,
            Reactive2D = 1,
            Controller = 2,
            Controller2D = 3
        }
        /// <summary>
        /// Data sent to observers to keep object in sync.
        /// </summary>
        private struct SyncData
        {
            public SyncData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
            {
                Position = position;
                Rotation = rotation;
                Velocity = velocity;
                AngularVelocity = angularVelocity;
            }

            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector2 Velocity2D { get { return new Vector2(Velocity.x, Velocity.y); } }

            public Vector3 AngularVelocity;
            public float AngularVelocity2D { get { return AngularVelocity.z; } }
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// Type of object which is being synchronized. Use Reactive for an object which doesn't move using player input.
        /// </summary>
        [Tooltip("Type of object which is being synchronized. Use Reactive for an object which doesn't move using player input.")]
        [SerializeField]
        private ObjectTypes _objectType = ObjectTypes.Reactive;
        /// <summary>
        /// How frequently to sync observers. If being used as a controller it's best to set this to the same rate that your controller sends movement.
        /// </summary>
        [Tooltip("How frequently to sync observers. If being used as a controller it's best to set this to the same rate that your controller sends movement.")]
        [Range(0.001f, 1f)]
        [SerializeField]
        private float _syncInterval = 0.1f;
        /// <summary>
        /// How strictly to synchronize this object. Lower values will still keep the object in synchronization but it may take marginally longer for the object to correct if out of synchronization. Use 0f to disable.
        /// </summary>
        [Tooltip("How strictly to synchronize this object. Lower values will still keep the object in synchronization but it may take marginally longer for the object to correct if out of synchronization. Use 0f to disable.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _strength = 1f;
        #endregion

        #region Private.
        /// <summary>
        /// SyncData client should move towards.
        /// </summary>
        private SyncData? _syncData = null;
        /// <summary>
        /// Rigidbody on this object. May be null.
        /// </summary>
        private Rigidbody _rigidbody;
        /// <summary>
        /// Rigidbody2D on this object. May be null.
        /// </summary>
        private Rigidbody2D _rigidbody2D;
        /// <summary>
        /// Last SyncData values sent by server.
        /// </summary>
        private SyncData _lastSentSyncData;
        /// <summary>
        /// Next time server can send SyncData.
        /// </summary>
        private double _nextSendTime = 0f;
        #endregion

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            //Assign current data as a new sync data. Server will update as needed.
            _syncData = new SyncData(transform.position, transform.rotation, Vector3.zero, Vector3.zero);
        }

        private void Update()
        {
            //Check to send sync data if server.
            if (base.isServer)
                CheckSendSyncData();
            //Only move towards if not server.
            if (base.isClientOnly)
                MoveTowardsSyncDatas();
        }

        /// <summary>
        /// Moves towards most recent sync data values.
        /// </summary>
        private void MoveTowardsSyncDatas()
        {
            if (_syncData == null)
                return;
            if (_strength == 0f)
                return;
            //If data matches no reason to continue.
            if (SyncDataMatchesObject(_syncData.Value))
                return;

            //If owner use configured strength, otherwise always use 1f.
            float strength = (base.hasAuthority) ? _strength : 1f;
            //Smoothing multiplier based on sync interval and frame rate.
            float deltaMultiplier = Time.deltaTime / _syncInterval;

            float distance;
            //Position.
            distance = Vector3.Distance(transform.position, _syncData.Value.Position);
            distance = Mathf.Max(0.01f, distance * distance);
            transform.position = Vector3.MoveTowards(transform.position, _syncData.Value.Position, deltaMultiplier * distance * strength);
            //Rotation
            distance = Quaternion.Angle(transform.rotation, _syncData.Value.Rotation);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _syncData.Value.Rotation, deltaMultiplier * distance * strength);
            //Velocity.
            if (Using2D())
            {
                //Angular.
                distance = Mathf.Abs(_rigidbody2D.angularVelocity - _syncData.Value.AngularVelocity2D);
                _rigidbody2D.angularVelocity = Mathf.MoveTowards(_rigidbody2D.angularVelocity, _syncData.Value.AngularVelocity2D, deltaMultiplier * distance * strength);
                //Velocity.
                distance = Vector2.Distance(_rigidbody2D.velocity, _syncData.Value.Velocity2D);
                _rigidbody2D.velocity = Vector2.MoveTowards(_rigidbody2D.velocity, _syncData.Value.Velocity2D, deltaMultiplier * distance * strength);
            }
            else
            {
                //Angular.
                distance = Vector3.Distance(_rigidbody.angularVelocity, _syncData.Value.AngularVelocity);
                _rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, _syncData.Value.AngularVelocity, deltaMultiplier * distance * strength);
                //Velocity
                distance = Vector3.Distance(_rigidbody.velocity, _syncData.Value.Velocity);
                _rigidbody.velocity = Vector3.MoveTowards(_rigidbody.velocity, _syncData.Value.Velocity, deltaMultiplier * distance * strength);
            }

            if (_objectType == ObjectTypes.Controller)
                Physics.SyncTransforms();
            else if (_objectType == ObjectTypes.Controller2D)
                Physics2D.SyncTransforms();
        }

        /// <summary>
        /// Returns true if using a 2D object type.
        /// </summary>
        /// <returns></returns>
        private bool Using2D()
        {
            return (_objectType == ObjectTypes.Controller2D || _objectType == ObjectTypes.Reactive2D);
        }

        /// <summary>
        /// Returns if the specified SyncData matches values on this object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool SyncDataMatchesObject(SyncData data)
        {
            bool posMatch = (transform.position == data.Position);
            bool rotMatch = (transform.rotation == data.Rotation);
            bool velocityMatch;
            if (Using2D())
                velocityMatch = ((_rigidbody2D.velocity == data.Velocity2D) && (_rigidbody2D.angularVelocity == data.AngularVelocity2D));
            else
                velocityMatch = ((_rigidbody.velocity == data.Velocity) && (_rigidbody.angularVelocity == data.AngularVelocity));

            return (posMatch && rotMatch && velocityMatch);
        }

        /// <summary>
        /// Checks if SyncData needs to be sent over the network.
        /// </summary>
        private void CheckSendSyncData()
        {
            //Not enough time has passed to send.
            if (NetworkTime.time < _nextSendTime)
                return;
            //Values haven't changed.
            if (SyncDataMatchesObject(_lastSentSyncData))
                return;

            /* If here a new sync data needs to be sent. */

            //Set sync data being set, and next time data can send.
            Vector3 velocity;
            if (Using2D())
                velocity = new Vector3(_rigidbody2D.velocity.x, _rigidbody2D.velocity.y, 0f);
            else
                velocity = _rigidbody.velocity;

            Vector3 angularVelocity;
            if (Using2D())
                angularVelocity = new Vector3(0f, 0f, _rigidbody2D.angularVelocity);
            else
                angularVelocity = _rigidbody.angularVelocity;

            _lastSentSyncData = new SyncData(transform.position, transform.rotation, velocity, angularVelocity);
            _nextSendTime = NetworkTime.time + _syncInterval;

            //Send new SyncData to clients.
            RpcUpdateSyncData(_lastSentSyncData);
        }

        /// <summary>
        /// Updates SyncData on clients.
        /// </summary>
        /// <param name="data"></param>
        [ClientRpc]
        private void RpcUpdateSyncData(SyncData data)
        {
            //If received on client host, no need to update.
            if (base.isServer)
                return;

            _syncData = data;
        }
    }


}