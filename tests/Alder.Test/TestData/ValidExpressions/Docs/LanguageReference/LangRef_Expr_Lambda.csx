var doubler = (int x) => x * 2;
var add = (int a, int b) => a + b;
var constant = () => 42;
return doubler(5) + add(3, 4) + constant();
