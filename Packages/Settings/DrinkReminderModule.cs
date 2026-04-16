using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using 模块;

namespace ZW_Tool
{
    /// <summary>
    /// 饮水提醒模块
    /// </summary>
    public class DrinkReminderModule : I工具
    {
        public string 工具命名 => "饮水提醒";
        public string 作者 => "周维";
        public string 版本 => "1.0";

        public UserControl? 获取工具面板() => null; // 后台模块，无UI

        public void Initialize()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null) return;

            SetupDrinkReminder();
            
            // 检查并重置饮水数据（如果日期不同）
            CheckAndResetDrinkData();
            
            // 更新饮水进度UI，确保根据存储文件加载
            UpdateDrinkProgressUI();

            // 确保定时器状态符合当前设置
            RestartTimer();
        }

        public void Shutdown()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow?.饮水提醒定时器 != null)
            {
                mainWindow.饮水提醒定时器.Stop();
            }
        }

        private void SetupDrinkReminder()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null) return;

            mainWindow.饮水提醒定时器 = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(mainWindow._窗口数据.饮水提醒间隔)
            };
            mainWindow.饮水提醒定时器.Tick += async (s, e) => await CheckDrinkReminder();
            
            // 只有在开启饮水提醒时才启动定时器
            if (mainWindow._窗口数据.开启饮水提醒)
            {
                mainWindow.饮水提醒定时器.Start();
            }
        }
        
        // 重启定时器，应用新的设置
        public void RestartTimer()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null || mainWindow.饮水提醒定时器 == null) return;
            
            // 先停止定时器
            mainWindow.饮水提醒定时器.Stop();
            
            // 更新定时器间隔
            mainWindow.饮水提醒定时器.Interval = TimeSpan.FromMinutes(mainWindow._窗口数据.饮水提醒间隔);
            
            // 只有在开启饮水提醒且未达标时才启动定时器
            if (mainWindow._窗口数据.开启饮水提醒 && mainWindow._窗口数据.累计饮水量 < mainWindow._窗口数据.每日饮水量)
            {
                mainWindow.饮水提醒定时器.Start();
            }
        }

        private async Task CheckDrinkReminder()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null) return;

            // 检查是否需要提醒
            var now = DateTime.Now;
            var lastDrinkTime = mainWindow._窗口数据.最后饮水时间;

            if ((now - lastDrinkTime).TotalMinutes >= mainWindow._窗口数据.饮水提醒间隔)
            {
                // 显示提醒
                if (mainWindow.饮水提醒弹窗 == null)
                {
                    mainWindow.饮水提醒弹窗 = new 饮水提醒
                    {
                        TotalDrunk = mainWindow._窗口数据.累计饮水量,
                        DailyTarget = mainWindow._窗口数据.每日饮水量
                    };
                    mainWindow.饮水提醒弹窗.Closed += (s, args) => mainWindow.饮水提醒弹窗 = null;
                    mainWindow.饮水提醒弹窗.DrinkConfirmed += (s, ml) =>
                    {
                        mainWindow._窗口数据.累计饮水量 += ml;
                        mainWindow._窗口数据.最后饮水时间 = DateTime.Now;
                        mainWindow._窗口数据.上次饮水日期 = DateTime.Today;
                        
                        // 保存数据，确保更改持久化
                        mainWindow.保存数据();
                        
                        // 立即更新主窗口的饮水进度UI
                        UpdateDrinkProgressUI();
                        
                        // 通知设置模块更新（如果设置面板正在显示）
                        var settingsModule = ModulePackageManager.Instance.GetModules().FirstOrDefault(m => m.工具命名 == "设置");
                        if (settingsModule != null && settingsModule.获取工具面板() != null)
                        {
                            var settingsPanel = settingsModule.获取工具面板() as 设置面板;
                            if (settingsPanel != null && settingsPanel.IsVisible)
                            {
                                settingsPanel.加载(mainWindow._窗口数据);
                            }
                        }
                        
                        if (mainWindow._窗口数据.累计饮水量 >= mainWindow._窗口数据.每日饮水量)
                        {
                            mainWindow.饮水提醒定时器?.Stop();
                            new 日志("今日饮水目标达成！定时提醒已停止");
                        }
                        else
                        {
                            // 未达标，确保定时器继续运行
                            RestartTimer();
                        }
                    };
                    mainWindow.饮水提醒弹窗.Show(mainWindow);
                }
                else
                {
                    // 更新弹窗的数据
                    var mw = 主窗口.Instance;
                    if (mw?.饮水提醒弹窗 != null)
                    {
                        // 直接更新弹窗控件的属性而不是主窗口的属性
                        mw.饮水提醒弹窗.TotalDrunk = mw._窗口数据.累计饮水量;
                        mw.饮水提醒弹窗.DailyTarget = mw._窗口数据.每日饮水量;
                        mw.饮水提醒弹窗.Activate();
                    }
                }
            }
        }

        // 其他饮水相关方法可以移到这里
        public void CheckAndResetDrinkData()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null) return;

            var today = DateTime.Today;
            if (mainWindow._窗口数据.上次饮水日期.HasValue && mainWindow._窗口数据.上次饮水日期.Value.Date != today)
            {
                mainWindow._窗口数据.累计饮水量 = 0;
                mainWindow._窗口数据.上次饮水日期 = today;
                mainWindow._窗口数据.最后饮水时间 = DateTime.Now;
            }
            else if (!mainWindow._窗口数据.上次饮水日期.HasValue)
            {
                mainWindow._窗口数据.上次饮水日期 = today;
                mainWindow._窗口数据.最后饮水时间 = DateTime.Now;
            }
        }

        public void UpdateDrinkProgressUI()
        {
            var mainWindow = 主窗口.Instance;
            if (mainWindow == null) return;

            // 在UI线程上执行UI操作
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var progressBar = mainWindow.FindControl<ProgressBar>("DrinkProgressBar");
                var textBlock = mainWindow.FindControl<TextBlock>("DrinkProgressText");

                if (progressBar != null)
                {
                    progressBar.Maximum = mainWindow._窗口数据.每日饮水量;
                    progressBar.Value = mainWindow._窗口数据.累计饮水量;
                }

                if (textBlock != null)
                {
                    double percent = mainWindow._窗口数据.每日饮水量 > 0 ? (mainWindow._窗口数据.累计饮水量 / mainWindow._窗口数据.每日饮水量) * 100 : 0;
                    textBlock.Text = $"{mainWindow._窗口数据.累计饮水量:0} / {mainWindow._窗口数据.每日饮水量:0} ml ({percent:0}%)";
                }
            });
        }

        public async Task PerformInitialDrinkCheck()
        {
            await CheckDrinkReminder();
        }
    }
}