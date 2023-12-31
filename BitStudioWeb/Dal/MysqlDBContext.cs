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
        public string TokenAmount { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string AddDTM { get; set; }

        public string Message { get; set; }

        public bool IsSuccess { get; set; }

        [Column(TypeName = "varchar(60)")]
        public string SourceAddress { get; set; }

        [Column(TypeName = "varchar(60)")]
        public string ActivityCode { get; set; }

    }
    public class User
    {
        [Display(Name = "用户ID")]
        [Key]
        public long ID { get; set; }
        [Display(Name = "手机号")]
        public string PhoneNumber { get; set; }
        public string SmsCode { get; set; }

        /// <summary>
        /// 上次发送验证码时间。
        /// </summary>
        public DateTime LastSendSmsTime { get; set; }
        [Display(Name = "最近一次登陆IP")]
        public string IP { get; set; }
        public string AuthToken { get; set; }
        public DateTime ExpirationTime { get; set; }
        [Display(Name = "注册时间")]
        public DateTime AddTime { get; set; }

        [Display(Name = "角色")]
        public string Role { get; set; }

    }

    public class RoleConst
    {
        /// <summary>
        /// 普通用户
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// 管理员
        /// </summary>
        public const string Admin = "Admin";

    }

    public class ProductSku
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long ID { get; set; }
        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; }
        
        public long ProductId { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "DateTime")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 该商品对应的点数。负数不限。
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Value { get; set; }

        /// <summary>
        /// 有效期，单位（天）,从下订单开始时间开始算。
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ExpDay { get; set; }

        /// <summary>
        ///次数。0代表不限次数。
        /// </summary>
        [Required]
        [Column(TypeName = "int")]
        public decimal Count { get; set; }
    }

    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long ID { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Title { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        [Required]
        public int TypeId { get; set; }

        [Column(TypeName = "varchar(60)")]
        public string ImgUrl { get; set; }

        [Column(TypeName = "varchar(1000)")]
        public string Description { get; set; }
        [Required]
        [Column(TypeName = "DateTime")]
        public DateTime CreateTime { get; set; }

        [NotMapped] // This attribute tells Entity Framework to not map this property to the database
        public List<ProductSku> ProductSkus { get; set; }
    }

    public class Order
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public long UserId { get; set; }

        public long ProductId { get; set; }

        public int TypeId { get; set; }

        [Column(TypeName = "varchar(1000)")]
        public string Info { get; set; }

        public bool? IsPay { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        [Required]
        [Column(TypeName = "DateTime")]
        public DateTime CreateTime { get; set; }
        [Column(TypeName = "DateTime")]
        public DateTime? PayTime { get; set; }

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
            modelBuilder.Entity<User>();
            modelBuilder.Entity<Order>().Property(e => e.CreateTime).HasDefaultValueSql("datetime()");
            modelBuilder.Entity<Product>().Property(e => e.CreateTime).HasDefaultValueSql("datetime()");
            modelBuilder.Entity<ProductSku>().Property(e=>e.CreateTime).HasDefaultValueSql("datetime()");
        }
        public DbSet<AirDropTran> AirDropTrans { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<ProductSku> ProductSkus { get; set; }
    }
}
