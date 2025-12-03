using API.Data;
using Ticksi.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")] // localhost:5001/api/members
    [ApiController]
    public class MembersController: ControllerBase //public class MembersController(AppDbContext context) : ControllerBase <- ovo samo prekopiramo umjesto ovog lijevo
    {
        

        [HttpGet]
        // public async Task<ActionResult<IReadOnlyList<AppUser>>> GetMembers()
        // {
        //     var members = await context.Users.ToListAsync();
            
        //     return members;
        // }
        // [HttpGet("{id}")] // localhost:5001/api/members/isar-id
        // public async Task<ActionResult<AppUser>> GetMember(string id)
        // {
        //     var member = await context.Users.FindAsync(id);

        //     if (member == null) return NotFound();

        //     return member;
        // }
        
        public ActionResult<IEnumerable<object>> GetMembers()
        {
            return Ok(new[]
            {
                new { id = 1, username = "Sedad" },
                new { id = 2, username = "Isar" },
                new { id = 3, username = "Member" }
            });
        }

    }
}
