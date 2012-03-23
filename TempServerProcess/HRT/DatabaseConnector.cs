using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HRT
{
    class DatabaseConnector
    {
        private MongoDatabase _db;
        private static DatabaseConnector _singleton;
        public static DatabaseConnector Instance
        {
            get { return _singleton ?? (_singleton = new DatabaseConnector()); }
        }

        private DatabaseConnector() { }

        public void Init(string db)
        {
            var server = MongoServer.Create();
            _db = server.GetDatabase(db);
        }

        internal bool AddCheckin(BusCheckin checkin)
        {
            var existingCheckin = _db.GetCollection<BusCheckin.Model>("checkins").FindOne(Query.And(Query.EQ("CheckinTime", checkin.Data.CheckinTime), Query.EQ("BusId", checkin.Data.BusId)));

            if (existingCheckin == null)
            {
                _db.GetCollection<BusCheckin.Model>("checkins")
                   .Insert(checkin.Data);
                return true;
            }

            return false;
        }

        internal void CheckinsToJson()
        {
            var checkins = _db.GetCollection<BusCheckin.Model>("checkins")
                              .Find(Query.GTE("CheckinTime", DateTime.Now.AddHours(-1)))
                              .SetSortOrder(SortBy.Descending("CheckinTime"));

            List<int> busIds = new List<int>();
            List<BusCheckin.OutputModel> checkinsForFile = new List<BusCheckin.OutputModel>();
            foreach (var checkin in checkins)
            {
                if (!busIds.Contains(checkin.BusId) && checkin.Route != -1)
                {
                    busIds.Add(checkin.BusId);
                    checkinsForFile.Add(new BusCheckin.OutputModel
                                            {
                                                CheckinTime = BusCheckin.CheckinTimeToString(checkin),
                                                BusId = checkin.BusId,
                                                Lat = BusCheckin.LatToString(checkin),
                                                Lon = BusCheckin.LonToString(checkin),
                                                Adherence = checkin.Adherence,
                                                Route = checkin.Route,
                                                Direction = checkin.Direction
                                            });
                }
            }

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();

            using (var file = new StreamWriter(@"C:\Users\Bootcamp\Dropbox\Public\hrt_bus_data.json", false))
            {
                file.Write(serializer.Serialize(checkinsForFile));
            }
                
        }

        internal void PurgeOldCheckins()
        {
            _db.GetCollection<BusCheckin.Model>("checkins")
               .Remove(
                    Query.LT("CheckinTime", DateTime.Now.AddHours(-8)));
        }

        internal bool GetCurrentRouteAndDirectionForBus(int busId, out int route, out int direction)
        {
            route = -1;
            direction = -1;

            var checkins = _db.GetCollection<BusCheckin.Model>("checkins")
                              .Find(
                                  Query.And(
                                      Query.EQ("BusId", busId), 
                                      Query.NE("Route", -1)))
                              .SetSortOrder(SortBy.Descending("CheckinTime"));

            foreach (var checkin in checkins)
            {
                route = checkin.Route;
                direction = checkin.Direction;
                return true;
            }

            return false;
        }
    }
}
