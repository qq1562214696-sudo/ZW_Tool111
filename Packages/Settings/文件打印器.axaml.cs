using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ZW_Tool
{
    public partial class 文件打印器 : Window
    {
        // private ListBox FileListBox;

        public ObservableCollection<FileItem> Items { get; } = new();
        public FileItem? SelectedItem { get; set; }

        public 文件打印器()
        {
            InitializeComponent();
            DataContext = this;
            // 手动设置绑定
            if (FileListBox != null)
            {
                FileListBox.ItemsSource = Items;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // 拖拽进入
        public void OnDragEnter(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618
            e.DragEffects = e.Data.Contains(DataFormats.Files)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
#pragma warning restore CS0618
        }

        // 拖拽释放
        public async void OnDrop(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618
            if (!e.Data.Contains(DataFormats.Files))
                return;

            var storageItems = e.Data.GetFiles();
#pragma warning restore CS0618

            if (storageItems == null || !storageItems.Any())
                return;

            var paths = new List<string>();
            foreach (var item in storageItems)
            {
                var path = item.TryGetLocalPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    paths.Add(path);
                }
            }

            if (paths.Count == 0) return;

            foreach (var path in paths)
            {
                Items.Add(new FileItem { FullPath = path });
            }
        }

        public async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                UpdateStatus("没有文件可复制");
                return;
            }

            var sb = new StringBuilder();
            int index = 1;
            foreach (var item in Items)
            {
                string content;
                try
                {
                    content = await File.ReadAllTextAsync(item.FullPath);
                }
                catch (Exception ex)
                {
                    content = $"[读取失败: {ex.Message}]";
                }

                sb.AppendLine($"{index}、{item.FileName}");
                sb.AppendLine(content);
                sb.AppendLine();
                index++;
            }

            var text = sb.ToString().TrimEnd();
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                UpdateStatus("已复制到剪贴板");
            }
            else
            {
                UpdateStatus("无法访问剪贴板");
            }
        }

        public void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is FileItem item)
            {
                Items.Remove(item);
            }
        }

        public void OnListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && FileListBox.SelectedItem is FileItem selected)
            {
                Items.Remove(selected);
                e.Handled = true;
            }
        }

        public void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        // ==================== 把整个 CopyProjectButton_Click 替换为下面这个版本 ====================
        public async void CopyProjectButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("正在定位项目根目录并扫描...");

            try
            {
                // 1. 定位项目根目录
                string? projectRoot = null;

                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var currentDir = Path.GetDirectoryName(assemblyLocation);

                if (currentDir != null)
                {
                    projectRoot = FindProjectRoot(currentDir);
                }

                if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
                {
                    var baseDir = AppContext.BaseDirectory;
                    projectRoot = FindProjectRoot(baseDir);
                }

                if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
                {
                    UpdateStatus("无法定位项目根目录，请确保程序从项目目录附近运行");
                    return;
                }

                var files = GetAllProjectFiles(projectRoot);

                if (files.Count == 0)
                {
                    UpdateStatus("未找到任何符合条件的源代码文件");
                    return;
                }

                string outputPath = Path.Combine(projectRoot, "全文件.txt");

                UpdateStatus($"正在写入 {files.Count} 个文件到：{outputPath} （覆盖模式）");

                // 使用 append: false 实现覆盖（不存在则自动创建）
                using var writer = new StreamWriter(outputPath, append: false, Encoding.UTF8);

                int index = 1;
                foreach (var file in files)
                {
                    string relativePath = Path.GetRelativePath(projectRoot, file)
                                            .Replace('\\', '/');

                    await writer.WriteLineAsync($"{index}、{relativePath}");
                    await writer.WriteLineAsync("────────────────────────────────────────");

                    try
                    {
                        await foreach (var line in File.ReadLinesAsync(file))
                        {
                            await writer.WriteLineAsync(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        await writer.WriteLineAsync($"[读取失败: {ex.Message}]");
                    }

                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("────────────────────────────────────────");
                    await writer.WriteLineAsync();

                    index++;

                    if (index % 50 == 0)
                        UpdateStatus($"已处理 {index - 1}/{files.Count} 个文件...");
                }

                UpdateStatus($"完成！已覆盖保存到根目录（共 {files.Count} 个文件）");

                // ==================== 修复后的自动打开代码 ====================
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.StorageProvider != null)
                    {
                        // 把本地路径转为 Avalonia 的 IStorageFile
                        var storageFile = await topLevel.StorageProvider.TryGetFileFromPathAsync(outputPath);

                        if (storageFile != null)
                        {
                            await topLevel.Launcher.LaunchFileAsync(storageFile);
                        }
                        else
                        {
                            UpdateStatus("文件已保存，但自动打开失败（路径无法转换）");
                        }
                    }
                }
                catch (Exception openEx)
                {
                    // 打开失败不影响主流程，只提示一下
                    System.Diagnostics.Debug.WriteLine($"自动打开失败: {openEx.Message}");
                }
                // ====================================================
            }
            catch (Exception ex)
            {
                UpdateStatus($"操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从任意目录向上查找真正的项目根目录（包含 .csproj 或 .sln 的目录）
        /// </summary>
        private string? FindProjectRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);

            while (dir != null)
            {
                // 如果当前目录包含 .csproj 或 .sln 文件，则认为是项目根目录
                if (dir.GetFiles("*.csproj").Any() || dir.GetFiles("*.sln").Any())
                {
                    return dir.FullName;
                }

                // 额外安全检查：如果遇到 .git 文件夹，也认为是根目录
                if (dir.GetDirectories(".git").Any())
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        /// <summary>
        /// 获取项目中需要复制的所有文件（严格排除 .git / bin / obj 和根目录特定文件）
        /// </summary>
        private List<string> GetAllProjectFiles(string rootDir)
        {
            var result = new List<string>();

            var excludedRootFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".gitattributes",
                ".gitignore",
                "*.sln",           // 可选：不复制解决方案文件
                "README.md"        // 可选：如果你不想复制
            };

            var excludedDirNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".git",
                "bin",
                "obj",
                "packages",        // NuGet 包
                ".vs"              // Visual Studio 缓存
            };

            foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var relativePath = Path.GetRelativePath(rootDir, file).Replace('\\', '/');
                var directoryParts = Path.GetDirectoryName(relativePath)?
                                    .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries) 
                                    ?? Array.Empty<string>();

                // 排除指定文件夹中的任何文件
                if (directoryParts.Any(part => excludedDirNames.Contains(part)))
                    continue;

                // 排除根目录下的特定文件
                if (string.IsNullOrEmpty(Path.GetDirectoryName(relativePath)) && 
                    excludedRootFiles.Any(ex => ex.EndsWith("*") 
                        ? fileName.EndsWith(ex.TrimEnd('*'), StringComparison.OrdinalIgnoreCase) 
                        : fileName.Equals(ex, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // 只复制文本文件（避免复制 .dll、.exe、.pdb 等二进制文件）
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (extension is ".dll" or ".exe" or ".pdb" or ".cache" or ".log")
                    continue;

                result.Add(file);
            }

            // 按相对路径排序，输出更整洁
            return result.OrderBy(f => Path.GetRelativePath(rootDir, f)).ToList();
        }
    }

    public class FileItem
    {
        public required string FullPath { get; set; }
        public string FileName => Path.GetFileName(FullPath);
    }
}