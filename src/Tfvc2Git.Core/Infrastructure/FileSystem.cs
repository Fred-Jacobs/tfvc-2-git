using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace Tfvc2Git.Core.Infrastructure
{
    public class FileSystem
    {
        #region Properties
        public char CsvSeparator { get; } = ';';

        public List<string> DontCopyFolders { get; set; } = new List<string>
        {
            "$tf"
        };

        public List<string> DontClearRootFolders { get; set; } = new List<string>
        {
            ".git",
            "$tf"
        };

        public List<string> DontClearRootFiles { get; set; } = new List<string>
        {
            ".gitignore",
            ".tfvc-2-git"
        };
        #endregion

        #region Fields
        private readonly Json _json;
        private readonly ILogger _log = Log.Logger.ForContext<FileSystem>();
        #endregion

        public FileSystem(Json json)
        {
            _json = json;
        }

        public void EnsureParentFolderExists(string filePath)
        {
            var parentFolder = Directory.GetParent(filePath);
            if (!parentFolder.Exists)
            {
                parentFolder.Create();
            }
        }

        public T FromJson<T>(string filePath) where T : class
        {
            var jsonContent = File.ReadAllText(filePath);
            return _json.FromString<T>(jsonContent);
        }

        public void Save<T>(T content, string filePath) where T : class
        {
            var jsonContent = _json.AsString(content);
            File.WriteAllText(filePath, jsonContent);
        }

        public void DeleteAllFiles(string path, bool clearAttributes = true)
        {
            DeleteAllFiles(path, 0, clearAttributes);
        }

        public IEnumerable<string> ReadLines(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _log.Warning(" - File '{FilePath}' doesn't exists.", filePath);
                yield break;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                yield return line;
            }
        }

        public void Delete(string filePath, bool clearAttributes = true, int maxRetry = 5)
        {
            var counter = 0;
            Exception lastException = null;
            while (counter < maxRetry)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    return;
                }
                catch (Exception e)
                {
                    lastException = e;
                    counter++;
                    _log.Warning("Retry file deletion of '{FilePath}' ({counter}) : {ExceptionMessage}", filePath, counter, e.Message);
                }
            }

            if (null != lastException)
            {
                throw lastException;
            }
        }

        private void DeleteAllFiles(string path, int nestedLevel, bool clearAttributes = true)
        {
            var folder = new DirectoryInfo(path);
            if (!folder.Exists)
            {
                throw new DirectoryNotFoundException($"Folder does not exist or could not be found: {path}");
            }

            var files = folder.GetFiles();
            foreach (var file in files)
            {
                if (DontClearRootFiles.Any(x => file.Name.Equals(x)))
                    continue;

                Delete(file.FullName, clearAttributes);
            }

            var subFolders = folder.GetDirectories();
            foreach (var subFolder in subFolders)
            {
                if (nestedLevel == 0 && DontClearRootFolders.Contains(subFolder.Name))
                    continue;

                DeleteAllFiles(subFolder.FullName, nestedLevel + 1, clearAttributes);
            }
        }
    }
}