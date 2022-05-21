﻿// <auto-generated />
using System;
using Fakemail.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Fakemail.Data.EntityFramework.Migrations
{
    [DbContext(typeof(FakemailDbContext))]
    partial class FakemailDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.5");

            modelBuilder.Entity("Fakemail.Data.EntityFramework.Attachment", b =>
                {
                    b.Property<Guid>("AttachmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Content")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("ContentChecksum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("EmailId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("AttachmentId");

                    b.HasIndex("EmailId");

                    b.ToTable("Attachment");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.Email", b =>
                {
                    b.Property<Guid>("EmailId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BodyChecksum")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BodyLength")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BodySummary")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CC")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DeliveredTo")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("From")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("MimeMessage")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("ReceivedFromDns")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceivedFromHost")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceivedFromIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceivedSmtpId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ReceivedTimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceivedTlsInfo")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SequenceNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SmtpUsername")
                        .HasColumnType("TEXT");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("To")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("EmailId");

                    b.HasIndex("SmtpUsername");

                    b.ToTable("Email");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.SmtpAlias", b =>
                {
                    b.Property<string>("Account")
                        .HasColumnType("TEXT");

                    b.HasKey("Account");

                    b.ToTable("SmtpAlias");

                    b.HasData(
                        new
                        {
                            Account = "fakemail"
                        });
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.SmtpUser", b =>
                {
                    b.Property<string>("SmtpUsername")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedTimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<int>("CurrentEmailSequenceNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SmtpPassword")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SmtpPasswordCrypt")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedTimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("SmtpUsername");

                    b.HasIndex("UserId");

                    b.ToTable("SmtpUser");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedTimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PasswordCrypt")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedTimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("User");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.Attachment", b =>
                {
                    b.HasOne("Fakemail.Data.EntityFramework.Email", "Email")
                        .WithMany("Attachments")
                        .HasForeignKey("EmailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Email");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.Email", b =>
                {
                    b.HasOne("Fakemail.Data.EntityFramework.SmtpUser", "SmtpUser")
                        .WithMany("Emails")
                        .HasForeignKey("SmtpUsername");

                    b.Navigation("SmtpUser");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.SmtpUser", b =>
                {
                    b.HasOne("Fakemail.Data.EntityFramework.User", "User")
                        .WithMany("SmtpUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.Email", b =>
                {
                    b.Navigation("Attachments");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.SmtpUser", b =>
                {
                    b.Navigation("Emails");
                });

            modelBuilder.Entity("Fakemail.Data.EntityFramework.User", b =>
                {
                    b.Navigation("SmtpUsers");
                });
#pragma warning restore 612, 618
        }
    }
}
