
using Protocol;
using Protocol.Enum;

namespace TowardsOptimalTransactionScheduling;

public static class UiCreator
{
    public static void DrawLinearSchedule(List<Transaction> transactions)
    {
        for (int i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"T{transaction.Id}(");
            Console.ForegroundColor = ConsoleColor.White;

            for (int j = 0; j < transaction.Operations.Count; j++)
            {
                var op = transaction.Operations[j];
                string operationSymbol = op.Type == OperationType.Read ? "r" : "w";

                Console.Write($"{operationSymbol}({Enum.GetName<Key>(op.Key)})");

                if (j < transaction.Operations.Count - 1)
                {
                    Console.Write(" -> ");
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(") ");

            if (i < transactions.Count - 1)
            {                
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("-> ");
            }            
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void DrawAsyncSchedule(List<Transaction> transactions)
    {
        var remainingItems = transactions;
        var drawnItems = new List<Transaction>();

        while (remainingItems.Count > 0) 
        { 
            var item = remainingItems.First();
            if (CanBePlaced(drawnItems, item))
            {
                Console.Write(item.ToString());
            }            

            drawnItems.Add(item);
            remainingItems.RemoveAt(0);
            Console.WriteLine();
        }
    }

    private static bool CanBePlaced(List<Transaction> drawnItems, Transaction toBePlacedItem, int checkIndex = 0)
    {
        return true;
    }
}    
