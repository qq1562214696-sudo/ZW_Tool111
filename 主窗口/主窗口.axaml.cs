using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;



namespace ZW_Tool.核心
{
    public partial class 主窗口 : Window, INotifyPropertyChanged
    {
        public static 主窗口? Instance { get; private set; }
        static 主窗口()
        {
            string appData目录 = Path.Combine(AppContext.BaseDirectory, "AppData");
            Directory.CreateDirectory(appData目录);
            存储路径 = Path.Combine(appData目录, 储存数据文件名);
        }
            
        public 主窗口()
        {
            Instance = this;
            InitializeComponent();
            DataContext = this;

            // 先加载和应用窗口设置，确保窗口大小和位置在模块加载之前就正确设置
            LoadAndApplyWindowSettings();

            // 手动查找 ModuleContent（以防编译器未生成）
            ModuleContent = this.FindControl<ContentControl>("ModuleContent");
            if (ModuleContent == null)
            new 日志("[警告] ModuleContent 控件未找到！");

            // 确保Assets目录结构存在
            EnsureAssetsDirectories();

            EnableDragAndDrop();

            Opened += OnWindowOpened;
            Closing += 关闭主窗口;

            // 初始化喝水提醒
            InitializeDrinkReminder();
        }

        public void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // 初始化喝水提醒功能
        private void InitializeDrinkReminder()
        {
            // 喝水提醒功能已移至DrinkReminder模块
            // 模块会在加载时自动初始化
        }

        public async void OnWindowOpened(object? sender, EventArgs e)
        {
            // 使用PluginLoader加载Assets目录下的所有插件
            ModulePackageManager.Instance.LoadModules();
            ModulePackageManager.Instance.InitializeAllModules();

            // 生成模块按钮
            生成模块按钮();

            // 加载Unity路径
            _ = TryLoadLastUnityPathAsync();

            // 新增：强制触发一次喝水提醒判断（确保程序启动至少有一次判断）
            // 从饮水提醒模块获取实例并通过反射执行初始检查（避免编译期类型依赖）
            var drinkReminderModule = ModulePackageManager.Instance.GetModules().FirstOrDefault(m => m.工具命名 == "饮水提醒");
            if (drinkReminderModule != null)
            {
                try
                {
                    var mi = drinkReminderModule.GetType().GetMethod("PerformInitialDrinkCheck", BindingFlags.Public | BindingFlags.Instance);
                    var result = mi?.Invoke(drinkReminderModule, null);
                    if (result is Task t) _ = t;
                }
                catch (Exception ex)
                {
                    new 日志($"[警告] 调用饮水提醒初始检查失败: {ex.Message}");
                }
            }

            // 根据保存的当前面板设置显示相应的面板
            var currentModule = ModulePackageManager.Instance.GetModules().FirstOrDefault(m => m.工具命名 == _窗口数据.当前面板);
            if (currentModule != null)
            {
                ShowModulePanel(currentModule);
            }
            else
            {
                // 默认显示设置面板
                var settingsModule = ModulePackageManager.Instance.GetModules().FirstOrDefault(m => m.工具命名 == "设置");
                if (settingsModule != null)
                {
                    ShowModulePanel(settingsModule);
                }
            }
        }

        public void 关闭主窗口(object? sender, WindowClosingEventArgs e)
        {
            保存数据();
            // StopMaxLogWatcher();

            // 关闭所有模块
            ModulePackageManager.Instance.ShutdownAllModules();
        }


        // ────────────────────────────────────────────────
        // 以下是原有方法（实现体只在这里，其他文件删除重复）
        // ────────────────────────────────────────────────

        public async Task TryLoadLastUnityPathAsync()
        {
            try
            {
                string configPath = GetConfigPath("Max", "QF_Config.txt");
                if (!System.IO.File.Exists(configPath)) return;

                var lines = await System.IO.File.ReadAllLinesAsync(configPath);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("AssetsPath=", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string savedPath = line.Substring("AssetsPath=".Length).Trim();
                    if (string.IsNullOrEmpty(savedPath) || !System.IO.Directory.Exists(savedPath))
                        continue;

                    var unityBox = this.FindControl<TextBox>("QF_UnityPathInput");
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

        public void LoadAndApplyWindowSettings()
        {
            加载数据();
            应用窗口设置();
        }
    }
}