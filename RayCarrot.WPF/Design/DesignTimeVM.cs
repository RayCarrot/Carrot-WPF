﻿using RayCarrot.CarrotFramework;
using RayCarrot.CarrotFramework.UI;
using RayCarrot.Windows;
using System.Collections.Generic;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Static view models to bind to during design time
    /// </summary>
    public static class DesignTimeVM
    {
        private static void BuildFramework()
        {
            if (!RCF.IsBuilt)
            {
                RCF.Build(x =>
                {
                    x.AddWindowsFileInfoManager<DefaultWindowsFileInfoManager>();
                    x.AddWindowsManager<DefaultWindowsManager>();
                    x.AddExceptionHandler<DefaultExceptionHandler>();
                    x.AddRegistryManager<DefaultRegistryManager>();
                });
            }
        }

        public static DialogMessageViewModel DialogMessageViewModel => new DialogMessageViewModel<bool>()
        {
            MessageText = "This is a text message which will appear for the user to read before pressing a button. Great huh?",
            MessageHeader = "This is the header",
            MessageType = MessageType.Error,
            DialogImageSource = @"C:\Users\RayCarrot\Desktop\Allt som jag bryr mig om\Visual Studio\Rayman Game Launcher\Files\Images\Info.png",
            DialogActions = new List<DialogMessageActionViewModel<bool>>
            {
                new DialogMessageActionViewModel<bool>()
                {
                    DisplayText = "CANCEL",
                    DisplayDescription = "Press the other button. Not this one. Stay AWAY",
                    ActionResult = false
                },
                new DialogMessageActionViewModel<bool>()
                {
                    DisplayText = "OK",
                    DisplayDescription = "It's okay. Press the button. I promise",
                    ActionResult = true
                },
            }
        };

        public static DriveSelectionViewModel DriveSelectionViewModel
        {
            get
            {
                BuildFramework();

                var vm = new DriveSelectionViewModel(new DriveBrowserViewModel()
                {
                    Title = "Select a Drive"
                });
                vm.Refresh();
                return vm;
            }
        }

        public static RegistrySelectionViewModel RegistrySelectionViewModel
        {
            get
            {
                BuildFramework();

                return new RegistrySelectionViewModel()
                {

                };
            }
        }
    }
}