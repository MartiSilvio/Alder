{
  var contacts = new[]
  {
    new { PhoneNumbers = new[] { "206-555-0101", "425-882-8080" } },
    new { PhoneNumbers = new[] { "650-555-0199" } }
  };
  return contacts[0].PhoneNumbers.Length + contacts[1].PhoneNumbers.Length;
}
