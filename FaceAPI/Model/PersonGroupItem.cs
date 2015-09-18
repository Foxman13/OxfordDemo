using System;
using System.Collections.ObjectModel;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceAPI.Model
{
    public class PersonGroupItem
    {
        public PersonGroup Group { get; set; }
        public ObservableCollection<Person> Persons { get; } = new ObservableCollection<Person>();
        public DateTime LastTrained { get; set; }
        public bool IsTraining { get; set; }
        public bool NeedsTraining { get; set; }
    }
}
