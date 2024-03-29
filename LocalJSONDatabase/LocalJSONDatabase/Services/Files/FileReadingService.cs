﻿using System.Text.Json;
using LocalJSONDatabase.Services.Utility;

namespace LocalJSONDatabase.Services.Files
{
    public class FileReadingService
    {
        public string FilePath { get; init; }
        private readonly FileStream file;
        private readonly StreamReader reader;

        public FileReadingService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));

            FilePath = filePath;
            file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);
            reader = new(file);
        }

        public string Read() => reader.ReadToEnd();

        public IEnumerable<T>? Read<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<IEnumerable<T>>(Read()) ?? throw new NullReferenceException();
            }
            catch (Exception ex)
            {
                LogDebugger.LogError(ex);
                return null;
            }
        }

        ~FileReadingService()
        {
            file.Close();
            file.Dispose();
        }
    }
}
