using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace ZW_Tool
{
    public partial class 饮水提醒 : Window
    {
        public event EventHandler<double>? DrinkConfirmed;

        public ProgressBar? _progressBar;
        public TextBlock? _progressText;

        public 饮水提醒()
        {
            InitializeComponent();
            _progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
            _progressText = this.FindControl<TextBlock>("DrinkProgressText");
            UpdateUI();
        }

        public double TotalDrunk
        {
            get => GetValue(TotalDrunkProperty);
            set
            {
                SetValue(TotalDrunkProperty, value);
                UpdateUI();
            }
        }
        public static readonly StyledProperty<double> TotalDrunkProperty =
            AvaloniaProperty.Register<饮水提醒, double>(nameof(TotalDrunk), 0);

        public double DailyTarget
        {
            get => GetValue(DailyTargetProperty);
            set
            {
                SetValue(DailyTargetProperty, value);
                UpdateUI();
            }
        }
        public static readonly StyledProperty<double> DailyTargetProperty =
            AvaloniaProperty.Register<饮水提醒, double>(nameof(DailyTarget), 2000);

        public void UpdateUI()
        {
            if (_progressBar != null)
            {
                _progressBar.Maximum = DailyTarget;
                _progressBar.Value = TotalDrunk;
            }
            if (_progressText != null)
            {
                double percent = DailyTarget > 0 ? (TotalDrunk / DailyTarget) * 100 : 0;
                _progressText.Text = $"{TotalDrunk:0} / {DailyTarget:0} ml ({percent:0}%)";
            }
        }

        public void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            double amount = slider.Value;
            DrinkConfirmed?.Invoke(this, amount);
            Close();
        }
    }
}