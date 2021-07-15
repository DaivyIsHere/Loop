using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{

    [DisallowMultipleComponent]
    public class FlexNetworkTransform : FlexNetworkTransformBase
    {
        protected override Transform TargetTransform => base.transform;
    }
}

