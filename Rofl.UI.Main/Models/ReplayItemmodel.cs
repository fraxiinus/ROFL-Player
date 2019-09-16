﻿using Rofl.Reader.Models;
using Rofl.Reader.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rofl.UI.Main.Models
{
    public class ReplayItemModel : INotifyPropertyChanged
    {

        public ReplayItemModel(ReplayFile replayFile, bool newFile = false)
        {
            FileName = replayFile.Name;
            IsNewFile = newFile;

            switch (replayFile.Data.InferredData.MapID)
            {
                case Map.HowlingAbyss:
                    MapName = "Howling Abyss";
                    break;
                case Map.SummonersRift:
                    MapName = "Summoner's Rift";
                    break;
                case Map.TwistedTreeline:
                    MapName = "Twisted Treeline";
                    break;
                default:
                    MapName = "Uknown Map";
                    break;
            }

            MatchId = (long) replayFile.Data.PayloadFields.MatchId;
            GameLength = (int) replayFile.Data.MatchMetadata.GameDuration / 1000;
            PatchNumber = replayFile.Data.MatchMetadata.GameVersion;

            BluePlayers = (from bplayers in replayFile.Data.MatchMetadata.BluePlayers
                           select new PlayerInfoModel()
                           {
                               ChampionName = bplayers.SafeGet("SKIN"),
                               PlayerName = bplayers.SafeGet("NAME"),
                               IsKnownPlayer = false
                           }).ToArray();

            RedPlayers = (from rplayers in replayFile.Data.MatchMetadata.RedPlayers
                          select new PlayerInfoModel()
                          {
                              ChampionName = rplayers.SafeGet("SKIN"),
                              PlayerName = rplayers.SafeGet("NAME"),
                              IsKnownPlayer = false
                          }).ToArray();

            IsBlueVictorious = replayFile.Data.InferredData.BlueVictory;
        }

        public string FileName { get; set; }

        public bool IsNewFile { get; set; }

        public string MapName { get; set; }

        public long MatchId { get; set; }

        public int GameLength { get; set; }

        public string GameLengthString
        {
            get {
                int minutes = GameLength / 60;
                int seconds = GameLength - (minutes * 60);
                return $"{minutes} m {seconds} s";
            }
        }

        public string PatchNumber { get; set; }

        public PlayerInfoModel[] BluePlayers { get; set; }

        public PlayerInfoModel[] RedPlayers { get; set; }

        public bool IsBlueVictorious { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
