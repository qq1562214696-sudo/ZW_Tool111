using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZW_Tool.Tcp
{
    [InitializeOnLoad]
    public static class ZW_UnityTcpServer
    {
        private const string Host = "127.0.0.1";
        private const int Port = 16837;

        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private static readonly SemaphoreSlim _connectionLimit = new SemaphoreSlim(8);

        private static readonly Dictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();
        private static readonly object _cacheLock = new object();

        private static Thread _serverThread;

        static ZW_UnityTcpServer()
        {
            _serverThread = new Thread(ServerThreadProc) { IsBackground = true, Name = "TcpServerThread" };
            _serverThread.Start();

            EditorApplication.quitting += OnEditorQuit;
            EditorApplication.update += Update;

            Debug.Log($"[ZW_TcpServer] TCP 服务已启动 → {Host}:{Port}");
        }

        private static void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[ZW_Tcp] 主线程执行异常: {ex}"); }
            }
        }

        private static void ServerThreadProc()
        {
            TcpListener listener = null;
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (listener == null)
                    {
                        listener = new TcpListener(IPAddress.Parse(Host), Port);
                        listener.Start();
                    }

                    // 使用 AcceptTcpClientAsync 的非阻塞版本，但要兼容 .NET 3.5
                    var asyncResult = listener.BeginAcceptTcpClient(null, null);
                    while (!asyncResult.IsCompleted && !_cts.IsCancellationRequested)
                    {
                        Thread.Sleep(10);
                    }

                    if (_cts.IsCancellationRequested) break;

                    var client = listener.EndAcceptTcpClient(asyncResult);
                    _connectionLimit.Wait();
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                        Debug.LogWarning($"[ZW_TcpServer] 监听异常: {ex.Message}");
                    // 发生异常时，稍等后重建 listener
                    Thread.Sleep(100);
                    listener = null;
                }
            }

            listener?.Stop();
        }

        private static void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var buffer = new byte[32768];

                    while (client.Connected && !_cts.IsCancellationRequested)
                    {
                        // 读取长度前缀（4字节）
                        if (!ReadExactly(stream, buffer, 0, 4)) break;
                        int dataLen = BitConverter.ToInt32(buffer, 0);
                        if (dataLen <= 0 || dataLen > buffer.Length) break;

                        // 读取实际数据
                        if (!ReadExactly(stream, buffer, 0, dataLen)) break;

                        string json = Encoding.UTF8.GetString(buffer, 0, dataLen);
                        Debug.Log($"[ZW_Tcp] 收到命令: {json}");

                        var cmd = JsonUtility.FromJson<PipeCommand>(json);
                        PipeResponse response;

                        if (cmd?.Command == "InvokeMethod")
                        {
                            object result = null;
                            string error = null;

                            var are = new AutoResetEvent(false);
                            _mainThreadActions.Enqueue(() =>
                            {
                                try
                                {
                                    result = InvokeMethod(cmd.Data ?? "");
                                }
                                catch (Exception ex)
                                {
                                    error = ex.Message;
                                }
                                finally
                                {
                                    are.Set();
                                }
                            });

                            are.WaitOne(5000);

                            response = string.IsNullOrEmpty(error)
                                ? new PipeResponse(true, JsonUtility.ToJson(new MethodResult { result = result?.ToString() ?? "" }))
                                : new PipeResponse(false, $"调用失败: {error}");
                        }
                        else
                        {
                            response = new PipeResponse(false, $"未知命令: {cmd?.Command}");
                        }

                        WriteResponse(stream, response);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ZW_Tcp] 客户端异常: {ex.Message}");
                }
            }
        }

        private static bool ReadExactly(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read == 0) return false;
                total += read;
            }
            return true;
        }

        private static void WriteResponse(NetworkStream stream, PipeResponse resp)
        {
            try
            {
                string json = JsonUtility.ToJson(resp);
                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] len = BitConverter.GetBytes(data.Length);

                stream.Write(len, 0, 4);
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch { }
        }

        // ==================== 反射调用（与 Pipe 版本一致） ====================
        private static object InvokeMethod(string callString)
        {
            if (string.IsNullOrWhiteSpace(callString))
                throw new ArgumentException("调用字符串为空");

            var match = System.Text.RegularExpressions.Regex.Match(callString, @"^([^(]+)\((.*)\)$");
            if (!match.Success)
                throw new ArgumentException("调用格式错误");

            string methodFull = match.Groups[1].Value.Trim();
            string paramString = match.Groups[2].Value.Trim();

            int lastDot = methodFull.LastIndexOf('.');
            if (lastDot == -1)
                throw new ArgumentException("必须包含类名.方法名");

            string typeName = methodFull.Substring(0, lastDot).Trim();
            string methodName = methodFull.Substring(lastDot + 1).Trim();

            MethodInfo method = GetCachedMethod(typeName, methodName);
            if (method == null)
                throw new MissingMethodException($"找不到静态方法: {typeName}.{methodName}");

            object[] parameters = ParseParameters(paramString);
            return method.Invoke(null, parameters);
        }

        private static MethodInfo GetCachedMethod(string typeName, string methodName)
        {
            string key = $"{typeName}.{methodName}";
            lock (_cacheLock)
            {
                if (_methodCache.TryGetValue(key, out var method)) return method;

                Type targetType = Type.GetType(typeName);
                if (targetType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        targetType = assembly.GetType(typeName);
                        if (targetType != null) break;
                    }
                }
                if (targetType == null) return null;

                var methodInfo = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (methodInfo != null)
                    _methodCache[key] = methodInfo;
                return methodInfo;
            }
        }

        private static object[] ParseParameters(string paramString)
        {
            if (string.IsNullOrWhiteSpace(paramString))
                return Array.Empty<object>();

            var parameters = new List<object>();
            var parts = new List<string>();
            bool inQuote = false;
            int start = 0;

            for (int i = 0; i < paramString.Length; i++)
            {
                char c = paramString[i];
                if (c == '"') inQuote = !inQuote;
                else if (c == ',' && !inQuote)
                {
                    parts.Add(paramString.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            parts.Add(paramString.Substring(start).Trim());

            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (int.TryParse(p, out int intVal)) parameters.Add(intVal);
                else if (float.TryParse(p, out float floatVal)) parameters.Add(floatVal);
                else if (bool.TryParse(p, out bool boolVal)) parameters.Add(boolVal);
                else if (p.StartsWith("\"") && p.EndsWith("\"")) parameters.Add(p.Substring(1, p.Length - 2));
                else parameters.Add(p);
            }
            return parameters.ToArray();
        }

        private static void OnEditorQuit()
        {
            _cts.Cancel();
            _serverThread?.Join(1000);
            EditorApplication.update -= Update;
            Debug.Log("[ZW_TcpServer] TCP 服务已停止");
        }
    }

    // 定义与命名管道相同的协议类，以便兼容
    [Serializable]
    public class PipeCommand { public string Command = ""; public string Data; }
    [Serializable]
    public class PipeResponse
    {
        public bool Success;
        public string Message;
        public PipeResponse() { }
        public PipeResponse(bool success, string message) { Success = success; Message = message; }
    }
    [Serializable]
    public class MethodResult { public string result; }
}