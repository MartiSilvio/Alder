using BenchmarkDotNet.Running;

Alder.Benchmarks.BenchmarkCommand command;
try
{
    command = Alder.Benchmarks.BenchmarkCommand.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    PrintProfileHelp(Console.Error);
    Environment.ExitCode = 1;
    return;
}

if (command.Kind == Alder.Benchmarks.BenchmarkCommandKind.ShowHelp)
{
    PrintProfileHelp(Console.Out);
    return;
}

if (command.Kind == Alder.Benchmarks.BenchmarkCommandKind.Validate)
{
    var validatorExitCode = Alder.Benchmarks.BenchmarkSmokeValidator.Run();
    var fecExitCode = Alder.Benchmarks.FecSmokeTest.Run();
    Environment.ExitCode = validatorExitCode == 0 && fecExitCode == 0 ? 0 : 1;
    return;
}

Environment.SetEnvironmentVariable(
    Alder.Benchmarks.BenchmarkProfileContext.EnvironmentVariable,
    command.Profile.ToString());

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(command.BenchmarkDotNetArgs);
var runManifest = Alder.Benchmarks.BenchmarkManifestWriter.BuildRunManifest(
    summaries.Cast<object>(),
    command.Profile,
    args);
Alder.Benchmarks.BenchmarkManifestWriter.WriteManifest(runManifest);

static void PrintProfileHelp(TextWriter writer)
{
    writer.WriteLine("Alder benchmark profiles:");
    writer.WriteLine("  --profile validate    Run parity and resiliency checks only.");
    writer.WriteLine("  --profile perf-smoke  Run local BenchmarkDotNet smoke measurements.");
    writer.WriteLine("  --profile publish     Run publishable BenchmarkDotNet measurements.");
    writer.WriteLine("  --profile exhaustive  Run the explicit full benchmark matrix.");
    writer.WriteLine();
    writer.WriteLine("Pass --filter or --list after the profile, for example:");
    writer.WriteLine("  --profile publish --filter '*DynamicLinq*'");
}
