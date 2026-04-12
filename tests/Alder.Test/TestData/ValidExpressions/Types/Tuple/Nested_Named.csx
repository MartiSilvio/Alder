// §12.8.6: nested named tuples
var t = (outer: (inner: 42, flag: true), label: "test");
return t.outer.inner;
