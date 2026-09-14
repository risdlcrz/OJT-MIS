using System;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class OJTProgram
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Abbrev { get; set; }

        public string? Level { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}
