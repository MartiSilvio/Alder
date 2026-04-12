// §10.2.13: implicit tuple conversion — int elements widen to long
(long, long) t = (1, 2);
return t.Item1 + t.Item2;
