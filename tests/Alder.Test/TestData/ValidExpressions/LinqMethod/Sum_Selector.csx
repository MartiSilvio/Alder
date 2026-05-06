var people = new[]
{
    new { Name = "a", Age = 10 },
    new { Name = "b", Age = 20 },
    new { Name = "c", Age = 30 }
};
return people.Sum(p => p.Age);
