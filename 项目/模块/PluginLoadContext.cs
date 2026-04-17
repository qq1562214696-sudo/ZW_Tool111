using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace ZW_Tool.核心;

/// <summary>
/// 插件加载上下文，优先使用主程序已加载的程序集，避免类型重复
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. 优先返回主程序默认上下文已加载的程序集（包括 Avalonia、模块.dll 等）
        foreach (var loadedAsm in AssemblyLoadContext.Default.Assemblies)
        {
            if (loadedAsm.GetName().Name == assemblyName.Name)
                return loadedAsm;
        }

        // 2. 尝试从插件所在目录解析依赖（仅当主程序没有时才加载）
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null && File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // 3. 返回 null，交由默认加载逻辑处理
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null && File.Exists(libraryPath))
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        return IntPtr.Zero;
    }
}

public class PluginLoader
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, (PluginLoadContext Context, I工具 Instance)> _loadedPlugins
        = new(StringComparer.OrdinalIgnoreCase);

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

        // 递归获取所有 .dll，不再过滤 Avalonia（依赖解析会处理）
        var dllFiles = Directory.GetFiles(_pluginsPath, "*.dll", SearchOption.AllDirectories)
                                .Where(f => !f.Contains("System.") &&
                                           !f.Contains("Microsoft."))
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

        if (_loadedPlugins.ContainsKey(fileName))
            UnloadPlugin(fileName);

        var context = new PluginLoadContext(dllPath);

        try
        {
            var assembly = context.LoadFromAssemblyPath(dllPath);
            Console.WriteLine($"   已加载程序集: {assembly.FullName}");

            var toolType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(I工具).IsAssignableFrom(t)
                                  && !t.IsInterface
                                  && !t.IsAbstract);

            if (toolType == null)
            {
                var allPublicTypes = assembly.GetTypes()
                    .Where(t => t.IsPublic)
                    .Select(t => t.FullName);
                Console.WriteLine($"   公共类型列表: {string.Join(", ", allPublicTypes)}");
                Console.WriteLine($"   └─ 未找到 I工具 实现");
                return null;
            }

            Console.WriteLine($"   找到 I工具 实现: {toolType.FullName}");

            var instance = Activator.CreateInstance(toolType) as I工具;
            if (instance != null)
            {
                _loadedPlugins[fileName] = (context, instance);
                return instance;
            }
            else
            {
                Console.WriteLine($"   └─ 创建实例失败（可能构造函数抛出异常）");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   └─ 加载异常: {ex.GetType().Name} - {ex.Message}");
            if (ex is ReflectionTypeLoadException rtle)
            {
                foreach (var loaderEx in rtle.LoaderExceptions.Where(e => e != null))
                    Console.WriteLine($"      └─ {loaderEx!.Message}");
            }
            else if (ex.InnerException != null)
            {
                Console.WriteLine($"      └─ 内部异常: {ex.InnerException.Message}");
            }

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
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] 卸载 {pluginName} 时出错: {ex.Message}");
            }
        }
    }

    public void UnloadAllPlugins()
    {
        foreach (var name in _loadedPlugins.Keys.ToList())
            UnloadPlugin(name);
    }
}