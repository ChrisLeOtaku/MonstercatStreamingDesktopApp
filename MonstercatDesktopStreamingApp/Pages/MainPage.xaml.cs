﻿using MonstercatDesktopStreamingApp.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace MonstercatDesktopStreamingApp.Pages
{
    public sealed partial class MainPage : Page
    {
        public static Stack<TrackObject> queue;
        public static string authentication = "";
        public static TextBlock nowPlaying;
        public static Frame window;
        public static MediaPlayer mediaPlayer;
        public static List<Album> albums;
        public static MediaPlayerElement mediaPlayerGUI;
        public static Stack<TrackObject> history;
        public static TrackObject currentSong;
        public static SystemMediaTransportControls smtc;

        public MainPage()
        {
            this.InitializeComponent();
            currentSong = null;
            window = windowView;
            queue = new Stack<TrackObject>();
            albums = new List<Album>();
            history = new Stack<TrackObject>();
            nowPlaying = this.nowPlayingTitle;
            mediaPlayerGUI = this.mediaPlayerUI;
            mediaPlayerUI.Visibility = Visibility.Collapsed;
            mediaPlayer = mediaPlayerUI.MediaPlayer;
            windowView.Navigate(typeof(LoginPage));
            mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.SourceChanged += MediaPlayer_SourceChanged;
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (!mediaPlayer.IsLoopingEnabled)
                {
                    if (queue.Count > 0)
                    {
                        history.Push(currentSong);
                        TrackObject o = queue.Pop();
                        currentSong = o;
                        window.Navigate(typeof(SongView));
                    }
                    else if (queue.Count == 0)
                    {
                        EndGUIPlayback();
                        currentSong = null;
                        mediaPlayer.Source = null;
                        history = new Stack<TrackObject>();
                        ReturnToHomePage();
                    }
                }
            });
        }

        private async void MediaPlayer_SourceChanged(MediaPlayer sender, object args)
        {
            if (mediaPlayer.Source != null)
            {
                StartGUIPlayback();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (queue.Count == 0)
                    {
                        mediaPlayerGUI.TransportControls.IsNextTrackButtonVisible = false;
                        mediaPlayerGUI.TransportControls.IsPreviousTrackButtonVisible = false;
                    }
                    else
                    {
                        mediaPlayerGUI.TransportControls.IsNextTrackButtonVisible = true;
                    }

                    if (history.Count > 0)
                    {
                        mediaPlayerGUI.TransportControls.IsPreviousTrackButtonVisible = true;
                    }
                });

                mediaPlayer.Play();
            }
        }

        private async void ReturnToHomePage()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                nowPlaying.Text = "Now Playing: ";
                window.Navigate(typeof(LibraryView));
            });
        }

        public async void StartGUIPlayback()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                mediaPlayerGUI.Visibility = Visibility.Visible;
                if (queue.Count == 0)
                {
                    mediaPlayerGUI.TransportControls.IsNextTrackButtonVisible = false;
                    mediaPlayerGUI.TransportControls.IsPreviousTrackButtonVisible = false;
                }
                else
                {
                    mediaPlayerGUI.TransportControls.IsNextTrackButtonVisible = true;
                }
            });
        }

        public async void EndGUIPlayback()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                mediaPlayerGUI.Visibility = Visibility.Collapsed;
            });
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if (history.Count >= 1)
            {
                queue.Push(currentSong);
                currentSong = history.Pop();
                window.Navigate(typeof(SongView));
            }
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            if (queue.Count >= 1)
            {
                history.Push(currentSong);
                currentSong = queue.Pop();
                window.Navigate(typeof(SongView));
            }
        }

        private void Library_Clicked(object sender, RoutedEventArgs e)
        {
            if (windowView.CurrentSourcePageType != typeof(LibraryView) && windowView.CurrentSourcePageType != typeof(LoginPage)
                && windowView.CurrentSourcePageType != typeof(ForgotPage) && windowView.CurrentSourcePageType != typeof(RegisterPage))
            {
                windowView.Navigate(typeof(LibraryView));
            }
            else if (authentication.Equals(""))
            {
                DisplayNotLoggedInDialog();
            }
        }

        private void NowPlaying_Clicked(object sender, RoutedEventArgs e)
        {
            if (windowView.CurrentSourcePageType != typeof(SongView) && windowView.CurrentSourcePageType != typeof(LoginPage)
                && windowView.CurrentSourcePageType != typeof(ForgotPage) && windowView.CurrentSourcePageType != typeof(RegisterPage))
            {
                if (currentSong != null)
                {
                    windowView.Navigate(typeof(SongView), currentSong);
                }
            }
            else if (authentication.Equals(""))
            {
                DisplayNotLoggedInDialog();
            }
        } 

        private void Change_Clicked(object sender, RoutedEventArgs e)
        {
            if (windowView.CurrentSourcePageType != typeof(ChangePage) && windowView.CurrentSourcePageType != typeof(LoginPage)
               && windowView.CurrentSourcePageType != typeof(ForgotPage) && windowView.CurrentSourcePageType != typeof(RegisterPage))
            {
                windowView.Navigate(typeof(ChangePage));
            }
            else if (authentication.Equals(""))
            {
                DisplayNotLoggedInDialog();
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return "Basic " + System.Convert.ToBase64String(plainTextBytes);
        }

        private async void DisplayNotLoggedInDialog()
        {
            ContentDialog notLoggedIn = new ContentDialog
            {
                RequestedTheme = ElementTheme.Dark,
                Title = "User Login is Required",
                Content = "Please log in and try again!",
                CloseButtonText = "Ok"
            };

            ContentDialogResult result = await notLoggedIn.ShowAsync();
        }

        private async void DisplayNextDialog()
        {
            ContentDialog notLoggedIn = new ContentDialog
            {
                RequestedTheme = ElementTheme.Dark,
                Title = "NEXT",
                Content = "NEXT SONG BUTTON WAS PRESSED",
                CloseButtonText = "Ok"
            };

            ContentDialogResult result = await notLoggedIn.ShowAsync();
        }
    }
}
