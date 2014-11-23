﻿using System;
using System.Threading;
using System.Windows;
using PacMan_gui.Annotations;
using PacMan_gui.Controllers;
using PacMan_gui.View.Champions;
using PacMan_gui.View.Level;
using PacMan_model.champions;

namespace PacMan_gui.View {
    /// <summary>
    /// Interaction logic for MainWindowContent.xaml
    /// </summary>
    public partial class MainWindowContent {

        private readonly MainWindow _mainWindow;
        private readonly IChampionsTable _championsTable;

        public MainWindowContent([NotNull] MainWindow mainWindow) {
            if (null == mainWindow) {
                throw new ArgumentNullException("mainWindow");
            }
            InitializeComponent();
            _championsTable = new ChampionsTable();
            _mainWindow = mainWindow;
            Application.Current.Exit += (sender, args) => OnExit();
        }

        //  changes content of window to main window
        private void OnBackToMainWindow() {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(delegate {
                _mainWindow.ContentControl.Content = this;

            }));
        }

        private void PlayButton_OnClick(object sender, RoutedEventArgs e) {
            
            _mainWindow.ContentControl.Content = new GameView();

            (_mainWindow.ContentControl.Content as GameView).Loaded += delegate {

                var gameController = new GameController(_mainWindow.ContentControl.Content as GameView, OnGameEnds);
                gameController.Run();
            };

        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e) {

            OnExit();
            _mainWindow.Close();
        }

        private void OnExit() {
            //  save champions to file
            _championsTable.Dispose();
        }

        private void OnGameEnds(int bestScore, int gameScore) {

            OnBackToMainWindow();
            
            if (_championsTable.IsNewRecord(gameScore)) {
                AddNewRecord(gameScore);
            }

        }

        private static readonly string ChampionNameEmpty = String.Empty;

        private string _newChampionName = ChampionNameEmpty;
        private readonly AutoResetEvent _nameEnteredEvent = new AutoResetEvent(false);
        private void AddNewRecord(int score) {

            //  show input box
            Application.Current.Dispatcher.BeginInvoke(
                new Action(delegate {
                    InputNameBox.Visibility = Visibility.Visible;

                }));

            //  wait for valid name
            while (WrongName(_newChampionName)) {
                _nameEnteredEvent.WaitOne();
            }
            //  reset waiter
            _nameEnteredEvent.Reset();

            //  add new champion
            _championsTable.AddNewResult(score, _newChampionName);

            //  clean champion name
            _newChampionName = ChampionNameEmpty;
        }

        //  calls when user entered his name
        private void OkButton_OnClick(object sender, RoutedEventArgs e) {

            //  check if name is valid
            //  else wait for valid one
            if (WrongName(InputNameTextBox.Text)) {
                MessageBox.Show("Invalid name! Try again");
                return;
            }

            //  set name
            _newChampionName = InputNameTextBox.Text;

            //  clean input area and hide input box
            InputNameTextBox.Text = ChampionNameEmpty;
            InputNameBox.Visibility = Visibility.Collapsed;

            //  notify name has been entered
            _nameEnteredEvent.Set();

        }

        private bool WrongName(string name) {
            if (null == name) {
                return false;
            }
            return name.Equals(ChampionNameEmpty);
        }

        private void ChampionsButton_OnClick(object sender, RoutedEventArgs e) {
            _mainWindow.ContentControl.Content = new ChampionsTableView();

            (_mainWindow.ContentControl.Content as ChampionsTableView).Loaded += delegate {

                var championsController = new ChampionsController(_mainWindow.ContentControl.Content as ChampionsTableView, _championsTable, OnBackToMainWindow);
                championsController.Run();
            };
        }
    }
}
