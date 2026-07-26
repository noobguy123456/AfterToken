using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig;
using Newtonsoft.Json.Linq;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 配置加载器（Luban JSON 模式）。
    /// </summary>
    public class ConfigSystem
    {
        private static ConfigSystem _instance;

        public static ConfigSystem Instance => _instance ??= new ConfigSystem();

        private bool _init = false;

        private Tables _tables;

        public Tables Tables
        {
            get
            {
                if (!_init)
                {
                    Load();
                }

                return _tables;
            }
        }

        private IResourceModule _resourceModule;

        private static readonly string[] _tableFiles = new[]
        {
            "cfg_tbweapon",
            "cfg_tblevel",
            "cfg_tbitem",
            "cfg_tbplayer",
            "cfg_tbenemy",
            "cfg_tbwave",
            "cfg_tbdrop",
            "cfg_tbbuff",
            "cfg_tbportal",
        };

        /// <summary>
        /// 同步加载配置（不推荐；请使用 <see cref="LoadAsync"/> 在启动时预加载）。
        /// </summary>
        public void Load()
        {
            _tables = new Tables(LoadJson);
            _init = true;
        }

        /// <summary>
        /// 异步预加载所有 Luban JSON 配置。
        /// </summary>
        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_init) return;

            _resourceModule ??= ModuleSystem.GetModule<IResourceModule>();

            var jsonCache = new Dictionary<string, JArray>();
            foreach (var file in _tableFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                jsonCache[file] = await LoadJsonAsync(file, cancellationToken);
            }

            _tables = new Tables(file => jsonCache.TryGetValue(file, out var json) ? json : new JArray());
            _init = true;
        }

        /// <summary>
        /// 重新加载配置。
        /// </summary>
        public void Reload()
        {
            _init = false;
            _tables = null;
            Load();
        }

        /// <summary>
        /// 异步重新加载配置。
        /// </summary>
        public async UniTask ReloadAsync(CancellationToken cancellationToken = default)
        {
            _init = false;
            _tables = null;
            await LoadAsync(cancellationToken);
        }

        /// <summary>
        /// 加载 JSON 配置。
        /// </summary>
        /// <param name="file">Luban 生成的 JSON 文件名（如 cfg_tbweapon）</param>
        /// <returns>JArray</returns>
        private JArray LoadJson(string file)
        {
            _resourceModule ??= ModuleSystem.GetModule<IResourceModule>();

            TextAsset textAsset = TryLoadTextAsset(file);
            if (textAsset == null && !file.EndsWith(".json"))
            {
                textAsset = TryLoadTextAsset(file + ".json");
            }

            if (textAsset == null)
            {
                Log.Error($"[ConfigSystem] 加载配置失败: {file}");
                return new JArray();
            }

            return JArray.Parse(textAsset.text);
        }

        private async UniTask<JArray> LoadJsonAsync(string file, CancellationToken cancellationToken)
        {
            _resourceModule ??= ModuleSystem.GetModule<IResourceModule>();

            TextAsset textAsset = await TryLoadTextAssetAsync(file, cancellationToken);
            if (textAsset == null && !file.EndsWith(".json"))
            {
                textAsset = await TryLoadTextAssetAsync(file + ".json", cancellationToken);
            }

            if (textAsset == null)
            {
                Log.Error($"[ConfigSystem] 加载配置失败: {file}");
                return new JArray();
            }

            return JArray.Parse(textAsset.text);
        }

        private TextAsset TryLoadTextAsset(string file)
        {
            try
            {
                return _resourceModule.LoadAsset<TextAsset>(file);
            }
            catch
            {
                return null;
            }
        }

        private async UniTask<TextAsset> TryLoadTextAssetAsync(string file, CancellationToken cancellationToken)
        {
            try
            {
                return await _resourceModule.LoadAssetAsync<TextAsset>(file, cancellationToken);
            }
            catch
            {
                return null;
            }
        }
    }
}
