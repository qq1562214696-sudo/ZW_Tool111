using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ZW_Tool.核心;


namespace ZW_Tool
{
    public class DrinkReminderModule
    {
        public UserControl? 获取工具面板() => null;

        public void Initialize()
        {
            // 通过设置面板静态方式调用（避免直接依赖主窗口）
            var settingsPanel = ModulePackageManager.Instance.GetModules()
                .FirstOrDefault(m => m.工具命名 == "设置")?.获取工具面板() as 设置面板;

            settingsPanel?.更新饮水进度();
        }

        public void Shutdown()
        {
            var settingsPanel = ModulePackageManager.Instance.GetModules()
                .FirstOrDefault(m => m.工具命名 == "设置")?.获取工具面板() as 设置面板;

            settingsPanel?.Shutdown();   // 调用设置面板的 Shutdown
        }

        // 保留原有公开方法，但内部使用设置面板逻辑
        public void UpdateDrinkProgressUI()
        {
            var settingsPanel = ModulePackageManager.Instance.GetModules()
                .FirstOrDefault(m => m.工具命名 == "设置")?.获取工具面板() as 设置面板;
            settingsPanel?.更新饮水进度();
        }

        public void RestartTimer()
        {
            // 目前由设置面板控制定时器
        }

        public void CheckAndResetDrinkData() { }

        public async Task PerformInitialDrinkCheck()
        {
            await Task.CompletedTask;
        }
    }
}