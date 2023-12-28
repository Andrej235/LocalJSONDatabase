using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LocalJSONDatabase.Services.Files
{
    public class FileWritingService
    {
        public string FilePath { get; init; }
        private readonly FileStream file;
        private readonly StreamWriter writer;

        public FileWritingService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));

            FilePath = filePath;
            file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

            writer = new(file);
        }

        public void Write(string text)
        {
            if (writer.BaseStream.Length > 1)
            {
                writer.BaseStream.Seek(-1, SeekOrigin.End);
                writer.Write("," + text + "]");
            }
            else if (writer.BaseStream.Length == 0)
            {
                writer.Write("[");
                writer.Write(text + "]");
            }

            writer.Flush();
        }

        ~FileWritingService()
        {
            file.Close();
            file.Dispose();
        }
    }
}
