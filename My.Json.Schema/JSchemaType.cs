using System;

namespace My.Json.Schema
{
    [Flags]
    public enum JSchemaType
    {
        None = 0,
        Array = 1, 
        Boolean = 2, 
        Integer = 4, 
        Number = 8,
        Null = 16,
        Object = 32,
        String = 64,
    }
}
