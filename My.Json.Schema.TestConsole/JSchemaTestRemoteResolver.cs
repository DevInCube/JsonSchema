using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace My.Json.Schema.TestConsole
{
    class JSchemaTestRemoteResolver : JSchemaResolver
    {

        private string remoteHost;
        private string remoteDirectory;

        public JSchemaTestRemoteResolver(string remoteHost, string remoteDirectory)
        {
            this.remoteHost = remoteHost;
            this.remoteDirectory = remoteDirectory;
        }

        public override Stream GetSchemaResource(Uri newUri)
        {
            if (newUri == null) throw new ArgumentNullException("uri");

            string content = File.ReadAllText(remoteDirectory + newUri.AbsolutePath);
            return new MemoryStream(Encoding.UTF8.GetBytes(content));            
        }
    }
}
