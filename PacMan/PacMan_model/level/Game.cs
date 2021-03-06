﻿//  author: Artem Sumanev

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using PacMan_model.level.cells.ghosts;
using PacMan_model.level.cells.pacman;
using PacMan_model.util;

namespace PacMan_model.level {
    public sealed class Game : IGame {
        //  directory where levels are
        private string _pathToLevels;

        private Ticker _ticker;

        //  current level of company
        private Level _currentLevel;
        private readonly ILevelLoader _levelLoader;

        //  best score of current company
        private int _bestScore;
        //  score of current game
        private int _currentScore;
        //  score of current level
        private int _currentLevelScore;


        private bool _isWon;
        private bool _isFinished;

        private string[] _levelFiles;
        private int _currentLevelNumber;


        private readonly ICollection<IDirectionEventObserver> _observers = new List<IDirectionEventObserver>();

        #region Initialization

        public Game(string pathToLevels, string pathToGhosts, int bestScore) {
            if (null == pathToLevels) {
                throw new ArgumentNullException("pathToLevels");
            }
            if (null == pathToGhosts) {
                throw new ArgumentNullException("pathToGhosts");
            }

            _levelLoader = new LevelLoader(new GhostFactory(pathToGhosts));


            _ticker = new Ticker(DoATick);


            NewGame(bestScore, pathToLevels);
        }

        public void NewGame(int bestScore) {
            _ticker.Stop();

            if (_ticker.IsDisposed()) {
                _ticker = new Ticker(DoATick);
            }

            _isWon = false;
            _isFinished = false;

            _bestScore = bestScore;
            _currentScore = 0;
            _currentLevelScore = 0;

            try {
                _levelFiles = Directory.GetFiles(_pathToLevels);
            }
            catch (Exception e) {
                throw new InvalidLevelDirectory(_pathToLevels, e);
            }

            if (0 == _levelFiles.Length) {
                throw new InvalidLevelDirectory(_pathToLevels);
            }

            _currentLevelNumber = -1;
            LoadNextLevel();
        }

        public void NewGame(int bestScore, string pathToLevels) {
            if (null == pathToLevels) {
                throw new ArgumentNullException("pathToLevels");
            }
            _pathToLevels = pathToLevels;

            NewGame(bestScore);
        }

        #endregion

        #region Level loading

        private bool HasNextLevel {
            get { return _levelFiles.Length - 1 != _currentLevelNumber; }
        }

        public bool LoadNextLevel() {
            _ticker.Stop();

            if (_levelFiles.Length - 1 == _currentLevelNumber) {
                return false;
            }

            ++_currentLevelNumber;

            _currentLevelScore = 0;

            using (var nextLevelSource = new FileStream(_levelFiles[_currentLevelNumber], FileMode.Open)) {
                if (null != _currentLevel) {
                    _currentLevel.Dispose();
                }

                try {
                    _currentLevel = _levelLoader.LoadFromSource(nextLevelSource);
                }
                catch (InvalidLevelSource e) {
                    throw new CannotPlayGameException(
                        "Invalid level source: " + _levelFiles[_currentLevelNumber] + ", cause " + e.GetMessage());
                }


                _currentLevel.PacMan.PacmanState += OnPacManChanged;
                _currentLevel.Field.DotsEnds += OnDotsEnds;

                foreach (var directionEventObserver in _observers) {
                    _currentLevel.RegisterOnDirectionObserver(directionEventObserver);
                }
            }

            return true;
        }

        #endregion

        #region Start Stop

        public void Start() {
            _ticker.Start();
        }

        public void Pause() {
            _ticker.Stop();
        }

        public bool IsOn() {
            return _ticker.IsOn();
        }

        #endregion

        #region Getters

        public int GetGameScore() {
            return _currentScore;
        }

        public int GetLevelScore() {
            return _currentLevelScore;
        }

        public int GetBestScore() {
            return _bestScore;
        }

        #endregion

        #region Win Loose

        public void Win() {
            _ticker.Stop();

            if (!HasNextLevel) {
                //  no more levels
                //  total win    
                _isWon = true;
                _isFinished = true;
            }

            NotifyLevelFinished();
        }

        public void Loose() {
            _ticker.Stop();

            _isWon = false;
            _isFinished = true;

            NotifyLevelFinished();
        }

        public bool IsWon() {
            return _isWon;
        }

        public bool IsFinished() {
            return _isFinished;
        }

        #endregion

        #region Observing

        public void RegisterOnDirectionObserver(IDirectionEventObserver directionEventObserver) {
            if (null == directionEventObserver) {
                throw new ArgumentNullException("directionEventObserver");
            }

            if (null != _currentLevel) {
                _currentLevel.RegisterOnDirectionObserver(directionEventObserver);
            }

            _observers.Add(directionEventObserver);
        }

        public ILevelObserverable Level {
            get { return _currentLevel; }
        }

        #endregion

        #region Events

        private void OnPacManChanged(Object sender, PacmanStateChangedEventArgs e) {
            if (null == e) {
                throw new ArgumentNullException("e");
            }

            _currentScore += (e.Score - _currentLevelScore);
            _currentLevelScore = e.Score;


            if (_currentScore > _bestScore) {
                _bestScore = _currentScore;
            }

            if (e.HasDied) {
                _ticker.Stop();

                if (0 == e.Lives) {
                    Loose();
                }
            }
        }

        private void OnDotsEnds(Object sender, EventArgs e) {
            if (null == e) {
                throw new ArgumentNullException("e");
            }

            if (!IsFinished()) {
                Win();
            }
        }

        public event EventHandler LevelFinished;

        private void NotifyLevelFinished() {
            OnLevelFinishedNotify(EventArgs.Empty);
        }

        private void OnLevelFinishedNotify(EventArgs e) {
            if (null == e) {
                throw new ArgumentNullException("e");
            }

            e.Raise(this, ref LevelFinished);
        }

        #endregion

        #region Ticking

        private void DoATick() {
            _currentLevel.DoATick();
        }

        private class Ticker : IDisposable {
            private const int Delay = 5;

            private readonly Timer _timer;
            private readonly Action _tickAction;

            private bool _isDisposed;

            public Ticker(Action tickAction) {
                _tickAction = tickAction;

                _timer = new Timer(Delay);
                _timer.Elapsed += Tick;

                _isDisposed = false;
            }

            public bool IsOn() {
                return _timer.Enabled;
            }

            public void Start() {
                _timer.Start();
            }

            public void Stop() {
                _timer.Stop();
            }

            private void Tick(Object source, ElapsedEventArgs e) {
                _tickAction();
            }

            public void Dispose() {
                _timer.Stop();
                _timer.Dispose();

                _isDisposed = true;
            }

            public bool IsDisposed() {
                return _isDisposed;
            }
        }

        #endregion

        public void Dispose() {
            _ticker.Dispose();
        }
    }
}