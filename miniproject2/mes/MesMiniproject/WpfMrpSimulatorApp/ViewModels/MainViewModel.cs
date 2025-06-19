using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMrpSimulatorApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private string _greeting;
        public string Greeting
        {
            get => _greeting;
            set => SetProperty(ref _greeting, value);
        }

        public MainViewModel()
        {
            Greeting = "MRP 공정관리";
        }
    }
}
