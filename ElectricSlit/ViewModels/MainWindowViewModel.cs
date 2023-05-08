﻿using Prism.Mvvm;



using MotorAPIPlus;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectricSlit.ViewModels
{
    public class MainWindowViewModel : BindableBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _title = "光源控制";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }


        public MainWindowViewModel()
        {

        }
    }
}
