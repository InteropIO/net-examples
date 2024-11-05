using System.Collections.Generic;
using Tick42.Entities;

namespace FDC3ChannelsClientProfileDemo.POCO
{
    class ClientData
    {
        public string Name { get; set; }
        public T42Contact Contact { get; set; }
        public double PortfolioValue { get; set; }
        public IEnumerable<PortfolioData> Portfolio { get; internal set; }
    }
}