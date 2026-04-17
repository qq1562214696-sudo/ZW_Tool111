// 模块加载系统核心接口
using Avalonia.Controls;

namespace ZW_Tool.核心;

public interface I工具
{
    string 工具命名 { get; }     // 显示名称
    string 作者 { get; }         // 作者
    string 版本 { get; }        // 工具版本

    // 工具面板
    UserControl? 获取工具面板();

    /// <summary>
    /// 初始化模块（在主窗口启动时调用）
    /// </summary>
    void Initialize();

    /// <summary>
    /// 关闭模块（在主窗口关闭时调用）
    /// </summary>
    void Shutdown();
}