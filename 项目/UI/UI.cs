using Avalonia.Controls;

namespace ZW_Tool.UI
{
    /// <summary>
    /// UI控件值助手类 - 安全地获取控件状态
    /// </summary>
    public class UI方法
    {
        /// <summary>
        /// 从各种控件中安全获取内容文本（Button、TextBlock、TextBox、Label 等）
        /// </summary>
        /// <param name="控件">要获取文本的控件</param>
        /// <param name="默认值">如果获取失败或为空时返回的默认值</param>
        /// <returns>控件的内容文本</returns>
        public static string 获取内容文本(object? 控件, string 默认值 = "")
        {
            if (控件 == null)
                return 默认值;

            string? 文本 = 控件 switch
            {
                Button button => button.Content?.ToString()?.Trim(),
                TextBlock textBlock => textBlock.Text?.Trim(),
                TextBox textBox => textBox.Text?.Trim(),
                Label label => label.Content?.ToString()?.Trim(),
                _ => null
            };

            return string.IsNullOrWhiteSpace(文本) ? 默认值 : 文本;
        }

        /// <summary>
        /// 从控件中安全获取布尔值（适用于 CheckBox.IsChecked、Expander.IsExpanded、ToggleButton.IsChecked 等）
        /// </summary>
        /// <param name="控件">目标控件</param>
        /// <param name="默认值">获取失败时的默认返回值</param>
        public static bool 获取布尔值(object? 控件, bool 默认值 = false)
        {
            return 控件 switch
            {
                CheckBox checkBox   => checkBox.IsChecked == true,
                Expander expander   => expander.IsExpanded,
                // ToggleButton toggle => toggle.IsChecked == true,     // 支持 ToggleButton
                RadioButton radio   => radio.IsChecked == true,      // 支持 RadioButton
                _                   => 默认值
            };
        }

        // 如果你以后还想支持其他控件，可以继续加
    }
}
