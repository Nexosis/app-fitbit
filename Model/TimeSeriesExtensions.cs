using System;
using System.Collections.Generic;
using System.Linq;
using Fitbit.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nexosis.Api.Client.Model;

namespace NexosisFitbit.Model
{
    public static class TimeSeriesExtensions
    {
        public static IEnumerable<Point> ToPoints(this TimeSeriesDataListInt timeSeries)
        {
            return timeSeries.DataList.Select(d => new Point() {x = d.DateTime.ToUnixTime(), y = d.Value});
        }

        public static IEnumerable<Point> ToPoints(this SessionResult result)
        {
            if (result == null)
            {
                return Enumerable.Empty<Point>();
            }

            return result.Data.Select(r =>
            {
                return new Point()
                {
                    x = DateTime.Parse(r["timeStamp"]).ToUnixTime(),
                    y = (int)double.Parse(r["steps"])
                };
            });

        }

        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }
    }
}