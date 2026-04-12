// §10.2.8: array covariance does not apply to value-type element arrays
int[] ints = new int[] { 1, 2, 3 };
object[] objs = ints;
return objs;
