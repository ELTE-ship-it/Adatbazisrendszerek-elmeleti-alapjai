
using Protocol.Enum;

namespace Protocol
{
    public class Operation
    {
        public Operation(OperationType type, Key key)
        {
            Type = type;
            Key = key;
        }

        public OperationType Type {  get; set; }
        public Key Key { get; set; }

        public override string ToString()
        {
            return $"{(Type == OperationType.Read ? "r" : "w")}({Key.ToString().ToLower()})";
        }
    }
}
