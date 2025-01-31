﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;
using Smartstore.Domain;

namespace Smartstore.Forums.Domain
{
    internal class PrivateMessageMap : IEntityTypeConfiguration<PrivateMessage>
    {
        public void Configure(EntityTypeBuilder<PrivateMessage> builder)
        {
            builder.HasOne(c => c.FromCustomer)
                .WithMany()
                .HasForeignKey(c => c.FromCustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.ToCustomer)
                .WithMany()
                .HasForeignKey(c => c.ToCustomerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents a private message.
    /// </summary>
    [Table("Forums_PrivateMessage")]
    public partial class PrivateMessage : BaseEntity
    {
        public PrivateMessage()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PrivateMessage(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier who sent the message.
        /// </summary>
        public int FromCustomerId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier who should receive the message.
        /// </summary>
        public int ToCustomerId { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        [Required, StringLength(450)]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Required, MaxLength]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indivating whether message is read.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Gets or sets a value indivating whether message is deleted by author.
        /// </summary>
        public bool IsDeletedByAuthor { get; set; }

        /// <summary>
        /// Gets or sets a value indivating whether message is deleted by recipient.
        /// </summary>
        public bool IsDeletedByRecipient { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private Customer _fromCustomer;
        /// <summary>
        /// Gets the customer who sent the message.
        /// </summary>
        public Customer FromCustomer
        {
            get => _fromCustomer ?? LazyLoader.Load(this, ref _fromCustomer);
            set => _fromCustomer = value;
        }

        private Customer _toCustomer;
        /// <summary>
        /// Gets the customer who should receive the message.
        /// </summary>
        public Customer ToCustomer
        {
            get => _toCustomer ?? LazyLoader.Load(this, ref _toCustomer);
            set => _toCustomer = value;
        }
    }
}
