// Known limitation: `params` modifier on local function parameters is not supported.
// §15.6.2.6: single variadic argument is wrapped into a 1-element array
int Sum(params int[] nums)
{
    int total = 0;
    for (int i = 0; i < nums.Length; i++) total += nums[i];
    return total;
}
return Sum(42);
