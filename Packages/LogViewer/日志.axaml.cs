using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ZW_Tool.核心;

namespace ZW_Tool;

public partial class 日志面板 : UserControl, I工具
{
    public string 工具命名 => "日志面板";
    public string 作者 => "周维";
    public string 版本 => "1.0";

    public FileSystemWatcher? _logWatcher;
    public string _logFilePath = Path.Combine(AppContext.BaseDirectory, "Max", "QF", "Max_Log.txt");

    public static readonly SemaphoreSlim _logFileSemaphore = new(1, 1);
    private static 日志弹窗? _弹窗实例;

    public UserControl? 获取工具面板() => this;

    public void Initialize()
    {
        StartMaxLogWatcher();
    }

    public void Shutdown()
    {
        _弹窗实例?.Close();
        _弹窗实例 = null;
        StopMaxLogWatcher();
    }

    public void 打开日志弹窗_Click(object? sender, RoutedEventArgs e)
    {
        if (_弹窗实例 == null)
        {
            _弹窗实例 = new 日志弹窗();
            _弹窗实例.Closed += (_, _) => _弹窗实例 = null;
            _弹窗实例.Show();
        }
        else
        {
            _弹窗实例.Activate();
        }
    }

    public ObservableCollection<日志数据> 日志列表 => 日志服务.日志列表;
    public static 日志面板? 实例 { get; private set; }

    public 日志面板()
    {
        实例 = this;
        try
        {
            InitializeComponent();
            DataContext = this;
        }
        catch
        {
            Content = new TextBlock { Text = "日志面板 - XAML加载失败" };
        }
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void 清空日志_Click(object? sender, RoutedEventArgs e)
    {
        日志服务.Clear();
        日志服务.Add("日志已清空");
    }

    public void 复制全部日志_Click(object? sender, RoutedEventArgs e)
    {
        if (日志列表.Count == 0)
        {
            日志服务.Add("没有可复制的日志");
            return;
        }

        var sb = new StringBuilder();
        foreach (var item in 日志列表)
            sb.AppendLine(item.内容);

        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(sb.ToString());
        日志服务.Add($"已复制 {日志列表.Count} 条日志到剪贴板");
    }

    public void StartMaxLogWatcher()
    {
        try
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            string? dir = Path.GetDirectoryName(_logFilePath);
            if (dir == null) return;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            _logWatcher = new FileSystemWatcher
            {
                Path = dir,
                Filter = "Max_Log.txt",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _logWatcher.Changed += OnMaxLogChanged;
            _logWatcher.Created += OnMaxLogChanged;
        }
        catch (Exception ex)
        {
            EventAggregator.PublishLog($"[系统] Max 日志监控启动失败：{ex.Message}", LogLevel.Error);
        }
    }

    public void StopMaxLogWatcher()
    {
        if (_logWatcher != null)
        {
            _logWatcher.Changed -= OnMaxLogChanged;
            _logWatcher.Created -= OnMaxLogChanged;
            _logWatcher.EnableRaisingEvents = false;
            _logWatcher.Dispose();
            _logWatcher = null;
        }
    }

    public async void OnMaxLogChanged(object? sender, FileSystemEventArgs e)
    {
        await Task.Delay(400);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _logFileSemaphore.WaitAsync();
            try
            {
                if (!File.Exists(_logFilePath)) return;

                string content;
                using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    content = await sr.ReadToEndAsync();
                    fs.SetLength(0);
                    await fs.FlushAsync();
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    EventAggregator.PublishLog($"────── MaxScript 日志 ──────\n{content.Trim()}");
                }
            }
            catch (Exception ex)
            {
                EventAggregator.PublishLog($"读取 Max 日志文件失败：{ex.Message}", LogLevel.Error);
            }
            finally
            {
                _logFileSemaphore.Release();
            }
        });
    }
}