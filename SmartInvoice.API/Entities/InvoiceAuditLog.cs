using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartInvoice.API.Entities.JsonModels;

namespace SmartInvoice.API.Entities;

[Table("InvoiceAuditLogs")]
public class InvoiceAuditLog
{
    [Key]
    public Guid AuditId { get; set; }

    // --- Invoice Relation ---
    public Guid? InvoiceId { get; set; }
    [ForeignKey(nameof(InvoiceId))]
    public Invoice? Invoice { get; set; }

    public string? InvoiceNumber { get; set; } // Snapshot số hóa đơn để hiển thị khi hóa đơn gốc bị xóa
    
    public Guid CompanyId { get; set; } // Phục vụ bảo mật đa thuê (Multi-tenancy) khi Invoice bị xóa
    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    // --- User Info ---
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [MaxLength(100)]
    public string? UserEmail { get; set; } // Denormalized

    [MaxLength(50)]
    public string? UserRole { get; set; }

    // --- Action Info ---
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = null!; // UPLOAD, EDIT, SUBMIT, APPROVE, REJECT, OVERRIDE

    // --- Data Changes (JSONB) ---
    [Column(TypeName = "jsonb")]
    public string? OldData { get; set; }

    [Column(TypeName = "jsonb")]
    public string? NewData { get; set; }

    [Column(TypeName = "jsonb")]
    public List<AuditChange>? Changes { get; set; }

    // --- Context ---
    public string? Reason { get; set; }
    public string? Comment { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? RequestId { get; set; }

    // --- Metadata (IMMUTABLE) ---
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
