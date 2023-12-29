using System.Diagnostics;

namespace LocalJSONDatabase.Services
{
    public static class LogDebugger
    {
        public static void LogError(Exception ex, bool includeStackTrace = true)
        {
            if (includeStackTrace)
            {
                if (ex.InnerException is null)
                    Debug.WriteLine($"---> Error occurred: {ex.Message}\n{ex.StackTrace}\n");
                else
                    Debug.WriteLine($"---> Error occurred: {ex.Message}\n{ex.InnerException.Message}\n{ex.StackTrace}\n");
            }
            else
            {
                if (ex.InnerException is null)
                    Debug.WriteLine($"---> Error occurred: {ex.Message}");
                else
                    Debug.WriteLine($"---> Error occurred: {ex.Message}\n{ex.InnerException.Message}");
            }
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
