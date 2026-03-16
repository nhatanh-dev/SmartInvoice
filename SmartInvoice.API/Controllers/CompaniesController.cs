using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvoice.API.DTOs.Company;
using SmartInvoice.API.Entities;
using SmartInvoice.API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInvoice.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
    {
        var companies = await _companyService.GetAllAsync();
        var dtos = companies.Select(c => new CompanyDto
        {
            CompanyId = c.CompanyId,
            CompanyName = c.CompanyName,
            TaxCode = c.TaxCode,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Address = c.Address,
            Website = c.Website,
            LegalRepresentative = c.LegalRepresentative,
            BusinessType = c.BusinessType,
            BusinessLicense = c.BusinessLicense,
            SubscriptionPackageId = c.SubscriptionPackageId,
            SubscriptionTier = c.SubscriptionTier,
            RequireTwoStepApproval = c.RequireTwoStepApproval,
            TwoStepApprovalThreshold = c.TwoStepApprovalThreshold,
            BillingCycle = c.BillingCycle,
            MaxUsers = c.MaxUsers,
            MaxInvoicesPerMonth = c.MaxInvoicesPerMonth,
            StorageQuotaGB = c.StorageQuotaGB,
            IsActive = c.IsActive
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id)
    {
        var c = await _companyService.GetByIdAsync(id);
        if (c == null)
            return NotFound();

        var dto = new CompanyDto
        {
            CompanyId = c.CompanyId,
            CompanyName = c.CompanyName,
            TaxCode = c.TaxCode,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Address = c.Address,
            Website = c.Website,
            LegalRepresentative = c.LegalRepresentative,
            BusinessType = c.BusinessType,
            BusinessLicense = c.BusinessLicense,
            SubscriptionPackageId = c.SubscriptionPackageId,
            SubscriptionTier = c.SubscriptionTier,
            RequireTwoStepApproval = c.RequireTwoStepApproval,
            TwoStepApprovalThreshold = c.TwoStepApprovalThreshold,
            BillingCycle = c.BillingCycle,
            MaxUsers = c.MaxUsers,
            MaxInvoicesPerMonth = c.MaxInvoicesPerMonth,
            StorageQuotaGB = c.StorageQuotaGB,
            IsActive = c.IsActive
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyDto dto)
    {
        var company = new Company
        {
            CompanyId = Guid.NewGuid(),
            CompanyName = dto.CompanyName,
            TaxCode = dto.TaxCode,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Website = dto.Website,
            LegalRepresentative = dto.LegalRepresentative,
            BusinessType = dto.BusinessType,
            BusinessLicense = dto.BusinessLicense,
            SubscriptionPackageId = dto.SubscriptionPackageId,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RegistrationDate = DateTime.UtcNow
        };

        var created = await _companyService.CreateAsync(company);

        return CreatedAtAction(nameof(GetById), new { id = created.CompanyId }, new CompanyDto
        {
            CompanyId = created.CompanyId,
            CompanyName = created.CompanyName,
            TaxCode = created.TaxCode,
            Email = created.Email
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto dto)
    {
        var existing = await _companyService.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.CompanyName = dto.CompanyName;
        existing.TaxCode = dto.TaxCode;
        existing.Email = dto.Email;
        existing.PhoneNumber = dto.PhoneNumber;
        existing.Address = dto.Address;
        existing.Website = dto.Website;
        existing.LegalRepresentative = dto.LegalRepresentative;
        existing.BusinessType = dto.BusinessType;
        existing.BusinessLicense = dto.BusinessLicense;
        existing.SubscriptionPackageId = dto.SubscriptionPackageId;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _companyService.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _companyService.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        await _companyService.DeleteAsync(id);
        return NoContent();
    }
}