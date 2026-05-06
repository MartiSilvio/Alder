var people = new[]
{
    new { Name = "Bob", Age = 30 },
    new { Name = "Alice", Age = 30 },
    new { Name = "Alice", Age = 25 }
};
return people.OrderBy(p => p.Name).ThenBy(p => p.Age).First().Age;
