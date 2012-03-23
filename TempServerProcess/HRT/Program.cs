using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

namespace HRT
{
    class Program
    {
        static void Main(string[] args)
        {
            // Open DB
            DatabaseConnector db = DatabaseConnector.Instance;
            db.Init("HRT");

            List<int> busIdsRoute = new List<int>();
            List<int> busIdsNoRoute = new List<int>();
            while (true)
            {
                string contents = GetFileFromServer(new Uri("ftp://216.54.15.3/Anrd/hrtrtf.txt"));
                List<BusCheckin> checkins = GetBusCheckinsFromFile(contents);
                int routeInfo = 0;
                int checkinsAdded = 0;
                int both = 0;

                busIdsRoute.Clear();
                busIdsNoRoute.Clear();
                foreach (BusCheckin checkin in checkins)
                {
                    // For each checkin without a route number, try to find the correct route number in the db
                    bool hasRoute = false;
                    if (checkin.SetRoute(db))
                    {
                        hasRoute = true;
                        if (!busIdsRoute.Contains(checkin.Data.BusId))
                            busIdsRoute.Add(checkin.Data.BusId);
                    }
                    else
                    {
                        if (!busIdsNoRoute.Contains(checkin.Data.BusId))
                            busIdsNoRoute.Add(checkin.Data.BusId);
                    }

                    // Write checkins to db - this process will make sure not to write duplicates
                    bool added = false;
                    if (db.AddCheckin(checkin)) added = true;

                    if (hasRoute) routeInfo++;
                    if (added) checkinsAdded++;
                    if (hasRoute && added) both++;
                }

                Console.WriteLine(
                    String.Format("{0} - In File: {1} - With Route Info: {2} - Added To DB: {3} - Both: {4}", 
                    DateTime.Now.ToShortTimeString(), 
                    checkins.Count, 
                    routeInfo, 
                    checkinsAdded,
                    both));

                Console.WriteLine(busIdsRoute.Count + " Buses with a route");

                Console.Write(busIdsNoRoute.Count + " Buses without a route:");
                busIdsNoRoute.Sort();
                foreach (var bus in busIdsNoRoute)
                    Console.Write(" " + bus);
                Console.WriteLine("");

                // Write db contents as json to file
                db.CheckinsToJson();

                // Delete data older than one hour from db
                db.PurgeOldCheckins();

                Thread.Sleep(new TimeSpan(0, 0, 30));
            }
        }

        public static string GetFileFromServer(Uri serverUri)
        {
            // The serverUri parameter should start with the ftp:// scheme.
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return null;
            }
            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            try
            {
                byte[] newFileData = request.DownloadData(serverUri.ToString());
                string fileString = System.Text.Encoding.UTF8.GetString(newFileData);
                return fileString;
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
            }
            return null;
        }

        public static List<BusCheckin> GetBusCheckinsFromFile(string file)
        {
            List<BusCheckin> busCheckins = new List<BusCheckin>();

            if (!String.IsNullOrEmpty(file))
            {
                foreach (string line in file.Split('\n', '\r'))
                {
                    BusCheckin checkin = BusCheckin.Create(line);
                    if (checkin != null)
                        busCheckins.Add(checkin);
                }
            }

            return busCheckins;
        }
    }
}
