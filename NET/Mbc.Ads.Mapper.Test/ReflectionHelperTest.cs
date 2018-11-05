using FluentAssertions;
using System;
using System.Linq.Expressions;
using Xunit;

namespace Mbc.Ads.Mapper.Test
{
    public class ReflectionHelperTest
    {
        [Fact]
        public void MemberInfoForSimplePropertyAccess()
        {
            // Arrange
            var mock = new MockObject();

            // Act
            Expression<Func<MockObject, string>> act = (x) => x.Foo;
            var memberInfo = ReflectionHelper.FindProperty(act);

            // Assert
            memberInfo.Should().BeSameAs(typeof(MockObject).GetProperty(nameof(MockObject.Foo)));
        }

        [Fact]
        public void MemberInfoForCastedPropertyAccess()
        {
            // Arrange
            var mock = new MockObject();

            // Act
            Expression<Func<object, string>> act = (x) => ((MockObject)x).Foo;
            var memberInfo = ReflectionHelper.FindProperty(act);

            // Assert
            memberInfo.Should().BeSameAs(typeof(MockObject).GetProperty(nameof(MockObject.Foo)));
        }

        internal class MockObject
        {
            public string Foo { get; set; }
        }
    }
}
