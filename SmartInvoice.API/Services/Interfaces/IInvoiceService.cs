using System;
using System.Collections.Generic;
using System.Threading.Tasks;
<<<<<<< Updated upstream
using SmartInvoice.API.DTOs;
=======
>>>>>>> Stashed changes
using SmartInvoice.API.DTOs.Invoice;
using SmartInvoice.API.Entities;

namespace SmartInvoice.API.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<Invoice?> GetInvoiceByIdAsync(Guid id);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
<<<<<<< Updated upstream
        Task UpdateInvoiceAsync(Guid id, UpdateInvoiceDto request);
        Task<bool> DeleteInvoiceAsync(Guid id);
        Task<bool> ValidateInvoiceAsync(Guid id); 
        Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int pageIndex, int pageSize);
        Task<IEnumerable<InvoiceAuditLogDto>> GetAuditLogsAsync(Guid invoiceId);
=======
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(Guid id);
        Task<bool> ValidateInvoiceAsync(Guid id);
        Task<ValidationResultDto> ProcessInvoiceXmlAsync(string s3Key, string userId, string companyId);
>>>>>>> Stashed changes
    }
}
