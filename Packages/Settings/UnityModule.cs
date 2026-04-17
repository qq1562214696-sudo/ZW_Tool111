using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using ZW_Tool.核心;

namespace ZW_Tool
{
    /// <summary>
    /// Unity 相关模块
    /// </summary>
    public class UnityModule
    {
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