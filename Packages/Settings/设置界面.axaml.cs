using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Win32;

namespace ZW_Tool;

public partial class 设置面板 : UserControl, I工具
{
    public string 工具命名 => "设置";
    public string 作者 => "周维";
    public string 版本 => "1.0";

    // 用于将日志发回主窗口
    public Action<string>? 主窗口日志 { get; set; }
    
    // 添加主窗口引用，用于访问窗口数据
    public 主窗口? 主窗口引用 { get; set; }

    public bool _开启饮水提醒;
    public bool 开启饮水提醒
    {
        get => _开启饮水提醒;
        set
        {
            if (_开启饮水提醒 != value)
            {
                _开启饮水提醒 = value;
                
                // 如果有主窗口引用，同步到主窗口数据
                if (主窗口引用 != null)
                {
                    主窗口引用._窗口数据.开启饮水提醒 = value;
                    主窗口引用.保存数据(); // 立即保存更改
                }
                
                OnPropertyChanged(nameof(开启饮水提醒));
            }
        }
    }

    // 自启动属性（绑定用）
    public bool _开机自启;
    public bool 开机自启
    {
        get => _开机自启;
        set
        {
            if (_开机自启 != value)
            {
                _开机自启 = value;
                SetStartupEnabled(value);
                
                // 如果有主窗口引用，同步到主窗口数据
                if (主窗口引用 != null)
                {
                    主窗口引用._窗口数据.开机自启 = value;
                    主窗口引用.保存数据(); // 立即保存更改
                }
                
                OnPropertyChanged(nameof(开机自启));
            }
        }
    }

        // -------------------------自启动-------------------------
    public const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    public const string AppStartupName = "ZW_Tool";

    // 自启动注册表操作
    public void SetStartupEnabled(bool enabled)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = GetExePath();
                    if (exePath.Contains(" ")) exePath = "\"" + exePath + "\"";
                    key.SetValue(AppStartupName, exePath);
                  new 日志("启用 自启（注册表）");
                }
                else
                {
                    key.DeleteValue(AppStartupName, false);
                  new 日志("删除 自启（注册表）");
                }
            }
            catch (Exception ex)
            {
              new 日志($"设置开机自启失败：{ex.Message}");
            }
        }
        else
        {
          new 日志("开机自启功能仅支持 Windows 平台");
        }
    }

    public string GetExePath()//不知道有什么用
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }

    public void 加载(窗口数据 窗口数据)//待完善，后续看看是开启时加载还是每次切换界面都得加载
    {
        new 日志($"设置面板加载数据: 开机自启={窗口数据.开机自启}, 开启饮水提醒={窗口数据.开启饮水提醒}, 每日饮水量={窗口数据.每日饮水量}, 饮水提醒间隔={窗口数据.饮水提醒间隔}");
        
        // 直接设置字段值，避免触发setter导致的保存操作
        _开机自启 = 窗口数据.开机自启;
        _开启饮水提醒 = 窗口数据.开启饮水提醒;
        
        // 手动触发属性更改通知，更新UI
        OnPropertyChanged(nameof(开机自启));
        OnPropertyChanged(nameof(开启饮水提醒));
        
        // 更新界面上的数值控件
        var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
        var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");
        
        if (targetNumeric != null)
            targetNumeric.Value = (decimal)窗口数据.每日饮水量;
        
        if (intervalNumeric != null)
            intervalNumeric.Value = (decimal)窗口数据.饮水提醒间隔;
        
        UpdateDrinkProgressUI(窗口数据);
        
        new 日志($"设置面板加载完成: 开机自启={_开机自启}, 开启饮水提醒={_开启饮水提醒}");
    }

    private void UpdateDrinkProgressUI(窗口数据 数据)
    {
        var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
        var progressText = this.FindControl<TextBlock>("DrinkProgressText");

        if (progressBar != null)
        {
            progressBar.Maximum = 数据.每日饮水量;
            progressBar.Value = 数据.累计饮水量;
        }

        if (progressText != null)
        {
            double percent = 数据.每日饮水量 > 0 ? (数据.累计饮水量 / 数据.每日饮水量) * 100 : 0;
            progressText.Text = $"{数据.累计饮水量:0} / {数据.每日饮水量:0} ml ({percent:0}%)";
        }
    }

    // 公共方法，用于外部更新饮水进度
    public void 更新饮水进度(窗口数据 数据)
    {
        UpdateDrinkProgressUI(数据);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public 设置面板()
    {
        try
        {
            new 日志("=== 设置面板构造函数开始 ===");

            InitializeComponent();
            DataContext = this;

            // 检查关键控件是否存在
            var target = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
            var interval = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");
            var enableCheck = this.FindControl<CheckBox>("饮水提醒开关");

            new 日志($"FindControl DrinkTargetNumeric: {(target != null ? "找到" : "未找到")}");
            new 日志($"FindControl ReminderIntervalNumeric: {(interval != null ? "找到" : "未找到")}");
            new 日志($"FindControl 饮水提醒开关: {(enableCheck != null ? "找到" : "未找到")}");

            // 不要在这里设置默认值，而是在加载方法中从主窗口数据加载
            // if (target != null) target.Value = 2000;
            // if (interval != null) interval.Value = 10;

            this.Loaded += OnLoaded;//加载布局时触发

            new 日志("设置面板构造函数正常结束");
        }
        catch (Exception ex)
        {
            new 日志($"设置面板构造函数异常: {ex.Message}");
            new 日志($"异常堆栈: {ex.StackTrace}");
            // 如果XAML加载失败，创建基本UI
            Content = new TextBlock { Text = "设置面板 - XAML加载失败" };
        }
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        new 日志("设置面板 Loaded 事件触发");
        new 日志($"面板最终尺寸: {Bounds.Width:F0} x {Bounds.Height:F0}");
        
        // 手动设置勾选框的值，确保UI正确更新
        var autoStartCheck = this.FindControl<CheckBox>("开机自启开关");
        var drinkReminderCheck = this.FindControl<CheckBox>("饮水提醒开关");
        
        if (autoStartCheck != null)
            autoStartCheck.IsChecked = _开机自启;
        
        if (drinkReminderCheck != null)
            drinkReminderCheck.IsChecked = _开启饮水提醒;

        if (主窗口引用 != null)
        {
            UpdateDrinkProgressUI(主窗口引用._窗口数据);
        }
        
        new 日志($"OnLoaded: 开机自启={_开机自启}, 开启饮水提醒={_开启饮水提醒}");
    }

    public void ApplyDrinkSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new 日志("ApplyDrinkSettings_Click 被点击");
        var target = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
        var interval = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");
        if (target?.Value is decimal t && interval?.Value is decimal i)
        {
            new 日志($"饮水设置已应用 → 目标 {t}ml，间隔 {i}分钟");
            
            // 如果有主窗口引用，同步到主窗口数据
            if (主窗口引用 != null)
            {
                主窗口引用._窗口数据.每日饮水量 = (double)t;
                主窗口引用._窗口数据.饮水提醒间隔 = (double)i;
                主窗口引用.保存数据(); // 立即保存更改
                
                // 更新主窗口中的UI组件，调用DrinkReminderModule的方法
                var drinkReminderModule = ModulePackageManager.Instance.GetModules().FirstOrDefault(m => m.工具命名 == "饮水提醒") as DrinkReminderModule;
                if (drinkReminderModule != null)
                {
                    drinkReminderModule.UpdateDrinkProgressUI();
                    drinkReminderModule.RestartTimer();
                }

                UpdateDrinkProgressUI(主窗口引用._窗口数据);
            }
        }
    }

    public void OnDrinkReminderCheckChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new 日志($"饮水提醒已{(开启饮水提醒 ? "开启" : "关闭")}");
    }

    // 打开文件打印器按钮点击事件
    public void 打开文件打印器_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new 日志("=== 打开文件打印器 ===");
        var 收集器窗口 = new 文件打印器();
        收集器窗口.Show();
    }

    protected virtual void OnPropertyChanged(string propertyName)//不知道这个方法有什么用，ai一下
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // I工具 接口实现
    public UserControl? 获取工具面板() => this;

    public void Initialize()
    {
        // 初始化模块列表
        RefreshModules();

        // 添加事件处理
        if (ModuleListBox != null)
        {
            ModuleListBox.SelectionChanged += ModuleListBox_SelectionChanged;
        }
        
        // 从主窗口实例加载数据
        var mainWindow = 主窗口.Instance;
        if (mainWindow != null)
        {
            加载(mainWindow._窗口数据);
            主窗口引用 = mainWindow;
        }
    }

    public void Shutdown()
    {
        // 清理逻辑，如果需要
    }

    // 模块管理相关方法
    private void RefreshModules()
    {
        if (ModuleListBox == null)
            return;

        // 从ModulePackageManager获取模块列表并转换为ModuleListItem
        var modules = ModulePackageManager.Instance.GetDiscoveredModules()
            .Select(m => new ModuleListItem
            {
                Name = m.Name,
                DisplayName = m.DisplayName,
                Version = m.Version,
                Author = m.Author,
                Enabled = m.Enabled,
                Description = m.Description
            })
            .ToList();

        ModuleListBox.ItemsSource = modules;
    }

    private void RefreshModules_Click(object? sender, RoutedEventArgs e)
    {
        RefreshModules();
        UpdateModuleInfo("模块列表已刷新");
    }

    private void InstallModule_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: 实现模块安装对话框
        UpdateModuleInfo("模块安装功能开发中...");
    }

    private void UninstallModule_Click(object? sender, RoutedEventArgs e)
    {
        if (ModuleListBox == null)
        {
            UpdateModuleInfo("模块列表未初始化");
            return;
        }

        var selectedItem = ModuleListBox.SelectedItem as ModuleListItem;
        if (selectedItem != null)
        {
            try
            {
                // 禁用指定的模块
                ModulePackageManager.Instance.SetModuleEnabled(selectedItem.Name, false);
                
                // 重新加载所有模块
                ModulePackageManager.Instance.ReloadAllModules();
                
                RefreshModules();
                UpdateModuleInfo($"模块 '{selectedItem.DisplayName}' 已禁用");
            }
            catch (Exception ex)
            {
                UpdateModuleInfo($"卸载模块 '{selectedItem.DisplayName}' 失败: {ex.Message}");
            }
        }
        else
        {
            UpdateModuleInfo("请先选择要卸载的模块");
        }
    }

    private void UpdateModuleInfo(string info)
    {
        if (ModuleInfoText != null)
        {
            ModuleInfoText.Text = info;
        }
    }

    // 当模块选择改变时更新信息
    private void ModuleListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ModuleListBox == null)
        {
            UpdateModuleInfo("模块列表未初始化");
            return;
        }

        var selectedItem = ModuleListBox.SelectedItem as ModuleListItem;
        if (selectedItem != null)
        {
            UpdateModuleInfo($"{selectedItem.DisplayName} v{selectedItem.Version}\n作者: {selectedItem.Author}\n{selectedItem.Description}");
        }
        else
        {
            UpdateModuleInfo("选择模块查看详细信息");
        }
    }
}

// 模块列表项类
public class ModuleListItem
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public bool Enabled { get; set; }
    public string Description { get; set; } = "";
}