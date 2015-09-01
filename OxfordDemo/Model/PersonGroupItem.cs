using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OxfordDemo.Model
{
    public class PersonGroupItem
    {
        public PersonGroup Group { get; set; }
        public ObservableCollection<Person> Persons { get; } = new ObservableCollection<Person>();
        public DateTime LastTrained { get; set; }
    }
}
