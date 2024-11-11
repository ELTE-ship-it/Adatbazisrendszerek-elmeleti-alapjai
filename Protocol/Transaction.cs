
using System.Collections.Generic;
using System.Linq;

namespace Protocol
{
    public class Transaction
    {
        public Transaction(int id)
        {
            Id = id;
            Operations = new List<Operation>();
            ConflictCost = null;
        }

        public int Id { get; set; }
        public List<Operation> Operations { get; set; }
        public int? ConflictCost { get; set; }

        public override string ToString()
        {
            var ops = Operations.Select(op => op.ToString()).ToList();
            var text = $"T{Id}(";
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                text += op.ToString();
                if (i < ops.Count - 1)
                {
                    text += " -> ";
                }
                else text += ")";
            }

            return text;
        }
    }    
}
