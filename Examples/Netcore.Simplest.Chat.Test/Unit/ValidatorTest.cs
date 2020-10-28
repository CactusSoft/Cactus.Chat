using System;
using Cactus.Chat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Netcore.Simplest.Chat.Test.Unit
{
    [TestClass]
    public class ValidatorTest
    {
        [TestMethod]
        public void NotNullTest()
        {
            Validate.NotNull(new object());
            Assert.ThrowsException<ArgumentException>(() => Validate.NotNull(null));
            Assert.ThrowsException<ArgumentException>(() => Validate.NotNull(null, "some message"));
        }

        [TestMethod]
        public void NotEmptyStringTest()
        {
            Validate.NotEmptyString("test");
            Assert.ThrowsException<ArgumentException>(() => Validate.NotEmptyString(null));
            Assert.ThrowsException<ArgumentException>(() => Validate.NotEmptyString("", "some message"));
        }

        [TestMethod]
        public void NotNullOrEmptyCollectionTest()
        {
            Validate.NotNullOrEmpty(new[] { new object() });
            Assert.ThrowsException<ArgumentException>(() => Validate.NotNullOrEmpty<object>(null));
            Assert.ThrowsException<ArgumentException>(() => Validate.NotNullOrEmpty(new object[] { }, "some message"));
        }
    }
}
