namespace CsEval;

public interface IAotTypeMetadata
{
    Type Type { get; }
    bool TryGetProperty(string name, object instance, out object? value);
    bool TrySetProperty(string name, object instance, object? value);
    bool TryGetField(string name, object instance, out object? value);
    bool TrySetField(string name, object instance, object? value);
    bool TryGetIndex(object instance, object key, out object? value);
    bool TrySetIndex(object instance, object key, object? value);
    bool TryGetStaticProperty(string name, out object? value);
    bool TryGetStaticField(string name, out object? value);
    bool TryCreateInstance(object?[] args, out object? instance);
    bool TryInvokeMethod(string name, object instance, object?[] args, out object? result);
    bool TryInvokeStaticMethod(string name, object?[] args, out object? result);
}
