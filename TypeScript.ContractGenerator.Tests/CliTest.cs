using System.Diagnostics;

using FluentAssertions;

using NUnit.Framework;

namespace SkbKontur.TypeScript.ContractGenerator.Tests
{
    public class CliTest
    {
        private static readonly string pathToSlnDirectory = $"{TestContext.CurrentContext.TestDirectory}/../../../../";
        private static readonly string pathToAspNetCoreExampleGeneratorAssemblyDirectory = $"{pathToSlnDirectory}/AspNetCoreExample.Generator/bin/Debug";
        private static readonly string pathToCliDirectory = $"{pathToSlnDirectory}/TypeScript.ContractGenerator.Cli/bin/Debug";

        [TestCase("net48")]
#if NETCOREAPP
        [TestCase("netcoreapp3.1")]
#endif
        public void CliGenerated(string framework)
        {
#if NETCOREAPP
            const string toolFramework = "netcoreapp3.1";
#else
            const string toolFramework = "net48";
#endif
            BuildProjectByPath($"{pathToSlnDirectory}/AspNetCoreExample.Generator/AspNetCoreExample.Generator.csproj");
            BuildProjectByPath($"{pathToSlnDirectory}/TypeScript.ContractGenerator.Cli/TypeScript.ContractGenerator.Cli.csproj");

            var extension = framework == "net48" ? "exe" : "dll";
            RunCmdCommand($"{pathToCliDirectory}/{toolFramework}/SkbKontur.TypeScript.ContractGenerator.Cli.exe " +
                          $"-a {pathToAspNetCoreExampleGeneratorAssemblyDirectory}/{framework}/AspNetCoreExample.Generator.{extension} " +
                          $"-o {TestContext.CurrentContext.TestDirectory}/cliOutput " +
                          "--nullabilityMode Optimistic " +
                          "--lintMode TsLint " +
                          "--globalNullable true");

            var expectedDirectory = $"{pathToSlnDirectory}/AspNetCoreExample.Generator/output";
            var actualDirectory = $"{TestContext.CurrentContext.TestDirectory}/cliOutput";
            TestBase.CheckDirectoriesEquivalenceInner(expectedDirectory, actualDirectory, generatedOnly : true);
        }

        private static void RunCmdCommand(string command)
        {
            var process = new Process
                {
                    StartInfo =
                        {
                            FileName = "cmd.exe",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            Arguments = "/C " + command,
                        }
                };
            process.Start();
            process.WaitForExit();
            process.ExitCode.Should().Be(0);
        }

        private static void BuildProjectByPath(string pathToCsproj)
        {
            RunCmdCommand($"dotnet build {pathToCsproj}");
        }
    }
}