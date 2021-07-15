using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{
    [System.Serializable, System.Flags]
    public enum Axes : int
    {
        X = 1,
        Y = 2,
        Z = 4
    }


    /// <summary>
    /// Transform properties which need to be synchronized.
    /// </summary>
    public enum DirtyProperties : byte
    {
        None = 0,
        Position = 1,
        Rotation = 2,
        Scale = 4
    }

    /// <summary>
    /// Container holding latest transform values.
    /// </summary>
    [System.Serializable]
    public class TransformSyncData
    {
        public TransformSyncData() { }
        public TransformSyncData(byte dirtyProperties, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            DirtyProperties = dirtyProperties;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }

        public byte DirtyProperties;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public float TransitionRate;
    }

    public static class AdvancedNetworkTransformSerializers
    {
        /// <summary>
        /// Writes TransformSyncData into a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="syncData"></param>
        public static void WriteTransformSyncData(this NetworkWriter writer, TransformSyncData syncData)
        {
            DirtyProperties dp = (DirtyProperties)syncData.DirtyProperties;
            writer.WriteByte(syncData.DirtyProperties);

            if (EnumContains(dp, DirtyProperties.Position))
                writer.WriteVector3(syncData.LocalPosition);
            if (EnumContains(dp, DirtyProperties.Rotation))
                writer.WriteQuaternion(syncData.LocalRotation);
            if (EnumContains(dp, DirtyProperties.Scale))
                writer.WriteVector3(syncData.LocalScale);
        }

        /// <summary>
        /// Converts reader data into a new TransformSyncData.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static TransformSyncData ReadTransformSyncData(this NetworkReader reader)
        {
            DirtyProperties dp = (DirtyProperties)reader.ReadByte();

            return new TransformSyncData(
                (byte)dp,
                EnumContains(dp, DirtyProperties.Position) ? reader.ReadVector3() : Vector3.zero,
                EnumContains(dp, DirtyProperties.Rotation) ? reader.ReadQuaternion() : Quaternion.identity,
                EnumContains(dp, DirtyProperties.Scale) ? reader.ReadVector3() : Vector3.zero
                );
        }

        /// <summary>
        /// Returns if whole(extended enum) has any of the part values.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part">Values to check for within whole.</param>
        /// <returns>Returns true part is within whole.</returns>
        public static bool EnumContains(System.Enum whole, System.Enum part)
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
    }


}