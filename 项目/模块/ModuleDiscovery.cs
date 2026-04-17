using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZW_Tool.核心;

/// <summary>
/// 模块自动发现引擎 - 无需配置，自动扫描Assets目录的所有模块
/// </summary>
public class ModuleDiscovery
{
    private readonly string _assetsPath;
    private readonly List<ModuleInfo> _discoveredModules = new List<ModuleInfo>();

    public ModuleDiscovery(string assetsPath = "")
    {
        _assetsPath = string.IsNullOrEmpty(assetsPath) 
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets")
            : assetsPath;
    }

    /// <summary>
    /// 自动发现所有模块（扫描Assets目录的所有子目录）
    /// </summary>
    public List<ModuleInfo> DiscoverModules()
    {
        _discoveredModules.Clear();

        if (!Directory.Exists(_assetsPath))
        {
            Console.WriteLine($"[警告] Assets目录不存在: {_assetsPath}");
            return _discoveredModules;
        }

        try
        {
            var moduleDirectories = Directory.GetDirectories(_assetsPath);

            foreach (var moduleDir in moduleDirectories)
            {
                var moduleInfo = DiscoverModule(moduleDir);
                if (moduleInfo != null && moduleInfo.Enabled)
                {
                    _discoveredModules.Add(moduleInfo);
                    Console.WriteLine($"[发现] 模块: {moduleInfo.DisplayName} (路径: {moduleInfo.ModulePath})");
                }
            }

            Console.WriteLine($"[信息] 自动发现了 {_discoveredModules.Count} 个模块");
            return _discoveredModules;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 扫描Assets目录时出错: {ex.Message}");
            return _discoveredModules;
        }
    }

    /// <summary>
    /// 发现单个模块的信息
    /// </summary>
    private ModuleInfo? DiscoverModule(string modulePath)
    {
        try
        {
            var moduleName = new DirectoryInfo(modulePath).Name;

            // 从源代码中提取模块信息
            var moduleInfo = ExtractModuleInfoFromSource(modulePath, moduleName);
            return moduleInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 发现模块失败 ({modulePath}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从源代码中提取模块信息
    /// </summary>
    private ModuleInfo ExtractModuleInfoFromSource(string modulePath, string moduleName)
    {
        var moduleInfo = new ModuleInfo
        {
            Name = moduleName,
            DisplayName = moduleName, // 默认显示名
            Version = "1.0.0",
            Author = "Unknown",
            Description = $"{moduleName} 模块",
            ModulePath = modulePath,
            Enabled = false // 默认为false，直到找到有效的模块实现
        };

        // 查找模块的源代码文件
        var csFiles = Directory.GetFiles(modulePath, "*.cs", SearchOption.TopDirectoryOnly);
        var axamlFiles = Directory.GetFiles(modulePath, "*.axaml", SearchOption.TopDirectoryOnly);

        if (csFiles.Length == 0 && axamlFiles.Length == 0)
        {
            Console.WriteLine($"[警告] 模块 {moduleName} 没有找到有效的源码或AXAML文件，将跳过加载");
            return moduleInfo;
        }

        // 遍历C#源文件寻找实现I工具接口的类
        foreach (var csFile in csFiles)
        {
            if (ImplementsIToolInterface(csFile))
            {
                // 提取类中的元数据
                UpdateModuleInfoFromSource(csFile, moduleInfo);
                moduleInfo.Enabled = true;
                break;
            }
        }

        // 如果还没有启用，尝试从axaml文件名推断
        if (!moduleInfo.Enabled && axamlFiles.Length > 0)
        {
            // 有些模块可能通过axaml文件名提供线索
            var displayName = Path.GetFileNameWithoutExtension(axamlFiles[0]);
            if (!string.IsNullOrEmpty(displayName))
            {
                moduleInfo.DisplayName = displayName;
                moduleInfo.Enabled = true;
            }
        }

        moduleInfo.LoadType = "Source";

        return moduleInfo;
    }

    /// <summary>
    /// 检查类是否实现了I工具接口
    /// </summary>
    private bool ImplementsIToolInterface(string filePath)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var root = syntaxTree.GetCompilationUnitRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                if (classDecl.BaseList != null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        if (baseType.ToString().Contains("I工具") || baseType.ToString().Contains("ITool"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从源代码中更新模块信息
    /// </summary>
    private void UpdateModuleInfoFromSource(string filePath, ModuleInfo moduleInfo)
    {
        try
        {
            var sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetCompilationUnitRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                // 检查是否实现了I工具接口
                if (classDecl.BaseList != null &&
                    classDecl.BaseList.Types.Any(baseType => 
                        baseType.ToString().Contains("I工具") || baseType.ToString().Contains("ITool")))
                {
                    // 提取类名作为显示名
                    moduleInfo.DisplayName = classDecl.Identifier.ValueText;
                    
                    // 尝试从注释中提取更多信息
                    var leadingTrivia = classDecl.GetLeadingTrivia();
                    foreach (var trivia in leadingTrivia)
                    {
                        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                        {
                            var comment = trivia.ToString();
                            // 这里可以添加解析注释的逻辑来提取更多元数据
                        }
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 从源码 {filePath} 提取模块信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取发现的所有模块
    /// </summary>
    public IEnumerable<ModuleInfo> GetDiscoveredModules()
    {
        return _discoveredModules;
    }

    /// <summary>
    /// 按名称获取模块信息
    /// </summary>
    public ModuleInfo? GetModuleInfo(string moduleName)
    {
        return _discoveredModules.FirstOrDefault(m => 
            m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase) ||
            m.DisplayName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// 模块信息
/// </summary>
public class ModuleInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("loadType")]
    public string LoadType { get; set; } = "Source";  // "Source" 或 "Plugin"

    // 以下属性不在JSON中
    [JsonIgnore]
    public string ModulePath { get; set; } = "";

    [JsonIgnore]
    public string PluginPath { get; set; } = "";

    /// <summary>
    /// 获取相对于Assets目录的路径
    /// </summary>
    [JsonIgnore]
    public string RelativePath => Path.GetRelativePath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"),
        ModulePath);
}