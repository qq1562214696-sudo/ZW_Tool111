using System;
using Avalonia.Controls;

namespace ZW_Tool
{
    /// <summary>
    /// 窗口数据类，用于存储窗口设置和用户数据
    /// </summary>
    public class 窗口数据
    {
        // 窗口设置
        public double 宽度 { get; set; } = 800;
        public double 高度 { get; set; } = 600;
        public double X坐标 { get; set; } = 100;
        public double Y坐标 { get; set; } = 100;
        public WindowState 窗口状态 { get; set; } = WindowState.Normal;
        public bool 置顶 { get; set; } = false;
        public bool 开机自启 { get; set; } = false;

        // 饮水提醒设置
        public bool 开启饮水提醒 { get; set; } = true;
        public bool 饮水提醒展开 { get; set; } = false;
        public double 每日饮水量 { get; set; } = 2000; // ml
        public double 饮水提醒间隔 { get; set; } = 60; // 分钟

        // 饮水数据
        public DateTime 饮水日期 { get; set; } = DateTime.Today;
        public int 今日饮水量 { get; set; } = 0;
        public DateTime 最后饮水时间 { get; set; } = DateTime.MinValue;
        public double 累计饮水量 { get; set; } = 0;
        public DateTime? 上次饮水日期 { get; set; } = null;
        
        // 模块面板数据
        public string 当前面板 { get; set; } = "设置面板"; // 默认显示设置面板
    }
}