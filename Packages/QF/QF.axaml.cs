using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ZW_Tool;

public partial class QF : UserControl, I工具
{
    public string 工具命名 => "QF";
    public string 作者 => "周维";
    public string 版本 => "1.0";

    public UserControl? 获取工具面板()
    {
        return this;
    }

    public void Initialize()
    {
        // QF模块初始化逻辑
    }

    public void Shutdown()
    {
        // QF模块关闭逻辑
    }

    public QF()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            // 如果XAML加载失败，创建基本UI
            Content = new TextBlock { Text = "QF工具 - XAML加载失败" };
        }
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    //需要好好重构一下---------------------------
    public async void QF_初始化按钮_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // 使用FindControl查找控件
        var unityPathInput = this.FindControl<TextBox>("QF_UnityPathInput");
        string 输入原始 = unityPathInput?.Text?.Trim() ?? "";
        string 新路径 = 输入原始.Trim().Replace("/", "\\").TrimEnd('\\');

        if (string.IsNullOrWhiteSpace(新路径))
        {
            new 报错("请输入 Unity Assets 路径！");
            return;
        }
        if (!Directory.Exists(新路径))
        {
            new 报错($"路径不存在或无效：\n{新路径}");
            return;
        }

        try
        {
            string config路径 = ((主窗口)this.Parent!).GetConfigPath("Max", "QF_Config.txt");
            string configDir = Path.GetDirectoryName(config路径);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            if (!File.Exists(config路径)) await File.WriteAllTextAsync(config路径, "");

            var lines = (await File.ReadAllTextAsync(config路径))
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
                if (lines[i].StartsWith("AssetsPath=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"AssetsPath={新路径}";
                    found = true;
                    break;
                }
            if (!found) lines.Add($"AssetsPath={新路径}");

            await File.WriteAllTextAsync(config路径, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            new 日志($"成功更新 QF_Config.txt 中的 AssetsPath 为：{新路径}");
        }
        catch (Exception ex)
        {
            new 报错($"保存配置失败：{ex.Message}");
        }
    }

    public void Max脚本(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ((主窗口)this.Parent!).通用Max脚本调用(sender, "QF");
    }

    public void Blender脚本(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ((主窗口)this.Parent!).通用Blender脚本调用(sender, "QF");
    }

}