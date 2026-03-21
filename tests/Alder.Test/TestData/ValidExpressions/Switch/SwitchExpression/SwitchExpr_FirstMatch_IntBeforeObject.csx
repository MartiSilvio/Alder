{ object x = 42; return x switch { int => "int", object => "object", _ => "other" }; }
