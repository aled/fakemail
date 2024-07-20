using System;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    public abstract class BaseEntity
    {
        [Required]
        public DateTime CreatedTimestampUtc { get; set; }

        public DateTime UpdatedTimestampUtc { get; set; }
    }
}
