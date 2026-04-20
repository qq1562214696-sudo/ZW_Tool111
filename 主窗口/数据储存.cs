using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace ZW_Tool.核心;

public partial class 主窗口
{
    public void 应用窗口设置()//现在仅仅只是设置窗口数据而没有设置其他杂项，但是未必要合并，可以考虑两者分开
    {
        try
        {
            if (_窗口数据.窗口状态 == WindowState.Normal)
            {
                if (_窗口数据.宽度 > 0) Width = _窗口数据.宽度;
                if (_窗口数据.高度 > 0) Height = _窗口数据.高度;
                if (_窗口数据.X坐标 >= 0 && _窗口数据.Y坐标 >= 0)
                {
                    Position = new PixelPoint((int)_窗口数据.X坐标, (int)_窗口数据.Y坐标);
                }
            }
            WindowState = _窗口数据.窗口状态;
        }
        catch { }
    }

    public void 保存数据()
    {
        try
        {
            窗口数据 现有数据;
            if (File.Exists(存储路径))
            {
                string existingJson = File.ReadAllText(存储路径);
                现有数据 = JsonSerializer.Deserialize<窗口数据>(existingJson) ?? new 窗口数据();
            }
            else
            {
                现有数据 = new 窗口数据();
            }

            if (WindowState == WindowState.Normal)
            {
                现有数据.宽度 = Width;
                现有数据.高度 = Height;
                现有数据.X坐标 = Position.X;
                现有数据.Y坐标 = Position.Y;
            }
            现有数据.窗口状态 = WindowState;
            现有数据.当前面板       = _窗口数据.当前面板;

            string json = JsonSerializer.Serialize(现有数据, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(存储路径, json);
        }
        catch (Exception ex)
        {
          EventAggregator.PublishLog($"窗口保存布局失败：{ex.Message}");
        }
    }

    public void 加载数据()
    {
        try
        {
            if (File.Exists(存储路径))
            {
                string json = File.ReadAllText(存储路径);
                _窗口数据 = JsonSerializer.Deserialize<窗口数据>(json) ?? new 窗口数据();
            }
        }
        catch (Exception ex)
        {
          EventAggregator.PublishLog($"加载数据失败：{ex.Message}");
            _窗口数据 = new 窗口数据();
        }
    }
}