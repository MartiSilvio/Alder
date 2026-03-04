var limit = 50;
var sieve = new bool[51];
foreach (var i in 0..50) sieve[i] = true;

sieve[0] = false;
sieve[1] = false;

for (var i = 2; i * i <= limit; i++)
{
    if (sieve[i])
    {
        for (var j = i * i; j <= limit; j += i)
        {
            sieve[j] = false;
        }
    }
}

var primes = "";
var primeCount = 0;
foreach (var i in 2..limit)
{
    if (sieve[i])
    {
        if (primes.Length > 0) primes += ",";
        primes += i.ToString();
        primeCount++;
    }
}

var check2 = sieve[2];
var check17 = sieve[17];
var check49 = sieve[49];

var sum = 0;
foreach (var i in 2..limit)
{
    if (sieve[i]) sum += i;
}

var result = $"primes={primes}|count={primeCount}|sum={sum}|";
result += $"is2prime={check2}|is17prime={check17}|is49prime={check49}";

return result;
