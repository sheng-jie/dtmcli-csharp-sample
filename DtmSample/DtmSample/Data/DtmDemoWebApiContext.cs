using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DtmSample.Models;

namespace DtmSample.Data
{
    public class DtmDemoWebApiContext : DbContext
    {
        public DtmDemoWebApiContext(DbContextOptions<DtmDemoWebApiContext> options)
            : base(options)
        {
        }

        public DbSet<BankAccount> BankAccount { get; set; } = default!;
    }
}
