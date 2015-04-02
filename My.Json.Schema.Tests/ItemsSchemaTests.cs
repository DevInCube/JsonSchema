using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace My.Json.Schema.Tests
{
    [TestClass]
    public class ItemsSchemaTests
    {
        [TestMethod]
        public void ItemsSchema_ConstructWithOneSchema_IsSchemaAndMatches()
        {
            ItemsSchema items = new ItemsSchema(new JSchema());

            Assert.IsTrue(items.IsSchema);
            Assert.AreNotEqual(null, items.Schema);
            Assert.IsFalse(items.IsArray);
            Assert.AreEqual(null, items.Array);
        }

        [TestMethod]
        public void ItemsSchema_ConstructWithEmptyArray_IsEmptyArray()
        {
            ItemsSchema items = new ItemsSchema(new List<JSchema>());

            Assert.IsFalse(items.IsSchema);
            Assert.AreEqual(null, items.Schema);
            Assert.IsTrue(items.IsArray);
            Assert.AreNotEqual(null, items.Array);
            Assert.AreEqual(0, items.Array.Count);
        }
    }
}
