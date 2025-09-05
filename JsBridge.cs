using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class JsBridge
{

    // ================== 文本文件操作 ==================
    public string ReadFile(string path)
    {
        try
        {
            string fullPath = GetFinalPath(path);
            return File.ReadAllText(fullPath);
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
            string fullPath = GetFinalPath(path);
            string? parentDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            File.WriteAllText(fullPath, content);
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
            string fullPath = GetFinalPath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ================== 文件管理操作 ==================
    public bool CopyFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = GetFinalPath(sourcePath);
            string fullDest = GetFinalPath(destPath);
            if (Directory.Exists(fullSource))
            {
                if (!Directory.Exists(fullDest))
                {
                    Directory.CreateDirectory(fullDest);
                }

                foreach (string file in Directory.GetFiles(fullSource, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(fullSource, file);
                    string targetFile = Path.Combine(fullDest, relativePath);

                    string? targetDir = Path.GetDirectoryName(targetFile);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    File.Copy(file, targetFile, true);
                }

                return true;
            }
            if (!File.Exists(fullSource))
            {
                return false;
            }

            string? destParentDir = Path.GetDirectoryName(fullDest);
            if (!string.IsNullOrEmpty(destParentDir) && !Directory.Exists(destParentDir))
            {
                Directory.CreateDirectory(destParentDir);
            }

            File.Copy(fullSource, fullDest, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool MoveFile(string sourcePath, string destPath)
    {
        try
        {
            string fullSource = GetFinalPath(sourcePath);
            string fullDest = GetFinalPath(destPath);
            if (Directory.Exists(fullSource))
            {
                if (Directory.Exists(fullDest))
                {
                    Directory.Delete(fullDest, true);
                }

                Directory.Move(fullSource, fullDest);
                return true;
            }
            if (!File.Exists(fullSource))
            {
                return false;
            }

            string? destParentDir = Path.GetDirectoryName(fullDest);
            if (!string.IsNullOrEmpty(destParentDir) && !Directory.Exists(destParentDir))
            {
                Directory.CreateDirectory(destParentDir);
            }
            if (File.Exists(fullDest))
            {
                File.Delete(fullDest);
            }

            File.Move(fullSource, fullDest);
            return true;
        }
        catch
        {
            return false;
        }
    }


    // ================== 二进制文件操作 ==================
    public byte[] ReadFileBytes(string path)
    {
        try
        {
            string fullPath = GetFinalPath(path);
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : Array.Empty<byte>();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public string ReadFileBase64(string path)
    {
        try
        {
            string fullPath = GetFinalPath(path);
            if (File.Exists(fullPath))
            {
                byte[] fileBytes = File.ReadAllBytes(fullPath);
                return Convert.ToBase64String(fileBytes);
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ================== 下载文件操作 ==================
    public async Task<string> DownloadFileAsync(string url, string savePath)
    {
        try
        {
            string fullSavePath = GetFinalPath(savePath);
            string? saveParentDir = Path.GetDirectoryName(fullSavePath);
            if (!string.IsNullOrEmpty(saveParentDir))
            {
                Directory.CreateDirectory(saveParentDir);
            }

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return $"[下载失败] HTTP 状态码 {response.StatusCode}";
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(fullSavePath, data);
            }

            return $"[下载完成] 保存路径: {fullSavePath}";
        }
        catch (Exception ex)
        {
            return "[下载失败] " + ex.Message;
        }
    }

    private string GetFinalPath(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("路径不能为空", nameof(inputPath));
        }
        bool isAbsolutePath = Path.IsPathRooted(inputPath) || inputPath.IndexOf(':') > 0;

        if (isAbsolutePath)
        {
            return Path.GetFullPath(inputPath);
        }
        else
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, inputPath));
        }
    }
}
