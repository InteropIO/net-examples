namespace FDC3ChannelsClientProfileDemo
{
    enum AppMode
    {
        // "Legacy" WPF app, not sticky, not using Glue
        Legacy = 1,

        // Sticky but still not using Glue
        Sticky = 2,

        // Full-blown interop.io application with full Glue capabilities
        Glue = 3,

        /// <summary>
        ///     Glue Channels and Glue Windows application
        /// </summary>
        Channels = 4,

        /// <summary>
        ///     Selected client will be published as an fdc3.contact
        /// </summary>
        FDC3 = 5,
    }
}