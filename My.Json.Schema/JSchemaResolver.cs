using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public abstract class JSchemaResolver
    {

        public abstract Stream GetSchemaResource(Uri newUri);
    }
}
