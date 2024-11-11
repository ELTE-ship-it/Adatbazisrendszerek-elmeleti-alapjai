
using Protocol;
using Protocol.Enum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ShortestMakespanFirst
{
    public class SMFProcessor : Processor<List<Transaction>>
    {
        public SMFProcessor(List<Transaction> transactions, int sampleSize = 5)
        {
            _transactions = transactions;
            _sampleSize = sampleSize;
            OptimizedTransactions = new List<Transaction>();
        }

        private readonly List<Transaction> _transactions;
        private readonly int _sampleSize;
        public List<Transaction> OptimizedTransactions { get; set; }

        public override List<Transaction> Process()
        {
            var unscheduledTransactions = new List<Transaction>(_transactions);
            var random = new Random();

            // Choose the first transaction randomly
            if (unscheduledTransactions.Any())
            {
                var firstTransaction = unscheduledTransactions[random.Next(unscheduledTransactions.Count)];
                OptimizedTransactions.Add(firstTransaction);
                unscheduledTransactions.Remove(firstTransaction);
            }

            // While there are still unscheduled transactions
            while (unscheduledTransactions.Any())
            {
                Transaction bestTransaction = null;
                int minConflictCost = int.MaxValue;

                // Randomly select `k` transactions from unscheduled transactions
                var sampledTransactions = unscheduledTransactions
                    .OrderBy(x => random.Next())
                    .Take(_sampleSize)
                    .ToList();

                // Calculate the conflict cost for each sampled transaction
                foreach (var transaction in sampledTransactions)
                {
                    int conflictCost = CalculateConflictCost(transaction, OptimizedTransactions.Last());

                    // Find the transaction with the smallest conflict cost in the sample
                    if (conflictCost < minConflictCost)
                    {
                        minConflictCost = conflictCost;
                        bestTransaction = transaction;
                    }
                }

                // Add the best transaction from the sample to the optimized schedule
                if (bestTransaction != null)
                {
                    OptimizedTransactions.Add(bestTransaction);
                    unscheduledTransactions.Remove(bestTransaction);
                }
            }

            return OptimizedTransactions;
        }

        public override int GetMakespan()
        {
            // Queue of operations, each transaction starts with its first operation
            var operationsQueue = OptimizedTransactions.Select(t => new Queue(t.Operations)).ToList();
            int makespan = 0;

            while (operationsQueue.Any(queue => queue.Count > 0)) // Continue until all queues are empty
            {
                var currentLayer = new Dictionary<Key, OperationType>(); // Track operations by key in the current layer

                // For each transaction, try to execute the next operation if possible
                foreach (var queue in operationsQueue)
                {
                    if (queue.Count == 0) continue; // Skip if the transaction has no remaining operations

                    var operation = (Operation)queue.Peek();
                    if (CanExecuteOperation(operation, currentLayer))
                    {
                        // Add operation to the current layer and dequeue it
                        currentLayer[operation.Key] = operation.Type;
                        queue.Dequeue();
                    }
                }

                // Increment makespan count for each new layer of parallelizable operations
                makespan++;
            }

            return makespan;
        }

        private int CalculateConflictCost(Transaction transaction, Transaction lastScheduledTransaction)
        {
            int cost = 0;

            foreach (var op in transaction.Operations)
            {
                // Check if there is a conflict with any operation in the last scheduled transaction
                if (lastScheduledTransaction.Operations.Any(sOp => sOp.Key == op.Key && sOp.Type == OperationType.Write))
                {
                    cost++; // Increase cost for each detected conflict
                }
            }

            return cost;
        }

        // Helper function to check if the current operation can execute in the current layer
        private bool CanExecuteOperation(Operation operation, Dictionary<Key, OperationType> currentLayer)
        {
            // If there's no operation on the same key in the current layer, it can execute
            if (!currentLayer.ContainsKey(operation.Key))
            {
                return true;
            }

            // If there is an operation on the same key, we check for conflicts
            var existingOperationType = currentLayer[operation.Key];

            // Rules:
            // - Reads on the same key can execute in parallel
            // - Writes cannot execute with other reads or writes on the same key in the same layer
            if (existingOperationType == OperationType.Read && operation.Type == OperationType.Read)
            {
                return true; // Both operations are reads on the same key; they can execute in parallel
            }

            // Any other combination (read-write or write-write) means they must be in separate layers
            return false;
        }
    }
}
