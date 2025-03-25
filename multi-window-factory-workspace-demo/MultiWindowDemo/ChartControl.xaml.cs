using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;

namespace MultiWindowFactoryDemo
{
    /// <summary>
    /// Interaction logic for ChartControl.xaml
    /// </summary>
    public partial class ChartControl : UserControl, IDisposable
    {
        private readonly Timer timer_;

        public ChartControl()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<double> {4, 6, 5, 2, 4}
                },
                new LineSeries
                {
                    Title = "Series 2",
                    Values = new ChartValues<double> {6, 7, 3, 4, 6},
                    PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Series 3",
                    Values = new ChartValues<double> {4, 2, 7, 2, 7},
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 6
                }
            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };
            YFormatter = value => value.ToString("C");

            //modifying the series collection will animate and update the chart
            SeriesCollection.Add(new LineSeries
            {
                Title = "Series 4",
                Values = new ChartValues<double> { 5, 3, 2, 4 },
                LineSmoothness = 1, //0: straight lines, 1: really smooth lines
                PointGeometry = Geometry.Parse("m 25 70.36218 20 -28 -20 22 -8 -6 z"),
                PointGeometrySize = 10,
                PointForeground = Brushes.Gray
            });

            //modifying any series values will also animate and update the chart
            SeriesCollection[3].Values.Add(5d);

            DataContext = this;

            Random rnd = new Random();
            timer_ = new Timer(_ =>
            {
                ISeriesView seriesView = SeriesCollection[rnd.Next(0, 3)];
                seriesView.Values.Add(rnd.NextDouble() * 15);
                if (seriesView.Values.Count > 15)
                {
                    seriesView.Values.RemoveAt(0);
                }
            }, null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.7));
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public void Dispose()
        {
            timer_.Dispose();
        }
    }
}
