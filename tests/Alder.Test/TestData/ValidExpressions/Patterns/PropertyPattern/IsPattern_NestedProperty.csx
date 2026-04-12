var exc = new Exception("outer", new Exception("inner"));
return exc is { InnerException: { Message: "inner" } };
