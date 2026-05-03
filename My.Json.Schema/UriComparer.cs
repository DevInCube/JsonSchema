using System;
using System.Collections.Generic;

namespace My.Json.Schema;

internal sealed class UriComparer : IEqualityComparer<Uri>
{
    public static readonly UriComparer Instance = new();

    private UriComparer() { }

    public bool Equals(Uri x, Uri y)
    {
        return string.Equals(x?.OriginalString, y?.OriginalString, StringComparison.Ordinal);
    }

    public int GetHashCode(Uri obj)
    {
        return obj.OriginalString.GetHashCode(StringComparison.Ordinal);
    }
}
