using Microsoft.AspNetCore.Mvc;
using web_DACS.Repositories.Interfaces;

[Route("api/[controller]")]
[ApiController]
public class BanAnController : ControllerBase
{
    private readonly IBanAnRepository _repo;
    public BanAnController(IBanAnRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _repo.GetAllAsync());
}