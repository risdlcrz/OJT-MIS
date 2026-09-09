using System;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class Intern
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string InternNo { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string? EmailAddress { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Program { get; set; }

        public string? School { get; set; }

        public string? Status { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}
