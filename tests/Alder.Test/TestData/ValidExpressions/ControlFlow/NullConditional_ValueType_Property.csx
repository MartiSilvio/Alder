// §12.8.8: null-conditional on value type result — lifts to nullable
string s = "hello";
int? len = s?.Length;
return len.HasValue && len.Value == 5;
