using System;
using System.Collections.Generic;

namespace My.Json.Schema
{
    internal class UriComparer : IEqualityComparer<Uri>
    {

        public static readonly UriComparer Instance = new UriComparer();

        private UriComparer() { }

        public bool Equals(Uri x, Uri y)
        {
            if (x != y)
                return false;

            return !x.IsAbsoluteUri 
                ? String.Equals(x.OriginalString, y.OriginalString, StringComparison.Ordinal) 
                : String.Equals(x.Fragment, y.Fragment, StringComparison.Ordinal);
        }

        public int GetHashCode(Uri obj)
        {
            if (!obj.IsAbsoluteUri || String.IsNullOrEmpty(obj.Fragment))
                return obj.GetHashCode();

            return obj.GetHashCode() ^ obj.Fragment.GetHashCode();
        }
    }
}
