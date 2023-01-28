using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dtmcli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using DtmSample.Data;
using DtmSample.Models;

namespace DtmSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountsController : ControllerBase
    {
        private readonly DtmDemoWebApiContext _context;
        private readonly IConfiguration _configuration;
        private readonly IDtmClient _dtmClient;
        private readonly IDtmTransFactory _transFactory;
        private readonly ILogger<BankAccountsController> _logger;

        public BankAccountsController(DtmDemoWebApiContext context, IConfiguration configuration, IDtmClient dtmClient, IDtmTransFactory transFactory, ILogger<BankAccountsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _dtmClient = dtmClient;
            _transFactory = transFactory;
            _logger = logger;
        }

        [HttpPost("Transfer")]
        public async Task<IActionResult> Transfer(int fromUserId, int toUserId, decimal amount, CancellationToken cancellationToken = default)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var bizUrl = _configuration.GetValue<string>("BizUrl");
            var saga = _transFactory.NewSaga(gid)
                .Add(bizUrl + "/TransferOut", bizUrl + "/TransferOut_Compensate", new TransferRequest(fromUserId, amount))
                .Add(bizUrl + "/TransferIn", bizUrl + "/TransferIn_Compensate", new TransferRequest(toUserId, amount))
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);
            return Ok(new { dtm_result = "SUCCESS" });
        }

        [HttpPost("TransferIn")]
        public async Task<IActionResult> TransferIn([FromBody] TransferRequest request)
        {
            var bankAccount = await _context.BankAccount.FindAsync(request.UserId);
            if (bankAccount == null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            bankAccount.Balance += request.Amount;
            await _context.SaveChangesAsync();
            return Ok(new { dtm_result = "SUCCESS" });
        }

        [HttpPost("TransferIn_Compensate")]
        public async Task<IActionResult> TransferIn_Compensate([FromBody] TransferRequest request)
        {
            var bankAccount = await _context.BankAccount.FindAsync(request.UserId);
            if (bankAccount == null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            bankAccount.Balance -= request.Amount;
            await _context.SaveChangesAsync();
            return Ok(new { dtm_result = "SUCCESS" });
        }

        [HttpPost("TransferOut")]
        public async Task<IActionResult> TransferOut([FromBody] TransferRequest request)
        {
            var bankAccount = await _context.BankAccount.FindAsync(request.UserId);

            if (bankAccount == null || bankAccount.Balance < request.Amount)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            bankAccount.Balance -= request.Amount;
            await _context.SaveChangesAsync();
            return Ok(new { dtm_result = "SUCCESS" });
        }

        [HttpPost("TransferOut_Compensate")]
        public async Task<IActionResult> TransferOut_Compensate([FromBody] TransferRequest request)
        {
            var bankAccount = await _context.BankAccount.FindAsync(request.UserId);
            if (bankAccount == null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            bankAccount.Balance += request.Amount;
            await _context.SaveChangesAsync();
            return Ok(new { dtm_result = "SUCCESS" });
        }


        // GET: api/BankAccounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BankAccount>>> GetBankAccount()
        {
            return await _context.BankAccount.ToListAsync();
        }

        // GET: api/BankAccounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BankAccount>> GetBankAccount(int id)
        {
            var bankAccount = await _context.BankAccount.FindAsync(id);

            if (bankAccount == null)
            {
                return NotFound();
            }

            return bankAccount;
        }

        // PUT: api/BankAccounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBankAccount(int id, BankAccount bankAccount)
        {
            if (id != bankAccount.Id)
            {
                return BadRequest();
            }

            _context.Entry(bankAccount).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BankAccountExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/BankAccounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BankAccount>> PostBankAccount(BankAccount bankAccount)
        {
            await _context.Database.MigrateAsync();
            _context.BankAccount.Add(bankAccount);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBankAccount", new { id = bankAccount.Id }, bankAccount);
        }

        // DELETE: api/BankAccounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            var bankAccount = await _context.BankAccount.FindAsync(id);
            if (bankAccount == null)
            {
                return NotFound();
            }

            _context.BankAccount.Remove(bankAccount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BankAccountExists(int id)
        {
            return _context.BankAccount.Any(e => e.Id == id);
        }
    }
}
