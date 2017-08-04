using System;
using System.Collections.Generic;
using System.Linq;
using Fitbit.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;
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

        public static IEnumerable<Point> ToPoints(this TimeSeriesDataList timeSeries)
        {
            return timeSeries.DataList.Select(DataToPoint);
        }

        private static Point DataToPoint(TimeSeriesDataList.Data data)
        {
            double value = 0;
            double.TryParse(data.Value, out value);
            return new Point() {x = data.DateTime.ToUnixTime(), y = value};
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
                    y = (int)double.Parse(r[result.TargetColumn])
                };
            });

        }

        public static IEnumerable<Point> AlignWith(this List<Point> left, List<Point> right)
        {
            var leftLookup = left.ToLookup(k => k.x, k => k);
            var superset = left.Union(right).OrderBy(p=>p.x);
            foreach (var point in superset)
            {
                if (leftLookup.Contains(point.x))
                {
                    foreach (var existing in leftLookup[point.x])
                    {
                        yield return existing;
                    }
                }
                else
                {
                    yield return new Point() {x = point.x};
                }
            }
            
            
        }

        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }
    }
}