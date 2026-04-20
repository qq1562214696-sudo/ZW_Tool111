using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ZW_Tool;

public partial class 文件打印器 : Window
{
    private const string ListStorageFileName = "filelist.json";

    public ObservableCollection<FileItemWithIndex> Items { get; } = new();
    public FileItemWithIndex? SelectedItem { get; set; }

    private string? _projectRoot;
    private readonly string _storageFilePath;

    public 文件打印器()
    {
        InitializeComponent();
        DataContext = this;

        _projectRoot = FindProjectRoot(AppContext.BaseDirectory);
        _storageFilePath = Path.Combine(_projectRoot ?? AppContext.BaseDirectory, ListStorageFileName);

        LoadListFromFile();
        Items.CollectionChanged += OnItemsChanged;

        var listBox = this.FindControl<ListBox>("FileListBox");
        if (listBox != null)
        {
            listBox.AddHandler(DragDrop.DropEvent, OnDrop);
            listBox.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        }

        UpdateAllIndices();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void UpdateAllIndices()
    {
        for (int i = 0; i < Items.Count; i++)
            Items[i].Index = i + 1;
    }

    private void LoadListFromFile()
    {
        if (!File.Exists(_storageFilePath)) return;
        try
        {
            var json = File.ReadAllText(_storageFilePath);
            var paths = JsonSerializer.Deserialize<List<string>>(json);
            if (paths == null) return;

            foreach (var path in paths)
                if (File.Exists(path))
                    Items.Add(new FileItemWithIndex { FullPath = path, Index = Items.Count + 1 });

            UpdateStatus($"已加载 {Items.Count} 个历史文件");
        }
        catch (Exception ex)
        {
            UpdateStatus($"加载历史列表失败: {ex.Message}");
        }
    }

    private void SaveListToFile()
    {
        try
        {
            var paths = Items.Select(i => i.FullPath).ToList();
            var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFilePath, json);
            UpdateStatus($"列表已保存至 {_storageFilePath}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"保存列表失败: {ex.Message}");
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateAllIndices();
        SaveListToFile();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.GetFiles()?.Any() == true)
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files == null || !files.Any()) return;

        var paths = new List<string>();
        foreach (var item in files)
        {
            var path = item.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) continue;

            if (Directory.Exists(path))
                paths.AddRange(CollectTextFilesFromDirectory(path));
            else if (File.Exists(path) && IsTextFile(path))
                paths.Add(path);
        }

        int addedCount = 0;
        foreach (var path in paths.Distinct())
        {
            if (!Items.Any(i => i.FullPath == path))
            {
                Items.Add(new FileItemWithIndex { FullPath = path, Index = Items.Count + 1 });
                addedCount++;
            }
        }

        UpdateStatus($"已添加 {addedCount} 个文件");
    }

    private static List<string> CollectTextFilesFromDirectory(string rootDir)
    {
        var result = new List<string>();
        var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", ".vs", "packages" };

        try
        {
            foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
            {
                var dirName = Path.GetDirectoryName(file);
                if (dirName != null && excludeDirs.Any(ex => dirName.Contains(Path.DirectorySeparatorChar + ex + Path.DirectorySeparatorChar) ||
                                                              dirName.EndsWith(Path.DirectorySeparatorChar + ex)))
                    continue;

                if (IsTextFile(file))
                    result.Add(file);
            }
        }
        catch { }

        return result;
    }

    private static bool IsTextFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".cs" or ".xaml" or ".axaml" or ".xml" or ".json" or ".config"
                or ".resx" or ".css" or ".js" or ".html" or ".htm" or ".md" or ".csproj"
                or ".sln" or ".props" or ".targets" or ".vb" or ".fs" or ".cpp" or ".h"
                or ".hpp" or ".c" or ".py" or ".java" or ".kt" or ".swift" or ".sql"
                or ".yml" or ".yaml" or ".toml" or ".ini" or ".cfg" or ".sh" or ".bat"
                or ".ps1" => true,
            _ => false
        };
    }

    private async void CopyListToClipboard_Click(object? sender, RoutedEventArgs e)
    {
        if (Items.Count == 0)
        {
            UpdateStatus("列表为空，无内容可复制");
            return;
        }

        var sb = new StringBuilder();
        foreach (var item in Items)
        {
            string content;
            try { content = await File.ReadAllTextAsync(item.FullPath); }
            catch (Exception ex) { content = $"[读取失败: {ex.Message}]"; }

            sb.AppendLine($"{item.Index}、{item.FileName}");
            sb.AppendLine("────────────────────────────────────────");
            sb.AppendLine(content);
            sb.AppendLine();
            sb.AppendLine("────────────────────────────────────────");
            sb.AppendLine();
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(sb.ToString().TrimEnd());
            UpdateStatus($"已复制 {Items.Count} 个文件内容到剪贴板");
        }
        else UpdateStatus("无法访问剪贴板");
    }

    private void ClearList_Click(object? sender, RoutedEventArgs e)
    {
        Items.Clear();
        UpdateStatus("列表已清空");
    }

    private void RemoveMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: FileItemWithIndex item })
        {
            Items.Remove(item);
            UpdateStatus($"已移除: {item.FileName}");
        }
    }

    private void OnListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && SelectedItem != null)
        {
            Items.Remove(SelectedItem);
            e.Handled = true;
            UpdateStatus($"已移除: {SelectedItem.FileName}");
        }
    }

    public async void CopyProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        UpdateStatus("正在定位项目根目录...");

        if (string.IsNullOrEmpty(_projectRoot) || !Directory.Exists(_projectRoot))
            _projectRoot = FindProjectRoot(AppContext.BaseDirectory);

        if (string.IsNullOrEmpty(_projectRoot))
        {
            UpdateStatus("❌ 无法定位项目根目录（未找到 .csproj 或 .sln）");
            return;
        }

        UpdateStatus($"项目根目录: {_projectRoot}");
        var files = GetAllProjectTextFiles(_projectRoot);

        if (files.Count == 0)
        {
            UpdateStatus("⚠️ 项目中未找到任何文本文件");
            return;
        }

        string outputPath = Path.Combine(_projectRoot, "全文件.txt");
        UpdateStatus($"正在生成 {files.Count} 个文件到 {outputPath} ...");

        try
        {
            await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            int index = 1;
            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(_projectRoot, file).Replace('\\', '/');
                await writer.WriteLineAsync($"{index}、{relativePath}");
                await writer.WriteLineAsync("────────────────────────────────────────");

                try
                {
                    await foreach (var line in File.ReadLinesAsync(file, Encoding.UTF8))
                        await writer.WriteLineAsync(line);
                }
                catch (Exception ex) { await writer.WriteLineAsync($"[读取失败: {ex.Message}]"); }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync("────────────────────────────────────────");
                await writer.WriteLineAsync();
                index++;

                if (index % 50 == 0)
                    UpdateStatus($"已处理 {index - 1}/{files.Count} 个文件...");
            }

            UpdateStatus($"✅ 完成！已生成全文件.txt（共 {files.Count} 个文件）");

            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.StorageProvider != null)
                {
                    var storageFile = await topLevel.StorageProvider.TryGetFileFromPathAsync(outputPath);
                    if (storageFile != null)
                        await topLevel.Launcher.LaunchFileAsync(storageFile);
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ 生成失败: {ex.Message}");
        }
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Any() || dir.GetFiles("*.sln").Any() || dir.GetDirectories(".git").Any())
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static List<string> GetAllProjectTextFiles(string rootDir)
    {
        var result = new List<string>();
        var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", ".vs", "packages" };

        foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
        {
            var dir = Path.GetDirectoryName(file);
            if (dir != null && excludeDirs.Any(ex => dir.Contains(Path.DirectorySeparatorChar + ex + Path.DirectorySeparatorChar) ||
                                                      dir.EndsWith(Path.DirectorySeparatorChar + ex)))
                continue;

            if (IsTextFile(file))
                result.Add(file);
        }

        return result.OrderBy(f => Path.GetRelativePath(rootDir, f)).ToList();
    }

    private void UpdateStatus(string message)
    {
        Dispatcher.UIThread.Post(() => StatusText.Text = message);
    }
}

// 数据模型（确保属性通知正确）
public class FileItemWithIndex : INotifyPropertyChanged
{
    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            OnPropertyChanged(nameof(Index));
            OnPropertyChanged(nameof(DisplayIndex));
        }
    }

    public string DisplayIndex => $"{Index}.";
    public required string FullPath { get; set; }
    public string FileName => Path.GetFileName(FullPath);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ZeroToVisibleConverter : IValueConverter
{
    public static readonly ZeroToVisibleConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int count)
        {
            bool isZero = count == 0;
            if (parameter?.ToString() == "reverse")
                isZero = !isZero;
            return isZero;
        }
        return false;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}