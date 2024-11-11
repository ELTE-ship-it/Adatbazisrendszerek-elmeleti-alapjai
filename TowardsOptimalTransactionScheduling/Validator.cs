
namespace TowardsOptimalTransactionScheduling;

public static class Validator
{
    public static bool ValidateInputIfNumber(in string input, out int value)
    {
        if (!int.TryParse(input, out int newValue) || newValue <= 0)
        {
            Console.WriteLine("Please use only positive whole numbers larger than zero!");
            value = newValue;
            return false;
        }
        else
        {
            value = newValue;
            return true;
        }
    }

    public static bool ValidateInputIfNumberOrNull(in string? input, out int value)
    {
        int newValue = 5;
        if (!string.IsNullOrEmpty(input) && (!int.TryParse(input, out newValue) || newValue <= 0))
        {
            Console.WriteLine("Please use only positive whole numbers larger than zero!");
            value = newValue;
            return false;
        }
        else
        {
            value = newValue;
            return true;
        }
    }
}
