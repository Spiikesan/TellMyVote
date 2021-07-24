using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tell My Vote", "Spiikesan", "1.1.0")]
    [Description("A Cui panel for players to vote at admin polls")]

    /*======================================================================================================================= 
    *
    *   SET UP TO - 4 QUESTIONS / 3 ANSWERS - IN CONFIG FILE, AND USE TELLMYVOTE CUI TO VOTE AND CHECK COUNTS
    *   THANKS TO THE OXIDE/UMOD TEAM for coding quality, ideas, and time spent for the community
    *   
    *   1.0.0   20190906    code refresh
    *
    *   permission : tellmyvote.admin
    *   chat commands   /myvote     /myvote_poll X Y [args]
    *   It is case sensitive
    *   
    *   example :   /myvote_poll 1 0 question       ---> set "question" for poll#1 title
    *               /myvote_poll 1 1 first choice   ---> set "first choice" for poll#1 answer#1 
    *               /myvote_poll 1 2 second choice   ---> set "second choice" for poll#1 answer#2
    *
    *   if question is set empty ---> the whole poll won't be displayed
    *   if answer is set empty ---> the answer line won't be displayed
    *
    *   POLL#1      (myvote_poll 1 0 [args])               POLL#3      (myvote_poll 3 0 [args])
    *   answer#1    (myvote_poll 1 1 [args])               answer#1    (myvote_poll 3 1 [args])          
    *   answer#2    (myvote_poll 1 2 [args])               answer#2    (myvote_poll 3 2 [args])
    *   answer#3    (myvote_poll 1 3 [args])               answer#3    (myvote_poll 3 3 [args])
    *
    *   POLL#2      (myvote_poll 2 0 [args])               POLL#4      (myvote_poll 4 0 [args])
    *   answer#1    (myvote_poll 2 1 [args])               answer#1    (myvote_poll 4 1 [args])          
    *   answer#2    (myvote_poll 2 2 [args])               answer#2    (myvote_poll 4 2 [args])
    *   answer#3    (myvote_poll 2 3 [args])               answer#3    (myvote_poll 4 3 [args])
    *=======================================================================================================================*/

    public class TellMyVote : RustPlugin
    {

        /*
         * For left to right alignment, we need to satisfy the following generic formula :
         * MARGIN_LR * 2 + SEP_POLL + ( SEP_IN + SIZE_SUBCOL_1 + SIZE_SUBCOL_2 ) * 2 = 1.00
         *
         * For top to bottom alignment, we need to satisfy the following generic formula :
         * MARGIN_TB * 2 + SEP_POLL + SEP_IN * 6 + SIZE_ROW * 8 = 1.00
         *
         */

        const float MARGIN_LR = 0.03f; // Left / Right borders margins.
        const float MARGIN_TB = 0.05f; // Top / Bottom borders margins.
        const float SEP_POLL = 0.04f; // Polls separator
        const float SEP_IN = 0.01f; // In-between rows and columns separators.

        const float SIZE_SUBCOL_1 = 0.34f; // Answer button width
        const float SIZE_SUBCOL_2 = 0.10f; // count button width
        const float SIZE_ROW = 0.08f; // Each row height

        const float PollWidth = SIZE_SUBCOL_1 + SIZE_SUBCOL_2 + SEP_IN;
        const float PollHeight = SIZE_ROW * 4 + SEP_IN * 3;

        //precalculate rows and columns coordinates from floats to "x.xx" strings
        string[] pos_rows = new string[16];
        string[] pos_cols = new string[8];

        const string HelpButtonTxt = "0.0 1.0 1.0 0.5";
        const string HelpButtonColor = "0.0 0.5 1.0 0.5";
        const string PanelColor = "0.0 0.0 0.0 0.8";
        const string buttonCloseColor = "0.6 0.26 0.2 1";
        const string QuestionColor = "1.0 1.0 1.0 1.0";
        const string AnswerColor = "0.5 1.0 0.5 0.5";
        const string CountColor = "0.0 1.0 1.0 0.5";
        const string version = "version 1.1.0";
        const bool debug = false;
        const string TMVAdmin = "tellmyvote.admin";
        static string MyVotePanel;
        static string MyVoteInfoPanel;
        string Prefix = "[TMV] :";                      // CHAT PLUGIN PREFIX
        string PrefixColor = "#c12300";                 // CHAT PLUGIN PREFIX COLOR
        string ChatColor = "#ffcd7c";                   // CHAT MESSAGE COLOR
        ulong SteamIDIcon = 76561198215959719;          // SteamID FOR PLUGIN ICON
        private bool ConfigChanged;

        float BannerShowTimer = 30;
        float BannerHideTimer = 30;

        string[,] polls = new string[4, 4];
        private Timer tmvbanner;

        void Init()
        {
            LoadVariables();
            permission.RegisterPermission(TMVAdmin, this);
            GenerateCoordinates();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TellMyVote");
        }

        #region CONFIG

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        private void GenerateCoordinates()
        {
            for (int i = 0; i < pos_rows.Length; i++)
            {
                int polls = i / 8;         // 0, 0, 0, 0, 0, 0, 0, 0, 1...
                int rows = i % 8;         //0, 1, 2, 3, 4, 5, 6, 7, 0...
                pos_rows[pos_rows.Length - i - 1] = ((MARGIN_TB //y coordinates are inverted...
                                + polls * (PollHeight + SEP_IN) //Polls separation (is 0 for i in [0...7])
                                + (rows / 2) * SEP_IN
                                + ((rows + 1) / 2) * SIZE_ROW //Rows end : 0, 1, 1, 2, 2, 3, 3, 4, 0...
                              )).ToString("F", CultureInfo.InvariantCulture);
                if (debug) { Puts($"pos_rows[{pos_rows.Length - i - 1}] = {pos_rows[pos_rows.Length - i - 1]}"); }
            }

            for (int i = 0; i < pos_cols.Length; i++)
            {
                int polls = i / 4;           // 0, 0, 0, 0, 1, 1, 1, 1
                int columns = i % 4;         // 0, 1, 2, 3, 0, 1, 2, 4
                int subcolumn = columns / 2; // 0, 0, 1, 1, 0, 0, 1, 1
                pos_cols[i] = (MARGIN_LR
                                + polls * (PollWidth + SEP_POLL) //Poll separation (is 0 for i in [0..3])
                                + (columns / 2) * (SIZE_SUBCOL_1 + SEP_IN) //Answer or Result column begins
                                + (columns % 2) * (subcolumn == 1 ? SIZE_SUBCOL_2 : SIZE_SUBCOL_1) //Answer or Result column end
                              ).ToString("F", CultureInfo.InvariantCulture);
                if (debug) { Puts($"pos_col[{i}] = {pos_cols[i]}"); }
            }
        }

        private void LoadVariables()
        {
            Prefix = Convert.ToString(GetConfig("Chat Settings", "Prefix", "[TMV] :"));                     // CHAT PLUGIN PREFIX
            PrefixColor = Convert.ToString(GetConfig("Chat Settings", "PrefixColor", "#c12300"));           // CHAT PLUGIN PREFIX COLOR
            ChatColor = Convert.ToString(GetConfig("Chat Settings", "ChatColor", "#ffcd7c"));               // CHAT  COLOR
            SteamIDIcon = Convert.ToUInt64(GetConfig("Settings", "SteamIDIcon", 76561198215959719));        // SteamID FOR PLUGIN ICON
            BannerShowTimer = Convert.ToSingle(GetConfig("TIMER", "Vote Banner will display every (in seconds)", "30"));
            BannerHideTimer = Convert.ToSingle(GetConfig("TIMER", "Banner hide (in seconds)", "30"));
            for (int poll = 0; poll < polls.GetLength(0); poll++)
            {
                for (int answer = 0; answer < polls.GetLength(1); answer++)
                {
                    string dataValue = answer > 0 ? "Answer#" + answer : "Question";
                    string defaultValue = answer > 0 ? "set answer here" : "set your question here";
                    polls[poll, answer] = Convert.ToString(GetConfig("Poll #" + (poll + 1), dataValue, defaultValue));
                }
            }

            if (!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

        private void SetConfig(string menu, string datavalue, string value)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            if (data.ContainsKey(datavalue))
                data[datavalue] = value;
            SaveConfig();
        }

        #endregion

        void Loaded()
        {
            if (storedData.myVoteIsON == true)
            {
                PopUpVote("start");
            }
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TellMyVote", storedData);
            if (tmvbanner != null) tmvbanner.Destroy();
        }

        #region MESSAGES

        void LoadDefaultMessages()
        {

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermMsg", "You don't have admin permission."},
                {"AdminPermMsg", "You are allowed as admin. You can start/end/clear the votes."},
                {"QAlreadyMsg", "You already have voted for this Question"},
                {"VoteLogMsg", "Thank you, we recorded your vote for Question"},
                {"VoteBannerMsg", "To help our community : please vote with /myvote or click here"},
                {"TMVoffMsg", "Vote session is now over."},
                {"PurgeMsg", "Counters has been reset"},
                {"Info01Msg", "Players with admin permission can start/end/clear votes from main panel"},
                {"Info02Msg", "Questions/Answers has to be set from TellMyVote.json config file or with chat command /myvote_poll."},
                {"Info03Msg", "IF A QUESTION IS SET EMPTY : it and its answers won't be displayed."},
                {"Info04Msg", "IF AN ANSWER IS SET EMPTY : its button won't be displayed."},
                {"HowToMsg", "Please use this format :\n/myvote_poll 1 0 here the words for the poll#1 title - check plugin webpage"},

            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermMsg", "Vous n'avez pas la permission."},
                {"AdminPermMsg", "Vous êtes admin. et avez accès aux commandes start/end/clear."},
                {"QAlreadyMsg", "Vous avez déjà voté à cette question."},
                {"VoteLogMsg", "Merci, nous avons enregistré votre choix."},
                {"VoteBannerMsg", "Pour aider la communauté : votez avec /myvote ou cliquez ici"},
                {"TMVoffMsg", "Le sondage est maintenant terminé."},
                {"PurgeMsg", "Les compteurs sont remis à zéro."},
                {"Info01Msg", "La permission .admin permet de lancer/stopper/purger depuis le panneau principal"},
                {"Info02Msg", "Les Questions/Réponses sont à définir depuis le fichier de config TellMyVote.json ou avec la commande chat /myvote_poll."},
                {"Info03Msg", "SI UNE QUESTION EST LAISSÉE VIDE : elle et ses questions ne seront pas affichées."},
                {"Info04Msg", "SI UNE REPONSE EST VIDE : son bouton ne s'affichera pas."},
                {"HowToMsg", "S'il vous plait utilisez ce format :\n/myvote_poll 1 0 taper ici le titre#1 - consultez la page du plugin"},

            }, this, "fr");
        }

        #endregion

        class StoredData
        {
            public List<ulong>[,] votes = new List<ulong>[4, 3] {
                {
                    new List<ulong>(),
                    new List<ulong>(),
                    new List<ulong>()
                },
                {
                    new List<ulong>(),
                    new List<ulong>(),
                    new List<ulong>()
                },
                {
                    new List<ulong>(),
                    new List<ulong>(),
                    new List<ulong>()
                },
                {
                    new List<ulong>(),
                    new List<ulong>(),
                    new List<ulong>()
                }
            };
            public bool myVoteIsON;

            public StoredData()
            {
            }
        }
        private StoredData storedData;

        #region CHAT SET Q/A

        [ChatCommand("myvote_poll")]
        private void TellMyVotePollSet(BasePlayer player, string command, string[] args)
        {
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            string sentence = string.Empty;

            if (isadmin == false)
            {
                if (debug) { Puts($"-> NOT ADMIN access to set polls"); }
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("NoPermMsg", this, player.UserIDString)}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            else if (args.Length == 0)
            {
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("HowToMsg", this, player.UserIDString)}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                if (debug) { Puts($"-> SETTING POLLS with no arguments"); }
            }
            else if (args.Length == 1)
            {
                try
                {
                    int pollnum = int.Parse(args[0]);

                    if (pollnum >= 1 && pollnum <= 4)
                    {
                        polls[pollnum - 1, 0] = ""; SetConfig("Poll #" + pollnum, "Question", "");
                        Player.Message(player, $"Poll#{args[0]} has been set to empty", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                        if (debug == true) { Puts($"-> SETTING POLL {args[0]}, with no arguments"); }
                    }
                }
                catch (Exception e)
                {
                    Puts("TellMyVotePollSet: An error occured: " + e);
                }
            }
            else
            {
                sentence = string.Join(" ", args.Skip(2));
                try
                {
                    int pollnum = int.Parse(args[0]);
                    int parameter = int.Parse(args[1]);

                    if (pollnum >= 1 && pollnum <= 4 &&
                        parameter >= 0 && parameter <= 3)
                    {
                        string parameterName = "Question";

                        polls[pollnum - 1, parameter] = sentence;

                        if (parameter > 0) parameterName = "Answer#" + parameter;
                        SetConfig("Poll #" + pollnum, parameterName, sentence);
                        Player.Message(player, $"Poll#{pollnum} {parameterName} has been set to : {sentence}", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);

                    }
                }
                catch (Exception e)
                {
                    Puts("TellMyVotePollSet: An error occured: " + e);
                }
            }
        }

        void PlayerMessage(BasePlayer player, string poll, string answer, string sentence)
        {
            Player.Message(player, $"Poll#{poll}/Answer#{answer} has been set to : {sentence}", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
        }

        #endregion

        #region VOTING

        private bool voteNeeded(ulong playerID, int pollNum)
        {
            if (polls[pollNum, 0] == string.Empty) return false;
            for (int i = 0; i < storedData.votes.GetLength(1); i++)
            {
                if (storedData.votes[pollNum, i].Contains(playerID))
                    return false;
            }
            return true;
        }

        [ConsoleCommand("TellMyVote")]
        private void MySurveySpotOnly(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            ulong playerID = player.userID;
            int answernumber;

            try
            {
                answernumber = int.Parse(arg.Args.FirstOrDefault()) - 1;
                if (answernumber >= 0 && answernumber < 12)
                {
                    int pollnum = answernumber / 3;
                    int parameter = answernumber % 3;
                    if (storedData.myVoteIsON == false)
                    {
                        Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("TMVoffMsg", this, player.UserIDString)} </color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    else
                    {

                        if (voteNeeded(playerID, pollnum))
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber + 1} - POLL #{pollnum + 1} vote recorded on {parameter + 1}"); }
                            storedData.votes[pollnum, parameter].Add(playerID);
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #{pollnum + 1}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                            RefreshMyVotePanel(player);
                        }
                        else
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber + 1} - POLL #{pollnum + 1} - already voted"); }
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #{pollnum + 1}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Puts("MySurveySpotOnly: An error occured: " + e);
            }
        }
        #endregion

        #region REFRESH VOTE PANEL

        private void RefreshMyVotePanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MyVotePanel);
            TellMyVotePanel(player, null, null);
        }
        #endregion

        #region CHANGE STATUS

        [ConsoleCommand("TellMyVoteChangeStatus")]
        private void TellMyVoteChangeStatus(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            ulong playerID = player.userID;
            if (arg.Args.Contains("start"))
            {
                if (debug) { Puts($"-> START OF MY VOTE"); }
                if (storedData.myVoteIsON == true)
                {
                    if (debug) { Puts($"-> START ASKED, BUT MY VOTE ALREADY ON."); }
                    return;
                }
                storedData.myVoteIsON = true;
                PopUpVote("start");
                RefreshMyVotePanel(player);
            }
            else if (arg.Args.Contains("end"))
            {
                if (debug) { Puts($"-> END OF MY VOTE SESSION"); }
                if (storedData.myVoteIsON == false)
                {
                    if (debug) { Puts($"-> END ASKED, BUT MY ALREADY OFF."); }
                    return;
                }
                storedData.myVoteIsON = false;
                PopUpVote("end");
                RefreshMyVotePanel(player);
            }
            else if (arg.Args.Contains("purge"))
            {
                if (debug) { Puts($"-> PURGE OF DATAS"); }
                Purge();
                RefreshMyVotePanel(player);
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("PurgeMsg", this, player.UserIDString)}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            else if (arg.Args.Contains("info"))
            {
                if (debug) { Puts($"-> DISPLAY INFO PANEL"); }
                CuiHelper.DestroyUi(player, MyVotePanel);
                TellMyVoteInfoPanel(player);
            }
            else if (arg.Args.Contains("back"))
            {
                if (debug) { Puts($"-> BACK TO MAIN MY VOTE PANEL"); }
                CuiHelper.DestroyUi(player, MyVoteInfoPanel);
                TellMyVotePanel(player, null, null);
            }
        }
        #endregion

        private void Purge()
        {
            foreach (var polls in storedData.votes)
            {
                polls.Clear();
            }
        }

        #region POPUP BANNER

        private void PopUpPlayer(BasePlayer player, string state)
        {
            string bannertxt = "";

            bool playerVoteNeeded = false;

            for (int i = 0; i < storedData.votes.GetLength(0) && !playerVoteNeeded; i++)
            {
                playerVoteNeeded = voteNeeded(player.userID, i);
                if (debug) { Puts($"-> {player} Poll #{i} vote needed = {playerVoteNeeded}"); }
            }

            if (playerVoteNeeded || state == "end")
            {
                if (state == "start")
                {
                    bannertxt = $"{lang.GetMessage("VoteBannerMsg", this, player.UserIDString)}";
                }
                else if (state == "end")
                {
                    bannertxt = $"{lang.GetMessage("TMVoffMsg", this, player.UserIDString)}";
                }

                CuiElementContainer CuiElement = new CuiElementContainer();
                var MyVoteBanner = CuiElement.Add(new CuiPanel { Image = { Color = "0.5 1.0 0.5 0.5" }, RectTransform = { AnchorMin = "0.20 0.85", AnchorMax = "0.80 0.90" }, CursorEnabled = false });
                var closeButton = new CuiButton { Button = { Close = MyVoteBanner, Color = "0.0 0.0 0.0 0.6" }, RectTransform = { AnchorMin = "0.90 0.01", AnchorMax = "0.99 0.99" }, Text = { Text = "X", FontSize = 18, Align = TextAnchor.MiddleCenter } };
                CuiElement.Add(closeButton, MyVoteBanner);
                CuiElement.Add(new CuiButton
                {
                    Button = { Command = "chat.say /myvote", Color = "0.0 0.0 0.0 0.0" },
                    Text = { Text = $"{bannertxt}", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "0.0 0.0 0.0 1" },
                    RectTransform = { AnchorMin = "0.10 0.10", AnchorMax = "0.90 0.90" }
                }, MyVoteBanner);
                CuiHelper.AddUi(player, CuiElement);
                timer.Once(state == "start" ? BannerHideTimer : BannerHideTimer / 3f, () =>
                {
                    CuiHelper.DestroyUi(player, MyVoteBanner);
                });
            }
        }

        private void PopUpVote(string newstate)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(pl => pl.IsConnected))
            {
                PopUpPlayer(player, newstate);
            }
            if (newstate == "start")
            {
                if (tmvbanner == null)
                {
                    tmvbanner = timer.Every(BannerShowTimer, () =>
                    {
                        PopUpVote("start");
                    });
                }
            }
            else
            {
                tmvbanner.Destroy();
                tmvbanner = null;
            }
        }

        #endregion

        #region INFOPANEL

        private void TellMyVoteInfoPanel(BasePlayer player)
        {
            const string PanelColor = "0.0 0.0 0.0 0.8";
            const string buttonCloseColor = "0.6 0.26 0.2 1";
            string information = $"{lang.GetMessage("Info01Msg", this, player.UserIDString)}\n\n{lang.GetMessage("Info02Msg", this, player.UserIDString)}\n\n\n\n{lang.GetMessage("Info03Msg", this, player.UserIDString)}\n\n{lang.GetMessage("Info04Msg", this, player.UserIDString)}";
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            if (isadmin)
            {
                if (debug) { Puts($"-> ADMIN access to info panel"); }
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("AdminPermMsg", this, player.UserIDString)}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            else
            {
                if (debug) { Puts($"-> NOT ADMIN access to info panel"); }
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("NoPermMsg", this, player.UserIDString)}</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
            }
            var CuiElement = new CuiElementContainer();
            MyVoteInfoPanel = CuiElement.Add(new CuiPanel { Image = { Color = $"{PanelColor}" }, RectTransform = { AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.80" }, CursorEnabled = true });
            var closeButton = new CuiButton { Button = { Close = MyVoteInfoPanel, Color = $"{buttonCloseColor}" }, RectTransform = { AnchorMin = "0.85 0.85", AnchorMax = "0.95 0.95" }, Text = { Text = "[X]\nClose", FontSize = 16, Align = TextAnchor.MiddleCenter } };
            CuiElement.Add(closeButton, MyVoteInfoPanel);
            var BackButton = CuiElement.Add(new CuiButton
            {
                Button = { Command = "TellMyVoteChangeStatus back", Color = $"0.0 0.5 1.0 0.5" },
                RectTransform = { AnchorMin = $"0.78 0.85", AnchorMax = $"0.83 0.95" },
                Text = { Text = "BACK", Color = "1.0 1.0 1.0 0.8", FontSize = 10, Align = TextAnchor.MiddleCenter }
            }, MyVoteInfoPanel);
            var TextIntro = CuiElement.Add(new CuiLabel
            {
                Text = { Color = "1.0 1.0 1.0 1.0", Text = "Tell My Vote Panel", FontSize = 22, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.30 0.87", AnchorMax = "0.70 0.95" }
            }, MyVoteInfoPanel);
            var ButtonAnswer1 = CuiElement.Add(new CuiButton
            {
                Button = { Command = "", Color = $"0.5 1.0 0.5 0.5" },
                RectTransform = { AnchorMin = $"0.05 0.05", AnchorMax = $"0.95 0.70" },
                Text = { Text = $"{information}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, MyVoteInfoPanel);
            CuiHelper.AddUi(player, CuiElement);
        }
        #endregion

        #region TELLMYVOTE PANEL START

        [ChatCommand("myvote")]
        private void TellMyVotePanel(BasePlayer player, string command, string[] args)
        {
            string StatusColor = "";
            string Status = "";
            bool isadmin = permission.UserHasPermission(player.UserIDString, TMVAdmin);
            if (storedData.myVoteIsON == true)
            {
                Status = "SESSION IS OPEN : CHOOSE YOUR ANSWERS !";
                StatusColor = "0.2 1.0 0.2 0.8";
            }
            if (storedData.myVoteIsON == false)
            {
                Status = "SESSION HAS ENDED.";
                StatusColor = "1.0 0.1 0.1 0.8";
            }

            #endregion

            #region PANEL AND CLOSE BUTTON

            var CuiElement = new CuiElementContainer();
            MyVotePanel = CuiElement.Add(new CuiPanel
            {
                Image = { Color = $"{PanelColor}" },
                RectTransform = { AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.80" },
                CursorEnabled = true
            });
            CuiElement.Add(new CuiButton
            {
                Button = { Close = MyVotePanel, Color = $"{buttonCloseColor}" },
                RectTransform = { AnchorMin = "0.85 0.85", AnchorMax = "0.95 0.95" },
                Text = { Text = "[X]\nClose", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, MyVotePanel);
            CuiElement.Add(new CuiButton
            {
                Button = { Command = "TellMyVoteChangeStatus info", Color = $"{HelpButtonColor}" },
                RectTransform = { AnchorMin = $"0.78 0.85", AnchorMax = $"0.83 0.95" },
                Text = { Text = "?", Color = $"{HelpButtonTxt}", FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, MyVotePanel);
            CuiElement.Add(new CuiLabel
            {
                Text = { Color = "1.0 1.0 1.0 1.0", Text = $"<i>{version}</i>", FontSize = 11, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.78 0.78", AnchorMax = "0.95 0.84" }
            }, MyVotePanel);
            CuiElement.Add(new CuiLabel
            {
                Text = { Color = "1.0 1.0 1.0 1.0", Text = "Tell My Vote Panel", FontSize = 22, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.30 0.87", AnchorMax = "0.70 0.95" }
            }, MyVotePanel);
            CuiElement.Add(new CuiLabel
            {
                Text = { Color = $"{StatusColor}", Text = $"{Status}", FontSize = 16, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.23 0.78", AnchorMax = "0.77 0.86" }
            }, MyVotePanel);
            if (isadmin == true)
            {
                CuiElement.Add(new CuiButton
                {
                    Button = { Command = "TellMyVoteChangeStatus start", Color = "0.2 0.6 0.2 0.8" },
                    RectTransform = { AnchorMin = $"0.05 0.85", AnchorMax = $"0.15 0.95" },
                    Text = { Text = "START", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter }
                }, MyVotePanel);
                CuiElement.Add(new CuiButton
                {
                    Button = { Command = "TellMyVoteChangeStatus end", Color = "1.0 0.2 0.2 0.8" },
                    RectTransform = { AnchorMin = $"0.16 0.85", AnchorMax = $"0.22 0.95" },
                    Text = { Text = "END", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter }
                }, MyVotePanel);
                CuiElement.Add(new CuiButton
                {
                    Button = { Command = "TellMyVoteChangeStatus purge", Color = "1.0 0.5 0.0 0.8" },
                    RectTransform = { AnchorMin = $"0.05 0.78", AnchorMax = $"0.22 0.84" },
                    Text = { Text = "RESET COUNTERS", Color = "1.0 1.0 1.0 1.0", FontSize = 10, Align = TextAnchor.MiddleCenter }
                }, MyVotePanel);
            }

            #endregion

            #region POLLS DRAWING
            for (int y = 0; y < polls.GetLength(0); y++)
            {
                if (polls[y, 0] != string.Empty)
                {
                    int midY = y / 2;
                    int YpollIndex = midY * 4;
                    int XpollIndex = (y % 2) * 8;
                    string column1Begin = pos_cols[YpollIndex];
                    string column1End = pos_cols[YpollIndex + 1];
                    string column2Begin = pos_cols[YpollIndex + 2];
                    string column2End = pos_cols[YpollIndex + 3];
                    string row1Begin = pos_rows[XpollIndex + 1];
                    string row1End = pos_rows[XpollIndex];
                    CuiElement.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"{column1Begin} {row1Begin}", AnchorMax = $"{column2End} {row1End}" },
                        Text = { Text = $"#{y + 1}. {polls[y, 0]}", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft }
                    }, MyVotePanel);

                    for (int x = 1; x < polls.GetLength(1); x++)
                    {
                        if (polls[y, x] != string.Empty)
                        {
                            int xRowIndex = XpollIndex + x * 2;
                            string rowBegin = pos_rows[xRowIndex + 1];
                            string rowEnd = pos_rows[xRowIndex];
                            CuiElement.Add(new CuiButton
                            {
                                Button = { Command = $"TellMyVote {y * 3 + x}", Color = $"{AnswerColor}" },
                                RectTransform = { AnchorMin = $"{column1Begin} {rowBegin}", AnchorMax = $"{column1End} {rowEnd}" },
                                Text = { Text = $"{polls[y, x]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                            }, MyVotePanel);

                            CuiElement.Add(new CuiButton
                            {
                                Button = { Command = "", Color = $"{CountColor}" },
                                RectTransform = { AnchorMin = $"{column2Begin} {rowBegin}", AnchorMax = $"{column2End} {rowEnd}" },
                                Text = { Text = $"{storedData.votes[y, x - 1].Count}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                            }, MyVotePanel);
                        }
                    }

                }
            }
            CuiHelper.AddUi(player, CuiElement);
        }
        #endregion

    }
}
