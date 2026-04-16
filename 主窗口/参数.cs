using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace ZW_Tool.核心
{
    public partial class 主窗口
    {
        //-------------------------数据储存-------------------------
        public const string 储存数据文件名 = "主窗口数据.json";
        public static readonly string 存储路径;
        public 窗口数据 _窗口数据 = new 窗口数据();
        
        // -------------------------路径管理-------------------------
        /// <summary>
        /// 获取Assets目录下的脚本路径
        /// </summary>
        /// <param name="tool">工具名称（如：Blender, Max, Unity, PS等）</param>
        /// <param name="module">模块名称（如：QF, 小岛植物, 通用功能等）</param>
        /// <param name="scriptName">脚本名称（不带扩展名）</param>
        /// <param name="extension">脚本扩展名（如：.py, .ms等）</param>
        /// <returns>完整的脚本路径</returns>
        public string GetScriptPath(string tool, string module, string scriptName, string extension)
        {
            if (string.IsNullOrEmpty(module))
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    tool,
                    scriptName + extension);
            }
            else
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    tool,
                    module,
                    scriptName + extension);
            }
        }
        
        /// <summary>
        /// 获取Assets目录下的配置文件路径
        /// </summary>
        /// <param name="tool">工具名称</param>
        /// <param name="configName">配置文件名</param>
        /// <returns>完整的配置文件路径</returns>
        public string GetConfigPath(string tool, string configName)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                tool,
                configName);
        }
        
        /// <summary>
        /// 确保Assets目录结构存在
        /// </summary>
        public void EnsureAssetsDirectories()
        {
            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            Directory.CreateDirectory(assetsDir);
            
            // 创建工具子目录
            string[] tools = { "Blender", "Max", "Photoshop", "SubstancePainter", "Unity" };
            foreach (string tool in tools)
            {
                Directory.CreateDirectory(Path.Combine(assetsDir, tool));
            }
            
            // 创建通用功能子目录
            string[] modules = { "QF", "小岛植物", "通用功能" };
            foreach (string module in modules)
            {
                foreach (string tool in tools)
                {
                    Directory.CreateDirectory(Path.Combine(assetsDir, tool, module));
                }
            }
        }
        
        // 在事件定义上方添加
        #pragma warning disable CS0067
        public new event PropertyChangedEventHandler? PropertyChanged;
        #pragma warning restore CS0067

        // -------------------------全局热键-------------------------
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
    
    #region P/Invoke 定义 - 与 Windows 窗口和拖放消息交互
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    /// <summary>
    /// 用于模拟文件拖放的结构（WM_DROPFILES 消息需要）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DROPFILES
    {
        public uint 文件列表偏移; // 文件列表相对于结构开头的偏移
        public POINT 拖放点; // 拖放点（屏幕坐标）
        public int 是否非客户区; // 是否非客户区（通常为0）
        public int 是否宽字符; // 是否使用宽字符路径（我们用 Unicode）
    }
    public const uint WM_DROPFILES = 0x0233;

    #endregion
    }
}