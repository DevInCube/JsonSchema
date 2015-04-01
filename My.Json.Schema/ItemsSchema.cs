using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public class ItemsSchema
    {
        public bool IsSchema { get { return Schema != null; } }
        public bool IsArray { get { return Array != null; } }
        public JSchema Schema { get; set; }
        public IList<JSchema> Array { get; set; }

        public ItemsSchema()
        {
            //
        }
        
    }
}
