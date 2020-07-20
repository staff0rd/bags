﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using coach_bags_selenium.Data;

namespace coach_bags_selenium.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20200720130344_ProductImageAndCategory")]
    partial class ProductImageAndCategory
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("coach_bags_selenium.Data.Product", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("Category")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Image")
                        .HasColumnType("text");

                    b.Property<DateTime?>("LastPostedUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("LastUpdatedUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Link")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Savings")
                        .HasColumnType("numeric");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });
#pragma warning restore 612, 618
        }
    }
}
