using System;
using System.IO;
using System.Threading.Tasks;
using Rofl.Reader.Parsers;
using Rofl.Reader.Models;
using Rofl.Reader.Utilities;

namespace Rofl.Reader
{
    public class ReplayReader
    {
        private readonly string exceptionOriginName = "ReplayReader";

        /// <summary>
        /// Given non-null ReplayFile object with valid Location, Name, and Type - 
        /// Returns ReplayFile object with filled out Data.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<ReplayFile> ReadFile(ReplayFile file)
        {
            CheckInput(file);
            file.Data = await ParseFile(file);

            // Make some educated guesses
            GameDetailsInferrer detailsInferrer = new GameDetailsInferrer();

            file.Data.InferredData = new InferredData()
            {
                MapID = detailsInferrer.InferMap(file.Data.MatchMetadata)
            };

            return file;
        }

        private void CheckInput(ReplayFile file)
        {
            CheckFileReference(file);
            CheckFileExistence(file);
        }

        private void CheckFileReference(ReplayFile file)
        {
            if (file == null || String.IsNullOrEmpty(file.Location) || String.IsNullOrEmpty(file.Name))
            {
                throw new ArgumentNullException($"{exceptionOriginName} - File reference is null");
            }
        }

        private void CheckFileExistence(ReplayFile file)
        {
            if (!File.Exists(file.Location))
            {
                throw new FileNotFoundException($"{exceptionOriginName} - File path not found, does the file exist?");
            }
        }

        private async Task<ReplayHeader> ParseFile(ReplayFile file)
        {
            IReplayParser parser = SelectParser(file);
            using (FileStream fs = new FileStream(file.Location, FileMode.Open))
            {
                return await parser.ReadReplayAsync(fs);
            }
        }

        private IReplayParser SelectParser(ReplayFile file)
        {
            IReplayParser parser = null;
            switch (file.Type)
            {
                case REPLAYTYPES.ROFL:
                    parser = new RoflParser();
                    break;
                case REPLAYTYPES.LRF:
                    parser = new LrfParser();
                    break;
                case REPLAYTYPES.LPR:
                    parser = new LprParser();
                    break;
                default:
                    throw new Exception($"{exceptionOriginName} - Unknown replay file type");
            }
            return parser;
        }
    }
}
