using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZW_Tool.Pipe
{
    [InitializeOnLoad]
    public static class ZW_UnityPipeServer
    {
        private const string PipeName = "ZW_Tool_QF";
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private static readonly SemaphoreSlim _connectionLimit = new SemaphoreSlim(5); // 最大并发连接数

        // 反射缓存
        private static readonly Dictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();
        private static readonly object _cacheLock = new object();

        static ZW_UnityPipeServer()
        {
            Task.Run(() => StartServerAsync(_cts.Token));
            EditorApplication.quitting += OnEditorQuit;
            EditorApplication.update += Update;
            Debug.Log("[ZW_Pipe] 服务已初始化 → " + PipeName);
        }

        private static void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ZW_Pipe] 主线程执行异常: {ex}");
                }
            }
        }

        private static async Task StartServerAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    try
                    {
                        await server.WaitForConnectionAsync(token);
                        Debug.Log("[ZW_Pipe] 客户端已连接");
                        // 限制并发连接数，避免任务堆积
                        await _connectionLimit.WaitAsync(token);
                        _ = HandleClientAsync(server).ContinueWith(_ => _connectionLimit.Release());
                    }
                    catch (OperationCanceledException)
                    {
                        server.Dispose();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ZW_Pipe] 连接异常: {ex.Message}");
                        server.Dispose();
                        await Task.Delay(300, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZW_Pipe] 服务崩溃: {ex}");
            }
            finally
            {
                Debug.Log("[ZW_Pipe] 服务已停止");
            }
        }

        private static async Task HandleClientAsync(NamedPipeServerStream pipe)
        {
            try
            {
                var buffer = new byte[16384];
                while (pipe.IsConnected && pipe.CanRead && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // 读取长度头
                        int lenRead = await pipe.ReadAsync(buffer, 0, 4, _cts.Token);
                        if (lenRead < 4) break;

                        int dataLen = BitConverter.ToInt32(buffer, 0);
                        if (dataLen <= 0 || dataLen > buffer.Length) break;

                        // 读取数据
                        int dataRead = await pipe.ReadAsync(buffer, 0, dataLen, _cts.Token);
                        if (dataRead < dataLen) break;

                        string json = Encoding.UTF8.GetString(buffer, 0, dataRead);
                        Debug.Log($"[ZW_Pipe] 收到 JSON: {json}");

                        var cmd = JsonUtility.FromJson<PipeCommand>(json);
                        PipeResponse response;

                        if (cmd == null || string.IsNullOrEmpty(cmd.Command))
                        {
                            response = new PipeResponse(false, "命令解析失败");
                        }
                        else if (cmd.Command == "InvokeMethod")
                        {
                            Debug.Log($"[ZW_Pipe] 收到 InvokeMethod 调用: {cmd.Data}");
                            var tcs = new TaskCompletionSource<PipeResponse>();
                            _mainThreadActions.Enqueue(() =>
                            {
                                try
                                {
                                    object result = InvokeMethod(cmd.Data);
                                    // 将结果包装为可序列化对象
                                    var resultObj = new MethodResult { result = result?.ToString() ?? "" };
                                    string resultJson = JsonUtility.ToJson(resultObj);
                                    tcs.SetResult(new PipeResponse(true, resultJson));
                                    Debug.Log($"[ZW_Pipe] 方法调用成功，返回: {resultJson}");
                                }
                                catch (Exception ex)
                                {
                                    tcs.SetResult(new PipeResponse(false, $"调用失败: {ex.Message}"));
                                    Debug.LogError($"[ZW_Pipe] 方法调用异常: {ex}");
                                }
                            });
                            response = await tcs.Task;
                        }
                        else
                        {
                            response = cmd.Command switch
                            {
                                "SayHello" => new PipeResponse(true, "哈喽世界！"),
                                _ => new PipeResponse(false, $"未知命令: {cmd.Command}")
                            };
                        }

                        await WriteResponse(pipe, response);
                    }
                    catch (IOException ex) when (ex.Message.Contains("管道已结束"))
                    {
                        Debug.Log("[ZW_Pipe] 管道已断开");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ZW_Pipe] 处理单条消息异常: {ex.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务正常关闭
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Cannot access a closed pipe"))
                {
                    Debug.LogWarning($"[ZW_Pipe] 处理异常: {ex.Message}");
                }
            }
            finally
            {
                try
                {
                    if (pipe.IsConnected)
                        pipe.Disconnect();
                    pipe.Dispose();
                }
                catch { }
                Debug.Log("[ZW_Pipe] 客户端处理结束，管道已关闭");
            }
        }

        private static object InvokeMethod(string callString)
        {
            if (string.IsNullOrWhiteSpace(callString))
                throw new ArgumentException("调用字符串为空");

            // 使用正则提取方法全名和参数列表
            var match = System.Text.RegularExpressions.Regex.Match(callString, @"^([^(]+)\((.*)\)$");
            if (!match.Success)
                throw new ArgumentException("调用格式错误，应为 ClassName.MethodName(param1, param2)");

            string methodFull = match.Groups[1].Value.Trim();
            string paramString = match.Groups[2].Value.Trim();

            int lastDot = methodFull.LastIndexOf('.');
            if (lastDot == -1)
                throw new ArgumentException("必须包含类名和方法名，格式应为 ClassName.MethodName");

            string typeName = methodFull.Substring(0, lastDot).Trim();
            string methodName = methodFull.Substring(lastDot + 1).Trim();

            // 尝试从缓存获取方法
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
                if (_methodCache.TryGetValue(key, out var method))
                    return method;

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

            // 手动分割参数（处理引号内的逗号）
            for (int i = 0; i < paramString.Length; i++)
            {
                char c = paramString[i];
                if (c == '"')
                    inQuote = !inQuote;
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
                if (int.TryParse(p, out int intVal))
                    parameters.Add(intVal);
                else if (float.TryParse(p, out float floatVal))
                    parameters.Add(floatVal);
                else if (bool.TryParse(p, out bool boolVal))
                    parameters.Add(boolVal);
                else if (p.StartsWith("\"") && p.EndsWith("\""))
                    parameters.Add(p.Substring(1, p.Length - 2));
                else
                    parameters.Add(p);
            }
            return parameters.ToArray();
        }

        private static async Task WriteResponse(NamedPipeServerStream pipe, PipeResponse resp)
        {
            try
            {
                if (!pipe.IsConnected || !pipe.CanWrite) return;

                string json = JsonUtility.ToJson(resp);
                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] len = BitConverter.GetBytes(data.Length);

                await pipe.WriteAsync(len, 0, 4, _cts.Token);
                await pipe.WriteAsync(data, 0, data.Length, _cts.Token);
                await pipe.FlushAsync(_cts.Token);
            }
            catch { }
        }

        private static void OnEditorQuit()
        {
            _cts.Cancel();
            EditorApplication.update -= Update;
            Debug.Log("[ZW_Pipe] Unity 退出，服务停止");
        }
    }

    [Serializable]
    public class PipeCommand
    {
        public string Command = "";
        public string Data;
    }

    [Serializable]
    public class PipeResponse
    {
        public bool Success;
        public string Message;

        public PipeResponse() { }
        public PipeResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    [Serializable]
    public class MethodResult
    {
        public string result;
    }
}

namespace ZW_Tool
{
    public static class Test
    {
        public static string Hello(string name, int age)
        {
            Debug.Log($"[Test] Hello called with name={name}, age={age}");
            return $"Hello {name}, you are {age} years old.";
        }
    }
}