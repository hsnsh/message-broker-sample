using System.Globalization;

namespace Hosting;

public static class ConsoleWriter
{
    private static readonly object MessageLock = new();

    public static void Info(IEnumerable<string> messages) => Each(messages, i => Info(i));
    public static void Info(string message, params object?[] args) => WriteMessage(message, args: args);

    public static void Warning(IEnumerable<string> messages) => Each(messages, i => Warning(i));
    public static void Warning(string message, params object?[] args) => WriteMessage(message, ConsoleColor.Yellow, args);

    public static void Error(IEnumerable<string> messages) => Each(messages, i => Error(i));
    public static void Error(string message, params object?[] args) => WriteMessage(message, ConsoleColor.Red, args);

    private static void WriteMessage(string? message, ConsoleColor? frColor = null, params object?[] args)
    {
        if (message is null) return;

        lock (MessageLock)
        {
            var oldFgColor = Console.ForegroundColor;

            if (frColor is not null) Console.ForegroundColor = frColor.Value;

            if (args is { Length: > 0 })
            {
                message = string.Format(CultureInfo.CurrentCulture, message, args);
            }

            Console.WriteLine(message);

            Console.ForegroundColor = oldFgColor;
        }
    }

    private static void Each(IEnumerable<string> items, Action<string> action)
    {
        foreach (var item in items) action(item);
    }
}