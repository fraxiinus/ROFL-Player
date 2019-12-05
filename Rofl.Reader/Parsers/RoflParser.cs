using Newtonsoft.Json.Linq;
using Rofl.Reader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rofl.Reader.Parsers
{
    /// <summary>
    /// Parses Official League of Legends Replays
    /// </summary>
    public class RoflParser : IReplayParser
    {
        private readonly string exceptionOriginName = "RoflParser";
        private readonly byte[] _magicNumbers = new byte[] { 0x52, 0x49, 0x4F, 0x54 };
        private const int lengthFieldOffset = 262;
        private const int lengthFieldByteSize = 26;
        private FileStream fileStream;
        private LengthFields lengthFields;

        public async Task<ReplayHeader> ReadReplayAsync(FileStream fs)
        {
            this.fileStream = fs;
            await RunChecksAsync();
            return await ExtractReplayHeaderAsync();
        }

        private async Task RunChecksAsync() {
            CheckFileStreamCanRead();
            await CheckFileIsRoflAsync();
        }

        private void CheckFileStreamCanRead()
        {
            if(!fileStream.CanRead)
            {
                throw new IOException($"{exceptionOriginName} - Stream does not support reading");
            }
        }

        private async Task CheckFileIsRoflAsync()
        {
            // Read and check Magic Numbers
            byte[] magicbuffer = new byte[4];
            try
            {
                await fileStream.ReadAsync(magicbuffer, 0, 4);
                if (!magicbuffer.SequenceEqual(_magicNumbers))
                {
                    throw new Exception($"{exceptionOriginName} - Selected file is not in valid ROFL format");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"{exceptionOriginName} - Reading Magic Number: {ex.Message}");
            }
        }

        private Task<ReplayHeader> ExtractReplayHeaderAsync() {
            try {
                return TryExtractReplayHeaderAsync();
            }
            catch (Exception ex)
            {
                throw new IOException($"{exceptionOriginName} - Reading Header: {ex.Message}");
            }
        }

        private async Task<ReplayHeader> TryExtractReplayHeaderAsync() {
            lengthFields = await ExtractLengthFields();
            var matchMetadata = await ExtractMatchMetadata();
            var payloadFields = await ExtractPayloadFields();

            return new ReplayHeader
            {
                LengthFields = lengthFields,
                MatchMetadata = matchMetadata,
                PayloadFields = payloadFields
            };
        }

        private async Task<LengthFields> ExtractLengthFields() {
            byte[] lengthFieldBuffer = await Read(
                lengthFieldOffset,
                lengthFieldByteSize
            );
            return ParseLengthFields(lengthFieldBuffer);
        }

        private async Task<MatchMetadata> ExtractMatchMetadata() {
            byte[] buffer = await Read(
                (int)lengthFields.MetadataOffset,
                (int)lengthFields.MetadataLength
            );
            return ParseMetadata(buffer);
        }

        private async Task<PayloadFields> ExtractPayloadFields() {
            byte[] buffer = await Read(
                (int)lengthFields.PayloadHeaderOffset,
                (int)lengthFields.PayloadHeaderLength
            );
            return ParsePayloadFields(buffer);
        }
        
        private async Task<byte[]> Read(int offset, int count) {
            byte[] buffer = new byte[count];
            fileStream.Seek(offset, SeekOrigin.Begin);
            await fileStream.ReadAsync(buffer, 0, count);
            return buffer;
        }

        private static PayloadFields ParsePayloadFields(byte[] bytedata)
        {
            var result = new PayloadFields { };

            result.MatchId = BitConverter.ToUInt64(bytedata, 0);
            result.MatchLength = BitConverter.ToUInt32(bytedata, 8);
            result.KeyframeAmount = BitConverter.ToUInt32(bytedata, 12);
            result.ChunkAmount = BitConverter.ToUInt32(bytedata, 16);
            result.EndChunkID = BitConverter.ToUInt32(bytedata, 20);
            result.StartChunkID = BitConverter.ToUInt32(bytedata, 24);
            result.KeyframeInterval = BitConverter.ToUInt32(bytedata, 28);
            result.EncryptionKeyLength = BitConverter.ToUInt16(bytedata, 32);
            result.EncryptionKey = Encoding.UTF8.GetString(bytedata, 34, result.EncryptionKeyLength);

            return result;
        }

        private static MatchMetadata ParseMetadata(byte[] bytedata)
        {
            var result = new MatchMetadata { };
            var jsonstring = Encoding.UTF8.GetString(bytedata);

            var jsonobject = JObject.Parse(jsonstring);

            result.GameDuration = (ulong)jsonobject["gameLength"];
            result.GameVersion = (string)jsonobject["gameVersion"];
            result.LastGameChunkID = (uint)jsonobject["lastGameChunkId"];
            result.LastKeyframeID = (uint)jsonobject["lastKeyFrameId"];

            // Create new lists of player dictionaries for sorting
            var blueTeam = new List<Dictionary<string, string>>();
            var redTeam = new List<Dictionary<string, string>>();

            // Sort blue and red teams
            foreach (JObject player in JArray.Parse(((string)jsonobject["statsJson"]).Replace(@"\", "")))
            {
                if(player["TEAM"].ToString() == "100")
                {
                    blueTeam.Add(player.ToObject<Dictionary<string, string>>());
                }
                else if (player["TEAM"].ToString() == "200")
                {
                    redTeam.Add(player.ToObject<Dictionary<string, string>>());
                }
            }

            result.BluePlayers = blueTeam.ToArray();
            result.RedPlayers = redTeam.ToArray();

            //result.Players = JArray.Parse(((string)jsonobject["statsJson"]).Replace(@"\", ""));

            return result;
        }

        private static LengthFields ParseLengthFields(byte[] bytedata)
        {
            var result = new LengthFields { };
            result.HeaderLength = BitConverter.ToUInt16(bytedata, 0);
            result.FileLength = BitConverter.ToUInt32(bytedata, 2);
            result.MetadataOffset = BitConverter.ToUInt32(bytedata, 6);
            result.MetadataLength = BitConverter.ToUInt32(bytedata, 10);
            result.PayloadHeaderOffset = BitConverter.ToUInt32(bytedata, 14);
            result.PayloadHeaderLength = BitConverter.ToUInt32(bytedata, 18);
            result.PayloadOffset = BitConverter.ToUInt32(bytedata, 22);

            return result;
        }
    }
}
