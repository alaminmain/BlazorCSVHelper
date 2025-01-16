using CSVAPI.Entities;
using System.Text;

namespace CSVAPI.Services
{
    public class CSVGenerate
    {
        public static string CreateCsv(List<Person> people)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id,FirstName,LastName,DateOfBirth,Address,Email,PhoneNumber");

            foreach (var person in people)
            {
                // Enclose the address in double quotes to handle commas
                var address = $"\"{person.Address}\"";
                csvBuilder.AppendLine($"{person.Id},{person.FirstName},{person.LastName},{person.DateOfBirth:yyyy-MM-dd},{address},{person.Email},{person.PhoneNumber}");
            }

            return csvBuilder.ToString();
        }

    }
}
