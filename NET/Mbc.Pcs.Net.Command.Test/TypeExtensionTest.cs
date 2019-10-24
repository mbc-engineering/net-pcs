using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Mbc.Pcs.Net.Command.Test
{
    public class TypeExtensionTest
    {
        public static IEnumerable<object[]> GetDataForGetDefaultValueTest()
        {
            yield return new object[] { typeof(byte), 0 };
            yield return new object[] { typeof(int), 0 };
            yield return new object[] { typeof(uint), 0 };
            yield return new object[] { typeof(float), 0.0f };
            yield return new object[] { typeof(double), 0.0 };
            yield return new object[] { typeof(TestEnum), TestEnum.None };
            yield return new object[] { typeof(string), string.Empty };
        }

        [Theory]
        [MemberData(nameof(GetDataForGetDefaultValueTest))]
        public void GetDefaultValueTest(Type type, object expecedValue)
        {
            // Arrange
            // Act
            var res = type.GetDefaultValue();

            // Assert
            res.Should().Be(expecedValue);
        }

        internal enum TestEnum
        {
            None,
            One,
            Two,
        }
    }
}
