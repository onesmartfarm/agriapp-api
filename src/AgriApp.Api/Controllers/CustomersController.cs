using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;
    private readonly ICurrentUser _currentUser;

    public CustomersController(CustomerService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);
        return customer == null ? NotFound(new { error = "Customer not found" }) : Ok(customer);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager,Sales")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var customer = await _service.CreateAsync(request);
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
        var customer = await _service.UpdateAsync(id, request);
        return customer == null ? NotFound(new { error = "Customer not found" }) : Ok(customer);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok(new { message = "Customer deleted" }) : NotFound(new { error = "Customer not found" });
    }
}
