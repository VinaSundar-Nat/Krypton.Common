using System.Text.Json;

namespace Kr.Common.Extensions;

public static class JsonExtension
{
    public const string StreamCreateError = "Stream create content not supported.";
    public const string StreamReadError = "Stream read content not supported.";

    private static int bufferSize { get; } = 1024;

    public static void SerializeToJsonStream<T>(this Stream stream, T request,
                bool leaveOpen = true, bool reset = true) =>
                SerializeToJsonStream<T>(stream, request, bufferSize, leaveOpen, reset);

    public static void SerializeToJsonStream<T>(
       this Stream stream,
       T request,
       int bufferSize,
       bool leaveOpen,
       bool reset)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanWrite)
            throw new NotSupportedException(StreamCreateError);

        using (var bufferedStream = new BufferedStream(stream, bufferSize))
        using (var utf8JsonWriter = new Utf8JsonWriter(bufferedStream, new JsonWriterOptions { Indented = true }))
        {
            JsonSerializer.Serialize(utf8JsonWriter, request);
            utf8JsonWriter.Flush();
        }

        if (reset && stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        if (!leaveOpen)
        {
            stream.Close();
        }
    }

    public static T? ReadAndDeserializeFromJson<T>(this Stream stream) =>
          ReadAndDeserializeFromJson<T>(stream, true, bufferSize, false);

    public static T? ReadAndDeserializeFromJson<T>(
        this Stream stream,
        bool detectEncodingFactor,
        int bufferSize,
        bool leaveOpen)
    {
        try
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new NotSupportedException(StreamReadError);

            using var bufferStream = new BufferedStream(stream, bufferSize);
            using var memoryStream = new MemoryStream();
            bufferStream.CopyTo(memoryStream);
            var reader = new Utf8JsonReader(memoryStream.ToArray());
            return JsonSerializer.Deserialize<T>(ref reader);
        }
        finally
        {
            if (!leaveOpen)
            {
                stream.Dispose();
            }
        }
    }

    public static string ToJson(this object model, bool camelCase = true, bool formatting = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = formatting, 
            PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : default 
        };

        return JsonSerializer.Serialize(model, options);
    }

    public static T? ToModel<T>(this string source, bool camelCase = true, bool formatting = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = formatting, 
            PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : default 
        };

        return JsonSerializer.Deserialize<T>(source, options);
    }
}


