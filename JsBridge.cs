using System;
using System.IO;

public class JsBridge
{
    private readonly string allowedRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");

    // 检查路径是否在允许的 assets 文件夹下
    private bool IsUnderAllowedFolder(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase);
    }

    // ================== 文本文件操作 ==================

    // 读取文本文件
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

    // 写入文本文件
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

    // 删除文件
    public bool DeleteFile(string path)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return false;
            if (File.Exists(full)) File.Delete(full);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ================== 文件管理操作 ==================

    // 复制文件
    public bool CopyFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourcePath));
            string fullDest = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, destPath));
            if (!IsUnderAllowedFolder(fullSource) || !IsUnderAllowedFolder(fullDest)) return false;
            if (!File.Exists(fullSource)) return false;
            File.Copy(fullSource, fullDest, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 移动文件
    public bool MoveFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourcePath));
            string fullDest = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, destPath));
            if (!IsUnderAllowedFolder(fullSource) || !IsUnderAllowedFolder(fullDest)) return false;
            if (!File.Exists(fullSource)) return false;
            if (File.Exists(fullDest)) File.Delete(fullDest);
            File.Move(fullSource, fullDest);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ================== 二进制文件操作 ==================

    // 读取文件字节
    public byte[] ReadFileBytes(string path)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return Array.Empty<byte>();
            if (File.Exists(full)) return File.ReadAllBytes(full);
            return Array.Empty<byte>();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    // 读取文件并返回 Base64 字符串，用于图片/音频/视频显示
    public string ReadFileBase64(string path)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (!IsUnderAllowedFolder(full)) return string.Empty;
            if (File.Exists(full)) return Convert.ToBase64String(File.ReadAllBytes(full));
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
