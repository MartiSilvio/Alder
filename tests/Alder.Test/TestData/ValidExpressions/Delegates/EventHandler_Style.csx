// §20.3: EventHandler<T>-style delegate called through its Invoke member
int captured = 0;
EventHandler<int> handler = (sender, value) => captured = value;
handler.Invoke(null, 42);
return captured;
