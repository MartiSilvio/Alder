// §20.5: Multicast delegate composition with +=
var log = new List<int>();
Action inc = () => log.Add(1);
Action two = inc;
two += inc;
two();
return log.Count;
