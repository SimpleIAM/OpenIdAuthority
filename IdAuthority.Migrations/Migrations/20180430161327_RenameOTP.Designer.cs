﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using SimpleIAM.IdAuthority.Entities;
using System;

namespace SimpleIAM.IdAuthority.Migrations.Migrations
{
    [DbContext(typeof(IdAuthorityDbContext))]
    [Migration("20180430161327_RenameOTP")]
    partial class RenameOTP
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SimpleIAM.IdAuthority.Entities.OneTimeCode", b =>
                {
                    b.Property<string>("Email")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(254);

                    b.Property<DateTime>("ExpiresUTC");

                    b.Property<string>("LinkCode")
                        .HasMaxLength(36);

                    b.Property<string>("OTC")
                        .HasMaxLength(8);

                    b.Property<string>("RedirectUrl")
                        .HasMaxLength(2048);

                    b.HasKey("Email");

                    b.ToTable("OneTimeCodes");
                });

            modelBuilder.Entity("SimpleIAM.IdAuthority.Entities.PasswordHash", b =>
                {
                    b.Property<string>("SubjectId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(36);

                    b.Property<int>("FailedAuthenticationCount");

                    b.Property<string>("Hash")
                        .IsRequired();

                    b.Property<DateTime>("LastChangedUTC");

                    b.Property<DateTime?>("TempLockUntilUTC");

                    b.HasKey("SubjectId");

                    b.ToTable("PasswordHashes");
                });

            modelBuilder.Entity("SimpleIAM.IdAuthority.Entities.Subject", b =>
                {
                    b.Property<string>("SubjectId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(36);

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(254);

                    b.HasKey("SubjectId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Subjects");
                });
#pragma warning restore 612, 618
        }
    }
}