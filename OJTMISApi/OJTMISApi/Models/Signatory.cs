using System;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class Signatory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Position { get; set; }

        public string? Department { get; set; }

        public string? Signature { get; set; }

        public string? Status { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}
