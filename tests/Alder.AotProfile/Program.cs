using System.Diagnostics;
using System.Reflection;
using Alder;

var testDataDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Alder.Test", "TestData", "ValidExpressions");

testDataDir = Path.GetFullPath(testDataDir);

if (!Directory.Exists(testDataDir))
{
    Console.Error.WriteLine($"TestData directory not found: {testDataDir}");
    return 1;
}

var files = Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories)
    .Where(f => !f.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .ToArray();

Console.WriteLine($"Found {files.Length} expressions");
Console.WriteLine();

// Hook into MakeGenericType/MakeGenericMethod via RuntimeGenericFactory
// Since we can't directly instrument, we'll use AppDomain.FirstChanceException
// to catch any issues, and reflection to track the static caches.

var makeGenericTypeCalls = new List<(string File, string OpenType, string[] TypeArgs)>();
var makeGenericMethodCalls = new List<(string File, string Method, string[] TypeArgs)>();
var expressionCompileCalls = new List<(string File, string Info)>();

// Use reflection to get the CastCache to monitor its growth
var typeHelpersType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.TypeHelpers");
var castCacheField = typeHelpersType?.GetField("CastCache", BindingFlags.NonPublic | BindingFlags.Static);
var castCache = castCacheField?.GetValue(null);
var castCacheCountProp = castCache?.GetType().GetProperty("Count");

var methodDispatchType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.MethodDispatchCache");
var fastInvokerCacheField = methodDispatchType?.GetField("FastInvokerCache", BindingFlags.NonPublic | BindingFlags.Static);
var fastInvokerCache = fastInvokerCacheField?.GetValue(null);
var fastInvokerCountProp = fastInvokerCache?.GetType().GetProperty("Count");

var paramCacheField = methodDispatchType?.GetField("ParameterCache", BindingFlags.NonPublic | BindingFlags.Static);
var paramCache = paramCacheField?.GetValue(null);
var paramCacheCountProp = paramCache?.GetType().GetProperty("Count");

var extensionResolverType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.ExtensionMethodResolver");
var extByNameField = extensionResolverType?.GetField("ExtensionMethodsByNameCache", BindingFlags.NonPublic | BindingFlags.Static);
var extByName = extByNameField?.GetValue(null);
var extByNameCountProp = extByName?.GetType().GetProperty("Count");

var resolvedCallField = extensionResolverType?.GetField("ResolvedCallCache", BindingFlags.NonPublic | BindingFlags.Static);
var resolvedCall = resolvedCallField?.GetValue(null);
var resolvedCallCountProp = resolvedCall?.GetType().GetProperty("Count");

var resolutionCacheType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.OverloadResolution.ResolutionCache");
var resCacheField = resolutionCacheType?.GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static);
var resCache = resCacheField?.GetValue(null);
var resCacheCountProp = resCache?.GetType().GetProperty("Count");

var operatorsType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.Operators");
var udBinaryField = operatorsType?.GetField("UserDefinedBinaryOperatorCache", BindingFlags.NonPublic | BindingFlags.Static);
var udBinaryCache = udBinaryField?.GetValue(null);
var udBinaryCountProp = udBinaryCache?.GetType().GetProperty("Count");

var constructionType = typeof(AlderEngine).Assembly.GetType("Alder.Runtime.Semantics.ConstructionRuntime");
var addInvokerField = constructionType?.GetField("_addInvokerCache", BindingFlags.NonPublic | BindingFlags.Static);
var addInvokerCache = addInvokerField?.GetValue(null);
var addInvokerCountProp = addInvokerCache?.GetType().GetProperty("Count");

int GetCount(object? cache, PropertyInfo? prop) =>
    cache != null && prop != null ? (int)prop.GetValue(cache)! : -1;

Console.WriteLine("Cache field discovery:");
Console.WriteLine($"  CastCache:              {(castCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  FastInvokerCache:       {(fastInvokerCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  ParameterCache:         {(paramCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  ExtMethodsByName:       {(extByName != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  ResolvedCallCache:      {(resolvedCall != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  ResolutionCache:        {(resCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  UDBinaryOpCache:        {(udBinaryCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine($"  AddInvokerCache:        {(addInvokerCache != null ? "found" : "NOT FOUND")}");
Console.WriteLine();

Console.WriteLine($"{"#",-5} {"Expression",-55} {"Cast",5} {"Fast",5} {"Param",6} {"ExtNm",6} {"ExtRs",6} {"OvRes",6} {"UdBin",6} {"AddIn",6} {"Alloc KB",10} {"Time",6} {"Result",-10}");
Console.WriteLine(new string('-', 140));

var reportLines = new List<string>
{
    "Index\tFile\tCastCache\tFastInvokerCache\tParameterCache\tExtByName\tResolvedCall\tResolutionCache\tUDBinaryOp\tAddInvoker\tAllocKB\tTimeMs\tResult\tError"
};

var sw = new Stopwatch();

for (var i = 0; i < files.Length; i++)
{
    var file = files[i];
    var expr = File.ReadAllText(file).Trim();
    var relPath = Path.GetRelativePath(testDataDir, file);

    var allocBefore = GC.GetTotalAllocatedBytes(precise: true);

    string resultType = "OK";
    string error = "";

    sw.Restart();
    try
    {
        var engine = new AlderEngine(new AlderOptions
        {
            LanguageMode = LanguageMode.Extended,
            Constraints = new ExecutionConstraints { MaxStatements = 100_000 }
        });
        var result = engine.Evaluate(expr);
        resultType = result?.GetType().Name ?? "null";
    }
    catch (Exception ex)
    {
        resultType = "ERROR";
        error = $"{ex.GetType().Name}: {(ex.Message.Length > 80 ? ex.Message[..80] : ex.Message)}";
    }
    sw.Stop();

    var allocAfter = GC.GetTotalAllocatedBytes(precise: true);
    var allocKb = (allocAfter - allocBefore) / 1024.0;

    var castCount = GetCount(castCache, castCacheCountProp);
    var fastCount = GetCount(fastInvokerCache, fastInvokerCountProp);
    var paramCount = GetCount(paramCache, paramCacheCountProp);
    var extNameCount = GetCount(extByName, extByNameCountProp);
    var extResCount = GetCount(resolvedCall, resolvedCallCountProp);
    var ovResCount = GetCount(resCache, resCacheCountProp);
    var udBinCount = GetCount(udBinaryCache, udBinaryCountProp);
    var addInvCount = GetCount(addInvokerCache, addInvokerCountProp);

    Console.WriteLine($"{i + 1,-5} {relPath,-55} {castCount,5} {fastCount,5} {paramCount,6} {extNameCount,6} {extResCount,6} {ovResCount,6} {udBinCount,6} {addInvCount,6} {allocKb,10:F1} {sw.ElapsedMilliseconds,6} {resultType,-10}");

    reportLines.Add($"{i + 1}\t{relPath}\t{castCount}\t{fastCount}\t{paramCount}\t{extNameCount}\t{extResCount}\t{ovResCount}\t{udBinCount}\t{addInvCount}\t{allocKb:F1}\t{sw.ElapsedMilliseconds}\t{resultType}\t{error}");
}

Console.WriteLine();
Console.WriteLine("=== Final Cache Sizes ===");
Console.WriteLine($"  CastCache:              {GetCount(castCache, castCacheCountProp)}");
Console.WriteLine($"  FastInvokerCache:       {GetCount(fastInvokerCache, fastInvokerCountProp)}");
Console.WriteLine($"  ParameterCache:         {GetCount(paramCache, paramCacheCountProp)}");
Console.WriteLine($"  ExtMethodsByName:       {GetCount(extByName, extByNameCountProp)}");
Console.WriteLine($"  ResolvedCallCache:      {GetCount(resolvedCall, resolvedCallCountProp)}");
Console.WriteLine($"  ResolutionCache:        {GetCount(resCache, resCacheCountProp)}");
Console.WriteLine($"  UDBinaryOpCache:        {GetCount(udBinaryCache, udBinaryCountProp)}");
Console.WriteLine($"  AddInvokerCache:        {GetCount(addInvokerCache, addInvokerCountProp)}");

var reportPath = Path.Combine(AppContext.BaseDirectory, "aot-cache-profile.tsv");
File.WriteAllLines(reportPath, reportLines);
Console.WriteLine($"\nFull report: {reportPath}");

return 0;
