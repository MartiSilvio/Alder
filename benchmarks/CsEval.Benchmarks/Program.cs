using BenchmarkDotNet.Running;

if (args.Any(a => string.Equals(a, "--smoke-validate", StringComparison.OrdinalIgnoreCase)))
{
    Environment.ExitCode = CsEval.Benchmarks.BenchmarkSmokeValidator.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
