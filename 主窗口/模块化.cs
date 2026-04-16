using Avalonia.Media;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using 模块;

namespace ZW_Tool.核心;

public partial class 主窗口
{
    private readonly Dictionary<string, Button> _moduleButtons = new Dictionary<string, Button>();
    private I工具? _currentModule;

    // 动态生成模块按钮
    private void 生成模块按钮()
    {
        Console.WriteLine("开始生成模块按钮");
        var buttonPanel = this.FindControl<WrapPanel>("ModuleButtonPanel");
        if (buttonPanel == null) 
        {
            Console.WriteLine("ModuleButtonPanel 未找到");
            return;
        }

        buttonPanel.Children.Clear();
        _moduleButtons.Clear();

        var modules = ModulePackageManager.Instance.GetModules();
        Console.WriteLine($"找到 {modules.Count()} 个模块");

        foreach (var module in modules)
        {
            // 只为有UI面板的模块生成按钮，后台模块不显示按钮
            if (module.获取工具面板() == null) 
            {
                Console.WriteLine($"模块 {module.工具命名} 没有UI面板，跳过");
                continue;
            }

            Console.WriteLine($"为模块 {module.工具命名} 生成按钮");

            var button = new Button
            {
                Content = module.工具命名,
                Margin = new Avalonia.Thickness(5),
                Padding = new Avalonia.Thickness(10, 5),
                MinWidth = 80,
                Height = 30
            };

            button.Click += (s, e) => ShowModulePanel(module);
            buttonPanel.Children.Add(button);
            _moduleButtons[module.工具命名] = button;
        }
    }

    // 显示模块面板
    private void ShowModulePanel(I工具 module)
    {
        var panel = module.获取工具面板();
        if (panel != null)
        {
            ModuleContent.Content = panel;
            _currentModule = module;
            更新当前面板(module.工具命名);
        }
    }

    // 更新当前面板并保存
    private void 更新当前面板(string 面板名称)
    {
        _窗口数据.当前面板 = 面板名称;
        保存数据();
        更新按钮高亮状态(面板名称);
    }

    // 更新按钮高亮状态
    private void 更新按钮高亮状态(string 面板名称)
    {
        // 重置所有按钮的样式
        foreach (var button in _moduleButtons.Values)
        {
            ResetButtonStyle(button);
        }

        // 高亮当前面板对应的按钮
        if (_moduleButtons.TryGetValue(面板名称, out var currentButton))
        {
            HighlightButton(currentButton);
        }
    }

    // 按钮样式重置
    private void ResetButtonStyle(Button? button)
    {
        if (button != null)
        {
            button.Background = new SolidColorBrush(Avalonia.Media.Colors.LightGray);
            button.Foreground = new SolidColorBrush(Avalonia.Media.Colors.Black);
        }
    }

    // 按钮高亮
    private void HighlightButton(Button? button)
    {
        if (button != null)
        {
            button.Background = new SolidColorBrush(Avalonia.Media.Colors.DodgerBlue);
            button.Foreground = new SolidColorBrush(Avalonia.Media.Colors.White);
        }
    }
}