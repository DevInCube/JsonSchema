using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema.Utilities
{
    public static class JSchemaTypeHelpers
    {

        public static JSchemaType ParseType(string strType)
        {
            switch (strType)
            {                
                case ("array"): return JSchemaType.Array;
                case ("boolean"): return JSchemaType.Boolean;
                case ("integer"): return JSchemaType.Integer;
                case ("number"): return JSchemaType.Number;
                case ("null"): return JSchemaType.Null;
                case ("object"): return JSchemaType.Object;
                case ("string"): return JSchemaType.String;
                default: throw new JSchemaException();
            }    
        }

    }
}
