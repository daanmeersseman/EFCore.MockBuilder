using System;
using System.ComponentModel.DataAnnotations;

namespace EFCore.MockBuilder.Demo.Models
{
    public class AllDataTypesEntity
    {
        // Primary Key
        public int Id { get; set; }

        // Numeric Types
        public byte ByteValue { get; set; }
        public short ShortValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }

        // Text Types
        [Required]
        public string RequiredString { get; set; } = null!;

        [MaxLength(50)]
        public string MaxLengthString { get; set; } = null!;

        [MinLength(5)]
        public string MinLengthString { get; set; } = null!;

        [StringLength(100, MinimumLength = 10)]
        public string StringLengthString { get; set; } = null!;

        // Date and Time Types
        public DateTime DateTimeValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }

        // Other Types
        public bool BoolValue { get; set; }
        public Guid GuidValue { get; set; }

        // Binary Type
        public byte[] ByteArrayValue { get; set; } = null!;

        // Enum Type
        public SampleEnum EnumValue { get; set; }

        // Data Annotations
        [Range(1, 100)]
        public int RangeInt { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; } = null!;

        [Phone]
        public string PhoneNumber { get; set; } = null!;

        [Url]
        public string Url { get; set; } = null!;
    }

    public enum SampleEnum
    {
        Value1,
        Value2,
        Value3
    }
}