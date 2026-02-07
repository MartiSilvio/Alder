{
    // Sieve of Eratosthenes: find all primes up to 50
    // Exercises: arrays, nested loops, modulo, boolean logic, string building
    var limit = 50;
    var sieve = new[] {
        true,true,true,true,true,true,true,true,true,true,
        true,true,true,true,true,true,true,true,true,true,
        true,true,true,true,true,true,true,true,true,true,
        true,true,true,true,true,true,true,true,true,true,
        true,true,true,true,true,true,true,true,true,true,
        true
    };

    // 0 and 1 are not prime
    sieve[0] = false;
    sieve[1] = false;

    // Mark composites
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

    // Collect primes
    var primes = "";
    var primeCount = 0;
    for (var i = 2; i <= limit; i++)
    {
        if (sieve[i])
        {
            if (primes.Length > 0) primes += ",";
            primes += i.ToString();
            primeCount++;
        }
    }

    // Verify some known primes
    var check2 = sieve[2];
    var check17 = sieve[17];
    var check49 = sieve[49]; // 49 = 7*7, not prime

    // Sum of all primes
    var sum = 0;
    for (var i = 2; i <= limit; i++)
    {
        if (sieve[i]) sum += i;
    }

    var result = $"primes={primes}|count={primeCount}|sum={sum}|";
    result += $"is2prime={check2}|is17prime={check17}|is49prime={check49}";

    return result;
}
