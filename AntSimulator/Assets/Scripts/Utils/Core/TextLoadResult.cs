namespace Utils.Core
{
    public readonly struct TextLoadResult
    {
        public bool Success { get; }
        public string Text { get; }
        public string Error { get; }
        public string Path { get; }

        public TextLoadResult(bool success, string text, string error, string path)
        {
            Success = success;
            Text = text ?? string.Empty;
            Error = error ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public static TextLoadResult Ok(string text, string path)
        {
            return new TextLoadResult(true, text, string.Empty, path);
        }

        public static TextLoadResult Fail(string error, string path)
        {
            return new TextLoadResult(false, string.Empty, error, path);
        }
    }
}
