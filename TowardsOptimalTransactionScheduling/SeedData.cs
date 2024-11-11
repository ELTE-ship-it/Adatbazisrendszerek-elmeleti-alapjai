
using Protocol;
using Protocol.Enum;

namespace ShortestMakespanFirst
{
    public class SeedData
    {
        public SeedData(int numberOfTransactions, int minNumOfOperations, int maxNumOfTransactions)
        {
            _numberOfTransactions = numberOfTransactions;
            _minNumOfOperations = minNumOfOperations;
            _maxNumOfOperations = maxNumOfTransactions;

            TransactionList = CreateTransactionList();
            foreach (var transaction in TransactionList)
            {
                transaction.Operations = GenerateOperations();
            }

            MakeSpan = TransactionList
                .Select(t => t.Operations)
                .Select(o => o.Count)
                .Sum();
        }

        private int _numberOfTransactions;
        private int _minNumOfOperations;
        private int _maxNumOfOperations;
        public List<Transaction> TransactionList;
        public int MakeSpan;

        private List<Transaction> CreateTransactionList()
        {
            var result = new List<Transaction>();
            for (int i = 0; i < _numberOfTransactions; i++)
            {
                result.Add(new Transaction(i));
            }

            return result;
        }

        private List<Operation> GenerateOperations()
        {
            var result = new List<Operation>();
            var randomizer = new Random();
            var operationTypeValues = Enum.GetValues(typeof(OperationType));
            var keyValues = Enum.GetValues(typeof(Key));
            var numberOfOperations = randomizer.Next(_minNumOfOperations, _maxNumOfOperations);

            for (int i = 0; i < numberOfOperations; i++)
            {
                var randomType = (OperationType)operationTypeValues.GetValue(randomizer.Next(operationTypeValues.Length));
                var randomKey = (Key)keyValues.GetValue(randomizer.Next(keyValues.Length));

                result.Add(new Operation(randomType, randomKey));
            }

            return result;
        }
    }    
}

