using Microsoft.AspNetCore.Mvc;
using DotNetRefreshApp.Data;
using DotNetRefreshApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetRefreshApp.Controllers
{
    [ApiController]
    [Route("api/records")]
    public class RecordsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Constructor injection: The DI container provides the AppDbContext instance.
        public RecordsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Record>>> Get()
        {
            return await _context.Records.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Record>> Get(string id)
        {
            var record = await _context.Records.FindAsync(int.Parse(id));
            if (record == null)
            {
                return NotFound();
            }
            return record;
        }

        [HttpPost]
        public async Task<ActionResult<Record>> Post(Record record)
        {
            _context.Records.Add(record);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = record.Id }, record);
        }

        [HttpPut]
        public async Task<IActionResult> Put(Record record)
        {
            try
            {
                _context.Records.Update(record);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var record = await _context.Records.FindAsync(int.Parse(id));
            if (record == null)
            {
                return NotFound();
            }
            _context.Records.Remove(record);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
