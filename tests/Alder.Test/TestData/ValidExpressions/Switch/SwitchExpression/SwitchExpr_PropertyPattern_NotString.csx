{ object x = 42; return x switch { string { Length: 0 } => "empty", string { Length: > 0 } => "has content", _ => "null or not string" }; }
