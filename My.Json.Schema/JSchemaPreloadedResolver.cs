using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace My.Json.Schema
{
    public class JSchemaPreloadedResolver : JSchemaResolver
    {

        private readonly Dictionary<Uri, byte[]> _preloadedData;
        private readonly JSchemaResolver _resolver;

        public IEnumerable<Uri> PreloadedUris
        {
            get { return _preloadedData.Keys; }
        }

        public JSchemaPreloadedResolver(JSchemaResolver resolver)
            : this()
        {
            _resolver = resolver;
        }

        public JSchemaPreloadedResolver()
        {
            _preloadedData = new Dictionary<Uri, byte[]>();
        }

        public override Stream GetSchemaResource(Uri uri)
        {
            byte[] data;
            if (_preloadedData.TryGetValue(uri, out data))
                return new MemoryStream(data);

            return _resolver != null 
                ? _resolver.GetSchemaResource(uri) 
                : null;
        }
      
        public void Add(Uri uri, byte[] value)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _preloadedData[uri] = value;
        }
      
        public void Add(Uri uri, Stream value)
        {
            MemoryStream ms = new MemoryStream();
            value.CopyTo(ms);

            Add(uri, ms.ToArray());
        }

        public void Add(Uri uri, string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);

            Add(uri, data);
        }
    }
}
