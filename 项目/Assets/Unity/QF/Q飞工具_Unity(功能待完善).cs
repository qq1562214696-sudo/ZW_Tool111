using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public static class Q飞工具_Unity
{
    // 新增：通过精确名称查找预制体
    public static string FindPrefabByExactName(string name)
    {
        // 搜索所有预制体
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            
            // 检查文件名是否完全匹配
            if (fileName == name)
            {
                return prefabPath;
            }
        }
        
        return null;
    }

    // 新增：处理Head_前缀预制体
    public static bool ProcessHeadPrefab(string maxFileName)
    {
        try
        {
            Debug.Log($"开始处理Head_前缀文件: {maxFileName}");
            
            // 搜索与maxFileName完全匹配的预制体
            string prefabPath = FindPrefabByExactName(maxFileName);
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                // 如果没有找到对应的预制体
                string errorMessage = $"{maxFileName} 头饰的预制体暂未创建，添加动画失败";
                Debug.LogWarning(errorMessage);
                
                // 显示警告对话框（非阻塞式）
                EditorUtility.DisplayDialog("提示", errorMessage, "确定");
                
                return false;
            }
            
            // 加载预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"无法加载预制体: {prefabPath}");
                return false;
            }
            
            // 检查预制体是否已经有Animation组件
            Animation animationComponent = prefab.GetComponent<Animation>();
            if (animationComponent == null)
            {
                // 添加Animation组件
                animationComponent = prefab.AddComponent<Animation>();
                Debug.Log($"已为预制体 {maxFileName} 添加Animation组件");
            }
            else
            {
                Debug.Log($"预制体 {maxFileName} 已有Animation组件");
            }
            
            // 固定动画文件名：Head_00002@Idle
            string fixedAnimationName = "Head_00002@Idle";
            string animationPath = FindAnimationByName(fixedAnimationName);
            
            if (string.IsNullOrEmpty(animationPath))
            {
                string errorMessage = $"找不到动画文件: {fixedAnimationName}，请确保该动画文件存在";
                Debug.LogWarning(errorMessage);
                
                // 显示错误对话框
                EditorUtility.DisplayDialog("错误", errorMessage, "确定");
                return false;
            }
            
            // 加载动画
            AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
            
            if (animationClip == null)
            {
                string errorMessage = $"无法加载动画文件: {animationPath}";
                Debug.LogError(errorMessage);
                EditorUtility.DisplayDialog("错误", errorMessage, "确定");
                return false;
            }
            
            // 将动画添加到Animation组件
            // 首先清除所有现有动画（避免重复添加）
            animationComponent.clip = null;
            animationComponent.RemoveClip(animationClip);
            
            // 添加动画到组件
            animationComponent.AddClip(animationClip, animationClip.name);
            
            // 设置默认动画
            animationComponent.clip = animationClip;
            
            // 可选：设置动画播放模式
            // animationComponent.playAutomatically = true;
            // animationComponent.wrapMode = WrapMode.Loop;
            
            // 标记预制体为已修改
            EditorUtility.SetDirty(prefab);
            
            Debug.Log($"已为预制体 {maxFileName} 添加动画: {animationClip.name}");
            
            return true;
        }
        catch (System.Exception e)
        {
            string errorMessage = $"处理Head_前缀预制体时发生错误: {maxFileName}, 错误: {e.Message}";
            Debug.LogError(errorMessage);
            EditorUtility.DisplayDialog("错误", errorMessage, "确定");
            return false;
        }
    }
    
    // 新增：打开编辑器的方法
    public static void OpenAvatarEditor()
    {
        try
        {
            EditorApplication.ExecuteMenuItem("Art/Avatar/编辑器");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"打开编辑器时发生错误: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"打开编辑器时发生错误: {e.Message}", "确定");
        }
    }

    public static void CopyAndOpenExportWindow()
    {
        // 获取所有选中的对象
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个或多个GameObject", "确定");
            return;
        }

        // 如果只选中了一个对象，直接处理
        if (selectedObjects.Length == 1)
        {
            ProcessSingleObject(selectedObjects[0]);
            return;
        }
        
        // 多个对象的情况
        GameObject rootObject = null;
        
        // 情况1: 检查是否全部是父子级关系
        if (AreAllObjectsInSameHierarchy(selectedObjects, out rootObject))
        {
            // 找到最父级对象进行处理
            ProcessSingleObject(rootObject);
        }
        else
        {
            // 情况2: 不是全父子级关系，提示只能选一个
            EditorUtility.DisplayDialog("提示", 
                "选择了多个对象，只能选择一个！", 
                "确定");
        }
    }

    private static void ProcessSingleObject(GameObject originalObject)
    {
        try
        {
            // 复制对象到根层级
            var duplicatedObject = CopyObjectToRoot(originalObject);
            
            if (duplicatedObject == null)
            {
                EditorUtility.DisplayDialog("错误", "复制对象失败", "确定");
                return;
            }
            
            // 尝试打开FBX导出窗口
            TryOpenFbxExportWindow();
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"操作过程中发生错误: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"操作过程中发生错误: {e.Message}", "确定");
        }
    }

    private static GameObject CopyObjectToRoot(GameObject original)
    {
        GameObject duplicated = GameObject.Instantiate(original);
        duplicated.name = original.name + "_导出副本";
        // 重置Transform值
        ResetTransform(duplicated.transform);
        duplicated.transform.SetParent(null);
        RenameAllChildren(duplicated.transform);
        Selection.activeGameObject = duplicated;
        return duplicated;
    }

    // 新增：重置Transform值的方法
    private static void ResetTransform(Transform transform)
    {
        // 重置位置、旋转和缩放
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private static void RenameAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.name = child.name + "_导出副本";
            RenameAllChildren(child);
        }
    }

    private static void TryOpenFbxExportWindow()
    {
        // 尝试执行菜单命令打开导出窗口
        bool success = EditorApplication.ExecuteMenuItem("GameObject/Export To FBX...");
        
        if (!success)
        {
            // 如果菜单命令失败，显示提示
            EditorUtility.DisplayDialog("提示", 
                "无法自动打开FBX导出窗口。\n\n" +
                "请手动右键点击选中的对象，然后选择 'Export To FBX...'", 
                "确定");
        }
    }
    
    /// <summary>
    /// 检查所有选中的对象是否在同一层级树中
    /// </summary>
    /// <param name="objects">选中的对象数组</param>
    /// <param name="rootObject">输出最父级的对象</param>
    /// <returns>true表示在同一层级树中，false表示不在</returns>
    private static bool AreAllObjectsInSameHierarchy(GameObject[] objects, out GameObject rootObject)
    {
        rootObject = null;
        
        if (objects.Length == 0)
            return false;
        
        // 收集所有对象及其所有祖先
        HashSet<GameObject> allObjectsInHierarchy = new HashSet<GameObject>();
        
        foreach (GameObject obj in objects)
        {
            // 添加当前对象
            allObjectsInHierarchy.Add(obj);
            
            // 添加所有祖先
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                allObjectsInHierarchy.Add(parent.gameObject);
                parent = parent.parent;
            }
        }
        
        // 检查是否所有选中对象都在这个层级树中
        // 也就是说，每个选中对象的祖先中是否有其他选中对象
        List<GameObject> rootCandidates = new List<GameObject>();
        
        foreach (GameObject obj in objects)
        {
            bool hasSelectedAncestor = false;
            
            // 检查这个对象的所有祖先中是否有其他选中对象
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (Array.Exists(objects, o => o == parent.gameObject))
                {
                    hasSelectedAncestor = true;
                    break;
                }
                parent = parent.parent;
            }
            
            // 如果没有选中的祖先，那么它就是一个候选根对象
            if (!hasSelectedAncestor)
            {
                rootCandidates.Add(obj);
            }
        }
        
        // 如果只有一个候选根对象，那么所有对象都在同一层级树中
        if (rootCandidates.Count == 1)
        {
            rootObject = rootCandidates[0];
            return true;
        }
        
        // 多个候选根对象，说明选择了多个不相关的层级树
        return false;
    }

    // 新增：一键导出功能（批量导出列表中所有）
    public static void ExportFiles(string[] fileNames)
    {
        if (fileNames == null || fileNames.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请提供要导出的文件名列表", "确定");
            return;
        }
        
        // 自动获取桌面路径
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string exportRootPath = Path.Combine(desktopPath, "Unity批量导出_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        
        try
        {
            EditorUtility.DisplayProgressBar("批量导出中", "正在准备...", 0);
            
            // 确保导出根目录存在
            Directory.CreateDirectory(exportRootPath);
            
            int totalExported = 0;
            
            // 处理列表中的每个max文件名
            for (int i = 0; i < fileNames.Length; i++)
            {
                string maxFileName = fileNames[i];
                float progress = (float)i / fileNames.Length;
                EditorUtility.DisplayProgressBar("批量导出中", 
                    $"正在处理: {maxFileName} ({i+1}/{fileNames.Length})", 
                    progress);
                
                // 1. 导出完全对应的预制体
                int prefabCount = ExportPrefabsByName(maxFileName, exportRootPath);
                
                // 2. 导出完全对应的文件夹
                int folderCount = ExportFoldersByName(maxFileName, exportRootPath);
                
                if (prefabCount > 0 || folderCount > 0)
                {
                    totalExported++;
                    Debug.Log($"已处理: {maxFileName} (预制体: {prefabCount}个, 文件夹: {folderCount}个)");
                }
                else
                {
                    Debug.LogWarning($"未找到匹配的资源: {maxFileName}");
                }
            }
            
            EditorUtility.ClearProgressBar();
            
            // 显示结果
            string resultMessage = $"批量导出完成！\n\n" +
                                  $"导出路径: {exportRootPath}\n" +
                                  $"成功处理: {totalExported}/{fileNames.Length}个文件";
            
            EditorUtility.DisplayDialog("批量导出完成", resultMessage, "确定");
            
            // 自动打开导出文件夹
            System.Diagnostics.Process.Start(exportRootPath);
            
            Debug.Log($"批量导出完成: 共处理{totalExported}/{fileNames.Length}个文件, 路径: {exportRootPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"批量导出过程中发生错误: {e.Message}", "确定");
            Debug.LogError($"批量导出过程中发生错误: {e.Message}\n{e.StackTrace}");
        }
    }

    // 新增：导出指定名称的预制体
    private static int ExportPrefabsByName(string name, string exportRootPath)
    {
        int exportedCount = 0;
        
        // 搜索所有预制体
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            
            // 检查文件名是否完全匹配
            if (fileName == name)
            {
                try
                {
                    // 复制预制体和其.meta文件
                    CopyAssetWithMeta(prefabPath, exportRootPath);
                    exportedCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"导出预制体时出错: {prefabPath}, 错误: {e.Message}");
                }
            }
        }
        
        return exportedCount;
    }

    // 修改导出文件夹方法，使用简单粗暴的方法
    private static int ExportFoldersByName(string name, string exportRootPath)
    {
        int exportedCount = 0;
        
        // 搜索所有文件夹
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        
        foreach (string assetPath in allAssetPaths)
        {
            // 检查是否为文件夹
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                string folderName = Path.GetFileName(assetPath);
                
                // 检查文件夹名是否完全匹配
                if (folderName == name)
                {
                    try
                    {
                        // 使用简单粗暴的方法复制文件夹
                        string targetPath = Path.Combine(exportRootPath, assetPath);
                        CopyFolderSimple(assetPath, targetPath);
                        exportedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"导出文件夹时出错: {assetPath}, 错误: {e.Message}");
                    }
                }
            }
        }
        
        return exportedCount;
    }

    // 新增：简单的目录复制方法
    private static void CopyDirectorySimple(string sourceDir, string targetDir, bool copySubDirs)
    {
        // 获取源目录的子目录
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
        }
        
        // 创建目标目录
        Directory.CreateDirectory(targetDir);
        
        // 复制所有文件
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string targetFilePath = Path.Combine(targetDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }
        
        // 复制所有子目录
        if (copySubDirs)
        {
            DirectoryInfo[] subDirs = dir.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs)
            {
                string newTargetDir = Path.Combine(targetDir, subDir.Name);
                CopyDirectorySimple(subDir.FullName, newTargetDir, copySubDirs);
            }
        }
    }

    // 新增：简单粗暴的文件夹复制方法（备选方案）
    private static void CopyFolderSimple(string sourceFolderPath, string targetFolderPath)
    {
        try
        {
            // 创建目标文件夹
            Directory.CreateDirectory(targetFolderPath);
            
            // 获取Unity项目根目录
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            string sourceFullPath = Path.Combine(projectRoot, sourceFolderPath);
            
            // 复制所有文件和子文件夹（包括.meta文件）
            CopyDirectorySimple(sourceFullPath, targetFolderPath, true);
            
            Debug.Log($"成功复制文件夹: {sourceFolderPath} -> {targetFolderPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"简单复制文件夹时出错: {sourceFolderPath}, 错误: {ex.Message}");
            throw;
        }
    }

    // 新增：递归复制所有内容（修复版）
    private static void CopyAllContentsRecursive(string sourceRelativePath, string targetFullPath, string projectRoot)
    {
        string sourceFullPath = Path.Combine(projectRoot, sourceRelativePath);
        
        try
        {
            // 1. 复制所有文件
            string[] files = Directory.GetFiles(sourceFullPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                try
                {
                    // 跳过.meta文件，我们会单独处理
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    string fileName = Path.GetFileName(file);
                    string targetFile = Path.Combine(targetFullPath, fileName);
                    
                    // 复制文件
                    File.Copy(file, targetFile, true);
                    
                    // 复制对应的.meta文件
                    string metaFile = file + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Copy(metaFile, targetFile + ".meta", true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"复制文件时出错: {file}, 错误: {ex.Message}");
                }
            }
            
            // 2. 递归复制所有子文件夹
            string[] subDirs = Directory.GetDirectories(sourceFullPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string subDir in subDirs)
            {
                try
                {
                    string dirName = Path.GetFileName(subDir);
                    string targetSubDir = Path.Combine(targetFullPath, dirName);
                    
                    // 创建子文件夹
                    Directory.CreateDirectory(targetSubDir);
                    
                    // 复制子文件夹的.meta文件
                    string subDirMeta = subDir + ".meta";
                    if (File.Exists(subDirMeta))
                    {
                        File.Copy(subDirMeta, targetSubDir + ".meta", true);
                    }
                    
                    // 递归复制子文件夹内容
                    string relativeSubDirPath = Path.Combine(sourceRelativePath, dirName);
                    CopyAllContentsRecursive(relativeSubDirPath, targetSubDir, projectRoot);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"复制子文件夹时出错: {subDir}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"复制文件夹内容时出错: {sourceRelativePath}, 错误: {ex.Message}");
        }
    }

    // 新增：递归复制文件夹及其所有内容（修复版）
    private static void CopyFolderWithContentsRecursive(string folderPath, string exportRootPath)
    {
        // 获取Unity项目根目录
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        
        // 构建完整的目标路径
        string targetPath = Path.Combine(exportRootPath, folderPath);
        
        // 创建目标文件夹
        Directory.CreateDirectory(targetPath);
        
        // 复制文件夹自身的.meta文件
        string folderSourceFullPath = Path.Combine(projectRoot, folderPath);
        string folderMetaSourcePath = folderSourceFullPath + ".meta";
        string folderMetaTargetPath = targetPath + ".meta";
        
        if (File.Exists(folderMetaSourcePath))
        {
            File.Copy(folderMetaSourcePath, folderMetaTargetPath, true);
        }
        
        // 递归复制文件夹内所有文件和子文件夹
        CopyAllContentsRecursive(folderPath, targetPath, projectRoot);
    }

    // 新增：复制资产及其.meta文件（不复制父文件夹的.meta）
    private static void CopyAssetWithMeta(string assetPath, string exportRootPath)
    {
        // 获取资产在Assets内的相对路径
        string relativePath = assetPath;
        
        // 构建完整的目标路径
        string targetPath = Path.Combine(exportRootPath, relativePath);
        string targetDir = Path.GetDirectoryName(targetPath);
        
        // 确保目标目录存在
        Directory.CreateDirectory(targetDir);
        
        // 获取Unity项目根目录
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        
        // 复制主文件
        string sourceFullPath = Path.Combine(projectRoot, assetPath);
        
        if (File.Exists(sourceFullPath))
        {
            File.Copy(sourceFullPath, targetPath, true);
            
            // 复制.meta文件（仅复制资产对应的.meta）
            string metaSourcePath = sourceFullPath + ".meta";
            string metaTargetPath = targetPath + ".meta";
            
            if (File.Exists(metaSourcePath))
            {
                File.Copy(metaSourcePath, metaTargetPath, true);
            }
        }
    }

    // 新增：递归复制文件夹内容
    private static void CopyFolderContentsRecursive(string sourceFolderPath, string targetFolderPath, string projectRoot)
    {
        // 获取源文件夹完整路径
        string sourceFolderFullPath = Path.Combine(projectRoot, sourceFolderPath);
        
        // 复制所有文件
        string[] files = Directory.GetFiles(sourceFolderFullPath, "*.*", System.IO.SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            // 跳过.meta文件，我们会单独处理
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;
            
            string relativeFilePath = GetRelativePath(file, projectRoot);
            string targetFilePath = Path.Combine(targetFolderPath, Path.GetFileName(file));
            
            // 复制文件
            File.Copy(file, targetFilePath, true);
            
            // 复制对应的.meta文件
            string metaFile = file + ".meta";
            if (File.Exists(metaFile))
            {
                File.Copy(metaFile, targetFilePath + ".meta", true);
            }
        }
        
        // 递归处理子文件夹
        string[] subDirectories = Directory.GetDirectories(sourceFolderFullPath, "*", System.IO.SearchOption.TopDirectoryOnly);
        foreach (string subDir in subDirectories)
        {
            string folderName = Path.GetFileName(subDir);
            string relativeSubDirPath = GetRelativePath(subDir, projectRoot);
            string targetSubDirPath = Path.Combine(targetFolderPath, folderName);
            
            // 创建子文件夹
            Directory.CreateDirectory(targetSubDirPath);
            
            // 复制子文件夹的.meta文件
            string subDirMetaSource = subDir + ".meta";
            string subDirMetaTarget = targetSubDirPath + ".meta";
            
            if (File.Exists(subDirMetaSource))
            {
                File.Copy(subDirMetaSource, subDirMetaTarget, true);
            }
            
            // 递归复制子文件夹内容
            CopyFolderContentsRecursive(relativeSubDirPath, targetSubDirPath, projectRoot);
        }
    }

    // 新增：获取相对路径
    private static string GetRelativePath(string fullPath, string basePath)
    {
        Uri fullUri = new Uri(fullPath);
        Uri baseUri = new Uri(basePath);
        
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    // 新增：一键规范文件
    public static void StandardizeFiles(string[] fileNames)
    {
        if (fileNames == null || fileNames.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请提供要规范的文件名列表", "确定");
            return;
        }
        
        int processedFBXCount = 0;
        int processedMaterialCount = 0;
        int processedHeadPrefabCount = 0;
        
        try
        {
            // 显示进度条
            EditorUtility.DisplayProgressBar("规范文件中", "正在搜索和处理文件...", 0);
            
            // 处理每个max文件名
            for (int i = 0; i < fileNames.Length; i++)
            {
                string maxFileName = fileNames[i];
                EditorUtility.DisplayProgressBar("规范文件中", 
                    $"正在处理: {maxFileName} ({i+1}/{fileNames.Length})", 
                    (float)i / fileNames.Length);
                
                // 1. 搜索并处理FBX文件
                processedFBXCount += ProcessFBXFiles(maxFileName);
                
                // 2. 搜索并处理材质球
                processedMaterialCount += ProcessMaterials(maxFileName);

                // 3. 新增：如果是Head_前缀，进行特殊处理
                if (maxFileName.StartsWith("Head_", StringComparison.OrdinalIgnoreCase))
                {
                    if (ProcessHeadPrefab(maxFileName))
                    {
                        processedHeadPrefabCount++;
                    }
                }
            }
            
            // 保存所有更改
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.ClearProgressBar();
            
            // 显示结果
            string resultMessage = $"文件规范完成！\n" +
                                $"已处理FBX文件: {processedFBXCount}个\n" +
                                $"已处理材质球: {processedMaterialCount}个";
            
            // 如果有处理Head_前缀文件，添加到结果中
            if (processedHeadPrefabCount > 0)
            {
                resultMessage += $"\n已处理Head_前缀文件: {processedHeadPrefabCount}个";
            }
            
            EditorUtility.DisplayDialog("完成", resultMessage, "确定");
                
            Debug.Log($"规范文件完成: FBX={processedFBXCount}, 材质={processedMaterialCount}, Head_={processedHeadPrefabCount}");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"处理过程中发生错误: {e.Message}", "确定");
            Debug.LogError($"一键规范文件时发生错误: {e.Message}\n{e.StackTrace}");
        }
    }

    // 新增：精确查找动画文件
    private static string FindAnimationByName(string animationName)
    {
        // 搜索所有AnimationClip（.anim文件）
        string[] allAnimations = AssetDatabase.FindAssets("t:AnimationClip");
        
        foreach (string guid in allAnimations)
        {
            string animationPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(animationPath);
            
            // 精确匹配文件名
            if (fileName == animationName)
            {
                return animationPath;
            }
        }
        
        return null;
    }

    // 新增：处理FBX文件
    private static int ProcessFBXFiles(string maxFileName)
    {
        int processedCount = 0;
        
        // 搜索所有FBX文件
        string[] allFBXFiles = AssetDatabase.FindAssets("t:Model");
        
        foreach (string guid in allFBXFiles)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 检查文件名是否恰好匹配max文件名（不包含扩展名）
            if (fileName == maxFileName)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;
                if (modelImporter != null)
                {
                    // 检查当前设置
                    if (modelImporter.animationType == ModelImporterAnimationType.Generic)
                    {
                        // 改为Legacy
                        modelImporter.animationType = ModelImporterAnimationType.Legacy;
                        modelImporter.SaveAndReimport();
                        processedCount++;
                        Debug.Log($"已修改FBX: {path} -> Animation Type: Legacy");
                    }
                }
            }
        }
        
        return processedCount;
    }

    // 新增：处理材质球
    private static int ProcessMaterials(string maxFileName)
    {
        int processedCount = 0;
        
        // 搜索所有材质球
        string[] allMaterials = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in allMaterials)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 检查材质球名称是否包含max文件名并且以_A结尾
            if (fileName.Contains(maxFileName) && fileName.EndsWith("_A"))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && material.shader != null)
                {
                    // 检查shader名称
                    string shaderName = material.shader.name;
                    if (shaderName == "QF/Hang/Hang_AlphaBlend")
                    {
                        // 查找目标shader
                        Shader targetShader = Shader.Find("QF/Hang/Hang_AlphaBlend2Pass");
                        if (targetShader != null)
                        {
                            // 修改shader
                            material.shader = targetShader;
                            
                            // 修改_TintColor的V值为75
                            if (material.HasProperty("_TintColor"))
                            {
                                Color currentColor = material.GetColor("_TintColor");
                                Color.RGBToHSV(currentColor, out float h, out float s, out float v);
                                
                                // 设置V值为75%（0.75）
                                Color newColor = Color.HSVToRGB(h, s, 0.75f);
                                newColor.a = currentColor.a; // 保持alpha不变
                                
                                material.SetColor("_TintColor", newColor);
                                
                                EditorUtility.SetDirty(material);
                                processedCount++;
                                Debug.Log($"已修改材质: {path} -> Shader: {targetShader.name}, TintColor V: 75%");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"找不到Shader: QF/Hang/Hang_AlphaBlend2Pass");
                        }
                    }
                }
            }
        }
        
        return processedCount;
    }
}