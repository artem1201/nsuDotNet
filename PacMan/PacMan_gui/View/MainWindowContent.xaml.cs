﻿//  author: Artem Sumanev

using System;
using System.Threading;
using System.Windows;
using PacMan_gui.Annotations;
using PacMan_gui.Controllers;
using PacMan_gui.View.About;
using PacMan_gui.View.Level;

namespace PacMan_gui.View {
    /// <summary>
    ///     Interaction logic for MainWindowContent.xaml
    /// </summary>
    public partial class MainWindowContent {
        private readonly MainWindow _mainWindow;
        private AboutBox _aboutBox;


        private ChampionsController _championsController;
        private GameController _gameController;

        private SettingsController _settingsController;

        #region Initialization

        private bool _areElementsInitialized;

        public MainWindowContent([NotNull] MainWindow mainWindow) {
            if (null == mainWindow) {
                throw new ArgumentNullException("mainWindow");
            }
            InitializeComponent();

            _mainWindow = mainWindow;


            InitNestedElemets();
        }

        private void InitNestedElemets() {
            if (_areElementsInitialized) {
                return;
            }

            Application.Current.Exit += (sender, args) => OnExit();

            try {
                _championsController = new ChampionsController(OnBackToMainWindow);
            }
            catch (Exception) {
                MessageBox.Show("existed champions file is invalid, new file is used");
            }


            _settingsController = new SettingsController(OnBackToMainWindow);

            _gameController = new GameController(
                OnGameEnds,
                () => {
                    OnExit();
                    _mainWindow.Close();
                },
                _settingsController.GetSettingsViewModel().KeysToDirection,
                _settingsController.GetSettingsViewModel().PauseKeys);


            _aboutBox = new AboutBox(OnBackToMainWindow);

            _areElementsInitialized = true;
        }

        #endregion

        //  changes content of window to main window
        private void OnBackToMainWindow() {
            Application.Current.Dispatcher.Invoke(
                delegate { _mainWindow.ContentControl.Content = this; });
        }

        #region Playing

        private void PlayButton_OnClick(object sender, RoutedEventArgs e) {
            _mainWindow.ContentControl.Content = _gameController.GetGameView();

            var gameView = _mainWindow.ContentControl.Content as GameView;
            if (null != gameView) {
                gameView.Loaded +=
                    delegate { _gameController.Run(_championsController.GetChampionsTable().GetBestScore()); };
            }
            else {
                throw new Exception("game view has not been loaded");
            }
        }

        private void OnGameEnds(int gameScore) {
            OnBackToMainWindow();

            if (_championsController.GetChampionsTable().IsNewRecord(gameScore)) {
                AddNewRecord(gameScore);
            }
        }

        #endregion

        #region Exit

        private void ExitButton_OnClick(object sender, RoutedEventArgs e) {
            OnExit();
            _mainWindow.Close();
        }

        private void OnExit() {
            //  save champions to file
            _championsController.Dispose();
        }

        #endregion

        #region Champions

        private static readonly string ChampionNameEmpty = String.Empty;

        private readonly AutoResetEvent _nameEnteredEvent = new AutoResetEvent(false);
        private bool _addNewRecord;
        private string _newChampionName = ChampionNameEmpty;

        private void AddNewRecord(int score) {
            //  show input box
            Application.Current.Dispatcher.BeginInvoke(
                new Action(delegate { InputNameBox.Visibility = Visibility.Visible; }));

            //  wait for name
            _nameEnteredEvent.WaitOne();
            //  reset waiter
            _nameEnteredEvent.Reset();

            Application.Current.Dispatcher.BeginInvoke(
                new Action(
                    delegate {
                        //  clean input area and hide input box
                        InputNameTextBox.Text = ChampionNameEmpty;
                        InputNameBox.Visibility = Visibility.Collapsed;
                    }));


            if (_addNewRecord) {
                //  add new champion
                _championsController.GetChampionsTable().AddNewResult(score, _newChampionName);
            }

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

            _addNewRecord = true;

            //  notify name has been entered
            _nameEnteredEvent.Set();
        }

        private static bool WrongName(string name) {
            return null != name && name.Equals(ChampionNameEmpty);
        }

        private void ChampionsButton_OnClick(object sender, RoutedEventArgs e) {
            _mainWindow.ContentControl.Content = _championsController.GetChampionsTableView();

            _championsController.Run();
        }

        private void NoButton_OnClick(object sender, RoutedEventArgs e) {
            _addNewRecord = false;

            //  notify name has been entered
            _nameEnteredEvent.Set();
        }

        #endregion

        #region Settings

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e) {
            _mainWindow.ContentControl.Content = _settingsController.GetSettingsView();
        }

        #endregion

        #region About

        private void AboutButton_OnClick(object sender, RoutedEventArgs e) {
            _mainWindow.ContentControl.Content = _aboutBox;
        }

        #endregion
    }
}