using System.IO;
#if NETSTANDARD2_0
using System.Runtime.Serialization.Formatters.Binary;
#else
using System.Text.Json;
#endif

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Serializes objects to and from a stream.
    /// On netstandard2.0 (consumed by .NET Framework callers) the legacy BinaryFormatter
    /// wire format is kept. BinaryFormatter was removed from .NET 9+, so on modern .NET
    /// the payload is written as UTF-8 JSON with System.Text.Json instead.
    /// </summary>
    public class Serializing
    {
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
#if NETSTANDARD2_0
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, input);
#else
            JsonSerializer.Serialize(stream, input, input?.GetType() ?? typeof(object));
#endif
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
#if NETSTANDARD2_0
            var binaryFormatter = new BinaryFormatter();
            return binaryFormatter.Deserialize(stream);
#else
            // Without type information in the payload the result is a JsonElement.
            return JsonSerializer.Deserialize<JsonElement>(stream);
#endif
        }
    }
}
