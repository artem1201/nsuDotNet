﻿using System;
using System.Threading;
using PacMan_model.util;

namespace PacMan_model.level.cells.pacman {

    internal class PacMan : IPacMan {

        private const int MaxLives = 3;
        private const int CommonSpeed = 10;

        // cell with position, lives and current speed
        private readonly PacManCell _pacman;

        //  current number of ate points
        private int _score;
        //  current number of lives
        private int _lives = MaxLives;

        //  field where pacman is able to move
        private readonly IField _field;

        //  last direction where pacman was moved
        private Direction _currentDirection = Direction.Left;

        //  number of tick (from 0 to pacman's-current-speed tiks)
        private int _currentTick;
        //  position where pacman will be after end of cycle of movement
        private Point _nextPosition;
        //  calls when pacman finish movement to next cell
        private Action _onEndOfMovementAction;

        public PacMan(IField field, Point startPosition) {
            if (null == field) {
                throw new ArgumentNullException("field");
            }
            if (null == startPosition) {
                throw new ArgumentNullException("startPosition");
            }
            _field = field;

            _pacman = new PacManCell(startPosition, CommonSpeed);
        }

        public event EventHandler<PacmanStateChangedEventArgs> PacmanState;
        public void ForceNotify() {
            NotifyChangedStatement();
        }

        /// <summary>
        ///     moves pacman in some direction on set field
        ///     one call is one tick
        /// </summary>
        /// <param name="nextDirection">direction, where player will be moved</param>
        public void Move(Direction nextDirection) {

            //  if pacman is on its move
            if (0 != _currentTick) {
                
                KeepMoving();
            }
            //  else pacman stops
            else {
                
                var cellInNextDirection = _field.GetCell(_pacman.GetPosition().GetNearByDirection(nextDirection));
                var cellInCurrentDirection = _field.GetCell(_pacman.GetPosition().GetNearByDirection(_currentDirection));
                
                if (cellInNextDirection.IsFreeForMoving()) {
                    _currentDirection = nextDirection;
                    _nextPosition = cellInNextDirection.GetPosition();

                    cellInNextDirection.HandlePacmanMovement(this, _field);
                }
                else if (cellInCurrentDirection.IsFreeForMoving()) {
                    _nextPosition = cellInCurrentDirection.GetPosition();

                    cellInCurrentDirection.HandlePacmanMovement(this, _field);
                }
            }
        }

        /// <summary>
        ///     moves pacman in left direction on set field
        ///     one call is one tick
        /// </summary>
        public void MoveLeft() {
            Move(Direction.Left);
        }

        /// <summary>
        ///     moves pacman in right direction on set field
        ///     one call is one tick
        /// </summary>
        public void MoveRight() {
            Move(Direction.Right);
        }

        /// <summary>
        ///     moves pacman in up direction on set field
        ///     one call is one tick
        /// </summary>
        public void MoveUp() {
            Move(Direction.Up);
        }

        /// <summary>
        ///     moves pacman in down direction on set field
        ///     one call is one tick
        /// </summary>
        public void MoveDown() {
            Move(Direction.Down);
        }

        public Point GetPosition() {
            return _pacman.GetPosition();
        }

        public int GetScore() {
            return _score;
        }

        public int GetLives() {
            return _lives;
        }


        public void StartMovingTo(Point newPosition, Action onEndOfMovement = null) {
            if (null == newPosition) {
                throw new ArgumentNullException("newPosition");
            }

            _onEndOfMovementAction = onEndOfMovement;

            KeepMoving();
        }

        public void Eat(ICellWithCost cell) {
            if (null == cell) {
                throw new ArgumentNullException("cell");
            }
            _score += cell.GetCost();

            NotifyChangedStatement();
        }

        public void Die() {

            --_lives;

            Stop();

            if (0 != _lives) {
                _pacman.MoveTo(_pacman.GetStartPosition());
            }

            NotifyDeath();
        }

        /// <summary>
        ///     resets ticks,
        ///     resets next position
        /// </summary>
        private void Stop() {
            _currentTick = 0;
            _nextPosition = null;
        }

        private void KeepMoving() {

            ++_currentTick;

            if (_pacman.GetSpeed() == _currentTick) {

                _pacman.MoveTo(_nextPosition);

                Stop();
                NotifyChangedStatement();

                if (null != _onEndOfMovementAction) {
                    _onEndOfMovementAction();
                    _onEndOfMovementAction = null;
                }
            }
        }

        protected virtual void OnStatementChanged(PacmanStateChangedEventArgs e) {
            if (null == e) {
                throw new ArgumentNullException("e");
            }
            var temp = Volatile.Read(ref PacmanState);

            if (temp != null) temp(this, e);
        }

        private void NotifyChangedStatement() {
            var e = new PacmanStateChangedEventArgs(_pacman.GetPosition(), _currentDirection, _lives, _score);
            OnStatementChanged(e);
        }

        private void NotifyDeath() {
            var e = new PacmanStateChangedEventArgs(_pacman.GetPosition(), _currentDirection, _lives, _score, true);
            OnStatementChanged(e);
        }

        class PacManCell : MovingCell {

            //  number of ticks per second
            private int _currentSpeed;


            public PacManCell(Point startPosition, int initialSpeed)
                : base(startPosition) {
                if (null == startPosition) {
                    throw new ArgumentNullException("startPosition");
                }
                if (initialSpeed <= 0) {
                    throw new ArgumentOutOfRangeException("initialSpeed");
                }

                _currentSpeed = initialSpeed;
            }

            public void SetSpeed(int newSpeed) {
                _currentSpeed = newSpeed;
            }

            /// <summary>
            /// returns number of ticks per one movement
            /// </summary>
            /// <returns>number of ticks per one movement</returns>
            public override int GetSpeed() {
                return _currentSpeed;
            }

            public void MoveTo(Point newPosition) {
                if (null == newPosition) {
                    throw new ArgumentNullException("newPosition");
                }
                Position = newPosition;
            }

            public void MoveTo(int x, int y) {
                Position.SetX(x);
                Position.SetY(y);
            }
        }
    }
}
