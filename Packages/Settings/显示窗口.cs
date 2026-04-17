using Avalonia.Controls;
using System;
using System.Runtime.InteropServices;

namespace ZW_Tool;

/// <summary>
/// 全局快捷键模块（简化版）
/// </summary>
public class 快捷键 : UserControl
{
    // 热键常量
    public const int 热键ID_F5 = 9001;
    public const uint 热键消息 = 0x0312;
    public const uint F5虚拟键 = 0x74;

    public Action? 热键触发事件 { get; set; }
    public Action<string>? 日志回调 { get; set; }

    public void 输出日志(string 消息)
    {
        日志回调?.Invoke($"[全局快捷键] {消息}");
    }

    // Windows API - 全局热键注册
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}