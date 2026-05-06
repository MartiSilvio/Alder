Func<int,int> compose(Func<int,int> f, Func<int,int> g) => x => f(g(x));
var h = compose(x => x * 2, x => x + 1);
h(5)