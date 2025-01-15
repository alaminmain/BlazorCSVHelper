﻿using Bogus;

namespace CSVAPI.Services
{
    public static class MockDataGenerator
    {
        public static List<CSVAPI.Entities.Person> GenerateMockPersons(int count)
        {
            // Create a Faker instance for the Person entity
            var personFaker = new Faker<CSVAPI.Entities.Person>()
                .RuleFor(p => p.Id, f => 0) // Id will be auto-generated by the database
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18))) // Age 18-50
                .RuleFor(p => p.Address, f => f.Address.FullAddress())
                .RuleFor(p => p.Email, f => f.Internet.Email())
                .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber());

            // Generate a list of mock data
            return personFaker.Generate(count);
        }
    }
}