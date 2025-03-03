﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NodaTime;
using Squidex.Areas.Api.Controllers.Contents.Generator;
using Squidex.Areas.Api.Controllers.News;
using Squidex.Areas.Api.Controllers.News.Service;
using Squidex.Areas.Api.Controllers.UI;
using Squidex.Caching;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Domain.Apps.Core.Scripting.Extensions;
using Squidex.Domain.Apps.Core.Tags;
using Squidex.Domain.Apps.Core.Templates;
using Squidex.Domain.Apps.Core.Templates.Extensions;
using Squidex.Domain.Apps.Entities.Contents.Counter;
using Squidex.Domain.Apps.Entities.Tags;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Diagnostics;
using Squidex.Infrastructure.Log;
using Squidex.Infrastructure.Translations;
using Squidex.Infrastructure.UsageTracking;
using Squidex.Pipeline.Robots;
using Squidex.Shared;
using Squidex.Text.Translations;
using Squidex.Text.Translations.GoogleCloud;
using Squidex.Web;
using Squidex.Web.Pipeline;

namespace Squidex.Config.Domain;

public static class InfrastructureServices
{
    public static void AddSquidexInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ExposedConfiguration>(config,
            "exposedConfiguration");

        services.Configure<ReplicatedCacheOptions>(config,
            "caching:replicated");

        services.Configure<JintScriptOptions>(config,
            "scripting");

        services.Configure<DiagnoserOptions>(config,
            "diagnostics");

        services.AddHttpClient("Jint");

        services.AddReplicatedCache();
        services.AddAsyncLocalCache();
        services.AddBackgroundCache();

        services.AddSingletonAs(_ => SystemClock.Instance)
            .As<IClock>();

        services.AddSingletonAs<BackgroundRequestLogStore>()
            .AsOptional<IRequestLogStore>();

        services.AddSingletonAs<Diagnoser>()
            .AsSelf();

        services.AddSingletonAs<ScriptingCompleter>()
            .AsSelf();

        services.AddSingletonAs<JintScriptEngine>()
            .As<IScriptEngine>().As<IScriptDescriptor>();

        services.AddSingletonAs<TagService>()
            .As<ITagService>();

        services.AddSingletonAs<CounterJintExtension>()
            .As<IJintExtension>().As<IScriptDescriptor>();

        services.AddSingletonAs<DateTimeJintExtension>()
            .As<IJintExtension>().As<IScriptDescriptor>();

        services.AddSingletonAs<StringJintExtension>()
            .As<IJintExtension>().As<IScriptDescriptor>();

        services.AddSingletonAs<StringWordsJintExtension>()
            .As<IJintExtension>().As<IScriptDescriptor>();

        services.AddSingletonAs<HttpJintExtension>()
            .As<IJintExtension>().As<IScriptDescriptor>();

        services.AddSingletonAs<FluidTemplateEngine>()
            .AsOptional<ITemplateEngine>();

        services.AddSingletonAs<ContentFluidExtension>()
            .As<IFluidExtension>();

        services.AddSingletonAs<DateTimeFluidExtension>()
            .As<IFluidExtension>();

        services.AddSingletonAs<StringFluidExtension>()
            .As<IFluidExtension>();

        services.AddSingletonAs<StringWordsFluidExtension>()
            .As<IFluidExtension>();

        services.AddSingletonAs<UserFluidExtension>()
            .As<IFluidExtension>();
    }

    public static void AddSquidexUsageTracking(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<UsageOptions>(config,
            "usage");

        services.AddSingletonAs(c => new CachingUsageTracker(
                c.GetRequiredService<BackgroundUsageTracker>(),
                c.GetRequiredService<IMemoryCache>()))
            .As<IUsageTracker>();

        services.AddSingletonAs<ApiUsageTracker>()
            .As<IApiUsageTracker>();

        services.AddSingletonAs<BackgroundUsageTracker>()
            .AsSelf();
    }

    public static void AddSquidexTranslation(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GoogleCloudTranslationOptions>(config,
            "translations:googleCloud");

        services.Configure<DeepLOptions>(config,
            "translations:deepL");

        services.Configure<LanguagesOptions>(config,
            "languages");

        services.AddSingletonAs<LanguagesInitializer>()
            .AsSelf();

        services.AddSingletonAs(c => new DeepLTranslationService(c.GetRequiredService<IOptions<DeepLOptions>>().Value))
            .As<ITranslationService>();

        services.AddSingletonAs(c => new GoogleCloudTranslationService(c.GetRequiredService<IOptions<GoogleCloudTranslationOptions>>().Value))
            .As<ITranslationService>();

        services.AddSingletonAs<Translator>()
            .As<ITranslator>();
    }

    public static void AddSquidexLocalization(this IServiceCollection services)
    {
        var translator = new ResourcesLocalizer(Texts.ResourceManager);

        T.Setup(translator);

        services.AddSingletonAs(c => translator)
            .As<ILocalizer>();
    }

    public static void AddSquidexControllerServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RobotsTxtOptions>(config,
            "robots");

        services.Configure<CachingOptions>(config,
            "caching");

        services.Configure<MyUIOptions>(config,
            "ui");

        services.Configure<MyNewsOptions>(config,
            "news");

        services.AddSingletonAs<FeaturesService>()
            .AsSelf();

        services.AddSingletonAs<SchemasOpenApiGenerator>()
            .AsSelf();
    }
}
