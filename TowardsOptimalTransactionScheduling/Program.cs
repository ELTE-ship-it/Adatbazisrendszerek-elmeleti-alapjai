
using ShortestMakespanFirst;
using TowardsOptimalTransactionScheduling;

int NumberOfTransaction;
int MinNumOfOperations;
int MaxNumOfOperations;
int SamplingCount;
string input;

Console.WriteLine(
@"Hi! You will be able to test Shortest Makespan First and MVSchedO algorithms.
But first, we need to know some parameters.

Please provide the number of transactions you want to test with:"
);

do
{
    input = Console.ReadLine() ?? "";
} while (!Validator.ValidateInputIfNumber(input, out NumberOfTransaction));

Console.WriteLine("\nPlease provide the number of MINIMUM operations per transaction:");

do
{
    input = Console.ReadLine() ?? "";
} while (!Validator.ValidateInputIfNumber(input, out MinNumOfOperations));

Console.WriteLine("\nPlease provide the number of MAXIMUM operations per transaction:");

do
{
    input = Console.ReadLine() ?? "";
} while (!Validator.ValidateInputIfNumber(input, out MaxNumOfOperations));

var seedData = new SeedData(NumberOfTransaction, MinNumOfOperations, MaxNumOfOperations);

Console.Clear();
Console.WriteLine("\nHere is your base schedule:");
UiCreator.DrawLinearSchedule(seedData.TransactionList);
Console.WriteLine($"\nMakespan: {seedData.MakeSpan}\n");

Console.WriteLine(
@"Please provide a sampling count.
This will be used as the amount of Transactions taken into consideration for the next step in the schedule.
The default value is 5, as mentioned in the article.");

do
{
    input = Console.ReadLine() ?? "";
} while (!Validator.ValidateInputIfNumberOrNull(input, out SamplingCount));

Console.WriteLine("\n\nHere is your SMF (Shortest Makespan First) optimized schedule:");
var smf = new SMFProcessor(seedData.TransactionList, SamplingCount);

smf.Process();

UiCreator.DrawLinearSchedule(smf.OptimizedTransactions);
Console.WriteLine($"\nMakeSpan: {smf.GetMakespan()}");

//Console.WriteLine($"\nTo visualize it further more:");
//UiCreator.DrawAsyncSchedule(smf.OptimizedTransactions);

// To avoid the exit comments on the console
Console.ReadLine();

return 0;
