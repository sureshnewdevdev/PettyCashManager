namespace PettyCashManager.UI;

public static class ConsoleHelpers
{
    public static void Header(string title)
    {
        Console.Clear();
        Console.WriteLine("====================================================");
        Console.WriteLine(title);
        Console.WriteLine("====================================================");
        Console.WriteLine();
    }

    public static void Pause(string message = "Press ENTER to continue...")
    {
        Console.WriteLine();
        Console.Write(message);
        Console.ReadLine();
    }

    public static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"Invalid input. Enter a number between {min} and {max}.");
        }
    }

    public static decimal ReadDecimal(string prompt, decimal minExclusive = 0)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (decimal.TryParse(input, out var value) && value > minExclusive)
                return value;

            Console.WriteLine($"Invalid amount. Enter a number > {minExclusive}.");
        }
    }

    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (DateTime.TryParse(input, out var dt))
                return dt;

            Console.WriteLine("Invalid date. Example: 2026-01-21");
        }
    }

    public static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = (Console.ReadLine() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(input))
                return input;

            Console.WriteLine("This field is required.");
        }
    }
}
