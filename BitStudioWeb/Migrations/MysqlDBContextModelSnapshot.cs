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

                    b.Property<string>("PhoneNumber")
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
