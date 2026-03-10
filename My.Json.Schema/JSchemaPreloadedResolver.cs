using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace My.Json.Schema;

public class JSchemaPreloadedResolver : JSchemaResolver
{
    private readonly Dictionary<Uri, byte[]> _preloadedData;
    private readonly JSchemaResolver _resolver;

    public IEnumerable<Uri> PreloadedUris => _preloadedData.Keys;

    public JSchemaPreloadedResolver(JSchemaResolver resolver)
        : this()
    {
        _resolver = resolver;
    }

    public JSchemaPreloadedResolver()
    {
        _preloadedData = [];
    }

    public override Stream GetSchemaResource(Uri newUri)
    {
        if (_preloadedData.TryGetValue(newUri, out byte[] data))
        {
            return new MemoryStream(data);
        }

        return _resolver?.GetSchemaResource(newUri);
    }

    public void Add(Uri uri, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(uri);

        ArgumentNullException.ThrowIfNull(value);

        _preloadedData[uri] = value;
    }

    public void Add(Uri uri, Stream value)
    {
        ArgumentNullException.ThrowIfNull(value);

        MemoryStream ms = new();
        value.CopyTo(ms);

        Add(uri, ms.ToArray());
    }

    public void Add(Uri uri, string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value);

        Add(uri, data);
    }
}
