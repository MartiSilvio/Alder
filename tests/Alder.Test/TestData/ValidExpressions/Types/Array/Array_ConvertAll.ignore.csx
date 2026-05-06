// Known limitation: Array.ConvertAll's TOutput type argument is not propagated into the lambda
// body, so the runtime tries to cast the string result back to int and throws InvalidCastException.
int[] ints = new[] { 1, 2, 3 };
string[] strs = Array.ConvertAll(ints, x => x.ToString());
return strs[2];
