using System;
using System.ComponentModel.DataAnnotations;

namespace Fakemail.Data.EntityFramework
{
    public abstract class BaseEntity
    { 
        [Required]
        public DateTimeOffset InsertedTimestamp { get; set; }
        
        public DateTimeOffset LastUpdatedTimestamp { get; set; }

        public DateTimeOffset DeletedTimestamp { get; set; }
    }
}
