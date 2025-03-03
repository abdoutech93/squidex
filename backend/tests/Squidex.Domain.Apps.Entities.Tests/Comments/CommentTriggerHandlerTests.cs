﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Rules.Triggers;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Events;
using Squidex.Domain.Apps.Events.Comments;
using Squidex.Domain.Apps.Events.Contents;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Shared.Users;

namespace Squidex.Domain.Apps.Entities.Comments;

public class CommentTriggerHandlerTests
{
    private readonly IScriptEngine scriptEngine = A.Fake<IScriptEngine>();
    private readonly IUserResolver userResolver = A.Fake<IUserResolver>();
    private readonly IRuleTriggerHandler sut;

    public CommentTriggerHandlerTests()
    {
        A.CallTo(() => scriptEngine.Evaluate(A<ScriptVars>._, "true", default))
            .Returns(true);

        A.CallTo(() => scriptEngine.Evaluate(A<ScriptVars>._, "false", default))
            .Returns(false);

        sut = new CommentTriggerHandler(scriptEngine, userResolver);
    }

    [Fact]
    public void Should_return_false_if_asking_for_snapshot_support()
    {
        Assert.False(sut.CanCreateSnapshotEvents);
    }

    [Fact]
    public void Should_handle_comment_event()
    {
        Assert.True(sut.Handles(new CommentCreated()));
    }

    [Fact]
    public void Should_not_handle_comment_update_event()
    {
        Assert.False(sut.Handles(new CommentUpdated()));
    }

    [Fact]
    public void Should_not_handle_other_event()
    {
        Assert.False(sut.Handles(new ContentCreated()));
    }

    [Fact]
    public async Task Should_create_enriched_events()
    {
        var ctx = Context().ToRulesContext();

        var user1 = UserMocks.User("1");
        var user2 = UserMocks.User("2");

        var users = new List<IUser> { user1, user2 };
        var userIds = users.Select(x => x.Id).ToArray();

        var @event = new CommentCreated { Mentions = userIds };
        var envelope = Envelope.Create<AppEvent>(@event);

        A.CallTo(() => userResolver.QueryManyAsync(userIds, default))
            .Returns(users.ToDictionary(x => x.Id));

        var actual = await sut.CreateEnrichedEventsAsync(envelope, ctx, default).ToListAsync();

        Assert.Equal(2, actual.Count);

        var enrichedEvent1 = actual[0] as EnrichedCommentEvent;
        var enrichedEvent2 = actual[1] as EnrichedCommentEvent;

        Assert.Equal(user1, enrichedEvent1!.MentionedUser);
        Assert.Equal(user2, enrichedEvent2!.MentionedUser);
        Assert.Equal("UserMentioned", enrichedEvent1.Name);
        Assert.Equal("UserMentioned", enrichedEvent2.Name);
    }

    [Fact]
    public async Task Should_not_create_enriched_events_if_users_cannot_be_resolved()
    {
        var ctx = Context().ToRulesContext();

        var user1 = UserMocks.User("1");
        var user2 = UserMocks.User("2");

        var users = new List<IUser> { user1, user2 };
        var userIds = users.Select(x => x.Id).ToArray();

        var @event = new CommentCreated { Mentions = userIds };
        var envelope = Envelope.Create<AppEvent>(@event);

        var actual = await sut.CreateEnrichedEventsAsync(envelope, ctx, default).ToListAsync();

        Assert.Empty(actual);
    }

    [Fact]
    public async Task Should_not_create_enriched_events_if_mentions_is_null()
    {
        var ctx = Context().ToRulesContext();

        var @event = new CommentCreated { Mentions = null };
        var envelope = Envelope.Create<AppEvent>(@event);

        var actual = await sut.CreateEnrichedEventsAsync(envelope, ctx, default).ToListAsync();

        Assert.Empty(actual);

        A.CallTo(() => userResolver.QueryManyAsync(A<string[]>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_not_create_enriched_events_if_mentions_is_empty()
    {
        var ctx = Context().ToRulesContext();

        var @event = new CommentCreated { Mentions = Array.Empty<string>() };
        var envelope = Envelope.Create<AppEvent>(@event);

        var actual = await sut.CreateEnrichedEventsAsync(envelope, ctx, default).ToListAsync();

        Assert.Empty(actual);

        A.CallTo(() => userResolver.QueryManyAsync(A<string[]>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public void Should_trigger_precheck_if_event_type_correct()
    {
        TestForCondition(string.Empty, ctx =>
        {
            var @event = new CommentCreated();

            var actual = sut.Trigger(Envelope.Create<AppEvent>(@event), ctx.Rule.Trigger);

            Assert.True(actual);
        });
    }

    [Fact]
    public void Should_trigger_check_if_condition_is_empty()
    {
        TestForCondition(string.Empty, ctx =>
        {
            var @event = new EnrichedCommentEvent();

            var actual = sut.Trigger(@event, ctx.Rule.Trigger);

            Assert.True(actual);
        });
    }

    [Fact]
    public void Should_trigger_check_if_condition_matchs()
    {
        TestForCondition("true", ctx =>
        {
            var @event = new EnrichedCommentEvent();

            var actual = sut.Trigger(new EnrichedCommentEvent(), ctx.Rule.Trigger);

            Assert.True(actual);
        });
    }

    [Fact]
    public void Should_not_trigger_check_if_condition_does_not_match()
    {
        TestForCondition("false", ctx =>
        {
            var @event = new EnrichedCommentEvent();

            var actual = sut.Trigger(@event, ctx.Rule.Trigger);

            Assert.False(actual);
        });
    }

    [Fact]
    public void Should_trigger_check_if_email_is_correct()
    {
        TestForRealCondition("event.mentionedUser.email == '1@email.com'", (handler, ctx) =>
        {
            var @event = new EnrichedCommentEvent
            {
                MentionedUser = UserMocks.User("1", "1@email.com")
            };

            var actual = handler.Trigger(@event, ctx.Rule.Trigger);

            Assert.True(actual);
        });
    }

    [Fact]
    public void Should_not_trigger_check_if_email_is_not_correct()
    {
        TestForRealCondition("event.mentionedUser.email == 'other@squidex.io'", (handler, ctx) =>
        {
            var @event = new EnrichedCommentEvent
            {
                MentionedUser = UserMocks.User("1", "1@email.com")
            };

            var actual = handler.Trigger(@event, ctx.Rule.Trigger);

            Assert.False(actual);
        });
    }

    [Fact]
    public void Should_trigger_check_if_text_is_urgent()
    {
        TestForRealCondition("event.text.indexOf('urgent') >= 0", (handler, ctx) =>
        {
            var @event = new EnrichedCommentEvent
            {
                Text = "very_urgent_text"
            };

            var actual = handler.Trigger(@event, ctx.Rule.Trigger);

            Assert.True(actual);
        });
    }

    [Fact]
    public void Should_not_trigger_check_if_text_is_not_urgent()
    {
        TestForRealCondition("event.text.indexOf('urgent') >= 0", (handler, ctx) =>
        {
            var @event = new EnrichedCommentEvent
            {
                Text = "just_gossip"
            };

            var actual = handler.Trigger(@event, ctx.Rule.Trigger);

            Assert.False(actual);
        });
    }

    private void TestForRealCondition(string condition, Action<IRuleTriggerHandler, RuleContext> action)
    {
        var trigger = new CommentTrigger
        {
            Condition = condition
        };

        var realScriptEngine =
            new JintScriptEngine(new MemoryCache(Options.Create(new MemoryCacheOptions())),
                Options.Create(new JintScriptOptions
                {
                    TimeoutScript = TimeSpan.FromSeconds(2),
                    TimeoutExecution = TimeSpan.FromSeconds(10)
                }));

        var handler = new CommentTriggerHandler(realScriptEngine, userResolver);

        action(handler, Context(trigger));
    }

    private void TestForCondition(string condition, Action<RuleContext> action)
    {
        var trigger = new CommentTrigger
        {
            Condition = condition
        };

        action(Context(trigger));

        if (string.IsNullOrWhiteSpace(condition))
        {
            A.CallTo(() => scriptEngine.Evaluate(A<ScriptVars>._, condition, default))
                .MustNotHaveHappened();
        }
        else
        {
            A.CallTo(() => scriptEngine.Evaluate(A<ScriptVars>._, condition, default))
                .MustHaveHappened();
        }
    }

    private static RuleContext Context(RuleTrigger? trigger = null)
    {
        trigger ??= new CommentTrigger();

        return new RuleContext
        {
            AppId = NamedId.Of(DomainId.NewGuid(), "my-app"),
            Rule = new Rule(trigger, A.Fake<RuleAction>()),
            RuleId = DomainId.NewGuid()
        };
    }
}
