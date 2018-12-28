using FluentAssertions;
using System.Linq;
using Xunit;

namespace Mbc.Ads.Mapper.Test
{
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

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
            var data = new MockDataObject { Array1 = new[] { 0, 42, 0 } };
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
            var data = new MockDataObject
            {
                Array2 = new[,]
                {
                    { 0, 0, 0 },
                    { 0, 42, 0 },
                },
            };
            var member = typeof(MockDataObject).GetMember(nameof(MockDataObject.Array2)).First();
            var getter = DataObjectAccessor.CreateValueGetter<MockDataObject>(member, 4);

            // Act
            var value = getter(data);

            // Assert
            value.Should().Be(42);
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class MockDataObject
    {
        public int Primitive { get; set; }

        public int[] Array1 { get; set; } = new int[3];

        public int[,] Array2 { get; set; } = new int[2, 2];
    }
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
}
