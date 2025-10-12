using System.Collections.Generic;
using NUnit.Framework;
using Prima.Extensions;

namespace Prima.Tests
{
    [TestFixture]
    public class ListExtensionsTests
    {
        [Test]
        public void Remove_Predicate_Works()
        {
            var list = new List<int> { 0, 1, 2, 2, 3, 3, 4, 5, 4, 2, 1 };
            var outList = new List<int> { 0, 1, 2, 3, 3, 4, 5, 4, 2, 1 };
            list.Remove(i => i == 2);
            for (var i = 0; i < outList.Count; i++)
            {
                Assert.That(outList[i], Is.EqualTo(list[i]));
            }
        }

        [Test]
        public void RemoveAll_Predicate_Works()
        {
            var list = new List<int> { 0, 1, 2, 2, 3, 3, 4, 5, 4, 2, 1 };
            var outList = new List<int> { 0, 1, 3, 3, 4, 5, 4, 1 };
            list.RemoveAll(i => i == 2);
            for (var i = 0; i < outList.Count; i++)
            {
                Assert.That(outList[i], Is.EqualTo(list[i]));
            }
        }

        [Test]
        public void IndexOf_Predicate_Works()
        {
            var list = new List<int> { 0, 1, 2, 2, 3, 3, 4, 5, 4, 2, 1 };
            Assert.That(list.IndexOf(i => i == 1), Is.EqualTo(1));
        }
    }
}