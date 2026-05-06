// Known limitation: tuple destructuring assignment `(a, b) = (b, a)` to existing locals is not
// supported; the parser rejects the LHS as a non-assignable expression.
// §12.21.2: tuple assignment used as swap idiom
int a = 1;
int b = 2;
(a, b) = (b, a);
return a * 10 + b;
