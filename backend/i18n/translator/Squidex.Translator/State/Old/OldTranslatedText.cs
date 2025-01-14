﻿// ==========================================================================
//  IFA  CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Translator.State.Old;

public class OldTranslatedText
{
    public SortedDictionary<string, string> Texts { get; set; } = new SortedDictionary<string, string>();

    public SortedSet<TextOrigin> Origins { get; set; } = new SortedSet<TextOrigin>();
}
