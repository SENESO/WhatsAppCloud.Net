using System.Collections.Generic;

namespace WhatsAppCloud.Models
{
    /// <summary>A contact card for <see cref="WhatsAppClient.SendContactsAsync"/>.</summary>
    public class WhatsAppContact
    {
        public string FormattedName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<ContactPhone> Phones { get; set; } = new List<ContactPhone>();
    }

    public class ContactPhone
    {
        public string Phone { get; set; }

        /// <summary>CELL, MAIN, IPHONE, HOME, WORK…</summary>
        public string Type { get; set; } = "CELL";

        /// <summary>Optional WhatsApp id for the number.</summary>
        public string WhatsAppId { get; set; }
    }

    /// <summary>A reply button for <see cref="WhatsAppClient.SendButtonsAsync"/> (max 3).</summary>
    public class ReplyButton
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    /// <summary>A section in an interactive list message.</summary>
    public class ListSection
    {
        public string Title { get; set; }
        public List<ListRow> Rows { get; set; } = new List<ListRow>();
    }

    public class ListRow
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
