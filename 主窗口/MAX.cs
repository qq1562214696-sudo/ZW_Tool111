using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ZW_Tool.核心;
public partial class 主窗口//Max区块
{
    /// <summary>
    /// 核心方法：通过模拟“拖放 .ms 文件到 3ds Max 窗口”来执行 MaxScript
    /// 这是目前最可靠的外部调用 MaxScript 的方式之一（无需开启 MaxScript Listener）
    /// </summary>
    public void 发送脚本到3dsMax(string 脚本路径)
    {
        if (!File.Exists(脚本路径))
        {
          EventAggregator.PublishLog($"找不到脚本：{脚本路径}");
            return;
        }
        // 尝试多种可能的窗口类名/标题来定位 3ds Max 主窗口
        IntPtr 窗口句柄 = FindWindow("3dsmax", null);//2014，2017前面
        // if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow("QT5QWindowIcon", null);//2018后
        // if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow("3DSMAX", null);
        // if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "3ds Max");
        // if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "Autodesk 3ds Max");
        // if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "Autodesk 3ds Max 2014"); // 可根据版本增加更多
        if (窗口句柄 == IntPtr.Zero)
        {
          EventAggregator.PublishLog("Max2014 未正确运行");
            return;
        }
        try
        {
            // 把 3ds Max 窗口置前（非常重要，否则拖放消息可能失效）
            SetForegroundWindow(窗口句柄);
            // 获取窗口矩形区域，用于计算中心点坐标
            if (!GetWindowRect(窗口句柄, out RECT 矩形区域))
            {
              EventAggregator.PublishLog("获取 3ds Max 窗口失败");
                return;
            }
            int 中心X = 矩形区域.Left + (矩形区域.Right - 矩形区域.Left) / 2;
            int 中心Y = 矩形区域.Top + (矩形区域.Bottom - 矩形区域.Top) / 2;
            // 构造拖放路径（必须以双空字符结尾，Unicode 编码）
            string 路径带双空 = 脚本路径 + "\0\0";
            byte[] 路径字节 = System.Text.Encoding.Unicode.GetBytes(路径带双空);
            // 计算全局内存块大小
            int 结构大小 = Marshal.SizeOf<DROPFILES>();
            int 总内存大小 = 结构大小 + 路径字节.Length;
            // 分配全局内存（PostMessage 需要 HGLOBAL）
            IntPtr 全局内存句柄 = Marshal.AllocHGlobal(总内存大小);
            if (全局内存句柄 == IntPtr.Zero)
            {
              EventAggregator.PublishLog("全局内存分配失败");
                return;
            }
            // 注意：这里移除了 finally 中的 FreeHGlobal，让 3ds Max 负责释放内存
            try
            {
                var 拖放结构 = new DROPFILES
                {
                    文件列表偏移 = (uint)结构大小, // 文件列表从结构后开始
                    拖放点 = new POINT { X = 中心X, Y = 中心Y },
                    是否非客户区 = 0, // 客户区拖放
                    是否宽字符 = 1 // 使用宽字符（Unicode）
                };
                // 把 DROPFILES 结构写入内存
                Marshal.StructureToPtr(拖放结构, 全局内存句柄, false);
                // 把路径字符串（含双\0）紧跟在结构后面
                Marshal.Copy(路径字节, 0, 全局内存句柄 + 结构大小, 路径字节.Length);
                // 发送 WM_DROPFILES 消息给 3ds Max
                PostMessage(窗口句柄, WM_DROPFILES, 全局内存句柄, IntPtr.Zero);
            }
            catch (Exception ex)
            {
              EventAggregator.PublishLog($"发送Max脚本发生异常：{ex.Message}");
                Marshal.FreeHGlobal(全局内存句柄); // 异常情况下，手动释放以防泄漏
            }
        }
        catch (Exception ex)
        {
          EventAggregator.PublishLog($"与 3ds Max 交互时发生异常：{ex.Message}");
        }
    }
    
    // ====================== 公共可调用的Max脚本调用方法 ======================
    /// <summary>
    /// 通用Max脚本调用方法（供其他模块使用）
    /// </summary>
    public void 通用Max脚本调用(object? sender, string moduleName)
    {
        string 按钮文本 = UI.UI方法.获取内容文本(sender);
        if (string.IsNullOrWhiteSpace(按钮文本))
        {
            EventAggregator.PublishLog("按钮文本为空，无法确定脚本名称");
            return;
        }
        
        string 脚本路径 = GetScriptPath("Max", moduleName, 按钮文本, ".ms");
        发送脚本到3dsMax(脚本路径);
    }
}