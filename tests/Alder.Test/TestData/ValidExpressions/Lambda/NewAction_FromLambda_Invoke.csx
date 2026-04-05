var called = false;
var a = new Action(() => { called = true; });
a.Invoke();
return called;
