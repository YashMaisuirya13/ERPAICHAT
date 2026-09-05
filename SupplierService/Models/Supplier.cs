using System.ComponentModel.DataAnnotations;

namespace SupplierService.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    public Guid SupplierGuid { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
