// §10.2.13: implicit tuple conversion — mixed element widening
(long, double) t = (1, 2.5f);
return t.Item1 == 1L && t.Item2 == 2.5;
