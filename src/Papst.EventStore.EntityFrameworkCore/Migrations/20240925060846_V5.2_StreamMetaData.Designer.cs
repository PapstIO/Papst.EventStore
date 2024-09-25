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
    [Migration("20240925060846_V5.2_StreamMetaData")]
    partial class V52_StreamMetaData
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Papst.EventStore.EntityFrameworkCore.Database.EventStreamDocumentEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DataType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("StreamId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("TargetType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

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

                    b.Property<decimal?>("LatestSnapshotVersion")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("MetaDataAdditionJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataComment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataTenantId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataUserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MetaDataUserName")
                        .HasColumnType("nvarchar(max)");

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

            modelBuilder.Entity("Papst.EventStore.EntityFrameworkCore.Database.EventStreamDocumentEntity", b =>
                {
                    b.OwnsOne("Papst.EventStore.EntityFrameworkCore.Database.EventStreamDocumentMetaDataEntity", "MetaData", b1 =>
                        {
                            b1.Property<Guid>("EventStreamDocumentEntityId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Comment")
                                .HasMaxLength(255)
                                .HasColumnType("nvarchar(255)");

                            b1.Property<string>("TenantId")
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.Property<string>("UserId")
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.Property<string>("UserName")
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.HasKey("EventStreamDocumentEntityId");

                            b1.ToTable("Documents");

                            b1.ToJson("MetaData");

                            b1.WithOwner()
                                .HasForeignKey("EventStreamDocumentEntityId");

                            b1.OwnsOne("System.Collections.Generic.Dictionary<string, string>", "Additional", b2 =>
                                {
                                    b2.Property<Guid>("EventStreamDocumentMetaDataEntityEventStreamDocumentEntityId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.HasKey("EventStreamDocumentMetaDataEntityEventStreamDocumentEntityId");

                                    b2.ToTable("Documents");

                                    b2.ToJson("Additional");

                                    b2.WithOwner()
                                        .HasForeignKey("EventStreamDocumentMetaDataEntityEventStreamDocumentEntityId");
                                });

                            b1.Navigation("Additional");
                        });

                    b.Navigation("MetaData")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
