using Microsoft.AspNetCore.Mvc;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public OpportunitiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.Opportunities.ToList());
    }

    [HttpPost]
    public IActionResult Create(
        Opportunity opportunity)
    {
        opportunity.CreatedOn = DateTime.UtcNow;

        _db.Opportunities.Add(opportunity);

        _db.SaveChanges();

        return Ok(opportunity);
    }
}