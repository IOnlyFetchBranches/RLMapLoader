using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.Firestore;

namespace RLMapLoader.Components.Models
{
    [FirestoreData]
    public class Group
    {
        public string ID { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public List<string> Members { get; set; }
        [FirestoreProperty]
        public bool IsNew { get; set; }
        [FirestoreProperty]
        public string Owner { get; set; }
    }
}
