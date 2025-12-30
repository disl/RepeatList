using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepeatList.Models;
using System.Collections.ObjectModel;

namespace RepeatList.ViewModels
{
    public partial class OnboardingViewModel : ObservableObject
    {
        public ObservableCollection<OnboardingSlide> Slides { get; }

        [ObservableProperty]
        private int position;

        public OnboardingViewModel()
        {
            Slides = new ObservableCollection<OnboardingSlide>
            {
                new OnboardingSlide
                {
                    Title = Properties.Resources.OrganizeEverything,
                    Description = Properties.Resources.MiniList_lets_you_manage_purchases,
                    Image = "onboarding1.webp"
                },
                new OnboardingSlide
                {
                    Title = Properties.Resources.FastInput,
                    Description = Properties.Resources.Enter_many_items_at_once,
                    Image = "onboarding2.webp"
                },
                new OnboardingSlide
                {
                    Title =Properties.Resources.Share_Collaborate,
                    Description = Properties.Resources.Export_and_import_full_lists_as_JSON,
                    Image = "onboarding3.webp"
                },
                new OnboardingSlide
                {
                    Title = Properties.Resources.Works_Offline,
                    Description = Properties.Resources.Use_MiniList_anytime,
                    Image = "onboarding4.webp"
                },
                new OnboardingSlide
                {
                    Title = Properties.Resources.Minimal_Intuitive,
                    Description = Properties.Resources.A_clean_simple_interface_designed,
                    Image = "onboarding5.webp"
                }
            };
        }

        [RelayCommand]
        private void Next()
        {
            if (Position < Slides.Count - 1)
                Position++;
            else
                Skip();
        }

        //[RelayCommand]
        //private void Skip()
        //{
        //    Preferences.Set("onboarding_seen", true);

        //    App.Current.MainPage = new AppShell();
        //}

        [RelayCommand]
        private async Task Skip()
        {
            // 1. Status speichern
            Preferences.Set("onboarding_seen", true);

            // 2. Zugriff auf die aktuelle Seite für die Animation
            var currentPage = Application.Current?.MainPage;

            if (currentPage != null)
            {
                // Onboarding langsam ausblenden (500 Millisekunden)
                await currentPage.FadeTo(0, 500, Easing.Linear);
            }

            // 3. Seite wechseln
            // Wir erstellen die Shell, setzen sie auf unsichtbar und weisen sie zu
            var newShell = new AppShell();
            newShell.Opacity = 0;

            Application.Current.MainPage = newShell;

            // 4. Die neue Shell langsam einblenden
            await newShell.FadeTo(1, 500, Easing.Linear);
        }
    }
}