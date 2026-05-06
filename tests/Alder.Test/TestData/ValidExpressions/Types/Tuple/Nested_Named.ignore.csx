// Known limitation: nested named tuple construction `(outer: (inner: ..., flag: ...), label: ...)`
// picks the wrong ValueTuple ctor — CS1729 at bind time.
// §12.8.6: nested named tuples
var t = (outer: (inner: 42, flag: true), label: "test");
return t.outer.inner;
