using System;

namespace net_channels_wpf
{
    class CustomFdc3ContactId
    {
        public string displayName { get; set; }
    }
    class CustomFdc3Contact
    {
        public string type { get; set; } = "fdc3.contact";
        public string name { get; set; }
        public DateTime lastUpdated { get; set; }
        public CustomFdc3ContactId id { get; set; }
    }
}
