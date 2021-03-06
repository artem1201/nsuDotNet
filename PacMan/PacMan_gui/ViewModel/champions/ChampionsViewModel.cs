﻿//  author: Artem Sumanev

using System;
using System.Collections.ObjectModel;
using System.Windows;
using PacMan_gui.Annotations;
using PacMan_model.champions;

namespace PacMan_gui.ViewModel.champions {
    internal sealed class ChampionsViewModel {
        public ObservableCollection<ChampionsTableItem> ChampionsTableItems { get; private set; }

        #region Initialization

        public ChampionsViewModel(IChampionsTableObserverable championsTableOberverable) {
            ChampionsTableItems = new ObservableCollection<ChampionsTableItem>();

            championsTableOberverable.ChampionsTableState += OnChampionsTableStateChanges;

            championsTableOberverable.ForceNotify();
        }

        #endregion

        #region Events

        private void OnChampionsTableStateChanges(
            object sender,
            [NotNull] ChampionsTableChangedEventArs championsTableChangedEventArs) {
            if (null == championsTableChangedEventArs) {
                throw new ArgumentNullException("championsTableChangedEventArs");
            }

            Application.Current.Dispatcher.BeginInvoke(
                new Action(
                    delegate {
                        ChampionsTableItems.Clear();

                        foreach (var champion in championsTableChangedEventArs.Champions) {
                            ChampionsTableItems.Add(new ChampionsTableItem(champion.GetName(), champion.GetScore()));
                        }
                    }));
        }

        #endregion
    }

    internal sealed class ChampionsTableItem {
        public ChampionsTableItem([NotNull] string name, int score) {
            if (null == name) {
                throw new ArgumentNullException("name");
            }
            Score = score;
            Name = name;
        }

        public string Name { get; private set; }
        public int Score { get; private set; }
    }
}