﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Papst.EventStore.EntityFrameworkCore.Database;

#nullable disable

namespace Papst.EventStore.EntityFrameworkCore.Migrations
{
    [DbContext(typeof(EventStoreDbContext))]
    [Migration("20230924140323_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.22")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Papst.EventStore.EntityFrameworkCore.Database.EventStreamDocumentEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DataType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataAdditional")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataComment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataTenantId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataUserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataUserName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("StreamId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("TargetType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Time")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("StreamId");

                    b.HasIndex("Version");

                    b.HasIndex("StreamId", "Version")
                        .IsUnique();

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("Papst.EventStore.EntityFrameworkCore.Database.EventStreamEntity", b =>
                {
                    b.Property<Guid>("StreamId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("NextVersion")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("TargetType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Updated")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("StreamId");

                    b.ToTable("Streams");
                });
#pragma warning restore 612, 618
        }
    }
}
