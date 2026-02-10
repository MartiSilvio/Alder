{ object x = "hi"; return x switch { string s when s.Length > 3 => "long", string s => "short", _ => "not string" }; }
