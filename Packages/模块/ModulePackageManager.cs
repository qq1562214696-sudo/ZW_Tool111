using System;
using System.Collections.Generic;
using System.Linq;

namespace 模块;

/// <summary>
/// 模块包管理器 - 独立的模块加载系统
/// </summary>
public class ModulePackageManager
{
    private static ModulePackageManager? _instance;
    public static ModulePackageManager Instance => _instance ??= new ModulePackageManager();

    private readonly List<I工具> _loadedModules = new List<I工具>();
    private readonly PluginLoader _pluginLoader;
    private readonly ModuleDiscovery _moduleDiscovery;

    private ModulePackageManager()
    {
        _pluginLoader = new PluginLoader("Assets");
        _moduleDiscovery = new ModuleDiscovery();
    }

    /// <summary>
    /// 加载所有插件（兼容旧接口，现在使用PluginLoader）
    /// </summary>
    public void LoadModuleConfig()
    {
        LoadModules();
    }

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public void LoadModules()
    {
        _loadedModules.Clear();
        var modules = _pluginLoader.LoadAllPlugins();
        _loadedModules.AddRange(modules);
        
        Console.WriteLine($"[信息] 加载了 {_loadedModules.Count} 个模块");
    }

    /// <summary>
    /// 获取所有已加载的模块
    /// </summary>
    public IEnumerable<I工具> GetModules()
    {
        return _loadedModules;
    }

    /// <summary>
    /// 初始化所有模块（注意：PluginLoader.LoadAllPlugins()已经调用了Initialize，这是备用方法）
    /// </summary>
    public void InitializeAllModules()
    {
        // PluginLoader.LoadAllPlugins()已经调用了Initialize()，通常无需再调用
        // 但保留此方法以兼容旧代码
        Console.WriteLine($"[信息] 所有模块已在加载时初始化");
    }

    /// <summary>
    /// 关闭所有模块
    /// </summary>
    public void ShutdownAllModules()
    {
        foreach (var module in _loadedModules)
        {
            try
            {
                module.Shutdown();
                Console.WriteLine($"[关闭] 模块 '{module.工具命名}' 已关闭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] 关闭模块 '{module.工具命名}' 失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取指定名称的模块
    /// </summary>
    public I工具? GetModule(string moduleName)
    {
        return _loadedModules.FirstOrDefault(m => m.工具命名 == moduleName);
    }

    /// <summary>
    /// 获取发现的所有模块信息
    /// </summary>
    public IEnumerable<ModuleInfo> GetDiscoveredModules()
    {
        return _moduleDiscovery.DiscoverModules();
    }

    /// <summary>
    /// 设置模块是否启用
    /// </summary>
    public bool SetModuleEnabled(string moduleName, bool enabled)
    {
        // 这里可以根据需要实现模块启用/禁用逻辑
        // 暂时只做日志记录
        Console.WriteLine($"[信息] 模块 {moduleName} 启用状态设置为: {enabled}");
        return true;
    }

    /// <summary>
    /// 重新加载所有模块
    /// </summary>
    public void ReloadAllModules()
    {
        ShutdownAllModules();  // 先关闭所有模块
        LoadModules();         // 重新加载所有模块
        InitializeAllModules(); // 初始化所有模块
    }
}