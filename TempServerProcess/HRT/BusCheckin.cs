using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HRT
{
    class BusCheckin
    {
        public class Model
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public DateTime CheckinTime { get; set; }
            public int BusId { get; set; }
            public string Location { get; set; }
            public bool LocationValid { get; set; }
            public int Adherence { get; set; }
            public bool AdherenceValid { get; set; }
            public bool HasRoute { get; set; }
            public bool RouteLookedUp { get; set; }
            public int Route { get; set; }
            public int Direction { get; set; }
            public int StopId { get; set; }
            public string RawData { get; set; }
        }

        public class OutputModel
        {
            public string CheckinTime { get; set; }
            public int BusId { get; set; }
            public string Lat { get; set; }
            public string Lon { get; set; }
            public int Adherence { get; set; }
            public int Route { get; set; }
            public int Direction { get; set; }
        }

        public Model Data { get; set; }

        private BusCheckin() { }
        public static BusCheckin Create(string data)
        {
            if (!String.IsNullOrEmpty(data))
            {
                BusCheckin checkin = new BusCheckin { Data = new Model() };
                string[] parts = data.Split(',');
                DateTime checkinTime;
                int busId;

                if (DateTime.TryParse(parts[1] + "/" + DateTime.Today.Year.ToString() + " " + parts[0], out checkinTime) &&
                    Int32.TryParse(parts[2], out busId))
                {
                    checkin.Data.RawData = data;
                    checkin.Data.RouteLookedUp = false;
                    checkin.Data.CheckinTime = checkinTime;
                    checkin.Data.BusId = busId;
                    checkin.Data.Location = parts[3];
                    checkin.Data.LocationValid = parts[4] == "V";
                    checkin.Data.Adherence = Int32.Parse(parts[5]);
                    checkin.Data.AdherenceValid = parts[6] == "V";

                    int route;
                    if (parts.Length > 7 && Int32.TryParse(parts[7], out route))
                    {
                        checkin.Data.HasRoute = true;
                        checkin.Data.Route = route;
                        checkin.Data.Direction = Int32.Parse(parts[8]);
                        int stopId;
                        checkin.Data.StopId = Int32.TryParse(parts[9], out stopId) ? stopId : -1;
                    }
                    else
                    {
                        checkin.Data.HasRoute = false;
                        checkin.Data.Route = -1;
                        checkin.Data.Direction = -1;
                        checkin.Data.StopId = -1;
                    }

                    return checkin;
                }
            }

            return null;
        }

        internal bool SetRoute(DatabaseConnector db)
        {
            if (Data.HasRoute) return true;

            int route;
            int direction;
            if (db.GetCurrentRouteAndDirectionForBus(Data.BusId, out route, out direction))
            {
                Data.RouteLookedUp = true;
                Data.Route = route;
                Data.Direction = direction;
                return true;
            }
            return false;
        }

        internal static string LatToString(Model checkin)
        {
            var lat = checkin.Location.Split('/')[0];
            return String.Format("{0}.{1}", lat.Substring(0, lat.StartsWith("-") ? 3 : 2), lat.Substring(lat.StartsWith("-") ? 3 : 2));
        }

        internal static string LonToString(Model checkin)
        {
            var lon = checkin.Location.Split('/')[1];
            return String.Format("{0}.{1}", lon.Substring(0, lon.StartsWith("-") ? 3 : 2), lon.Substring(lon.StartsWith("-") ? 3 : 2));
        }

        internal static string CheckinTimeToString(Model checkin)
        {
            var dt = TimeZoneInfo.ConvertTimeFromUtc(checkin.CheckinTime, TimeZoneInfo.Local);
            return String.Format("{0} {1}", dt.ToShortDateString(), dt.ToShortTimeString());
        }
    }
}
