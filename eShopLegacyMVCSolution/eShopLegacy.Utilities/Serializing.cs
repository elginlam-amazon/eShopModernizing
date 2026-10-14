using System.IO;
#if NETSTANDARD2_0
using System.Runtime.Serialization.Formatters.Binary;
#else
using System.Text.Json;
#endif

namespace eShopLegacy.Utilities
{
    public class Serializing
    {
#if NETSTANDARD2_0
        // .NET Framework consumers keep the original BinaryFormatter behaviour.
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, input);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object DeserializeBinary(Stream stream)
        {
            var binaryFormatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            return binaryFormatter.Deserialize(stream);
        }
#else
        // BinaryFormatter has been removed from .NET 9+ (always throws PlatformNotSupportedException),
        // so on modern .NET the payload is serialized as UTF-8 JSON using System.Text.Json.
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, input?.GetType() ?? typeof(object));
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<JsonElement>(stream);
        }
#endif
    }
}
