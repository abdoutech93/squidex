﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Collections;
using Squidex.Infrastructure.Reflection;
using Squidex.Web;

namespace Squidex.Areas.Api.Controllers.Schemas.Models.Fields;

[OpenApiRequest]
public sealed class ReferencesFieldPropertiesDto : FieldPropertiesDto
{
    /// <summary>
    /// The language specific default value as a list of content ids.
    /// </summary>
    public LocalizedValue<ReadonlyList<string>?>? DefaultValues { get; set; }

    /// <summary>
    /// The default value as a list of content ids.
    /// </summary>
    public ReadonlyList<string>? DefaultValue { get; set; }

    /// <summary>
    /// The minimum allowed items for the field value.
    /// </summary>
    public int? MinItems { get; set; }

    /// <summary>
    /// The maximum allowed items for the field value.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// True, if duplicate values are allowed.
    /// </summary>
    public bool AllowDuplicates { get; set; }

    /// <summary>
    /// True to resolve references in the content list.
    /// </summary>
    public bool ResolveReference { get; set; }

    /// <summary>
    /// True when all references must be published.
    /// </summary>
    public bool MustBePublished { get; set; }

    /// <summary>
    /// The editor that is used to manage this field.
    /// </summary>
    public ReferencesFieldEditor Editor { get; set; }

    /// <summary>
    /// The ID of the referenced schemas.
    /// </summary>
    public ReadonlyList<DomainId>? SchemaIds { get; set; }

    public static ReferencesFieldPropertiesDto FromDomain(ReferencesFieldProperties fieldProperties)
    {
        return SimpleMapper.Map(fieldProperties, new ReferencesFieldPropertiesDto());
    }

    public override FieldProperties ToProperties()
    {
        return SimpleMapper.Map(this, new ReferencesFieldProperties());
    }
}
