using FakeItEasy;
using FluentAssertions;
using Mbc.Ads.Utils.Connection;
using System.Threading;
using TwinCAT.Ads;
using Xunit;

namespace Mbc.Ads.Utils.Test.Connection
{
    public class ConnectionSynchronizationTest
    {
        [Fact]
        public void MethodCallIsDispatched()
        {
            // Arrange
            var proxiedConnection = A.Fake<IAdsConnection>();
            var proxyConnection = ConnectionSynchronization.MakeSynchronized(proxiedConnection);

            // Act
            proxyConnection.Connect();

            // Assert
            A.CallTo(() => proxiedConnection.Connect()).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void MethodCallIsLocked()
        {
            // Arrange
            var proxiedConnection = A.Fake<IAdsConnection>();
            var proxyConnection = ConnectionSynchronization.MakeSynchronized(proxiedConnection);
            var synchronizationInstance = ConnectionSynchronization.GetSynchronizationInstance(proxyConnection);

            var methodCalledEvent = new CountdownEvent(1);
            var methodContinueEvent = new CountdownEvent(1);

            bool wasLocked = false;
            A.CallTo(() => proxiedConnection.Connect())
                .Invokes(() =>
                {
                    wasLocked = synchronizationInstance.IsLocked;
                });

            // Act
            proxyConnection.Connect();

            // Assert
            wasLocked.Should().BeTrue();
        }
    }
}
