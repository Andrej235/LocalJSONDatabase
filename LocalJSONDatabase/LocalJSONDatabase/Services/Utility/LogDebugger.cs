using System.Diagnostics;

namespace LocalJSONDatabase.Services.Utility
{
    public static class LogDebugger
    {
        private static StreamWriter? writer;
        public static void InitializeWriter(StreamWriter writer, bool resetFileContent = true)
        {
            LogDebugger.writer = writer;

            if (resetFileContent)
                writer.BaseStream.SetLength(0);
        }

        public static void LogError(Exception ex, bool includeStackTrace = true)
        {
            var errorMessage = GetErrorMessage(ex, includeStackTrace);
            Debug.WriteLine(errorMessage);

            writer?.WriteLine(errorMessage);
            writer?.Flush();
        }

        public static string GetErrorMessage(Exception ex, bool includeStackTrace = true)
        {
            return includeStackTrace
                ? ex.InnerException is null
                    ? $"---> Error occurred: {ex.Message}\n{ex.StackTrace}\n"
                    : $"---> Error occurred: {ex.Message}\n{ex.InnerException.Message}\n{ex.StackTrace}\n"
                : ex.InnerException is null
                    ? $"---> Error occurred: {ex.Message}"
                    : $"---> Error occurred: {ex.Message}\n{ex.InnerException.Message}";
        }
    }
}
