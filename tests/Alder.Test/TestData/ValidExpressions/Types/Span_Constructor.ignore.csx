// Known limitation: Span<T> is a ref struct and cannot be boxed to object?.
// The interpreted path uses object? for all values, making ref structs unrepresentable.
// The compiled path cannot emit valid IL because Span<T> requires stack-only semantics.
new Span<int>(new[] { 1, 2, 3 }).Length
