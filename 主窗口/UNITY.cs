using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_Tool.核心
{
    public partial class 主窗口//Unity区块
    {       
        // 在主窗口类中新增（推荐放在 UNITY.cs partial 中）
        // 在主窗口类中修改为支持端口参数
        public async Task CallUnityViaTcpAsync(string command, string? data = null, int port = 12345)
        {
            const string host = "127.0.0.1";

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);

                var stream = client.GetStream();

                var cmd = new Pipe.PipeCommand 
                { 
                    Command = command, 
                    Data = data ?? string.Empty 
                };

                string json = System.Text.Json.JsonSerializer.Serialize(cmd, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] length = BitConverter.GetBytes(payload.Length);

                await stream.WriteAsync(length);
                await stream.WriteAsync(payload);
                await stream.FlushAsync();

                new 日志($"[TCP] 已发送命令: {command} (端口: {port})");

                // 读取响应（长度前缀）
                byte[] lenBuf = new byte[4];
                await stream.ReadExactlyAsync(lenBuf);
                int respLen = BitConverter.ToInt32(lenBuf, 0);

                if (respLen > 0)
                {
                    byte[] respData = new byte[respLen];
                    await stream.ReadExactlyAsync(respData);
                    string respJson = Encoding.UTF8.GetString(respData);
                    var response = System.Text.Json.JsonSerializer.Deserialize<ZW_Tool.Pipe.PipeResponse>(respJson);

                    if (response?.Success == true)
                        new 日志($"✅ Unity TCP 返回成功: {response.Message}");
                    else
                        new 报错($"Unity 执行失败: {response?.Message}");
                }
            }
            catch (Exception ex)
            {
                new 报错($"TCP 通信失败: {ex.Message}");
            }
        }

                private async void TestCustomCall_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            string tag = btn.Tag?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(tag))
            {
                new 报错("按钮 Tag 为空，无法解析调用信息");
                return;
            }

            // 匹配 TCP 格式：tcp{端口}:调用字符串
            var tcpMatch = System.Text.RegularExpressions.Regex.Match(tag, @"^tcp(\d+):(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (tcpMatch.Success)
            {
                // TCP 调用
                int port = int.Parse(tcpMatch.Groups[1].Value);
                string callString = tcpMatch.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(callString))
                {
                    new 报错("调用字符串为空");
                    return;
                }

                await CallUnityViaTcpAsync("InvokeMethod", callString, port);
                new 日志($"[TCP调用] {callString} (端口: {port})");
                return;
            }

            // 否则按管道格式解析：管道名:调用字符串
            int colonIndex = tag.IndexOf(':');
            if (colonIndex == -1)
            {
                new 报错("按钮 Tag 格式错误，应为 \"管道名:调用字符串\" 或 \"tcp端口:调用字符串\"");
                return;
            }

            string pipeName = tag.Substring(0, colonIndex).Trim();
            string callString2 = tag.Substring(colonIndex + 1).Trim();

            if (string.IsNullOrEmpty(callString2))
            {
                new 报错("调用字符串为空");
                return;
            }

            // 使用原来的命名管道调用
            await CallUnityAsync("InvokeMethod", callString2, pipeName);
        }

                public async Task CallUnityAsync(string command, string? data = null, string pipeName = "ZW_Tool_HighPerf")
        {
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                new 日志($"正在连接 Unity Pipe [{pipeName}]...");

                var connectTask = client.ConnectAsync(5000);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    new 报错($"连接超时（5秒）。请确认 Unity 编辑器已运行，且管道名为 {pipeName}。");
                    return;
                }

                await connectTask;
                new 日志($"已成功连接到 Unity Pipe [{pipeName}]");

                // 序列化选项：必须包含字段
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    IncludeFields = true
                };

                var cmd = new ZW_Tool.Pipe.PipeCommand
                {
                    Command = command,
                    Data = data ?? string.Empty
                };

                string json = System.Text.Json.JsonSerializer.Serialize(cmd, options);
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] length = BitConverter.GetBytes(payload.Length);

                await client.WriteAsync(length);
                await client.WriteAsync(payload);
                await client.FlushAsync();

                new 日志($"已发送命令: {command}");

                // 读取响应（必须等待完整读取）
                byte[] lenBuf = new byte[4];
                await client.ReadExactlyAsync(lenBuf);

                int respLen = BitConverter.ToInt32(lenBuf, 0);
                if (respLen <= 0) return;

                byte[] respBuf = new byte[respLen];
                await client.ReadExactlyAsync(respBuf);

                string respJson = Encoding.UTF8.GetString(respBuf);
                var response = System.Text.Json.JsonSerializer.Deserialize<ZW_Tool.Pipe.PipeResponse>(
                    respJson,
                    new System.Text.Json.JsonSerializerOptions { IncludeFields = true });

                if (response?.Success == true)
                    new 日志($"✅ Unity 返回成功: {response.Message}");
                else
                    new 报错($"Unity 执行失败: {(response?.Message ?? "无响应")}");
            }
            catch (Exception ex)
            {
                new 报错($"通信失败: {ex.Message}");
            }
        }

          
    }

}

namespace ZW_Tool.Pipe
{
    [Serializable]
    public class PipeCommand
    {
        public string Command = "";
        public string Data;

        public PipeCommand() 
        {
            Data = string.Empty;
        }
        public PipeCommand(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }

    [Serializable]
    public class PipeResponse
    {
        public bool Success;
        public string Message = "";

        public PipeResponse() { }
        public PipeResponse(bool success, string message = "")
        {
            Success = success;
            Message = message;
        }
    }
}