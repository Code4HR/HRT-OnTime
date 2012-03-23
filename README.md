# HRT OnTime
A mobile web app that allows a user to enter an HRT route number and then see a map with the current position of all buses running that route.

### How It Works
Written in Javascript and HTML5.
The app uses jQuery to pull and parse a chunk of JSON encoded bus location data from a CouchDB instance - http://hrt.iriscouch.com/hrtransit
Every bus object has a location and a bus id, but most do not have a route. The data is expected to have multiple entries for each bus id, each representing a different location.
The app first iterates through the data to find every bus id that is on the user's route.
It then iterates through the data a second time to find the latest location for each bus id found in the previous step.
Finally, the app uses the Google Maps javascript API to map the location of each bus, along with the user's location.

## no_couch Branch
At the time of writing this, the CouchDB is not being updated with new data.
This branch was created to demo the app with live data.

### How It Works
A C# program runs every 30 seconds on a sever.
This program grabs the real time bus data from the HRT ftp server - ftp://216.54.15.3/Anrd/hrtrtf.txt
Next, it creates a bus check-in object from each line in the file.
If a bus check-in object does not have a route, the program looks through older checkins stored in its local MongoDB instance for the most recent check-in with the same bus id and a route number. If one is found, the route number is assosicated with this check-in.
The program then checks to see if this check-in (same time and bus id) is already in MongoDB. If not, the check-in is stored.
Finally, the program pulls all the checkins from the last hour and creates a json file that contains the most recent check-in of each bus.

The mobile app was modified to handle the data from this temporary backend processing.
Since more processing is done on the backend and there are not multiple objects for any bus id, the app only has to iterate through the data once and find each object with the desired route number.
Finally, the app uses the Google Maps javascript API to map the location of each bus, along with the user's location.
