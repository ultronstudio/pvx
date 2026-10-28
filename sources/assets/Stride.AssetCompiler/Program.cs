// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core.Reflection;

namespace Stride.AssetCompiler
{
    class Program
    {
        private static int Main(string[] args)
        {
            // Must run before any compiler/command touches a vendored binary with
            // a stale baked-in assembly identity (e.g. BulletSharp.NetStandard.dll
            // against a renamed/rebranded Stride.Core AssemblyVersion) — see
            // AssemblyIdentityFallbackResolver's remarks.
            AssemblyIdentityFallbackResolver.Install();

            try
            {
                // Running first time? If yes, create nuget redirect package.
                //var packageVersion = new PackageVersion(StrideVersion.NuGetVersion);
                //if (PackageStore.Instance.IsDevelopmentStore)
                //{
                //    PackageStore.Instance.CheckDeveloperTargetRedirects("Stride", packageVersion, PackageStore.Instance.InstallationPath).Wait();
                //}

                var packageBuilder = new PackageBuilderApp();
                var returnValue =  packageBuilder.Run(args);

                return returnValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception in AssetCompiler: {0}", ex);
                return 1;
            }
            finally
            {
                // Free all native library loaded from the process
                // We cannot free native libraries are some of them are loaded from static module initializer
                // NativeLibrary.UnLoadAll();
            }
        }
    }
}
