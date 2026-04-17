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

    // MaxScript 日志监控相关字段
    public FileSystemWatcher? _logWatcher;
    public string _logFilePath = Path.Combine(AppContext.BaseDirectory, "Max", "QF", "Max_Log.txt");
    
    // 日志文件访问信号量
    public static readonly SemaphoreSlim _logFileSemaphore = new(1, 1);

    public UserControl? 获取工具面板()
    {
        return this;
    }

    public void Initialize()
    {
        // 日志面板模块初始化逻辑
        // 启动Max日志监控
        StartMaxLogWatcher();
    }

    public void Shutdown()
    {
        // 日志面板模块关闭逻辑
        if (_弹窗实例 != null)
        {
            _弹窗实例.Close();
            _弹窗实例 = null;
        }
        // 停止Max日志监控
        StopMaxLogWatcher();
    }

        private static 日志弹窗? _弹窗实例;   // 静态引用，用于单例

    // 打开日志弹窗按钮点击事件
    public void 打开日志弹窗_Click(object? sender, RoutedEventArgs e)
    {
        if (_弹窗实例 == null )
        {
            _弹窗实例 = new 日志弹窗();
            _弹窗实例.Closed += (_, _) => _弹窗实例 = null; // 窗口关闭时清空引用
            _弹窗实例.Show();
        }
        else
        {
            // 如果已存在且未关闭，激活该窗口
            _弹窗实例.Activate();
        }
    }



    public ObservableCollection<日志数据> 日志列表 => 日志服务.日志列表;
    private const int 最大日志条数 = 500;   // 可根据需要调整，500~2000 都合理
    public static 日志面板? 实例 { get; private set; }

    public 日志面板()
    {
        实例 = this;   // 先设置静态实例
        try
        {
            InitializeComponent();
            // 设置数据上下文，确保日志列表能够正确绑定
            DataContext = this;
        }
        catch
        {
            // 如果XAML加载失败，创建基本UI
            Content = new TextBlock { Text = "日志面板 - XAML加载失败" };
        }
    }

    public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // ====================== 清理旧日志的方法 ======================
    public void 自动清理旧日志()
    {
        if (日志列表.Count <= 最大日志条数) 
            return;

        // 一次删除 200 条，效率较高，避免频繁 RemoveAt(0) 导致性能问题
        int 要删除的数量 = 200;
        for (int i = 0; i < 要删除的数量 && 日志列表.Count > 0; i++)
        {
            日志列表.RemoveAt(0);
        }
    }

    // ====================== 按钮事件（已修复） ======================
    public void 清空日志_Click(object? sender, RoutedEventArgs e)
    {
        日志列表.Clear();                    // 移除无用的 if 判断
        new 日志("日志已清空");
    }

    public void 复制全部日志_Click(object? sender, RoutedEventArgs e)
    {
        if (日志列表.Count == 0)
        {
            new 日志("没有可复制的日志");
            return;
        }

        var sb = new StringBuilder();
        foreach (var item in 日志列表)
            sb.AppendLine(item.内容);

        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(sb.ToString());
        new 日志($"已复制 {日志列表.Count} 条日志到剪贴板");
    }

    // ====================== 自动滚动到底部（已完善） ======================
    public void 自动滚到底()
    {
        // 使用 Dispatcher 确保在 UI 线程，且在添加项之后再滚动
        Dispatcher.UIThread.Post(() =>
        {
            var listBox = this.FindControl<ListBox>("日志列表框");
            if (listBox == null || 日志列表.Count == 0) 
                return;

            // 滚动到最后一条
            listBox.ScrollIntoView(日志列表[日志列表.Count - 1]);

            // 可选：如果想更激进地滚动到底部，可以额外操作 ScrollViewer
            var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
            scrollViewer?.ScrollToEnd();
        }, DispatcherPriority.Background);   // 使用 Background 优先级，让 UI 先渲染新项
    }

    // ========== Max 日志监控相关方法 ==========
    public void StartMaxLogWatcher()
    {
        try
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
              new 日志("[警告] 日志文件路径为空，无法启动监控");
                return;
            }

            string? dir = Path.GetDirectoryName(_logFilePath);

            // null 安全处理
            if (dir is null)
            {
              new 日志("[错误] 无法获取日志文件目录");
                return;
            }

            // 现在 dir 已被确认非 null
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _logWatcher = new FileSystemWatcher
            {
                Path = dir,
                Filter = "Max_Log.txt",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _logWatcher.Changed += OnMaxLogChanged;
            _logWatcher.Created += OnMaxLogChanged;

        } catch (Exception ex)
        {
          new 日志($"[系统] Max 日志监控启动失败：{ex.Message}");
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
        await Task.Delay(400); // 初始延迟，避免文件刚写入时锁定
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // 使用异步信号量保证同一时间只有一个线程读写文件
            await _logFileSemaphore.WaitAsync();
            try
            {
                const int maxRetries = 5;
                int retryDelay = 200; // 毫秒
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        if (!File.Exists(_logFilePath))
                            return;

                        string content = "";
                        // 使用 GB2312 编码读取（MaxScript 默认写入 ANSI 中文）
                        using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            content = await sr.ReadToEndAsync();
                            // 清空文件，以便下次只读新增内容
                            fs.SetLength(0);
                            await fs.FlushAsync(); // 确保清空立即生效
                        }

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                          new 日志($"────── MaxScript 日志 ──────\n{content.Trim()}");
                        }
                        break; // 成功则退出循环
                    }
                    catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process") ||
                                                    ioEx.Message.Contains("正由另一进程使用"))
                    {
                        if (i == maxRetries - 1)
                        {
                          new 日志($"读取 Max 日志文件失败（重试 {maxRetries} 次后）：{ioEx.Message}");
                        }
                        else
                        {
                            await Task.Delay(retryDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                      new 日志($"读取 Max 日志文件失败：{ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                _logFileSemaphore.Release();
            }
        });
    }
}

// 日志数据与日志工具已移至主程序集中的 日志服务（项目/公共/LogService.cs）。