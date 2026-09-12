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
                    // 诊断：谁在 LoadAsync 完成前提前访问 Tables（会触发同步懒加载并可能加载失败）。
                    Log.Warning($"[ConfigSystem] Tables 被提前访问，触发同步懒加载。调用堆栈:\n{System.Environment.StackTrace}");
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
            "cfg_tbinventoryconfig",
            "cfg_tbcamera",
            "cfg_tbballistic",
            "cfg_tbuiconfig",
            "cfg_tbpickup",
            "cfg_tbbuilding",
            "cfg_tbproduction",
            "cfg_tborder",
            "cfg_tbsimtimeconfig",
            "cfg_tbcamera3d",
            "cfg_tblootcontainer",
            "cfg_tbnote",
            "cfg_tbplayerlevel",
            "cfg_tbunlock",
            "cfg_tbnpc",
            "cfg_tbdialogue",
            "cfg_tbdialoguenode",
            "cfg_tbquest",
            "cfg_tbquestobjective",
            "cfg_tblocalization",
            "cfg_tbcompanion",
            "cfg_tbcompanionbark",
            "cfg_tbaudio",
        };

        /// <summary>
        /// 同步加载配置（不推荐；请使用 <see cref="LoadAsync"/> 在启动时预加载）。
        /// </summary>
        public void Load()
        {
            _tables = new Tables(LoadJson);
            // 以核心表 TbLevel 为哨兵：资源系统未就绪时同步加载会拿到全空表，
            // 此时不标记初始化完成，留给 LoadAsync 的重试兜底重建配置。
            _init = _tables.TbLevel.DataList.Count > 0;
        }

        /// <summary>
        /// 异步预加载所有 Luban JSON 配置。
        /// 启动早期资源系统（YooAsset 编辑器文件系统 / AssetDatabase）可能尚未就绪，
        /// 导致全部表加载返回空；此处对"全部失败"做延迟重试兜底。
        /// </summary>
        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_init) return;

            _resourceModule ??= ModuleSystem.GetModule<IResourceModule>();

            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var jsonCache = new Dictionary<string, JArray>();
                foreach (var file in _tableFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    jsonCache[file] = await LoadJsonAsync(file, cancellationToken);
                }

                // 只要有一张表加载出数据就认为资源系统正常（允许个别表合法为空）。
                bool allEmpty = true;
                foreach (var json in jsonCache.Values)
                {
                    if (json != null && json.Count > 0)
                    {
                        allEmpty = false;
                        break;
                    }
                }

                if (!allEmpty || attempt == maxAttempts)
                {
                    _tables = new Tables(file => jsonCache.TryGetValue(file, out var json) ? json : new JArray());
                    _init = true;
                    if (allEmpty)
                    {
                        Log.Error("[ConfigSystem] 多次重试后配置仍全部为空，请检查资源系统！");
                    }
                    return;
                }

                Log.Warning($"[ConfigSystem] 第 {attempt} 次加载全部为空，{0.5f * attempt}s 后重试…");
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5 * attempt), cancellationToken: cancellationToken);
            }
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
                // 单表失败降级为 Warning：LoadAsync 有整体重试兜底，最终仍全空会统一报 Error。
                Log.Warning($"[ConfigSystem] 加载配置失败: {file}");
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
            catch (System.Exception e)
            {
                // 诊断：输出真实失败原因（此前 catch 吞掉异常导致只能看到"加载配置失败"）。
                Log.Error($"[ConfigSystem] 加载配置异常: {file}, {e.GetType().Name}: {e.Message}");
                return null;
            }
        }
    }
}
