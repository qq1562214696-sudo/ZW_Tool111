using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Win32;
using Avalonia.Threading;
using System.Threading.Tasks;
using 模块;

namespace ZW_Tool;

public partial class 设置面板 : UserControl, I工具
{
    public string 工具命名 => "设置";
    public string 作者 => "周维";
    public string 版本 => "1.0";

    public Action<string>? 日志事件 { get; set; }

    // 饮水提醒相关
    private DispatcherTimer? 饮水提醒定时器;

    public bool _开启饮水提醒;
    public bool 开启饮水提醒
    {
        get => _开启饮水提醒;
        set
        {
            if (_开启饮水提醒 != value)
            {
                _开启饮水提醒 = value;
                OnPropertyChanged(nameof(开启饮水提醒));
                处理饮水提醒开关变化(value);
            }
        }
    }

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
                OnPropertyChanged(nameof(开机自启));
            }
        }
    }

    // -------------------------自启动-------------------------
    public const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    public const string AppStartupName = "ZW_Tool";

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
                    日志事件?.Invoke("启用 自启（注册表）");
                }
                else
                {
                    key.DeleteValue(AppStartupName, false);
                    日志事件?.Invoke("删除 自启（注册表）");
                }
            }
            catch (Exception ex)
            {
                日志事件?.Invoke($"设置开机自启失败：{ex.Message}");
            }
        }
        else
        {
            日志事件?.Invoke("开机自启功能仅支持 Windows 平台");
        }
    }

    public string GetExePath()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }

    public void 加载() { }

    // 公共方法，用于外部更新饮水进度
    public void 更新饮水进度()
    {
        var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
        var progressText = this.FindControl<TextBlock>("DrinkProgressText");

        if (progressBar != null && progressText != null)
        {
            progressBar.Value = 0; // 可后续接实际数据
            progressText.Text = "0 / 2000 ml (0%)";
        }
    }

    private void 处理饮水提醒开关变化(bool isEnabled)
    {
        if (isEnabled)
            启动饮水提醒定时器();
        else
            饮水提醒定时器?.Stop();
    }

    private void 启动饮水提醒定时器()
    {
        if (饮水提醒定时器 == null)
        {
            饮水提醒定时器 = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
            饮水提醒定时器.Tick += async (s, e) => await 检查并显示饮水提醒();
        }
        饮水提醒定时器.Start();
    }

    private async Task 检查并显示饮水提醒()
    {
        if (!_开启饮水提醒) return;

        var reminder = new 饮水提醒();
        reminder.DrinkConfirmed += (s, ml) => 日志事件?.Invoke($"用户本次饮水 {ml}ml");
        reminder.Show();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    public 设置面板()
    {
        InitializeComponent();
        DataContext = this;
        this.Loaded += OnLoaded;
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var autoStartCheck = this.FindControl<CheckBox>("开机自启开关");
        var drinkReminderCheck = this.FindControl<CheckBox>("饮水提醒开关");

        if (autoStartCheck != null) autoStartCheck.IsChecked = _开机自启;
        if (drinkReminderCheck != null) drinkReminderCheck.IsChecked = _开启饮水提醒;

        更新饮水进度();
    }

    public void ApplyDrinkSettings_Click(object? sender, RoutedEventArgs e)
    {
        var target = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
        var interval = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");

        if (target?.Value is decimal t && interval?.Value is decimal i)
        {
            日志事件?.Invoke($"饮水设置已应用 → 目标 {t}ml，间隔 {i}分钟");
            更新饮水进度();
        }
    }

    public void OnDrinkReminderCheckChanged(object? sender, RoutedEventArgs e)
    {
        日志事件?.Invoke($"饮水提醒已{(_开启饮水提醒 ? "开启" : "关闭")}");
    }

    public void 打开文件打印器_Click(object? sender, RoutedEventArgs e)
    {
        日志事件?.Invoke("=== 打开文件打印器 ===");
        new 文件打印器().Show();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // I工具 接口实现
    public UserControl? 获取工具面板() => this;

    public void Initialize() { }

    public void Shutdown()
    {
        饮水提醒定时器?.Stop();
    }
}