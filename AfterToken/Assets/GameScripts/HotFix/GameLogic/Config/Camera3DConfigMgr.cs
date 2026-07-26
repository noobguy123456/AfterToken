using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 3D摄像机配置管理器。
    /// </summary>
    public class Camera3DConfigMgr
    {
        private const int CONFIG_ID = 1;

        private static Camera3DConfigMgr _instance;
        public static Camera3DConfigMgr Instance => _instance ??= new Camera3DConfigMgr();

        private Camera3D _cachedConfig;

        private Camera3D Get()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = ConfigSystem.Instance.Tables.TbCamera3D.GetOrDefault(CONFIG_ID);
            }
            return _cachedConfig;
        }

        public float PitchAngle => Get()?.PitchAngle ?? 60f;
        public float InitialHeight => Get()?.InitialHeight ?? 15f;
        public float InitialDistance => Get()?.InitialDistance ?? -10f;
        public float Fov => Get()?.Fov ?? 45f;
        public float MinZoom => Get()?.MinZoom ?? 5f;
        public float MaxZoom => Get()?.MaxZoom ?? 30f;
        public float MoveSpeed => Get()?.MoveSpeed ?? 20f;
        public float ZoomSpeed => Get()?.ZoomSpeed ?? 5f;
        public float MaxRotationAngle => Get()?.MaxRotationAngle ?? 30f;
        public float RotationSpeed => Get()?.RotationSpeed ?? 90f;
        public float FollowSmoothTime => Get()?.FollowSmoothTime ?? 0.08f;
    }
}
