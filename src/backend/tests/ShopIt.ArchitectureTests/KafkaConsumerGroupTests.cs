using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Identity.Persistence.Data;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the per-service Kafka consumer group.
/// </summary>
/// <remarks>
/// <para>
/// Kafka delivers each partition of a topic to exactly one consumer <em>within</em> a group, so two
/// services sharing a group id each receive only part of a topic's messages — split by partition
/// assignment, decided at runtime, with no error. The repository did exactly that until each service
/// was given its own group.
/// </para>
/// <para>
/// These tests pin the two halves of the fix: there is no shared default to inherit, and registering
/// the inbox without naming a group is impossible.
/// </para>
/// </remarks>
public class KafkaConsumerGroupTests
{
    [Fact]
    public void Inbox_options_have_no_default_group_to_inherit()
    {
        // A default — however it is spelled — is the bug: every service that does not think about the
        // group id would join the same one.
        Assert.Equal(string.Empty, new InboxOptions().ConsumerGroupId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registering_the_inbox_without_a_group_is_rejected(string consumerGroupId)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddKafkaIntegration<ApplicationDbContext>(consumerGroupId, InboxSubscription.None));
    }

    [Fact]
    public void Registering_the_inbox_with_no_group_at_all_does_not_compile_away_silently()
    {
        // The parameter is required; this asserts the guard covers the null case as well as the empty one.
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddKafkaIntegration<ApplicationDbContext>(consumerGroupId: null!, InboxSubscription.None));
    }
}
