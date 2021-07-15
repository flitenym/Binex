using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SharedLibrary.AbstractClasses;

namespace SharedLibrary.Helper.Classes
{
    public class Node : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public string Name { get; set; }
        public Guid ID { get; set; }
        public ObservableCollection<Node> Nodes { get; set; }
        public string ParentName { get; set; }
        public ModuleBase View { get; set; }

        private bool selected = false;
        public bool Selected
        {
            get { return selected; }
            set
            { 
                selected = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Selected)));
            }
        }

        private bool expanded = false;
        public bool Expanded
        {
            get { return expanded; }
            set
            {
                expanded = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Expanded)));
            }
        }

    }
}
