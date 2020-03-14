using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
    public class Profile
    {
		public int ID;
		public int Sex;
		public string Username;
        public string Name;
        public string[] Pictures;
        public string Description;

        public long RegisterDate;
        public long LastActiveDate;
        public float ResponseRate;

        public double? Latitude;
        public double? Longitude;
		public long? LocationTime;
		public float? Distance;

		public byte UserRelation; //only for logged in users
    }
}