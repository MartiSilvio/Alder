using CsEval.Aot;

namespace CsEval.Test.AOT;

public class TestModel
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public readonly int Id;
    public static string Label { get; set; } = "default";
    public static int Counter = 0;

    public TestModel() { }

    public TestModel(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public int Add(int a, int b) => a + b;
    public string Greet() => $"Hello, {Name}";
    public void IncrementValue() { Value++; }
    public static int Parse(string s) => int.Parse(s);
}

public class TestIndexedModel
{
    private readonly Dictionary<string, int> _data = new();

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var v) ? v : 0;
        set => _data[key] = value;
    }
}

public class TestGeneratedContext : CsEvalTypeContext
{
    public static TestGeneratedContext Default { get; } = new();

    private static readonly IAotTypeMetadata[] s_metadata =
    [
        new TestModelMetadata(),
        new TestIndexedModelMetadata(),
    ];

    public override IReadOnlyList<IAotTypeMetadata> GetTypeMetadata() => s_metadata;
}

internal sealed class TestModelMetadata : IAotTypeMetadata
{
    public Type Type => typeof(TestModel);

    public bool TryGetProperty(string name, object instance, out object? value)
    {
        var typed = (TestModel)instance;
        switch (name)
        {
            case "Name": value = typed.Name; return true;
            case "Value": value = typed.Value; return true;
            default: value = default; return false;
        }
    }

    public bool TrySetProperty(string name, object instance, object? value)
    {
        var typed = (TestModel)instance;
        switch (name)
        {
            case "Name": typed.Name = (string?)value; return true;
            case "Value": typed.Value = (int)value!; return true;
            default: return false;
        }
    }

    public bool TryGetField(string name, object instance, out object? value)
    {
        var typed = (TestModel)instance;
        switch (name)
        {
            case "Id": value = typed.Id; return true;
            default: value = default; return false;
        }
    }

    public bool TrySetField(string name, object instance, object? value) => false;

    public bool TryGetStaticProperty(string name, out object? value)
    {
        switch (name)
        {
            case "Label": value = TestModel.Label; return true;
            default: value = default; return false;
        }
    }

    public bool TryGetStaticField(string name, out object? value)
    {
        switch (name)
        {
            case "Counter": value = TestModel.Counter; return true;
            default: value = default; return false;
        }
    }

    public bool TryCreateInstance(object?[] args, out object? instance)
    {
        switch (args.Length)
        {
            case 0: instance = new TestModel(); return true;
            case 2: instance = new TestModel((string)args[0]!, (int)args[1]!); return true;
            default: instance = default; return false;
        }
    }

    public bool TryGetIndex(object instance, object key, out object? value)
    {
        value = default;
        return false;
    }

    public bool TrySetIndex(object instance, object key, object? value) => false;

    public bool TryInvokeMethod(string name, object instance, object?[] args, out object? result)
    {
        var typed = (TestModel)instance;
        switch (name)
        {
            case "Add":
                switch (args.Length)
                {
                    case 2: result = typed.Add((int)args[0]!, (int)args[1]!); return true;
                }
                break;
            case "Greet":
                switch (args.Length)
                {
                    case 0: result = typed.Greet(); return true;
                }
                break;
            case "IncrementValue":
                switch (args.Length)
                {
                    case 0: typed.IncrementValue(); result = null; return true;
                }
                break;
        }
        result = default;
        return false;
    }

    public bool TryInvokeStaticMethod(string name, object?[] args, out object? result)
    {
        switch (name)
        {
            case "Parse":
                switch (args.Length)
                {
                    case 1: result = TestModel.Parse((string)args[0]!); return true;
                }
                break;
        }
        result = default;
        return false;
    }
}

internal sealed class TestIndexedModelMetadata : IAotTypeMetadata
{
    public Type Type => typeof(TestIndexedModel);

    public bool TryGetProperty(string name, object instance, out object? value)
    {
        value = default;
        return false;
    }

    public bool TrySetProperty(string name, object instance, object? value) => false;

    public bool TryGetField(string name, object instance, out object? value)
    {
        value = default;
        return false;
    }

    public bool TrySetField(string name, object instance, object? value) => false;

    public bool TryGetStaticProperty(string name, out object? value)
    {
        value = default;
        return false;
    }

    public bool TryGetStaticField(string name, out object? value)
    {
        value = default;
        return false;
    }

    public bool TryCreateInstance(object?[] args, out object? instance)
    {
        instance = default;
        return false;
    }

    public bool TryGetIndex(object instance, object key, out object? value)
    {
        var typed = (TestIndexedModel)instance;
        if (key is string strKey)
        {
            value = typed[strKey];
            return true;
        }
        value = default;
        return false;
    }

    public bool TrySetIndex(object instance, object key, object? value)
    {
        var typed = (TestIndexedModel)instance;
        if (key is string strKey)
        {
            typed[strKey] = (int)value!;
            return true;
        }
        return false;
    }

    public bool TryInvokeMethod(string name, object instance, object?[] args, out object? result)
    {
        result = default;
        return false;
    }

    public bool TryInvokeStaticMethod(string name, object?[] args, out object? result)
    {
        result = default;
        return false;
    }
}
