using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace bitkanda.Dal
{
    public class AirDropTran
    {
        [Key]
        public long ID { get; set; }

        [Column(TypeName = "varchar(60)")] 
        public string Address { get; set; }

        [Column(TypeName = "varchar(80)")]
        public string TxnHash { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string TokenAmount{get;set;}

        [Column(TypeName = "varchar(50)")]
        public string AddDTM { get; set; }

        public string Message { get; set; }

        public bool IsSuccess { get; set; }

        [Column(TypeName = "varchar(60)")]
        public string SourceAddress { get; set; }

        [Column(TypeName = "varchar(60)")]
        public string ActivityCode { get; set; }

    }

    public class MysqlDBContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(@"Data Source=airdrop.db");
             
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AirDropTran>();
        }
        public DbSet<AirDropTran> AirDropTrans { get; set; }
    }
}
