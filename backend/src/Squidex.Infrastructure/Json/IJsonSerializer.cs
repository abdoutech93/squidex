﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Infrastructure.Json;

public interface IJsonSerializer
{
    string Serialize<T>(T value, bool indented = false);

    string Serialize(object? value, Type type, bool indented = false);

    void Serialize<T>(T value, Stream stream, bool indented = false);

    void Serialize(object? value, Type type, Stream stream, bool indented = false);

    Task SerializeAsync<T>(T value, Stream stream, bool indented = false,
        CancellationToken ct = default);

    Task SerializeAsync(object? value, Type type, Stream stream, bool indented = false,
        CancellationToken ct = default);

    T Deserialize<T>(string value, Type? actualType = null);

    T Deserialize<T>(Stream stream, Type? actualType = null);
}
