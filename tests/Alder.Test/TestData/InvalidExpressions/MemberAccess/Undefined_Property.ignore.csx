// Known limitation: Alder defers undefined-member access to the dynamic path and surfaces
// ALDR0306 at runtime rather than CS1061 at bind time, even on sealed primitives like string.
// Bind-time gating breaks extension method dispatch and typeof(T).Member reflection paths.
// Accessing a property that doesn't exist on the type
string s = "hello";
return s.Foo;
