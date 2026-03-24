using System.Reflection;
using Alder.Runtime;

namespace Alder.Test.Runtime;

public class ApplicabilityCheckerTests
{
    private static ParameterInfo[] GetParams(string methodName) =>
        typeof(TestMethods).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)!.GetParameters();

    [Test]
    public void SimplePositionalArgs_MatchingTypes_Applicable()
    {
        var parameters = GetParams("IntString");
        var args = new[]
        {
            ArgumentDescriptor.FromArgs([42])[0],
            ArgumentDescriptor.FromArgs(["hello"])[0]
        };

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Normal));
        Assert.That(map.Sources.Length, Is.EqualTo(2));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[0].ArgumentIndex, Is.EqualTo(0));
        Assert.That(map.Sources[1].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[1].ArgumentIndex, Is.EqualTo(1));
    }

    [Test]
    public void TooManyArgs_NotApplicable()
    {
        var parameters = GetParams("IntString");
        var args = ArgumentDescriptor.FromArgs([42, "hello", 3.0]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TooFewArgs_NoDefaults_NotApplicable()
    {
        var parameters = GetParams("IntString");
        var args = ArgumentDescriptor.FromArgs([42]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void DefaultParameter_OmittedArg_Applicable()
    {
        var parameters = GetParams("IntStringDefault");
        var args = ArgumentDescriptor.FromArgs([42]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Normal));
        Assert.That(map.Sources.Length, Is.EqualTo(2));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[1].Kind, Is.EqualTo(ParameterSourceKind.Default));
    }

    [Test]
    public void NamedArguments_CorrectMapping()
    {
        var parameters = GetParams("IntString");
        var args = ArgumentDescriptor.FromArgs([42, new NamedArg("b", "hello")]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Normal));
        Assert.That(map.Sources.Length, Is.EqualTo(2));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[0].ArgumentIndex, Is.EqualTo(0));
        Assert.That(map.Sources[1].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[1].ArgumentIndex, Is.EqualTo(1));
    }

    [Test]
    public void NamedArgSkipping_WithDefault_Applicable()
    {
        var parameters = GetParams("ThreeIntsWithDefault");
        var args = ArgumentDescriptor.FromArgs([1, new NamedArg("c", 3)]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Normal));
        Assert.That(map.Sources.Length, Is.EqualTo(3));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[1].Kind, Is.EqualTo(ParameterSourceKind.Default));
        Assert.That(map.Sources[2].Kind, Is.EqualTo(ParameterSourceKind.Argument));
    }

    [Test]
    public void ParamsNormalForm_ArrayArg_Applicable()
    {
        var parameters = GetParams("ParamsInts");
        var args = ArgumentDescriptor.FromArgs([new int[] { 1, 2, 3 }]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out _);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Normal));
    }

    [Test]
    public void ParamsExpandedForm_MultipleArgs_Applicable()
    {
        var parameters = GetParams("ParamsInts");
        var args = ArgumentDescriptor.FromArgs([1, 2]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Expanded));
        Assert.That(map.Sources.Length, Is.EqualTo(1));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.ParamsRange));
        Assert.That(map.Sources[0].ParamsStartIndex, Is.EqualTo(0));
        Assert.That(map.Sources[0].ParamsCount, Is.EqualTo(2));
    }

    [Test]
    public void ParamsWithPrefix_ExpandedForm_Applicable()
    {
        var parameters = GetParams("StringParamsInts");
        var args = ArgumentDescriptor.FromArgs(["hello", 1, 2]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out var form, out var map);

        Assert.That(result, Is.True);
        Assert.That(form, Is.EqualTo(ApplicableForm.Expanded));
        Assert.That(map.Sources.Length, Is.EqualTo(2));
        Assert.That(map.Sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(map.Sources[0].ArgumentIndex, Is.EqualTo(0));
        Assert.That(map.Sources[1].Kind, Is.EqualTo(ParameterSourceKind.ParamsRange));
        Assert.That(map.Sources[1].ParamsStartIndex, Is.EqualTo(1));
        Assert.That(map.Sources[1].ParamsCount, Is.EqualTo(2));
    }

    [Test]
    public void NullArg_ReferenceType_Applicable()
    {
        var parameters = GetParams("StringParam");
        var args = ArgumentDescriptor.FromArgs([null]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void NullArg_ValueType_NotApplicable()
    {
        var parameters = GetParams("IntParam");
        var args = ArgumentDescriptor.FromArgs([null]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void NullArg_NullableValueType_Applicable()
    {
        var parameters = GetParams("NullableIntParam");
        var args = ArgumentDescriptor.FromArgs([null]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void OutArg_ByRefParam_Applicable()
    {
        var parameters = GetParams("OutIntParam");
        var args = ArgumentDescriptor.FromArgs([new OutArgMarker("x", null, false)]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void LambdaArg_ArityMatch_Applicable()
    {
        var parameters = GetParams("FuncIntString");
        var args = new[] { ArgumentDescriptor.ForTest(ArgumentKind.Lambda, null, null, 1) };

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void LambdaArg_ArityMismatch_NotApplicable()
    {
        var parameters = GetParams("FuncIntString");
        var args = new[] { ArgumentDescriptor.ForTest(ArgumentKind.Lambda, null, null, 2) };

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void LambdaArg_NonDelegateParam_NotApplicable()
    {
        var parameters = GetParams("IntParam");
        var args = new[] { ArgumentDescriptor.ForTest(ArgumentKind.Lambda, null, null, 1) };

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ImplicitConversion_IntToLong_Applicable()
    {
        var parameters = GetParams("LongParam");
        var args = ArgumentDescriptor.FromArgs([42]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void NoImplicitConversion_StringToInt_NotApplicable()
    {
        var parameters = GetParams("IntParam");
        var args = ArgumentDescriptor.FromArgs(["hello"]);

        var result = ApplicabilityChecker.IsApplicable(parameters, args, out _, out _);

        Assert.That(result, Is.False);
    }

    // ReSharper disable UnusedMember.Local, UnusedParameter.Local
    private static class TestMethods
    {
        public static void IntString(int a, string b) { }
        public static void IntStringDefault(int a, string b = "x") { }
        public static void ThreeIntsWithDefault(int a, int b = 0, int c = 0) { }
        public static void ParamsInts(params int[] values) { }
        public static void StringParamsInts(string s, params int[] values) { }
        public static void StringParam(string s) { }
        public static void IntParam(int a) { }
        public static void NullableIntParam(int? a) { }
        public static void OutIntParam(out int a) { a = 0; }
        public static void FuncIntString(Func<int, string> f) { }
        public static void LongParam(long a) { }
    }
    // ReSharper restore UnusedMember.Local, UnusedParameter.Local
}
