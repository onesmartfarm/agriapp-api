using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly InquiryService _inquiryService;
    private readonly WorkOrderService _workOrderService;

    public CustomersController(
        CustomerService customerService,
        InquiryService inquiryService,
        WorkOrderService workOrderService)
    {
        _customerService = customerService;
        _inquiryService = inquiryService;
        _workOrderService = workOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _customerService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return customer == null ? NotFound(new { error = "Customer not found" }) : Ok(customer);
    }

    [HttpGet("{id:int}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _customerService.GetFinancialSummaryAsync(id);
        return summary == null ? NotFound(new { error = "Customer not found" }) : Ok(summary);
    }

    [HttpGet("{id:int}/inquiries")]
    public async Task<IActionResult> GetInquiries(int id)
    {
        if (await _customerService.GetByIdAsync(id) == null)
            return NotFound(new { error = "Customer not found" });
        return Ok(await _inquiryService.GetByCustomerIdAsync(id));
    }

    [HttpGet("{id:int}/workorders")]
    public async Task<IActionResult> GetWorkOrders(int id)
    {
        if (await _customerService.GetByIdAsync(id) == null)
            return NotFound(new { error = "Customer not found" });
        return Ok(await _workOrderService.GetByCustomerIdAsync(id));
    }

    [HttpGet("{id:int}/ledger")]
    public async Task<IActionResult> GetLedger(int id)
    {
        if (await _customerService.GetByIdAsync(id) == null)
            return NotFound(new { error = "Customer not found" });
        return Ok(await _customerService.GetLedgerAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager,Sales")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.CreateAsync(request);
            return Created($"/api/customers/{customer.Id}", customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerService.UpdateAsync(id, request);
        return customer == null ? NotFound(new { error = "Customer not found" }) : Ok(customer);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _customerService.DeleteAsync(id);
        return deleted ? Ok(new { message = "Customer deleted" }) : NotFound(new { error = "Customer not found" });
    }
}
