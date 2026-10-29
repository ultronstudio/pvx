// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Headless runtime check, run by the packaging tests after building: the compiled
// TestScene content must resolve from the deployed database and deserialize its
// PVXDoorComponent with the authored property values intact — proving the full
// PVX Studio -> asset compiler -> Content.Load round trip works for a consumer that
// only ever referenced PVX.World via PackageReference (see PvxConsumerPackagingTests).
using System;
using PVX.World;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;

// Forces PVX.World's module initializer (and therefore its AssemblyProcessor-generated
// serializer registration) to run before the first Content.Load call. There is no
// PVXGame here (ContentManager alone needs no graphics device), so this is the direct,
// minimal equivalent of PVX.Runtime.PVXContentRegistration.EnsureLoaded<T>() — see that
// type's remarks for why this is needed in a from-scratch process.
_ = new PVXDoorComponent();

var content = new ContentManager(new DatabaseFileProviderService(new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase())));

if (!content.Exists("TestScene"))
{
    Console.WriteLine("CONTENT MISSING TestScene");
    return 1;
}

var scene = content.Load<Scene>("TestScene");
var door = scene.Entities[0].Get<PVXDoorComponent>();

if (door is null)
{
    Console.WriteLine("PVXDoorComponent MISSING on loaded TestScene entity");
    return 1;
}

if (door.OpenAngleDegrees != 77f)
{
    Console.WriteLine($"PVXDoorComponent.OpenAngleDegrees mismatch: expected 77, got {door.OpenAngleDegrees}");
    return 1;
}

Console.WriteLine($"CONTENT OK TestScene PVXDoorComponent.OpenAngleDegrees={door.OpenAngleDegrees}");
return 0;
