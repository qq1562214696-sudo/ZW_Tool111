using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace ZW_Tool.核心
{
    public partial class 主窗口//Blender区块
    {
        // ====================== Blender 核心调用方法（统一入口） ======================
        /// <summary>
        /// 核心方法：发送脚本到已运行的 Blender（TCP方式）
        /// 和 Max 的 发送脚本到3dsMax 风格完全一致
        /// </summary>
        public async Task 发送脚本到Blender(string py路径)
        {
            if (!File.Exists(py路径))
            {
                new 报错($"找不到 Blender 脚本：{py路径}");
                return;
            }

            // 检查 Blender 是否正在运行
            var existingBlender = System.Diagnostics.Process.GetProcessesByName("blender");
            if (existingBlender.Length == 0)
            {
                new 报错("Blender 当前未运行！\n请先打开 Blender 并确保 ZW_Tool Remote Control 已启动。");
                return;
            }

            try
            {
                string code = await File.ReadAllTextAsync(py路径);

                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 12346);

                var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(code);

                await stream.WriteAsync(data);
                await stream.FlushAsync();

                new 日志($"[Blender] 已发送到运行中的实例：{Path.GetFileName(py路径)} ({data.Length} 字节)");
            }
            catch (Exception ex)
            {
                new 报错($"Blender TCP 连接失败: {ex.Message}\n请确认 Blender 中 ZW_Tool Remote Control 已在运行");
            }
        }

        // ====================== 公共可调用的Blender脚本调用方法 ======================
        /// <summary>
        /// Blender通用按钮调用（无模块）
        /// 按钮 Content 即为脚本名（不带 .py）
        /// </summary>
        public void Blender脚本(object? sender, RoutedEventArgs e)
        {
            string 按钮文本 = UI.UI方法.获取内容文本(sender);
            if (string.IsNullOrWhiteSpace(按钮文本))
            {
                new 报错("按钮文本为空，无法确定脚本名称");
                return;
            }

            string 脚本路径 = GetScriptPath("Blender", "", 按钮文本, ".py");
            _ = 发送脚本到Blender(脚本路径);   // 异步调用，不阻塞UI
        }

        /// <summary>
        /// 带模块的Blender脚本调用
        /// </summary>
        public void Blender脚本(string module, string scriptName)
        {
            string 脚本路径 = GetScriptPath("Blender", module, scriptName, ".py");
            _ = 发送脚本到Blender(脚本路径);
        }
        
        /// <summary>
        /// 通用Blender脚本调用方法（供其他模块使用）
        /// </summary>
        public void 通用Blender脚本调用(object? sender, string moduleName)
        {
            string 按钮文本 = UI.UI方法.获取内容文本(sender);
            if (string.IsNullOrWhiteSpace(按钮文本))
            {
                new 报错("按钮文本为空，无法确定脚本名称");
                return;
            }
            
            string 脚本路径 = GetScriptPath("Blender", moduleName, 按钮文本, ".py");
            _ = 发送脚本到Blender(脚本路径);
        }

        // ====================== 测试按钮（可选） ======================
        public void Blender测试按钮_Click(object? sender, RoutedEventArgs e)
        {
            new 日志("=== 启动 Blender 测试 ===");
            string 脚本路径 = GetScriptPath("Blender", "", "测试", ".py");

            _ = 发送脚本到Blender(脚本路径);
        }
    }
}