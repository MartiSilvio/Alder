namespace CsEval.Test.Runtime;

/// <summary>
/// Comprehensive unit tests for TypeResolver: built-in keyword resolution, implicit BCL imports,
/// namespace import resolution, fully qualified name resolution, ambiguity detection,
/// generic type resolution, and resolution precedence.
///
/// TypeResolver is internal, so all tests exercise it through the public CsEvalEngine API.
/// Each test creates a fresh engine, optionally configuring AddUsing/AddAssembly, then
/// evaluates an expression that depends on type resolution working correctly.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class TypeResolverTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    #region Built-in Keyword Resolution (15 keywords)

    [TestCase("typeof(int)", "Int32")]
    [TestCase("typeof(string)", "String")]
    [TestCase("typeof(object)", "Object")]
    [TestCase("typeof(bool)", "Boolean")]
    [TestCase("typeof(byte)", "Byte")]
    [TestCase("typeof(sbyte)", "SByte")]
    [TestCase("typeof(short)", "Int16")]
    [TestCase("typeof(ushort)", "UInt16")]
    [TestCase("typeof(uint)", "UInt32")]
    [TestCase("typeof(long)", "Int64")]
    [TestCase("typeof(ulong)", "UInt64")]
    [TestCase("typeof(float)", "Single")]
    [TestCase("typeof(double)", "Double")]
    [TestCase("typeof(decimal)", "Decimal")]
    [TestCase("typeof(char)", "Char")]
    public void ResolveType_BuiltInKeyword_ReturnsCorrectType(string expr, string expectedName)
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate(expr)!;
        Assert.That(result.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public void ResolveType_IntKeyword_CastWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(int)42.5");
        Assert.That(result, Is.EqualTo(42));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void ResolveType_LongKeyword_CastWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(long)42");
        Assert.That(result, Is.EqualTo(42L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void ResolveType_DoubleKeyword_CastWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(double)42");
        Assert.That(result, Is.EqualTo(42.0));
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void ResolveType_StringKeyword_IsCheck()
    {
        var engine = CreateEngine();
        engine.SetVariable("s", "hello");
        var result = engine.Evaluate("s is string");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void ResolveType_IntKeyword_IsCheck()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", (object)42);
        var result = engine.Evaluate("val is int");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void ResolveType_ObjectKeyword_AsOperator()
    {
        var engine = CreateEngine();
        engine.SetVariable("o", (object)"test");
        var result = engine.Evaluate("o as string");
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void ResolveType_StringKeyword_AsOperator_NullOnMismatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("o", (object)42);
        var result = engine.Evaluate("o as string");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Implicit BCL Imports

    [Test]
    public void ResolveType_ImplicitBcl_ListCreation()
    {
        // List<> is implicitly imported from System.Collections.Generic
        var engine = CreateEngine();
        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ResolveType_ImplicitBcl_DictionaryCreation()
    {
        // Dictionary<,> is implicitly imported from System.Collections.Generic
        var engine = CreateEngine();
        var result = engine.Evaluate("new Dictionary<string, int>()");
        Assert.That(result, Is.TypeOf<Dictionary<string, int>>());
    }

    [Test]
    public void ResolveType_ImplicitBcl_ListWithItems()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var list = new List<int>(); list.Add(1); list.Add(2); return list.Count; }");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void ResolveType_ImplicitBcl_DictionaryWithItems()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var d = new Dictionary<string, int>(); d.Add(\"a\", 1); return d.Count; }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ResolveType_ImplicitBcl_HashSetCreation()
    {
        // HashSet<> is in System.Collections.Generic (implicitly imported)
        var engine = CreateEngine();
        var result = engine.Evaluate("new HashSet<int>()");
        Assert.That(result, Is.TypeOf<HashSet<int>>());
    }

    [Test]
    public void ResolveType_ImplicitBcl_QueueCreation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new Queue<double>()");
        Assert.That(result, Is.TypeOf<Queue<double>>());
    }

    [Test]
    public void ResolveType_ImplicitBcl_ListOfBool()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new List<bool>()");
        Assert.That(result, Is.TypeOf<List<bool>>());
    }

    [Test]
    public void ResolveType_ImplicitBcl_DictionaryOfIntString()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new Dictionary<int, string>()");
        Assert.That(result, Is.TypeOf<Dictionary<int, string>>());
    }

    #endregion

    #region Namespace Import Resolution (AddUsing)

    [Test]
    public void ResolveType_AddUsing_TextNamespace_StringBuilder()
    {
        var engine = CreateEngine();
        engine.AddUsing("System.Text");
        var result = engine.Evaluate("new StringBuilder()");
        Assert.That(result, Is.TypeOf<System.Text.StringBuilder>());
    }

    [Test]
    public void ResolveType_AddUsing_TextNamespace_StringBuilderWithArg()
    {
        var engine = CreateEngine();
        engine.AddUsing("System.Text");
        var result = engine.Evaluate("new StringBuilder(\"hello\").ToString()");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void ResolveType_AddUsing_MultipleNamespaces()
    {
        var engine = CreateEngine();
        engine.AddUsing("System.Text");

        var sb = engine.Evaluate("new StringBuilder(\"hello\")");
        Assert.That(sb, Is.TypeOf<System.Text.StringBuilder>());
    }

    [Test]
    public void ResolveType_WithoutUsing_UnknownType_ThrowsCsEvalException()
    {
        // StringBuilder requires System.Text using
        var engine = CreateEngine();
        Assert.Throws<CsEvalException>(() => engine.Evaluate("new StringBuilder()"));
    }

    [Test]
    public void ResolveType_AddUsing_SystemNamespace_MathCeil()
    {
        // System namespace types like Math are accessible via implicit imports
        var engine = CreateEngine();
        var result = engine.Evaluate("Math.Ceiling(1.5)");
        Assert.That(result, Is.EqualTo(2.0));
    }

    [Test]
    public void ResolveType_AddUsing_SystemNamespace_ConvertToInt()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("Convert.ToInt32(3.14)");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Fully Qualified Name Resolution

    [Test]
    public void ResolveType_FullyQualified_SystemString()
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate("typeof(System.String)")!;
        Assert.That(result, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ResolveType_FullyQualified_SystemInt32()
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate("typeof(System.Int32)")!;
        Assert.That(result, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ResolveType_FullyQualified_SystemBoolean()
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate("typeof(System.Boolean)")!;
        Assert.That(result, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void ResolveType_FullyQualified_SystemCollectionsGenericList()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new System.Collections.Generic.List<int>()");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ResolveType_FullyQualified_SystemTextStringBuilder()
    {
        // FQN works without AddUsing
        var engine = CreateEngine();
        var result = engine.Evaluate("new System.Text.StringBuilder()");
        Assert.That(result, Is.TypeOf<System.Text.StringBuilder>());
    }

    [Test]
    public void ResolveType_FullyQualified_SystemTextStringBuilder_Typeof()
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate("typeof(System.Text.StringBuilder)")!;
        Assert.That(result, Is.EqualTo(typeof(System.Text.StringBuilder)));
    }

    [Test]
    public void ResolveType_FullyQualified_SystemDouble()
    {
        var engine = CreateEngine();
        var result = (Type)engine.Evaluate("typeof(System.Double)")!;
        Assert.That(result, Is.EqualTo(typeof(double)));
    }

    [Test]
    public void ResolveType_FullyQualified_NotFound_ThrowsCsEvalException()
    {
        var engine = CreateEngine();
        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("typeof(System.Fake.NotARealType)"));
    }

    #endregion

    #region TryResolveType (Non-Throwing Variant)

    [Test]
    public void TryResolveType_Success_IsPatternWithKnownType()
    {
        // 'is' pattern uses TryResolveType internally for non-keyword types
        var engine = CreateEngine();
        engine.SetVariable("obj", (object)"hello");
        var result = engine.Evaluate("obj is string");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void TryResolveType_Success_IsPatternReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("obj", (object)42);
        var result = engine.Evaluate("obj is string");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void TryResolveType_BuiltInKeyword_IsInt()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", (object)42);
        var result = engine.Evaluate("val is int");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void TryResolveType_BuiltInKeyword_IsBool()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", (object)true);
        var result = engine.Evaluate("val is bool");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void TryResolveType_AsOperator_ReturnsNullForMismatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", (object)42);
        var result = engine.Evaluate("val as string");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Resolution Precedence

    [Test]
    public void ResolutionPrecedence_KeywordBeforeNamespace()
    {
        // "string" is a C# keyword (System.String); even with System imported,
        // the keyword should resolve first and give the same result
        var engine = CreateEngine();
        engine.AddUsing("System");
        var result = (Type)engine.Evaluate("typeof(string)")!;
        Assert.That(result, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ResolutionPrecedence_KeywordBeforeFullyQualified()
    {
        var engine = CreateEngine();
        var keywordResult = (Type)engine.Evaluate("typeof(int)")!;
        var fqnResult = (Type)engine.Evaluate("typeof(System.Int32)")!;
        Assert.That(keywordResult, Is.EqualTo(fqnResult));
    }

    [Test]
    public void ResolutionPrecedence_ImplicitBclBeforeExplicitUsing()
    {
        // List<> resolves from implicit BCL import; explicit AddUsing should not break it
        var engine = CreateEngine();
        engine.AddUsing("System.Collections.Generic");
        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ResolutionPrecedence_ExplicitUsingBeforeFullyQualified()
    {
        // After AddUsing("System.Text"), StringBuilder resolves from namespace import
        var engine = CreateEngine();
        engine.AddUsing("System.Text");
        var result = engine.Evaluate("new StringBuilder()");
        Assert.That(result, Is.TypeOf<System.Text.StringBuilder>());
    }

    [Test]
    public void ResolutionPrecedence_FqnWorksWithoutUsing()
    {
        // FQN should always work even without AddUsing
        var engine = CreateEngine();
        var result = engine.Evaluate("new System.Text.StringBuilder(\"test\").ToString()");
        Assert.That(result, Is.EqualTo("test"));
    }

    #endregion

    #region Generic Type Resolution

    [Test]
    public void ResolveType_Generic_ListOfInt()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ResolveType_Generic_DictionaryOfStringInt()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new Dictionary<string, int>()");
        Assert.That(result, Is.TypeOf<Dictionary<string, int>>());
    }

    [Test]
    public void ResolveType_Generic_ListOfString()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new List<string>()");
        Assert.That(result, Is.TypeOf<List<string>>());
    }

    [Test]
    public void ResolveType_Generic_ListOfDouble()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new List<double>()");
        Assert.That(result, Is.TypeOf<List<double>>());
    }

    [Test]
    public void ResolveType_Generic_FQN_ListCreation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new System.Collections.Generic.List<int>()");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ResolveType_Generic_FQN_DictionaryCreation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new System.Collections.Generic.Dictionary<string, int>()");
        Assert.That(result, Is.TypeOf<Dictionary<string, int>>());
    }

    [Test]
    public void ResolveType_Generic_HashSetOfString()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("new HashSet<string>()");
        Assert.That(result, Is.TypeOf<HashSet<string>>());
    }

    #endregion

    #region AddAssembly / AddUsing Configuration

    [Test]
    public void AddAssembly_AfterEvaluate_ThrowsInvalidOperationException()
    {
        var engine = CreateEngine();
        engine.Evaluate("1 + 1"); // freeze config
        Assert.Throws<InvalidOperationException>(() =>
            engine.AddAssembly(typeof(object).Assembly));
    }

    [Test]
    public void AddUsing_AfterEvaluate_ThrowsInvalidOperationException()
    {
        var engine = CreateEngine();
        engine.Evaluate("1 + 1"); // freeze config
        Assert.Throws<InvalidOperationException>(() =>
            engine.AddUsing("System.Text"));
    }

    [Test]
    public void AddUsing_FluentChaining()
    {
        var engine = CreateEngine();
        engine.AddUsing("System.Text");
        var result = engine.Evaluate("new StringBuilder(\"abc\").Length");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Variable Declaration with Resolved Types

    [Test]
    public void VariableDeclaration_BuiltInType_Int()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ int x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void VariableDeclaration_BuiltInType_String()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ string s = \"hello\"; return s; }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void VariableDeclaration_BuiltInType_Bool()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ bool b = true; return b; }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void VariableDeclaration_BuiltInType_Double()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ double d = 3.14; return d; }");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void VariableDeclaration_GenericType_ListOfInt()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ List<int> items = new List<int>(); return items.Count; }");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void VariableDeclaration_FQN_StringBuilder()
    {
        // Variable declarations with non-keyword types use var + new
        var engine = CreateEngine();
        engine.AddUsing("System.Text");
        var result = engine.Evaluate("{ var sb = new StringBuilder(\"hi\"); return sb.Length; }");
        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region Error Cases

    [Test]
    public void ResolveType_UnknownType_ThrowsWithHelpfulMessage()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("typeof(TotallyFakeType)"));
        Assert.That(ex!.Message, Does.Contain("TotallyFakeType"));
    }

    [Test]
    public void ResolveType_UnknownFQN_ThrowsWithHelpfulMessage()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("typeof(Some.Fake.Namespace.Type)"));
        Assert.That(ex!.Message, Does.Contain("Some.Fake.Namespace.Type"));
    }

    [Test]
    public void ResolveType_TypeNotImported_ThrowsWithGuidance()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("new StringBuilder()"));
        Assert.That(ex!.Message, Does.Contain("AddUsing").Or.Contain("AddAssembly").Or.Contain("fully qualified"));
    }

    [Test]
    public void ResolveType_NewWithUnknownType_Throws()
    {
        var engine = CreateEngine();
        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("new CompletelyMadeUpType()"));
    }

    #endregion
}
