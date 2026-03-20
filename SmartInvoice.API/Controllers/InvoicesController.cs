using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartInvoice.API.Data;
using SmartInvoice.API.DTOs;
using SmartInvoice.API.DTOs.Invoice;
using SmartInvoice.API.Entities;
using SmartInvoice.API.Entities.JsonModels;
using SmartInvoice.API.Services;
using SmartInvoice.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using SmartInvoice.API.Enums;

namespace SmartInvoice.API.Controller
{
    [ApiController]
    [Route("api/invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly StorageService _storageService;
        private readonly IInvoiceProcessorService _invoiceProcessor;
        private readonly IInvoiceService _invoiceService;
        private readonly IQuotaService _quotaService;

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public InvoicesController(StorageService storageService, IInvoiceProcessorService invoiceProcessor, IInvoiceService invoiceService, IQuotaService quotaService)
        {
            _storageService = storageService;
            _invoiceProcessor = invoiceProcessor;
            _invoiceService = invoiceService;
            _quotaService = quotaService;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  HELPER: Extract user claims
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private (Guid UserId, Guid CompanyId, string UserRole, string UserEmail) GetUserInfo()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            var companyIdStr = User.FindFirst("CompanyId")?.Value;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Member";
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "unknown";

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(companyIdStr))
                throw new UnauthorizedAccessException("User identity or company information is missing in token.");

            return (Guid.Parse(userIdStr), Guid.Parse(companyIdStr), userRole, userEmail);
        }

        private string? GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  UPLOAD & PROCESS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [HttpPost("generate-upload-url")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public IActionResult GetUploadUrl([FromBody] UploadRequestDto request)
        {
            var result = _storageService.GeneratePresignedUrl(request.FileName, request.ContentType);
            return Ok(new { UploadUrl = result.Url, S3Key = result.Key });
        }

        [HttpPost("process-xml")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public async Task<IActionResult> ProcessXml([FromBody] ProcessXmlRequestDto request)
        {
            if (string.IsNullOrEmpty(request.S3Key))
                return BadRequest(new { Message = "S3Key is required." });

            try
            {
                var (userId, companyId, _, _) = GetUserInfo();

                // Quota check: lazy reset + consume
                await _quotaService.ValidateAndConsumeInvoiceQuotaAsync(companyId);

                var finalResult = await _invoiceService.ProcessInvoiceXmlAsync(request.S3Key, userId.ToString(), companyId.ToString());
                return Ok(finalResult);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Message = "User identity or company information is missing in token." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("process-ocr")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public async Task<IActionResult> ProcessOcr([FromBody] ProcessOcrRequestDto request)
        {
            if (request.OcrResult == null)
                return BadRequest(new { Message = "OCR data is required." });

            try
            {
                var (userId, companyId, _, _) = GetUserInfo();
                var finalResult = await _invoiceService.ProcessInvoiceOcrAsync(request, userId.ToString(), companyId.ToString());
                return Ok(finalResult);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Message = "User identity or company information is missing in token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("test-process-local")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public async Task<IActionResult> TestProcessLocal(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lÃ²ng chá»n 1 file XML Ä‘á»ƒ test.");

            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var structResult = _invoiceProcessor.ValidateStructure(tempFilePath);
                if (!structResult.IsValid) return BadRequest(structResult);

                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(tempFilePath);

                var sigResult = _invoiceProcessor.VerifyDigitalSignature(xmlDoc);
                if (!sigResult.IsValid) return BadRequest(sigResult);

                var logicResult = await _invoiceProcessor.ValidateBusinessLogicAsync(xmlDoc);
                logicResult.SignerSubject = sigResult.SignerSubject;
                logicResult.ExtractedData = _invoiceProcessor.ExtractData(xmlDoc, logicResult);

                return Ok(logicResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lá»—i: {ex.Message}" });
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  LIST & DETAIL
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [HttpGet]
        [Authorize(Policy = Constants.Permissions.InvoiceView)]
        public async Task<IActionResult> GetInvoices([FromQuery] GetInvoicesQueryDto query)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var result = await _invoiceService.GetInvoicesAsync(query, companyId, userId, userRole);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Message = "User identity or company information is missing in token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("trash")]
        [Authorize(Policy = Constants.Permissions.InvoiceView)]
        public async Task<IActionResult> GetTrashInvoices([FromQuery] GetInvoicesQueryDto query)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var result = await _invoiceService.GetTrashInvoicesAsync(query, companyId, userId, userRole);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = Constants.Permissions.InvoiceEdit)]
        public async Task<IActionResult> RestoreInvoice(Guid id)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var success = await _invoiceService.RestoreInvoiceAsync(id, companyId, userId, userRole);
                if (!success) return NotFound(new { Message = "Không tìm thấy hóa đơn trong thùng rác hoặc không có quyền." });
                return Ok(new { Message = "Phục hồi thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}/hard")]
        [Authorize(Policy = Constants.Permissions.InvoiceEdit)]
        public async Task<IActionResult> HardDeleteInvoice(Guid id)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var success = await _invoiceService.HardDeleteInvoiceAsync(id, companyId, userId, userRole);
                if (!success) return NotFound(new { Message = "Không tìm thấy hóa đơn trong thùng rác hoặc không có quyền." });
                return Ok(new { Message = "Xóa vĩnh viễn thành công. Đã hoàn trả dung lượng." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }


        [HttpGet("{id:guid}")]
        [Authorize(Policy = Constants.Permissions.InvoiceView)]
        public async Task<IActionResult> GetInvoiceById(Guid id)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var detail = await _invoiceService.GetInvoiceDetailAsync(id, companyId, userId, userRole);

                if (detail == null)
                    return NotFound(new { Message = $"KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n vá»›i ID: {id}" });

                return Ok(detail);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Message = "User identity or company information is missing in token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lá»—i server ná»™i bá»™", Error = ex.Message });
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  CRUD
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Constants.Permissions.InvoiceEdit)]
        public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceDto request)
        {
            try
            {
                var (userId, _, userRole, userEmail) = GetUserInfo();
                await _invoiceService.UpdateInvoiceAsync(id, request, userId, userEmail, userRole, GetClientIp());
                return Ok(new { Message = "Cáº­p nháº­t thÃ nh cÃ´ng" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Constants.Permissions.InvoiceEdit)]
        public async Task<IActionResult> DeleteInvoice(Guid id)
        {
            try
            {
                var (userId, companyId, userRole, _) = GetUserInfo();
                var isDeleted = await _invoiceService.DeleteInvoiceAsync(id, companyId, userId, userRole);

                if (!isDeleted)
                    return NotFound(new { Message = $"KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n vá»›i ID: {id}" });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lá»—i khi xÃ³a hÃ³a Ä‘Æ¡n", Error = ex.Message });
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  WORKFLOW
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [HttpPost("{id:guid}/submit")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public async Task<IActionResult> SubmitInvoice(Guid id, [FromBody] SubmitInvoiceDto? request)
        {
            try
            {
                var (userId, companyId, userRole, userEmail) = GetUserInfo();
                await _invoiceService.SubmitInvoiceAsync(id, companyId, userId, userEmail, userRole, request?.Comment, GetClientIp());
                return Ok(new { Message = "HÃ³a Ä‘Æ¡n Ä‘Ã£ Ä‘Æ°á»£c gá»­i duyá»‡t thÃ nh cÃ´ng." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("submit-batch")]
        [Authorize(Policy = Constants.Permissions.InvoiceUpload)]
        public async Task<IActionResult> SubmitBatch([FromBody] SubmitBatchDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var (userId, companyId, userRole, userEmail) = GetUserInfo();
                var result = await _invoiceService.SubmitBatchAsync(request.InvoiceIds, companyId, userId, userEmail, userRole, request.Comment, GetClientIp());
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Message = "User identity or company information is missing in token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Policy = Constants.Permissions.InvoiceApprove)]
        public async Task<IActionResult> ApproveInvoice(Guid id, [FromBody] ApproveInvoiceDto? request)
        {
            try
            {
                var (userId, companyId, userRole, userEmail) = GetUserInfo();
                await _invoiceService.ApproveInvoiceAsync(id, companyId, userId, userEmail, userRole, request?.Comment, GetClientIp());
                return Ok(new { Message = "HÃ³a Ä‘Æ¡n Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t thÃ nh cÃ´ng." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = Constants.Permissions.InvoiceReject)]
        public async Task<IActionResult> RejectInvoice(Guid id, [FromBody] RejectInvoiceDto request)
        {
            try
            {
                var (userId, companyId, userRole, userEmail) = GetUserInfo();
                await _invoiceService.RejectInvoiceAsync(id, companyId, userId, userEmail, userRole, request.Reason, request.Comment, GetClientIp());
                return Ok(new { Message = "HÃ³a Ä‘Æ¡n Ä‘Ã£ bá»‹ tá»« chá»‘i." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  AUDIT LOG
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [HttpGet("{id:guid}/audit-logs")]
        [Authorize(Policy = Constants.Permissions.InvoiceView)]
        public async Task<IActionResult> GetAuditLogs(Guid id)
        {
            try
            {
                var logs = await _invoiceService.GetAuditLogsAsync(id);
                return Ok(logs);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y hÃ³a Ä‘Æ¡n." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("stats")]
        [Authorize(Policy = Constants.Permissions.InvoiceView)]
        public async Task<IActionResult> GetInvoiceStats([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string? status)
        {
            try
            {
                var (_, companyId, _, _) = GetUserInfo();
                var stats = await _invoiceService.GetInvoiceStatsAsync(startDate, endDate, status, companyId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
