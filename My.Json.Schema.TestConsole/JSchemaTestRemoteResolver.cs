using System;
using System.IO;
using System.Text;

namespace My.Json.Schema.TestConsole;

internal class JSchemaTestRemoteResolver : JSchemaResolver
{

    private string _remoteHost;
    private readonly string _remoteDirectory;

    public JSchemaTestRemoteResolver(string remoteHost, string remoteDirectory)
    {
        _remoteHost = remoteHost;
        _remoteDirectory = remoteDirectory;
    }

    public override Stream GetSchemaResource(Uri newUri)
    {
        ArgumentNullException.ThrowIfNull(newUri);

        string content = File.ReadAllText(_remoteDirectory + newUri.AbsolutePath);
        return new MemoryStream(Encoding.UTF8.GetBytes(content));            
    }
}
