using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using ZW_Tool.核心;

namespace ZW_Tool
{
    /// <summary>
    /// 日志数据模型
    /// </summary>
    public class 日志数据
    {
        public required string 内容 { get; set; }
        public string 颜色 { get; set; } = "Black";
    }

    /// <summary>
    /// 静态日志服务，管理日志集合并订阅全局日志事件
    /// </summary>
    public static class 日志服务
    {
        public static ObservableCollection<日志数据> 日志列表 { get; } = new();

        // 静态构造函数，确保在程序启动时立即订阅事件
        static 日志服务()
        {
            // 订阅全局日志事件（使用弱引用或直接订阅，注意避免重复订阅）
            EventAggregator.LogPublished += OnLogPublished;
        }

        private static void OnLogPublished(object? sender, LogEventArgs e)
        {
            string color = e.Level switch
            {
                LogLevel.Warning => "Orange",
                LogLevel.Error => "Red",
                _ => "Black"
            };
            Add(e.Message, color);
        }

        public static void Add(string 消息, string 颜色 = "Black")
        {
            Dispatcher.UIThread.Post(() =>
            {
                日志列表.Add(new 日志数据
                {
                    内容 = $"[{DateTime.Now:HH:mm:ss}] {消息}",
                    颜色 = 颜色
                });

                // 自动清理，保持数量在合理范围
                while (日志列表.Count > 2000)
                {
                    日志列表.RemoveAt(0);
                }
            });
        }

        public static void Clear()
        {
            Dispatcher.UIThread.Post(() => 日志列表.Clear());
        }
    }
}