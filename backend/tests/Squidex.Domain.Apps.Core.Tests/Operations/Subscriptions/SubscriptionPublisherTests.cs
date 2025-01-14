﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Subscriptions;
using Squidex.Domain.Apps.Events.Apps;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Messaging.Subscriptions;

namespace Squidex.Domain.Apps.Core.Operations.Subscriptions;

public class SubscriptionPublisherTests
{
    private readonly ISubscriptionService subscriptionService = A.Fake<ISubscriptionService>();
    private readonly SubscriptionPublisher sut;

    private sealed class MyEvent : IEvent
    {
    }

    public SubscriptionPublisherTests()
    {
        sut = new SubscriptionPublisher(subscriptionService, Enumerable.Empty<ISubscriptionEventCreator>());
    }

    [Fact]
    public void Should_return_content_and_asset_filter_for_events_filter()
    {
        Assert.Equal("^(content-|asset-)", sut.EventsFilter);
    }

    [Fact]
    public async Task Should_do_nothing_on_clear()
    {
        await ((IEventConsumer)sut).ClearAsync();
    }

    [Fact]
    public void Should_return_custom_name_for_name()
    {
        Assert.Equal("Subscriptions", sut.Name);
    }

    [Fact]
    public void Should_not_support_clear()
    {
        Assert.False(sut.CanClear);
    }

    [Fact]
    public void Should_start_from_latest()
    {
        Assert.True(sut.StartLatest);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_handle_events_when_subscription_exists(bool hasSubscriptions)
    {
        A.CallTo(() => subscriptionService.HasSubscriptions)
            .Returns(hasSubscriptions);

        IEventConsumer consumer = sut;

        Assert.Equal(hasSubscriptions, consumer.Handles(null!));
    }

    [Fact]
    public async Task Should_not_publish_if_not_app_event()
    {
        var envelope = Envelope.Create(new MyEvent());

        await sut.On(envelope);

        A.CallTo(subscriptionService)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_publish_app_event()
    {
        var envelope = Envelope.Create(new AppCreated());

        await sut.On(envelope);

        A.CallTo(subscriptionService).Where(x => x.Method.Name.StartsWith("Publish"))
            .MustHaveHappened();
    }
}
