using System;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class InternRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RequestNo { get; set; } = string.Empty;

        public string? SchoolName { get; set; }

        public string? ProgramName { get; set; }

        public int? NumberOfSlots { get; set; }

        public string? Status { get; set; }

        public DateTime? RequestDate { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}
