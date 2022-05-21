// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmRunOutOfAppBundleTests : BuildTestBase
    {
        public WasmRunOutOfAppBundleTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {}

        public static IEnumerable<object?[]> MainMethodTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        // NOTE: V8 and Chrome now fail
        // Ref: https://github.com/dotnet/runtime/issues/69259
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ true, RunHost.NodeJS })]
        [MemberData(nameof(MainMethodTestData), parameters: new object[] { /*aot*/ false, RunHost.NodeJS })]
        public void RunOutOfAppBundle(BuildArgs buildArgs, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"outofappbundle_{buildArgs.Config}_{buildArgs.AOT}" };
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                DotnetWasmFromRuntimePack: !(buildArgs.AOT || buildArgs.Config == "Release")));

            string binDir = GetBinDir(baseDir: _projectDir!, config: buildArgs.Config);
            string baseBundleDir = Path.Combine(binDir, "AppBundle");
            string tmpBundleDir = Path.Combine(binDir, "AppBundleTmp");
            string deepBundleDir = Path.Combine(baseBundleDir, "AppBundle");

            Directory.Move(baseBundleDir, tmpBundleDir);
            Directory.CreateDirectory(baseBundleDir);

            // Create $binDir/AppBundle/AppBundle
            Directory.Move(tmpBundleDir, deepBundleDir);

            if (host == RunHost.Chrome)
            {
                string indexHtmlPath = Path.Combine(baseBundleDir, "index.html");
                if (!File.Exists(indexHtmlPath))
                {
                    var html = @"<html><body><script type=""text/javascript"" src=""AppBundle/test-main.js""></script></body></html>";
                    File.WriteAllText(indexHtmlPath, html);
                }
            }

            RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id, extraXHarnessMonoArgs: "--run-deep-work-dir=true");
        }
    }
}
