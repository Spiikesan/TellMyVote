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
    [Info("Tell My Vote", "BuzZ[PHOQUE] & Spiikesan", "1.1.0")]
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
        const string debutcolonne1 = "0.03";
        const string fincolonne1 = "0.37";
        const string debutcolonne1b = "0.38";
        const string fincolonne1b = "0.48";
        const string debutcolonne2 = "0.52";
        const string fincolonne2 = "0.86";
        const string debutcolonne2b = "0.87";
        const string fincolonne2b = "0.97";
        const string basligne8 = "0.05";
        const string hautligne8 = "0.13";
        const string basligne7 = "0.14";
        const string hautligne7 = "0.22";
        const string basligne6 = "0.23";
        const string hautligne6 = "0.31";
        const string basligne5 = "0.32";
        const string hautligne5 = "0.40";
        const string basligne4 = "0.41";
        const string hautligne4 = "0.49";
        const string basligne3 = "0.50";
        const string hautligne3 = "0.59";
        const string basligne2 = "0.60";
        const string hautligne2 = "0.68";
        const string basligne1 = "0.69";
        const string hautligne1 = "0.77";
        const string HelpButtonTxt = "0.0 1.0 1.0 0.5";
        const string HelpButtonColor = "0.0 0.5 1.0 0.5";
        const string PanelColor = "0.0 0.0 0.0 0.8";
        const string buttonCloseColor = "0.6 0.26 0.2 1";
        const string QuestionColor = "1.0 1.0 1.0 1.0";
        const string AnswerColor = "0.5 1.0 0.5 0.5";
        const string CountColor = "0.0 1.0 1.0 0.5";

        const string version = "version 1.0.0";
        const bool debug = false;
        const string TMVAdmin = "tellmyvote.admin";
        static string MyVotePanel;
        static string MyVoteInfoPanel;
        string Prefix = "[TMV] :";                       // CHAT PLUGIN PREFIX
        string PrefixColor = "#c12300";                 // CHAT PLUGIN PREFIX COLOR
        string ChatColor = "#ffcd7c";                   // CHAT MESSAGE COLOR
        ulong SteamIDIcon = 76561198215959719;          // SteamID FOR PLUGIN ICON
        private bool ConfigChanged;

        float BannerTimer = 10;

        string[,] polls = new string[4, 4];
        private Timer tmvbanner;

        void Init()
        {
            LoadVariables();
            permission.RegisterPermission(TMVAdmin, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TellMyVote");
        }

        #region CONFIG

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        private void LoadVariables()
        {
            Prefix = Convert.ToString(GetConfig("Chat Settings", "Prefix", "[TMV] :"));                       // CHAT PLUGIN PREFIX
            PrefixColor = Convert.ToString(GetConfig("Chat Settings", "PrefixColor", "#c12300"));                // CHAT PLUGIN PREFIX COLOR
            ChatColor = Convert.ToString(GetConfig("Chat Settings", "ChatColor", "#ffcd7c"));                    // CHAT  COLOR
            SteamIDIcon = Convert.ToUInt64(GetConfig("Settings", "SteamIDIcon", 76561198215959719));        // SteamID FOR PLUGIN ICON
            BannerTimer = Convert.ToSingle(GetConfig("TIMER", "Vote Banner will display every (in minutes)", "10"));
            polls[0, 0] = Convert.ToString(GetConfig("Poll #1", "Question", "set your question here"));
            polls[0, 1] = Convert.ToString(GetConfig("Poll #1", "Answer#1", "set answer here"));
            polls[0, 2] = Convert.ToString(GetConfig("Poll #1", "Answer#2", "set answer here"));
            polls[0, 3] = Convert.ToString(GetConfig("Poll #1", "Answer#3", "set answer here"));
            polls[1, 0] = Convert.ToString(GetConfig("Poll #2", "Question", "set your question here"));
            polls[1, 1] = Convert.ToString(GetConfig("Poll #2", "Answer#1", "set answer here"));
            polls[1, 2] = Convert.ToString(GetConfig("Poll #2", "Answer#2", "set answer here"));
            polls[1, 3] = Convert.ToString(GetConfig("Poll #2", "Answer#3", "set answer here"));
            polls[2, 0] = Convert.ToString(GetConfig("Poll #3", "Question", "set your question here"));
            polls[2, 1] = Convert.ToString(GetConfig("Poll #3", "Answer#1", "set answer here"));
            polls[2, 2] = Convert.ToString(GetConfig("Poll #3", "Answer#2", "set answer here"));
            polls[2, 3] = Convert.ToString(GetConfig("Poll #3", "Answer#3", "set answer here"));
            polls[3, 0] = Convert.ToString(GetConfig("Poll #4", "Question", "set your question here"));
            polls[3, 1] = Convert.ToString(GetConfig("Poll #4", "Answer#1", "set answer here"));
            polls[3, 2] = Convert.ToString(GetConfig("Poll #4", "Answer#2", "set answer here"));
            polls[3, 3] = Convert.ToString(GetConfig("Poll #4", "Answer#3", "set answer here"));

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
                {"ThankVoteMsg", "Thank you for your vote."},
                {"PopNewMsg", "Check and vote at the new poll with /myvote"},
                {"QAlreadyMsg", "You already have voted for this Question"},
                {"VoteLogMsg", "Thank you, we recorded your vote for Question"},
                {"VoteBannerMsg", "To help our community : please vote with /myvote"},
                {"TMVoffMsg", "Vote session is now over."},
                {"TMVonMsg", "A new vote session is open."},
                {"PurgeMsg", "Counters has been reset"},
                {"Info01Msg", "Players with admin permission can start/end/clear votes from main panel"},
                {"Info02Msg", "Questions/Answers has to be set from TellMyVote.json config file or with chat command /myvote_poll."},
                {"Info03Msg", "IF A QUESTION IS SET EMPTY : it and its answers won't be displayed."},
                {"Info04Msg", "IF AN ANSWER IS SET EMPTY : its button won't be displayed."},
                {"HowToMsg", "Please use this format :\n/myvote_poll 1 0 here the words for the poll#1 title - check plugin webpage"},

            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermMsg", "Vous n'avez pas la permission administrateur."},
                {"AdminPermMsg", "Vous êtes admin. et avez accès aux commandes start/end/clear."},
                {"ThankVoteMsg", "Thank you for your vote."},
                {"PopNewMsg", "Jetez un coup d'oeil au sondage /myvote !"},
                {"QAlreadyMsg", "Vous avez déjà voté à cette Question"},
                {"VoteLogMsg", "Merci, nous avons enregistré votre choix."},
                {"VoteBannerMsg", "Pour aider la communaté : votez avec /myvote"},
                {"TMVoffMsg", "Le sondage est maintenant terminé."},
                {"TMVonMsg", "Un nouveau sondage est lancé."},
                {"PurgeMsg", "Les compteurs sont remis à zéro"},
                {"Info01Msg", "La permission .admin permet de lancer/stopper/purger depuis le panneau principal"},
                {"Info02Msg", "Les Questions/Réponses sont à définir depuis le fichier de config TellMyVote.json ou avec la commande chat /myvote_poll."},
                {"Info03Msg", "SI UNE QUESTION EST LAISSéE VIDE : elle et ses questions ne seront pas affichés."},
                {"Info04Msg", "SI UNE REPONSE EST VIDE : son bouton ne s'affichera pas."},
                {"HowToMsg", "S'il vous plait utilisez ce format :\n/myvote_poll 1 0 taper ici le titre#1 - consultez la page du plugin"},

            }, this, "fr");
        }

        #endregion

        class StoredData
        {
            public int[] answers = new int[12];
            public List<ulong> voted01 = new List<ulong>();
            public List<ulong> voted02 = new List<ulong>();
            public List<ulong> voted03 = new List<ulong>();
            public List<ulong> voted04 = new List<ulong>();
            public List<ulong>[] votes = new List<ulong>[12] {
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>(),
                new List<ulong>()
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
                    Puts("Une erreur est survenue: " + e);
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
                    Puts("Une erreur est survenue: " + e);
                }
            }
        }

        void PlayerMessage(BasePlayer player, string poll, string answer, string sentence)
        {
            Player.Message(player, $"Poll#{poll}/Answer#{answer} has been set to : {sentence}", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
        }

        #endregion

        #region VOTING

        [ConsoleCommand("TellMyVote")]
        private void MySurveySpotOnly(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            ulong playerID = player.userID;
            int answernumber;

            if (storedData.myVoteIsON == false)
            {
                Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("TMVoffMsg", this, player.UserIDString)} #1</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                return;
            }
            try
            {
                answernumber = int.Parse(arg.Args.FirstOrDefault());
                if (answernumber > 0 && answernumber <= 12)
                {
                    if (answernumber <= 3)
                    {
                        if (storedData.voted01.Contains(playerID))
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #1 - already voted"); }
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #1</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                            return;
                        }
                        if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #1 vote recorded"); }
                        storedData.voted01.Add(playerID);
                        Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #1</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    else if (answernumber >= 4 && answernumber <= 6)
                    {
                        if (storedData.voted02.Contains(playerID))
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #2 - already voted"); }
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #2</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                            return;
                        }
                        if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #2 vote recorded"); }
                        storedData.voted02.Add(playerID);
                        Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #2</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    else if (answernumber >= 7 && answernumber <= 9)
                    {
                        if (storedData.voted03.Contains(playerID))
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #3 - already voted"); }
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #3</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                            return;
                        }
                        if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #3 vote recorded"); }
                        storedData.voted03.Add(playerID);
                        Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #3</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    else if (answernumber >= 10 && answernumber <= 12)
                    {
                        if (storedData.voted04.Contains(playerID))
                        {
                            if (debug == true) { Puts($"-> answernumber = {answernumber} - POLL #4 - already voted"); }
                            Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("QAlreadyMsg", this, player.UserIDString)} #4</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                            return;
                        }
                        storedData.voted04.Add(playerID);
                        Player.Message(player, $"<color={ChatColor}>{lang.GetMessage("VoteLogMsg", this, player.UserIDString)} #4</color>", $"<color={PrefixColor}> {Prefix} </color>", SteamIDIcon);
                    }
                    storedData.answers[answernumber - 1]++;
                    storedData.votes[answernumber - 1].Add(playerID);
                    RefreshMyVotePanel(player);
                }
            }
            catch (Exception e)
            {
                Puts("Une erreur est survenue: " + e);
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
            for (int i = 0; i < 12; i++) storedData.answers[i] = 0;
            storedData.voted01.Clear();
            storedData.voted02.Clear();
            storedData.voted03.Clear();
            storedData.voted04.Clear();
        }

        #region POPUP BANNER

        private void PopUpVote(string newstate)
        {
            string bannertxt = "";
            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(pl => pl.IsConnected)) //Lag was from here => using ALL the list instead of only connected players. Could be HUGE.
            {
                if (newstate == "start")
                {
                    bannertxt = $"{lang.GetMessage("VoteBannerMsg", this, player.UserIDString)}";
                }
                else if (newstate == "end")
                {
                    bannertxt = $"{lang.GetMessage("TMVoffMsg", this, player.UserIDString)}";
                    tmvbanner.Destroy();
                }

                CuiElementContainer CuiElement = new CuiElementContainer();
                var MyVoteBanner = CuiElement.Add(new CuiPanel { Image = { Color = "0.5 1.0 0.5 0.5" }, RectTransform = { AnchorMin = "0.20 0.85", AnchorMax = "0.80 0.90" }, CursorEnabled = false });
                var closeButton = new CuiButton { Button = { Close = MyVoteBanner, Color = "0.0 0.0 0.0 0.6" }, RectTransform = { AnchorMin = "0.90 0.01", AnchorMax = "0.99 0.99" }, Text = { Text = "X", FontSize = 18, Align = TextAnchor.MiddleCenter } };
                CuiElement.Add(closeButton, MyVoteBanner);
                CuiElement.Add(new CuiLabel { Text = { Text = $"{bannertxt}", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "0.0 0.0 0.0 1" }, RectTransform = { AnchorMin = "0.10 0.10", AnchorMax = "0.90 0.90" } }, MyVoteBanner);
                CuiHelper.AddUi(player, CuiElement);
                timer.Once(12f, () =>
                {
                    CuiHelper.DestroyUi(player, MyVoteBanner);
                });
                if (debug) { Puts($"-> TIMER IS SET TO {BannerTimer} minutes"); }
                if (storedData.myVoteIsON == true)
                {
                    if (debug) { Puts($"-> TIMER LOOP {BannerTimer * 60} seconds"); }
                    tmvbanner = timer.Repeat(BannerTimer * 60, 0, () =>
                     {
                         PopUpVote("start");
                     });
                }
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
            MyVotePanel = CuiElement.Add(new CuiPanel { Image = { Color = $"{PanelColor}" }, RectTransform = { AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.80" }, CursorEnabled = true });
            var closeButton = new CuiButton { Button = { Close = MyVotePanel, Color = $"{buttonCloseColor}" }, RectTransform = { AnchorMin = "0.85 0.85", AnchorMax = "0.95 0.95" }, Text = { Text = "[X]\nClose", FontSize = 16, Align = TextAnchor.MiddleCenter } };
            CuiElement.Add(closeButton, MyVotePanel);
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

            #region COLONNE GAUCHE

            if (polls[0, 0] != string.Empty)
            {
                CuiElement.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"{debutcolonne1} {basligne1}", AnchorMax = $"{fincolonne1b} {hautligne1}" },
                    Text = { Text = $"#1. {polls[0, 0]}", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft }
                }, MyVotePanel);

                if (polls[0, 1] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 1", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne2}", AnchorMax = $"{fincolonne1} {hautligne2}" },
                        Text = { Text = $"{polls[0, 1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne2}", AnchorMax = $"{fincolonne1b} {hautligne2}" },
                        Text = { Text = $"{storedData.answers[0]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }

                if (polls[0, 2] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 2", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne3}", AnchorMax = $"{fincolonne1} {hautligne3}" },
                        Text = { Text = $"{polls[0, 2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne3}", AnchorMax = $"{fincolonne1b} {hautligne3}" },
                        Text = { Text = $"{storedData.answers[1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[0, 3] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 3", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne4}", AnchorMax = $"{fincolonne1} {hautligne4}" },
                        Text = { Text = $"{polls[0, 3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne4}", AnchorMax = $"{fincolonne1b} {hautligne4}" },
                        Text = { Text = $"{storedData.answers[2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
            }

            if (polls[1, 0] != string.Empty)
            {
                CuiElement.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"{debutcolonne1} {basligne5}", AnchorMax = $"{fincolonne1b} {hautligne5}" },
                    Text = { Text = $"#2. {polls[1, 0]}", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft }
                }, MyVotePanel);

                if (polls[1, 1] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 4", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne6}", AnchorMax = $"{fincolonne1} {hautligne6}" },
                        Text = { Text = $"{polls[1, 1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne6}", AnchorMax = $"{fincolonne1b} {hautligne6}" },
                        Text = { Text = $"{storedData.answers[3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[1, 2] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 5", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne7}", AnchorMax = $"{fincolonne1} {hautligne7}" },
                        Text = { Text = $"{polls[1, 2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne7}", AnchorMax = $"{fincolonne1b} {hautligne7}" },
                        Text = { Text = $"{storedData.answers[4]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[1, 3] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 6", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1} {basligne8}", AnchorMax = $"{fincolonne1} {hautligne8}" },
                        Text = { Text = $"{polls[1, 3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne1b} {basligne8}", AnchorMax = $"{fincolonne1b} {hautligne8}" },
                        Text = { Text = $"{storedData.answers[5]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
            }


            #endregion

            #region COLONNE DROITE

            if (polls[2, 0] != string.Empty)
            {
                CuiElement.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"{debutcolonne2} {basligne1}", AnchorMax = $"{fincolonne2b} {hautligne1}" },
                    Text = { Text = $"#3. {polls[2, 0]}", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft }
                }, MyVotePanel);

                if (polls[2, 1] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 7", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne2}", AnchorMax = $"{fincolonne2} {hautligne2}" },
                        Text = { Text = $"{polls[2, 1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne2}", AnchorMax = $"{fincolonne2b} {hautligne2}" },
                        Text = { Text = $"{storedData.answers[6]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }

                if (polls[2, 2] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 8", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne3}", AnchorMax = $"{fincolonne2} {hautligne3}" },
                        Text = { Text = $"{polls[2, 2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne3}", AnchorMax = $"{fincolonne2b} {hautligne3}" },
                        Text = { Text = $"{storedData.answers[7]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[2, 3] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 9", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne4}", AnchorMax = $"{fincolonne2} {hautligne4}" },
                        Text = { Text = $"{polls[2, 3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne4}", AnchorMax = $"{fincolonne2b} {hautligne4}" },
                        Text = { Text = $"{storedData.answers[8]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
            }

            if (polls[3, 0] != string.Empty)
            {
                CuiElement.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"{debutcolonne2} {basligne5}", AnchorMax = $"{fincolonne2b} {hautligne5}" },
                    Text = { Text = $"#4. {polls[3, 0]}", Color = $"{QuestionColor}", FontSize = 16, Align = TextAnchor.MiddleLeft }
                }, MyVotePanel);
                if (polls[3, 1] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 10", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne6}", AnchorMax = $"{fincolonne2} {hautligne6}" },
                        Text = { Text = $"{polls[3, 1]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne6}", AnchorMax = $"{fincolonne2b} {hautligne6}" },
                        Text = { Text = $"{storedData.answers[9]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[3, 2] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 11", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne7}", AnchorMax = $"{fincolonne2} {hautligne7}" },
                        Text = { Text = $"{polls[3, 2]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);


                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne7}", AnchorMax = $"{fincolonne2b} {hautligne7}" },
                        Text = { Text = $"{storedData.answers[10]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
                if (polls[3, 3] != string.Empty)
                {
                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "TellMyVote 12", Color = $"{AnswerColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2} {basligne8}", AnchorMax = $"{fincolonne2} {hautligne8}" },
                        Text = { Text = $"{polls[3, 3]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);

                    CuiElement.Add(new CuiButton
                    {
                        Button = { Command = "", Color = $"{CountColor}" },
                        RectTransform = { AnchorMin = $"{debutcolonne2b} {basligne8}", AnchorMax = $"{fincolonne2b} {hautligne8}" },
                        Text = { Text = $"{storedData.answers[11]}", Color = "0.0 0.0 0.0 1", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, MyVotePanel);
                }
            }

            CuiHelper.AddUi(player, CuiElement);
        }
        #endregion

    }
}

/*
banner all other players when a player votes
server rewards points when votes
*/
