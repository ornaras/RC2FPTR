using RetailCorrector;
using System.Text.Json.Serialization;

namespace RC2FptrScript
{
    [JsonSerializable(typeof(Receipt[]))]
    public partial class ReceiptSerializationContext : JsonSerializerContext { }
}
