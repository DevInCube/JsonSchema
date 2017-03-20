using System;
using System.IO;

namespace My.Json.Schema
{
    public abstract class JSchemaResolver
    {

        public abstract Stream GetSchemaResource(Uri newUri);
    }
}
