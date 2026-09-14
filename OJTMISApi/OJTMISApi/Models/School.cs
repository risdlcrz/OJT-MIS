using System;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class School
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactNumber { get; set; }

        public string? EmailAddress { get; set; }

        public string? Status { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}
