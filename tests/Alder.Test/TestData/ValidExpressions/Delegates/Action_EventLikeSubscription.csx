// §20.5: Action as event-like subscription pattern
var log = new List<int>();
Action<int> a1 = x => log.Add(x);
Action<int> a2 = x => log.Add(x * 2);
Action<int> onChange = a1;
onChange += a2;
onChange(5);
return log.Count;
