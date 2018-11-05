using EnsureThat;
using Optional;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Represents a member mapping configuration between a <see cref="ISourceMemberConfiguration"/>
    /// and a <see cref="IDestinationMemberConfiguration"/>.
    /// <para>This class is immutable.</para>
    /// </summary>
    internal class MemberMappingConfiguration
    {
        internal MemberMappingConfiguration(ISourceMemberConfiguration sourceMemberConfiguration, Option<IDestinationMemberConfiguration> destinationMemberConfiguration)
        {
            EnsureArg.IsNotNull(sourceMemberConfiguration, nameof(sourceMemberConfiguration));

            Source = sourceMemberConfiguration;
            Destination = destinationMemberConfiguration;
        }

        internal ISourceMemberConfiguration Source { get; }

        internal Option<IDestinationMemberConfiguration> Destination { get; }
    }
}
