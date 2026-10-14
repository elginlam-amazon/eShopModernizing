using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Serializes objects to/from a stream.
    /// </summary>
    /// <remarks>
    /// Previously backed by BinaryFormatter, which is insecure and has been removed from .NET 9+
    /// (it always throws PlatformNotSupportedException). The payload is now UTF-8 JSON produced by
    /// System.Text.Json on every target framework, so .NET Framework (netstandard2.0) and .NET 10
    /// consumers exchange the same format. Method names are kept for source compatibility.
    /// </remarks>
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
        /// Deserializes the stream without type information; returns a <see cref="JsonElement"/>.
        /// Prefer <see cref="DeserializeBinary{T}(Stream)"/> when the target type is known.
        /// </summary>
        public object DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<JsonElement>(stream);
        }

        public T DeserializeBinary<T>(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<T>(stream);
        }
    }
}
