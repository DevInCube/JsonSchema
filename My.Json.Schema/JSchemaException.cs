using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public class JSchemaException : Exception
    {

        public JSchemaException() { }

        public JSchemaException(string p) : base(p) { }
    }
}
