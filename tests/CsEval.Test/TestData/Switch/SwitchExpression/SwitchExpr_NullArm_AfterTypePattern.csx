{ object x = null; return x switch { string s => "string", null => "null", _ => "other" }; }
