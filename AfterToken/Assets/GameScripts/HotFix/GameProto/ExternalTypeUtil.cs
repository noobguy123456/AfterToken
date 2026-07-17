using UnityEngine;

namespace GameLogic
{
    public static class ExternalTypeUtil
    {
        public static Vector2 NewVector2(GameConfig.vector2 v)
            => new Vector2(v.X, v.Y);

        public static Vector3 NewVector3(GameConfig.vector3 v)
            => new Vector3(v.X, v.Y, v.Z);

        public static Vector4 NewVector4(GameConfig.vector4 v)
            => new Vector4(v.X, v.Y, v.Z, v.W);

        public static Vector2Int NewVector2Int(GameConfig.vector2int v)
            => new Vector2Int(v.X, v.Y);

        public static Vector3Int NewVector3Int(GameConfig.vector3int v)
            => new Vector3Int(v.X, v.Y, v.Z);
    }
}
