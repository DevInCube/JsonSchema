using System;
using System.Collections.Generic;

namespace My.Json.Schema;

internal sealed class UriComparer : IEqualityComparer<Uri>
{
    public static readonly UriComparer Instance = new();

    private UriComparer() { }

    public bool Equals(Uri x, Uri y)
    {
        if (x != y)
        {
            return false;
        }

        return !x.IsAbsoluteUri
            ? string.Equals(x.OriginalString, y.OriginalString, StringComparison.Ordinal)
            : string.Equals(x.Fragment, y.Fragment, StringComparison.Ordinal);
    }

    public int GetHashCode(Uri obj)
    {
        if (!obj.IsAbsoluteUri || string.IsNullOrEmpty(obj.Fragment))
        {
            return obj.GetHashCode();
        }

        return obj.GetHashCode() ^ obj.Fragment.GetHashCode();
    }
}
