using System;
using System.IO;
using MessageStream2;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer.Messages
{
    public class ScenarioData
    {
        public static void SendScenarioModules(ClientObject client)
        {
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName)).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName)))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                currentScenarioModule++;
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);
                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandleScenarioModuleData(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                //Don't care about subspace / send time.
                string[] scenarioName = mr.Read<string[]>();
                DarkLog.Debug("Saving " + scenarioName.Length + " scenario modules from " + client.playerName);

                for (int i = 0; i < scenarioName.Length; i++)
                {
                    byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());
                    File.WriteAllBytes(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName, scenarioName[i] + ".txt"), scenarioData);
                    if (scenarioName[i] == "ScenarioDestructibles")
                    {
                        RelayScenarioModule(client, scenarioName[i], scenarioData);
                    }
                }
            }
        }

        public static void RelayScenarioModule(ClientObject fromClient, string scenarioName, byte[] scenarioData)
        {
            //Build messages
            ServerMessage uncompressedMessage = new ServerMessage();
            ServerMessage compressedMessage = new ServerMessage();
            uncompressedMessage.type = ServerMessageType.SCENARIO_DATA;
            compressedMessage.type = ServerMessageType.SCENARIO_DATA;
            //Compressed
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(new string[] { scenarioName });
                mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                compressedMessage.data = mw.GetMessageBytes();
            }
            //Uncompressed
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(new string[] { scenarioName });
                mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                uncompressedMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAllAutoCompressed(fromClient, compressedMessage, uncompressedMessage, false);
        }
    }
}

