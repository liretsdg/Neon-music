using System;
using System.IO;

public class JsBridge
{
    private readonly string allowedRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");

    private bool IsUnderAllowedFolder(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase);
    }

    public string ReadFile(string path)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return "[读取失败] 禁止访问非 assets 文件夹";

            return File.ReadAllText(full);
        }
        catch (Exception ex)
        {
            return "[读取失败] " + ex.Message;
        }
    }

    public bool WriteFile(string path, string content)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return false;

            File.WriteAllText(full, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteFile(string path)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return false;

            if (File.Exists(full))
                File.Delete(full);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 复制文件（覆盖目标）
    public bool CopyFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourcePath));
            string fullDest = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, destPath));

            if (!IsUnderAllowedFolder(fullSource) || !IsUnderAllowedFolder(fullDest))
                return false;

            if (!File.Exists(fullSource))
                return false;

            File.Copy(fullSource, fullDest, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 移动文件（剪切）
    public bool MoveFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourcePath));
            string fullDest = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, destPath));

            if (!IsUnderAllowedFolder(fullSource) || !IsUnderAllowedFolder(fullDest))
                return false;

            if (!File.Exists(fullSource))
                return false;

            // 如果目标文件已存在，先删除目标文件避免异常
            if (File.Exists(fullDest))
                File.Delete(fullDest);

            File.Move(fullSource, fullDest);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
