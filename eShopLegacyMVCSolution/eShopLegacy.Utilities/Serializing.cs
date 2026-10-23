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
        // .NET Framework consumers (netstandard2.0 leg) keep the original BinaryFormatter behaviour.
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
        // BinaryFormatter was removed from the runtime in .NET 9+ (always throws PlatformNotSupportedException),
        // so modern .NET serializes with System.Text.Json (UTF-8 JSON payload) instead.
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
