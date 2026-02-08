namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IndexPropertyAssignmentTests(CompilationMode mode)
{
    #region Index Assignment - Array/List

    // These use CsEval-specific [1,2,3] collection expression syntax -- engine-only tests
    [Test]
    public void IndexAssignment_List_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 10;
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IndexAssignment_List_FirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[0] = 100;
            return arr[0];
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void IndexAssignment_List_LastElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[2] = 999;
            return arr[2];
        }");

        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void IndexAssignment_List_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            var x = arr[1] = 42;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void IndexAssignment_ExternalList_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var list = new List<int> { 1, 2, 3 };
        engine.SetVariable("arr", list);

        engine.Evaluate("arr[1] = 99");

        Assert.That(list[1], Is.EqualTo(99));
    }

    [Test]
    public void IndexAssignment_ExternalArray_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var arr = new object?[] { 1, 2, 3 };
        engine.SetVariable("arr", arr);

        engine.Evaluate("arr[0] = 999");

        Assert.That(arr[0], Is.EqualTo(999));
    }

    [Test]
    public void IndexAssignment_OutOfRange_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Evaluate(@"
            {
                var arr = [1, 2, 3];
                arr[10] = 5;
                return arr;
            }"));
    }

    [Test]
    public void IndexAssignment_NegativeIndex_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Evaluate(@"
            {
                var arr = [1, 2, 3];
                arr[-1] = 5;
                return arr;
            }"));
    }

    [Test]
    public void IndexAssignment_OnNull_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("arr[0] = 5"));
    }

    #endregion

    #region Index Assignment - Dictionary (CsEval anonymous objects as dictionaries)

    // CsEval-specific: anonymous objects are mutable dictionaries -- engine-only tests
    [Test]
    public void IndexAssignment_Dictionary_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { name = ""John"" };
            dict[""name""] = ""Jane"";
            return dict[""name""];
        }");

        Assert.That(result, Is.EqualTo("Jane"));
    }

    [Test]
    public void IndexAssignment_Dictionary_AddsNewKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { name = ""John"" };
            dict[""age""] = 30;
            return dict[""age""];
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void IndexAssignment_ExternalDictionary_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var dict = new Dictionary<string, object?> { ["key"] = "old" };
        engine.SetVariable("dict", dict);

        engine.Evaluate(@"dict[""key""] = ""new""");

        Assert.That(dict["key"], Is.EqualTo("new"));
    }

    [Test]
    public void IndexAssignment_Dictionary_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { a = 1 };
            var x = dict[""a""] = 100;
            return x;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    #endregion

    #region Index Assignment - In Loops

    // CsEval-specific: [0,0,0,0,0] collection expression syntax -- engine-only tests
    [Test]
    public void IndexAssignment_InForLoop_PopulatesArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [0, 0, 0, 0, 0];
            for (var i = 0; i < 5; i++) {
                arr[i] = i * 10;
            }
            return arr[3];
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void IndexAssignment_InWhileLoop_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            var i = 0;
            while (i < 3) {
                arr[i] = arr[i] * 2;
                i++;
            }
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(4));
    }

    #endregion

    #region Property Assignment - Anonymous Object (CsEval-specific: mutable anonymous objects)

    [Test]
    public void PropertyAssignment_AnonymousObject_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj.Name = ""Jane"";
            return obj.Name;
        }");

        Assert.That(result, Is.EqualTo("Jane"));
    }

    [Test]
    public void PropertyAssignment_AnonymousObject_AddsNewProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj.Age = 30;
            return obj.Age;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void PropertyAssignment_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Value = 0 };
            var x = obj.Value = 42;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PropertyAssignment_NestedObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Inner = new { Value = 10 } };
            obj.Inner.Value = 99;
            return obj.Inner.Value;
        }");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void PropertyAssignment_OnNull_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("obj.Name = \"test\""));
    }

    #endregion

    #region Property Assignment - Typed Objects

    [Test]
    public void PropertyAssignment_TypedObject_SetsProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var person = new TestPerson { Name = "John", Age = 25 };
        engine.SetVariable("person", person);

        engine.Evaluate(@"person.Name = ""Jane""");

        Assert.That(person.Name, Is.EqualTo("Jane"));
    }

    [Test]
    public void PropertyAssignment_TypedObject_SetsIntProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var person = new TestPerson { Name = "John", Age = 25 };
        engine.SetVariable("person", person);

        engine.Evaluate("person.Age = 30");

        Assert.That(person.Age, Is.EqualTo(30));
    }

    [Test]
    public void PropertyAssignment_ReadOnlyProperty_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello");

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("text.Length = 10"));
    }

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion

    #region Property Assignment - In Loops (CsEval-specific: mutable anonymous objects)

    [Test]
    public void PropertyAssignment_InForLoop_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Counter = 0 };
            for (var i = 0; i < 5; i++) {
                obj.Counter = obj.Counter + 1;
            }
            return obj.Counter;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Mixed Index and Property Assignment (CsEval-specific: mutable anonymous objects)

    [Test]
    public void MixedAssignment_ArrayOfObjects()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [new { Value = 1 }, new { Value = 2 }];
            items[0].Value = 100;
            return items[0].Value;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void MixedAssignment_ObjectWithArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Items = [1, 2, 3] };
            obj.Items[1] = 99;
            return obj.Items[1];
        }");

        Assert.That(result, Is.EqualTo(99));
    }

    #endregion
}
