using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Ads.Mapper.Test
{
    public class DataObjectAccessorTest
    {
        [Fact]
        public void SetPrimitiveProperty()
        {
            // Arrange
            var data = new MockDataObject();
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Primitive)).First();
            var setter = DataObjectAccessor.CreateValueSetter<MockDataObject>(member);

            // Act
            setter(data, 42);

            // Assert
            data.Primitive.Should().Be(42);
        }

        [Fact]
        public void SetOneDimArrayProperty()
        {
            // Arrange
            var data = new MockDataObject();
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Array1)).First();
            var setter = DataObjectAccessor.CreateValueSetter<MockDataObject>(member, 1);

            // Act
            setter(data, 42);

            // Assert
            data.Array1[1].Should().Be(42);
        }

        [Fact]
        public void SetTwoDimArrayProperty()
        {
            // Arrange
            var data = new MockDataObject();
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Array2)).First();
            var setter = DataObjectAccessor.CreateValueSetter<MockDataObject>(member, 2);

            // Act
            setter(data, 42);

            // Assert
            data.Array2[1, 0].Should().Be(42);
        }

        [Fact]
        public void GetPrimitiveProperty()
        {
            // Arrange
            var data = new MockDataObject { Primitive = 42 };
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Primitive)).First();
            var getter = DataObjectAccessor.CreateValueGetter<MockDataObject>(member);

            // Act
            var value = getter(data);

            // Assert
            value.Should().Be(42);
        }

        [Fact]
        public void GetOneDimArrayProperty()
        {
            // Arrange
            var data = new MockDataObject { Array1 = new [] { 0, 42, 0 } };
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Array1)).First();
            var getter = DataObjectAccessor.CreateValueGetter<MockDataObject>(member, 1);

            // Act
            var value = getter(data);

            // Assert
            value.Should().Be(42);
        }

        [Fact]
        public void GetTwoDimArrayProperty()
        {
            // Arrange
            var data = new MockDataObject { Array2 = new[,] { { 0, 0, 0 }, { 0, 42, 0 } } };
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Array2)).First();
            var getter = DataObjectAccessor.CreateValueGetter<MockDataObject>(member, 4);

            // Act
            var value = getter(data);

            // Assert
            value.Should().Be(42);
        }

    }

    internal class MockDataObject
    {
        public int Primitive { get; set; }

        public int[] Array1 { get; set; } = new int[3];

        public int[,] Array2 { get; set; } = new int[2, 2];
    }
}
