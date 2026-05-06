// Known limitation: `params` modifier on local function parameters is not supported.
// §15.6.2.6: multiple variadic arguments build an array in call order
int Sum(params int[] nums)
{
    int total = 0;
    for (int i = 0; i < nums.Length; i++) total += nums[i];
    return total;
}
return Sum(1, 2, 3);
