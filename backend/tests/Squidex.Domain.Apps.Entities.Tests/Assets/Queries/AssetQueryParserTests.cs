﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Domain.Apps.Core.Tags;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Infrastructure.Queries;
using Squidex.Infrastructure.Validation;

namespace Squidex.Domain.Apps.Entities.Assets.Queries;

public class AssetQueryParserTests : GivenContext
{
    private readonly ITagService tagService = A.Fake<ITagService>();
    private readonly AssetQueryParser sut;

    public AssetQueryParserTests()
    {
        var options = Options.Create(new AssetOptions { DefaultPageSize = 30 });

        sut = new AssetQueryParser(TestUtils.DefaultSerializer, tagService, options);
    }

    [Fact]
    public async Task Should_skip_total_if_set_in_context()
    {
        var q = await sut.ParseAsync(ApiContext.Clone(b => b.WithoutTotal()), Q.Empty, CancellationToken);

        Assert.True(q.NoTotal);
    }

    [Fact]
    public async Task Should_throw_if_odata_query_is_invalid()
    {
        var query = Q.Empty.WithODataQuery("$filter=invalid");

        await Assert.ThrowsAsync<ValidationException>(() => sut.ParseAsync(ApiContext, query, CancellationToken));
    }

    [Fact]
    public async Task Should_throw_if_json_query_is_invalid()
    {
        var query = Q.Empty.WithJsonQuery("invalid");

        await Assert.ThrowsAsync<ValidationException>(() => sut.ParseAsync(ApiContext, query, CancellationToken));
    }

    [Fact]
    public async Task Should_parse_odata_query()
    {
        var query = Q.Empty.WithODataQuery("$top=100&$orderby=fileName asc&$search=Hello World");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("FullText: 'Hello World'; Take: 100; Sort: fileName Ascending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_parse_odata_query_and_enrich_with_defaults()
    {
        var query = Q.Empty.WithODataQuery("$top=200&$filter=fileName eq 'ABC'");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Filter: fileName == 'ABC'; Take: 200; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_parse_json_query_and_enrich_with_defaults()
    {
        var query = Q.Empty.WithJsonQuery("{ \"filter\": { \"path\": \"fileName\", \"op\": \"eq\", \"value\": \"ABC\" } }");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Filter: fileName == 'ABC'; Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_parse_json_full_text_query_and_enrich_with_defaults()
    {
        var query = Q.Empty.WithJsonQuery("{ \"fullText\": \"Hello\" }");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("FullText: 'Hello'; Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public async Task Should_apply_default_take_size_if_not_defined(long take)
    {
        var query = Q.Empty.WithQuery(new ClrQuery { Take = take });

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_set_take_to_ids_count_if_take_not_defined()
    {
        var query = Q.Empty.WithIds("1, 2, 3");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Take: 3; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_not_set_take_to_ids_count_if_take_defined()
    {
        var query = Q.Empty.WithIds("1, 2, 3").WithQuery(new ClrQuery { Take = 20 });

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Take: 20; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_apply_default_take_limit()
    {
        var query = Q.Empty.WithODataQuery("$top=300&$skip=20");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Skip: 20; Take: 200; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_not_apply_id_ordering_twice()
    {
        var query = Q.Empty.WithODataQuery("$top=300&$skip=20&$orderby=id desc");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Skip: 20; Take: 200; Sort: id Descending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_denormalize_tags()
    {
        A.CallTo(() => tagService.GetTagIdsAsync(AppId.Id, TagGroups.Assets, A<HashSet<string>>.That.Contains("name1"), CancellationToken))
            .Returns(new Dictionary<string, string> { ["name1"] = "id1" });

        var query = Q.Empty.WithODataQuery("$filter=tags eq 'name1'");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Filter: tags == 'id1'; Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_not_fail_if_tags_not_found()
    {
        A.CallTo(() => tagService.GetTagIdsAsync(AppId.Id, TagGroups.Assets, A<HashSet<string>>.That.Contains("name1"), CancellationToken))
            .Returns(new Dictionary<string, string>());

        var query = Q.Empty.WithODataQuery("$filter=tags eq 'name1'");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Filter: tags == 'name1'; Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());
    }

    [Fact]
    public async Task Should_not_normalize_other_field()
    {
        var query = Q.Empty.WithODataQuery("$filter=fileSize eq 123");

        var q = await sut.ParseAsync(ApiContext, query, CancellationToken);

        Assert.Equal("Filter: fileSize == 123; Take: 30; Sort: lastModified Descending, id Ascending", q.Query.ToString());

        A.CallTo(() => tagService.GetTagIdsAsync(AppId.Id, A<string>._, A<HashSet<string>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
