//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FluentAssertions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        [Theory]
        [InlineData(nameof(MockObject.Foo), MemberTypes.Property)]
        [InlineData(nameof(MockObject.Bar), MemberTypes.Field)]
        public void GetSettableTypeOfMember(string memberName, MemberTypes memberType)
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(memberName).First();

            // Act
            var dataType = memberInfo.GetSettableDataType();

            // Assert
            memberInfo.MemberType.Should().Be(memberType);
            dataType.Should().BeSameAs(typeof(string));
        }

        [Fact]
        public void SetValueOnField()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Bar)).First();
            var data = new MockObject();

            // Act
            memberInfo.SetValue(data, "this is bar");

            // Assert
            data.Bar.Should().Be("this is bar");
        }

        [Fact]
        public void SetValueOnProperty()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Foo)).First();
            var data = new MockObject();

            // Act
            memberInfo.SetValue(data, "this is foo");

            // Assert
            data.Foo.Should().Be("this is foo");
        }

        [Fact]
        public void GetValueOnField()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Bar)).First();
            var data = new MockObject { Bar = "bar" };

            // Act
            var value = memberInfo.GetValue(data);

            // Assert
            value.Should().Be("bar");
        }

        [Fact]
        public void GetValueOnProperty()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Foo)).First();
            var data = new MockObject { Foo = "foo" };

            // Act
            var value = memberInfo.GetValue(data);

            // Assert
            value.Should().Be("foo");
        }

        [Fact]
        public void GetElementTypeOnPrimitive()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Foo)).First();

            // Act
            var type = memberInfo.GetElementType();

            // Assert
            type.Should().BeSameAs(typeof(string));
        }

        [Fact]
        public void GetElementTypeOnArray()
        {
            // Arrange
            MemberInfo memberInfo = typeof(MockObject).GetMember(nameof(MockObject.Baz)).First();

            // Act
            var type = memberInfo.GetElementType();

            // Assert
            type.Should().BeSameAs(typeof(string));
        }

        internal class MockObject
        {
            public string Foo { get; set; }

            public string Bar;

            public string[] Baz { get; set; }
        }
    }
}
