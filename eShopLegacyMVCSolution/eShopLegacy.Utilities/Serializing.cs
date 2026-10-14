using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Serializes objects to/from a stream.
    /// BinaryFormatter was removed from .NET 9+ (it always throws PlatformNotSupportedException) and is
    /// insecure, so the payload is now UTF-8 JSON produced by System.Text.Json on every target framework.
    /// The method names are kept for source compatibility with existing callers.
    /// </summary>
    public class Serializing
    {
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, input?.GetType() ?? typeof(object));
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// Deserializes a payload produced by <see cref="SerializeBinary"/>.
        /// JSON carries no type information, so the result is a <see cref="JsonElement"/>;
        /// use <see cref="DeserializeBinary{T}"/> when the target type is known.
        /// </summary>
        public object DeserializeBinary(Stream stream)
        {
            return DeserializeBinary<JsonElement>(stream);
        }

        public T DeserializeBinary<T>(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<T>(stream);
        }
    }
}
