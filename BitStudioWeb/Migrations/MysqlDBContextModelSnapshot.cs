﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using bitkanda.Dal;

namespace bitkanda.Migrations
{
    [DbContext(typeof(MysqlDBContext))]
    partial class MysqlDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.17");

            modelBuilder.Entity("bitkanda.Dal.AirDropTran", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ActivityCode")
                        .HasColumnType("varchar(60)");

                    b.Property<string>("AddDTM")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Address")
                        .HasColumnType("varchar(60)");

                    b.Property<bool>("IsSuccess")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<string>("SourceAddress")
                        .HasColumnType("varchar(60)");

                    b.Property<string>("TokenAmount")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("TxnHash")
                        .HasColumnType("varchar(80)");

                    b.HasKey("ID");

                    b.ToTable("AirDropTrans");
                });

            modelBuilder.Entity("bitkanda.Dal.Order", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("DateTime")
                        .HasDefaultValueSql("datetime()");

                    b.Property<string>("Info")
                        .HasColumnType("varchar(1000)");

                    b.Property<bool?>("IsPay")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("PayTime")
                        .HasColumnType("DateTime");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(10,2)");

                    b.Property<long>("ProductId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TypeId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("bitkanda.Dal.Product", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("DateTime")
                        .HasDefaultValueSql("datetime()");

                    b.Property<string>("Description")
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("ImgUrl")
                        .HasColumnType("varchar(60)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<int>("TypeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("bitkanda.Dal.ProductSku", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Count")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("DateTime")
                        .HasDefaultValueSql("datetime()");

                    b.Property<decimal>("ExpDay")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(10,2)");

                    b.Property<long>("ProductId")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(10,2)");

                    b.HasKey("ID");

                    b.ToTable("ProductSkus");
                });

            modelBuilder.Entity("bitkanda.Dal.User", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthToken")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExpirationTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("IP")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastSendSmsTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<string>("Role")
                        .HasColumnType("TEXT");

                    b.Property<string>("SmsCode")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
