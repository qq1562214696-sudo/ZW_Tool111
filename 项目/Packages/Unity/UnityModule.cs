using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ZW_Tool
{
    /// <summary>
    /// Unity 相关模块
    /// </summary>
    public class UnityModule : I工具
    {
        public string 工具命名 => "Unity";
        public string 作者 => "周维";
        public string 版本 => "1.0";

        public void Initialize()
        {
            // 异步加载Unity路径
            Task.Run(async () => await TryLoadLastUnityPathAsync());
        }

        public UserControl? 获取工具面板()
        {
            // Unity模块没有UI面板，返回null表示这是一个后台模块
            return null;
        }

        public void Shutdown()
        {
            // 无需特殊关闭
        }

        private async Task TryLoadLastUnityPathAsync()
        {
            try
            {
                var mainWindow = 主窗口.Instance;
                if (mainWindow == null) return;

                string configPath = mainWindow.GetConfigPath("Max", "QF_Config.txt");
                if (!File.Exists(configPath)) return;

                var lines = await File.ReadAllLinesAsync(configPath);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("AssetsPath=", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string savedPath = line.Substring("AssetsPath=".Length).Trim();
                    if (string.IsNullOrEmpty(savedPath) || !Directory.Exists(savedPath))
                        continue;

                    var unityBox = mainWindow.FindControl<TextBox>("QF_UnityPathInput");
                    if (unityBox == null) continue;

                    unityBox.Text = savedPath;
                    return;
                }
            }
            catch (Exception ex)
            {
                new 日志($"自动加载 Unity 路径失败：{ex.Message}");
            }
        }
    }
}