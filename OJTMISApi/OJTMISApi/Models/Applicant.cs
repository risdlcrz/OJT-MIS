using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class Applicant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicantNo { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string? EmailAddress { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Program { get; set; }

        public string? EducationLevel { get; set; }

        public string? Status { get; set; }

        public DateTime? DateApplied { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public bool IsApproved { get; set; }

        public bool HasCOC { get; set; }

        public bool HasDTR { get; set; }

        public bool HasMedicalExam { get; set; }

        public bool HasParentalConsent { get; set; }

        public bool HasDAForm137 { get; set; }

        public bool HasNBI { get; set; }
    }
}
