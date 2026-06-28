using GameConfig;
using Newtonsoft.Json.Linq;
using TEngine;
using UnityEngine;

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

    /// <summary>
    /// 加载配置。
    /// </summary>
    public void Load()
    {
        _tables = new Tables(LoadJson);
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
    /// 加载 JSON 配置。
    /// </summary>
    /// <param name="file">Luban 生成的 JSON 文件名（如 cfg_tbweapon）</param>
    /// <returns>JArray</returns>
    private JArray LoadJson(string file)
    {
        if (_resourceModule == null)
        {
            _resourceModule = ModuleSystem.GetModule<IResourceModule>();
        }

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
}
