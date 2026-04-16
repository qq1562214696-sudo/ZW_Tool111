using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace 模块;

/// <summary>
/// 动态加载插件DLL的管理器
/// </summary>
public class PluginLoader
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, (AssemblyLoadContext Context, I工具 Instance)> _loadedPlugins = new();

    public PluginLoader(string assetsPath = "Assets")
    {
        _pluginsPath = Path.Combine(AppContext.BaseDirectory, assetsPath);
        
        // 创建Assets文件夹如果不存在
        if (!Directory.Exists(_pluginsPath))
        {
            Directory.CreateDirectory(_pluginsPath);
        }
    }

    /// <summary>
    /// 加载所有Assets文件夹及其子文件夹中的DLL
    /// </summary>
    public List<I工具> LoadAllPlugins()
    {
        var tools = new List<I工具>();
        
        if (!Directory.Exists(_pluginsPath))
            return tools;

        // 递归遍历所有子文件夹中的DLL文件
        var dllFiles = Directory.GetFiles(_pluginsPath, "*.dll", SearchOption.AllDirectories);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var tool = LoadPlugin(dllFile);
                if (tool != null)
                {
                    tools.Add(tool);
                    tool.Initialize();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载插件失败: {dllFile} - {ex.Message}");
            }
        }

        return tools;
    }

    /// <summary>
    /// 加载单个DLL插件
    /// </summary>
    public I工具? LoadPlugin(string dllPath)
    {
        if (!File.Exists(dllPath))
            return null;

        var fileName = Path.GetFileNameWithoutExtension(dllPath);
        
        // 如果已加载，先卸载
        if (_loadedPlugins.ContainsKey(fileName))
        {
            UnloadPlugin(fileName);
        }

        try
        {
            // 创建独立的AssemblyLoadContext，便于卸载
            var context = new AssemblyLoadContext(fileName, isCollectible: true);
            
            // 从文件流加载程序集，避免文件锁定
            using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
            var assembly = context.LoadFromStream(fs);

            // 查找实现I工具接口的类
            var toolType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(I工具).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (toolType == null)
                return null;

            // 创建实例
            var instance = (I工具?)Activator.CreateInstance(toolType);
            if (instance != null)
            {
                _loadedPlugins[fileName] = (context, instance);
            }

            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载插件出错: {dllPath} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 卸载指定的插件
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var plugin))
        {
            try
            {
                plugin.Instance.Shutdown();
                plugin.Context.Unload();
                _loadedPlugins.Remove(pluginName);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"卸载插件出错: {pluginName} - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public void UnloadAllPlugins()
    {
        var pluginNames = _loadedPlugins.Keys.ToList();
        foreach (var name in pluginNames)
        {
            UnloadPlugin(name);
        }
    }

    /// <summary>
    /// 获取所有已加载的插件
    /// </summary>
    public IEnumerable<I工具> GetLoadedPlugins() => _loadedPlugins.Values.Select(p => p.Instance);

    /// <summary>
    /// 获取plugins文件夹路径
    /// </summary>
    public string PluginsPath => _pluginsPath;
}