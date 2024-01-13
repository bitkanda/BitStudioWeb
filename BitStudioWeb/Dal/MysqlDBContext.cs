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

        //public long ProductId { get; set; }

        public int TypeId { get; set; }

        [Column(TypeName = "varchar(1000)")]
        public string Info { get; set; }

        //[Required]
        //public bool IsPay { get; set; }

        [Required]
        //[Column(TypeName = "decimal(10,2)")]
        //public decimal Price { get; set; }

        /// <summary>
        /// 结算金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal PayAmount { get; set; }

        [Required]
        /// <summary>
        /// 零售金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal RetailAmount { get; set; }

        [Required]
        /// <summary>
        /// 优惠金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal PromotionAmount { get; set; }

        [Required]
        [Column(TypeName = "DateTime")]
        public DateTime CreateTime { get; set; }

        [Required]
        [Column(TypeName = "DateTime")]
        public DateTime ModifyTime { get; set; }

        /// <summary>
        /// 支付时间。
        /// </summary>
        [Column(TypeName = "DateTime")]
        public DateTime? PayTime { get; set; }


         /// <summary>
         /// 支付的用户
         /// </summary>
        public long PayUserId { get; set; }

        /// <summary>
        /// 支付单号或手工单号
        /// </summary>
        [Column(TypeName = "varchar(200)")]
        public string PayOrderNo { get; set; }


        /// <summary>
        /// 买家留言
        /// </summary>
        [Column(TypeName = "varchar(200)")]
        public string BuyerMsg { get; set; }

        /// <summary>
        /// 卖家留言
        /// </summary>
        [Column(TypeName = "varchar(200)")]
        public string SellerMsg { get; set; }


        /// <summary>
        /// 手机
        /// </summary>
        [Column(TypeName = "varchar(200)")]
        public string Mobile { get; set; }

        /// <summary>
        /// 购买数量
        /// </summary>
       // [Required]
        public long Qty { get; set; }

        /// <summary>
        /// 订单状态.0待付款，1已付款，2实物发货，3已完成或发送券到买家短信。
        /// </summary>
        public long OrderStatus { get; set; }

        /// <summary>
        /// 订单明细。
        /// </summary>
        [NotMapped]
        public List<OrderDetal> Details { get; set; }

    }

    public class OrderDetal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        public long OrderId { get; set; }

        [Required]
        public long ProductId { get; set; }
         

        [Required]
        public long SkuId { get; set; }

        /// <summary>
        /// 购买数量
        /// </summary>
        [Required]
        public long Qty { get; set; }


        [Required]
        /// <summary>
        /// 优惠金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal PromotionAmount { get; set; }

        [Required]
        [Column(TypeName = "DateTime")]
        public DateTime CreateTime { get; set; }


        [Required]  
        /// <summary>
        /// 结算金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal PayAmount { get; set; }

        [Required]
        /// <summary>
        /// 零售金额
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal RetailAmount { get; set; }


        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; }
 
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
         
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


        public DbSet<OrderDetal> OrderDetals { get; set; }
    }
}
