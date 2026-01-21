namespace PettyCashManager.Services;

public static class ReportBuilder<T>
{
    /// <summary>
    /// Generic helper: converts a list of items into formatted report lines.
    /// Caller provides a formatter function, keeping this component reusable.
    /// </summary>
    public static List<string> BuildLines(IEnumerable<T> items, Func<T, string> formatter)
        => items.Select(formatter).ToList();
}
