using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ZW_Tool;

public partial class 小岛植物 : UserControl, I工具
{
    public string 工具命名 => "小岛植物";
    public string 作者 => "周维";
    public string 版本 => "1.0";

    public UserControl? 获取工具面板()
    {
        return this;
    }

    public void Initialize()
    {
        // 小岛植物模块初始化逻辑
    }

    public void Shutdown()
    {
        // 小岛植物模块关闭逻辑
    }

    public 小岛植物()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            // 如果XAML加载失败，创建基本UI
            Content = new TextBlock { Text = "小岛植物 - XAML加载失败" };
        }
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void Max脚本(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ((主窗口)this.Parent!).通用Max脚本调用(sender, "小岛植物");
    }

    public void Blender脚本(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ((主窗口)this.Parent!).通用Blender脚本调用(sender, "小岛植物");
    }
}