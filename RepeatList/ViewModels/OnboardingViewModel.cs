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
                    Title = "Organize Everything",
                    Description = "MiniList lets you manage purchases, tasks and notes effortlessly.",
                    Image = "onboarding1.webp"
                },
                new OnboardingSlide
                {
                    Title = "Fast Input",
                    Description = "Enter many items at once: \"apple 1kg; pear; meat 0.5kg\".",
                    Image = "onboarding2.webp"
                },
                new OnboardingSlide
                {
                    Title = "Share & Collaborate",
                    Description = "Export and import full lists as JSON via WhatsApp and work together.",
                    Image = "onboarding3.webp"
                },
                new OnboardingSlide
                {
                    Title = "Works Offline",
                    Description = "Use MiniList anytime — even without internet.",
                    Image = "onboarding4.webp"
                },
                new OnboardingSlide
                {
                    Title = "Minimal & Intuitive",
                    Description = "A clean, simple interface designed for productivity.",
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

        [RelayCommand]
        private void Skip()
        {
            Preferences.Set("onboarding_seen", true);
            App.Current.MainPage = new AppShell();
        }

    }
}