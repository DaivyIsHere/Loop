﻿using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{


    /// <summary>
    /// A component to synchronize the position of child transforms of networked objects.
    /// <para>There must be a NetworkTransform on the root object of the hierarchy. There can be multiple NetworkTransformChild components on an object. This does not use physics for synchronization, it simply synchronizes the localPosition and localRotation of the child transform and lerps towards the recieved values.</para>
    /// </summary>
    public class FlexNetworkTransformChild : FlexNetworkTransformBase
    {
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        public Transform Target;

        protected override Transform TargetTransform => Target;
    }
}

