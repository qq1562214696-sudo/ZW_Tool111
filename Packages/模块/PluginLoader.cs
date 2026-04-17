using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace 模块;

public class PluginLoader
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, (AssemblyLoadContext Context, I工具 Instance)> _loadedPlugins = new(StringComparer.OrdinalIgnoreCase);

    public PluginLoader(string assetsPath = "Assets")
    {
        _pluginsPath = Path.Combine(AppContext.BaseDirectory, assetsPath);

        if (!Directory.Exists(_pluginsPath))
        {
            Directory.CreateDirectory(_pluginsPath);
            Console.WriteLine($"[PluginLoader] 已创建目录: {_pluginsPath}");
        }

        Console.WriteLine($"[PluginLoader] 扫描路径 → {_pluginsPath}");
    }

    public List<I工具> LoadAllPlugins()
    {
        var tools = new List<I工具>();

        if (!Directory.Exists(_pluginsPath))
        {
            Console.WriteLine("[PluginLoader] Assets 目录不存在");
            return tools;
        }

        // 递归获取所有 .dll，排除常见系统/临时文件
        var dllFiles = Directory.GetFiles(_pluginsPath, "*.dll", SearchOption.AllDirectories)
                                .Where(f => !f.Contains("System.") && 
                                           !f.Contains("Microsoft.") && 
                                           !f.Contains("Avalonia."))
                                .ToList();

        Console.WriteLine($"[PluginLoader] 共发现 {dllFiles.Count} 个 DLL 文件");

        foreach (var dllPath in dllFiles)
        {
            var fileName = Path.GetFileName(dllPath);
            Console.WriteLine($"[PluginLoader] 尝试加载 → {fileName}");

            try
            {
                var tool = LoadSinglePlugin(dllPath);
                if (tool != null)
                {
                    tools.Add(tool);
                    tool.Initialize();
                    Console.WriteLine($"[PluginLoader] ✓ 加载成功: {tool.工具命名} (by {tool.作者})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] ✗ 加载失败 {fileName}: {ex.Message}");
                if (ex is ReflectionTypeLoadException rtle)
                {
                    foreach (var loaderEx in rtle.LoaderExceptions)
                        if (loaderEx != null)
                            Console.WriteLine($"   └─ {loaderEx.Message}");
                }
            }
        }

        Console.WriteLine($"[PluginLoader] 最终成功加载 {tools.Count} 个模块");
        return tools;
    }

    private I工具? LoadSinglePlugin(string dllPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(dllPath);

        // 如果已经加载过，先卸载
        if (_loadedPlugins.ContainsKey(fileName))
            UnloadPlugin(fileName);

        var context = new AssemblyLoadContext(fileName, isCollectible: true);

        try
        {
            using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var assembly = context.LoadFromStream(fs);

            // 查找实现了 I工具 接口的类
            var toolType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(I工具).IsAssignableFrom(t) 
                                  && !t.IsInterface 
                                  && !t.IsAbstract);

            if (toolType == null)
            {
                Console.WriteLine($"   └─ 未找到 I工具 实现");
                return null;
            }

            var instance = Activator.CreateInstance(toolType) as I工具;
            if (instance != null)
            {
                _loadedPlugins[fileName] = (context, instance);
                return instance;
            }
        }
        catch (Exception ex)
        {
            context.Unload();
            throw;
        }

        return null;
    }

    public void UnloadPlugin(string pluginName)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var plugin))
        {
            try
            {
                plugin.Instance.Shutdown();
                plugin.Context.Unload();
                _loadedPlugins.Remove(pluginName);
                Console.WriteLine($"[PluginLoader] 已卸载: {pluginName}");
            }
            catch { }
        }
    }

    public void UnloadAllPlugins()
    {
        foreach (var name in _loadedPlugins.Keys.ToList())
            UnloadPlugin(name);
    }
}