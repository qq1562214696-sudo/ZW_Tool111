using Avalonia.Controls;

namespace ZW_Tool.核心
{
    /// <summary>
    /// 窗口数据类，用于存储窗口设置和用户数据
    /// </summary>
    public class 窗口数据
    {
        // 窗口设置
        public double 宽度 { get; set; } = 300;
        public double 高度 { get; set; } = 600;
        public double X坐标 { get; set; } = 100;
        public double Y坐标 { get; set; } = 100;
        public WindowState 窗口状态 { get; set; } = WindowState.Normal;
        public string 当前面板 { get; set; } = "设置面板"; // 默认显示设置面板
    }
}