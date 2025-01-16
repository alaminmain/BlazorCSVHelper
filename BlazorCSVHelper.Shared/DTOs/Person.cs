using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorCSVHelper.Shared.DTOs
{
    public class Person
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

    
        public string LastName { get; set; }

     
        public DateTime DateOfBirth { get; set; }

     
        public string Address { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }
}
