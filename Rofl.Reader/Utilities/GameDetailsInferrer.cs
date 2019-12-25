using Rofl.Reader.Models;
using System.Linq;

namespace Rofl.Reader.Utilities
{
    public class GameDetailsInferrer
    {
        public Map InferMap(MatchMetadata matchMetadata)
        {
            Map inferredMap;
            if (!HasJungle(matchMetadata))
            {
                inferredMap = Map.HowlingAbyss;
            } else if (!HasWards(matchMetadata) && !HasDragon(matchMetadata))
            {
                inferredMap = Map.TwistedTreeline;
            } else
            {
                inferredMap = Map.SummonersRift;
            }
            return inferredMap;
        }

        // check if any players have killed jungle creeps, rules out HowlingAbyss
        private bool HasJungle(MatchMetadata metadata)
        {
            return (
                from player in metadata.AllPlayers
                where int.Parse(player["NEUTRAL_MINIONS_KILLED"]) > 0
                select player
            ).Count() > 0;
        } 

        // check if any players have placed wards, rules out TwistedTreeline and HowlingAbyss
        private bool HasWards(MatchMetadata metadata)
        {
            return (
                from player in metadata.AllPlayers
                where int.Parse(player["WARDS_PLACED"]) > 0
                select player
            ).Count() > 0;
        }

        // check if any player has killed a dragon, SummonersRift only
        private bool HasDragon(MatchMetadata metadata)
        {
            return (
                from player in metadata.AllPlayers
                where int.Parse(player["DRAGON_KILLS"]) > 0
                select player
            ).Count() > 0;
        }
    }
}
