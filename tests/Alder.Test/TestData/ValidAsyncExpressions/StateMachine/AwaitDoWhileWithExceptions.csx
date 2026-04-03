var attempts = 0;
var result = "";
do
{
    attempts++;
    try
    {
        if (attempts < 3)
            throw new Exception($"fail-{attempts}");
        result = await Task.FromResult("success");
    }
    catch (Exception ex)
    {
        result = await Task.FromResult(ex.Message);
    }
} while (attempts < 3);
return result;
