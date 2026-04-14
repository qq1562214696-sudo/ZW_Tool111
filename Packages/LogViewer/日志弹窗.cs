using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text;
using Avalonia.Interactivity;

namespace ZW_Tool;

public partial class 日志弹窗 : Window
{
    public 日志弹窗()
        {
            InitializeComponent();
            this.Title = "日志弹窗";
            this.Width = 600;
            this.Height = 400;
            this.DataContext = 日志面板.实例;

            // 手动设置ItemsSource
            var listBox = this.FindControl<ListBox>("弹窗日志列表框");
            if (listBox != null && 日志面板.实例 != null)
            {
                listBox.ItemsSource = 日志面板.实例.日志列表;
            }

            // 监听集合变化，自动滚动
            if (日志面板.实例 != null)
            {
                日志面板.实例.日志列表.CollectionChanged += (s, e) => 自动滚到底();
            }
        }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 清空日志按钮事件
    public void 清空日志_Click(object? sender, RoutedEventArgs e)
    {
        if (日志面板.实例 != null)
        {
            日志面板.实例.日志列表.Clear();
            new 日志("日志已清空（弹窗操作）");
        }
    }

    // 复制全部日志按钮事件
    public async void 复制全部日志_Click(object? sender, RoutedEventArgs e)
    {
        var 日志列表 = 日志面板.实例?.日志列表;
        if (日志列表 == null || 日志列表.Count == 0)
        {
            new 日志("没有可复制的日志");
            return;
        }

        var sb = new StringBuilder();
        foreach (var item in 日志列表)
        {
            sb.AppendLine(item.内容);
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(sb.ToString());
            new 日志($"已复制 {日志列表.Count} 条日志到剪贴板（弹窗操作）");
        }
        else
        {
            new 日志("无法访问剪贴板");
        }
    }

    // 自动滚动到底部
        private void 自动滚到底()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var listBox = this.FindControl<ListBox>("弹窗日志列表框");
                if (listBox == null || 日志面板.实例?.日志列表.Count == 0) return;
                var lastItem = 日志面板.实例.日志列表.LastOrDefault();
                if (lastItem != null)
                {
                    listBox.ScrollIntoView(lastItem);
                }
                var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
                scrollViewer?.ScrollToEnd();
            }, DispatcherPriority.Background);
        }
}