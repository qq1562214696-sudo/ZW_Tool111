using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace ZW_Tool.核心;

public class 日志数据
{
    public required string 内容 { get; set; }
    public string 颜色 { get; set; } = "Black";
}

public static class 日志服务
{
    public static ObservableCollection<日志数据> 日志列表 { get; } = new ObservableCollection<日志数据>();

    public static void Add(string 消息, string 颜色 = "Black")
    {
        Dispatcher.UIThread.Post(() =>
        {
            日志列表.Add(new 日志数据 { 内容 = $"[{DateTime.Now:mm:ss}] {消息}", 颜色 = 颜色 });

            // 自动清理，避免无限增长
            if (日志列表.Count > 2000)
            {
                while (日志列表.Count > 1800)
                    日志列表.RemoveAt(0);
            }
        });
    }
}

public class 日志
{
    public 日志(string 消息) => 日志服务.Add(消息, "Black");
}

public class 提示
{
    public 提示(string 消息) => 日志服务.Add($"提示:{消息}", "Yellow");
}

public class 报错
{
    public 报错(string 消息) => 日志服务.Add($"错误:{消息}", "Red");
}
