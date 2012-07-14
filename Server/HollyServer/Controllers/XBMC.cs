using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Speech.Recognition.SrgsGrammar;
using System.Threading;
using System.Globalization;
using System.Xml;
using System.Speech.Recognition;
using System.Text.RegularExpressions;

namespace HollyServer
{

    public class XBMCEventArgs : IControllableEventArgs
    {

        public XBMCEventArgs(string name, string action, int id, int s, int e)
            : base(name, action)
        {
            season = s;
            uniqueID = id;
            episode = e;
        }
        public int season;
        public int uniqueID;
        public int episode;
    }


    public class XBMC : IControllable
    {
        string mIPAddress;
        string mPort;
        string mUser;
        string mPassword;

        WebClient mClnt;
        List<XBMCProto.TVShow> mTVShows;
        List<XBMCProto.Movie> mMovies;
        List<XBMCProto.Genre> mGenres;


        SrgsItem CreateNumber(string prepend, SrgsDocument doc)
        {
            SrgsItem digit_0 = new SrgsItem("zero");
            digit_0.Add(new SrgsNameValueTag(prepend+"0", 0));
            SrgsItem digit_1 = new SrgsItem("one");
            digit_1.Add(new SrgsNameValueTag(prepend + "1", 1));
            SrgsItem digit_2 = new SrgsItem("two");
            digit_2.Add(new SrgsNameValueTag(prepend + "2", 2));
            SrgsItem digit_3 = new SrgsItem("three");
            digit_3.Add(new SrgsNameValueTag(prepend + "3", 3));
            SrgsItem digit_4 = new SrgsItem("four");
            digit_4.Add(new SrgsNameValueTag(prepend + "4", 4));
            SrgsItem digit_5 = new SrgsItem("five");
            digit_5.Add(new SrgsNameValueTag(prepend + "5", 5));
            SrgsItem digit_6 = new SrgsItem("six");
            digit_6.Add(new SrgsNameValueTag(prepend + "6", 6));
            SrgsItem digit_7 = new SrgsItem("seven");
            digit_7.Add(new SrgsNameValueTag(prepend + "7", 7));
            SrgsItem digit_8 = new SrgsItem("eight");
            digit_8.Add(new SrgsNameValueTag(prepend + "8", 8));
            SrgsItem digit_9 = new SrgsItem("nine");
            digit_9.Add(new SrgsNameValueTag(prepend + "9", 9));
            SrgsItem digit_10 = new SrgsItem("ten");
            digit_10.Add(new SrgsNameValueTag(prepend + "10", 10));
            SrgsItem digit_11 = new SrgsItem("eleven");
            digit_11.Add(new SrgsNameValueTag(prepend + "11", 11));
            SrgsItem digit_12 = new SrgsItem("twelve");
            digit_12.Add(new SrgsNameValueTag(prepend + "12", 12));
            SrgsItem digit_13 = new SrgsItem("thirteen");
            digit_13.Add(new SrgsNameValueTag(prepend + "13", 13));
            SrgsItem digit_14 = new SrgsItem("fourteen");
            digit_14.Add(new SrgsNameValueTag(prepend + "14", 14));
            SrgsItem digit_15 = new SrgsItem("fifteen");
            digit_15.Add(new SrgsNameValueTag(prepend + "15", 15));
            SrgsItem digit_16 = new SrgsItem("sixteen");
            digit_16.Add(new SrgsNameValueTag(prepend + "16", 16));
            SrgsItem digit_17 = new SrgsItem("seventeen");
            digit_17.Add(new SrgsNameValueTag(prepend + "17", 17));
            SrgsItem digit_18 = new SrgsItem("eighteen");
            digit_18.Add(new SrgsNameValueTag(prepend + "18", 18));
            SrgsItem digit_19 = new SrgsItem("nineteen");
            digit_19.Add(new SrgsNameValueTag(prepend + "19", 19));

            SrgsItem digit_20 = new SrgsItem("twenty");
            digit_20.Add(new SrgsNameValueTag(prepend + "20", 20));
            SrgsItem digit_30 = new SrgsItem("thirty");
            digit_30.Add(new SrgsNameValueTag(prepend + "30", 30));
            SrgsItem digit_40 = new SrgsItem("fourty");
            digit_40.Add(new SrgsNameValueTag(prepend + "40", 40));
            SrgsItem digit_50 = new SrgsItem("fifty");
            digit_50.Add(new SrgsNameValueTag(prepend + "50", 50));
            SrgsItem digit_60 = new SrgsItem("sixty");
            digit_60.Add(new SrgsNameValueTag(prepend + "60", 60));

            SrgsOneOf digit_onepart = new SrgsOneOf(digit_0, digit_1, digit_2, digit_3,
                digit_4, digit_5, digit_6, digit_7, digit_8, digit_9, digit_10,
                digit_11, digit_12, digit_13, digit_14, digit_15, digit_16, digit_17, digit_18, digit_19,
                digit_20, digit_30, digit_40, digit_50, digit_60);
            SrgsOneOf digit_single = new SrgsOneOf(digit_1, digit_2, digit_3,
                digit_4, digit_5, digit_6, digit_7, digit_8, digit_9);
            SrgsOneOf digit_second = new SrgsOneOf(digit_20, digit_30, digit_40, digit_50, digit_60);
            SrgsRule digit_twopart = new SrgsRule(prepend+"_twodigit");
            digit_twopart.Add(digit_second);
            digit_twopart.Add(digit_single);
            doc.Rules.Add(digit_twopart);
            SrgsOneOf number = new SrgsOneOf(
                new SrgsItem(digit_onepart), new SrgsItem(new SrgsRuleRef(digit_twopart)));

            SrgsItem ret = new SrgsItem(number);
            ret.Add(new SrgsNameValueTag(prepend, "true"));
            return ret;
        }
        public string GetName()
        {
            return "XBMC";
        }
        static SrgsDocument currentSrgsDoc = null;
        public System.Speech.Recognition.SrgsGrammar.SrgsDocument CreateGrammarDoc()
        {
            if (currentSrgsDoc != null) return currentSrgsDoc;

            SrgsDocument doc = new SrgsDocument();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
            SrgsOneOf ops = new SrgsOneOf();


            //TV Shows
            SrgsOneOf shows = new SrgsOneOf();
            if (mTVShows == null)
            {
                mTVShows = ListTVShows();
            }
            if (mTVShows == null) return null;
            foreach (XBMCProto.TVShow s in mTVShows)
            {
                SrgsItem showitem = new SrgsItem(CleanString(s.label));
                showitem.Add(new SrgsNameValueTag("tvshowid", s.tvshowid));
                shows.Add(showitem);
            }


            SrgsItem cmd_play = new SrgsItem("play show");
            cmd_play.Add(new SrgsNameValueTag("command", "play show"));
            SrgsItem cmd_resume = new SrgsItem("resume show");
            cmd_resume.Add(new SrgsNameValueTag("command", "resume show"));
            SrgsOneOf cmd_tv = new SrgsOneOf(cmd_play, cmd_resume);

            SrgsRule rule_playshow_episode = new SrgsRule("playshow_episode");
            rule_playshow_episode.Add(cmd_tv);
            rule_playshow_episode.Add(shows);
            rule_playshow_episode.Add(new SrgsItem("season"));
            rule_playshow_episode.Add(CreateNumber("seasonval", doc));  //episode number
            rule_playshow_episode.Add(new SrgsItem("episode"));
            rule_playshow_episode.Add(CreateNumber("episodeval", doc));   //episode number
            doc.Rules.Add(rule_playshow_episode);
            ops.Add(new SrgsItem(new SrgsRuleRef(rule_playshow_episode)));




            //Movies
            SrgsOneOf films = new SrgsOneOf();
            if (mMovies == null)
            {
                mMovies = ListMovies();
            }
            if (mMovies == null) return null;
            foreach (XBMCProto.Movie m in mMovies)
            {
                if (m.label == "" || m.movieid < 1) continue;
                SrgsItem filmItem = new SrgsItem(CleanString(m.label));
                filmItem.Add(new SrgsNameValueTag("movieid", m.movieid));
                films.Add(filmItem);
            }

            SrgsItem cmd_play_movie = new SrgsItem("play movie");
            cmd_play_movie.Add(new SrgsNameValueTag("command", "play movie"));
            SrgsItem cmd_resume_movie = new SrgsItem("resume movie");
            cmd_resume_movie.Add(new SrgsNameValueTag("command", "resume movie"));
            SrgsOneOf cmd_movie = new SrgsOneOf(cmd_play_movie, cmd_resume_movie);

            SrgsRule rule_playmovie = new SrgsRule("playmovie");
            rule_playmovie.Add(cmd_movie);
            rule_playmovie.Add(films);
            doc.Rules.Add(rule_playmovie);
            ops.Add(new SrgsItem(new SrgsRuleRef(rule_playmovie)));



            //Music Genres
            SrgsOneOf audioGenres = new SrgsOneOf();
            if (mGenres == null)
            {
                mGenres = ListAudioGenres();
            }
            if (mGenres == null) return null;
            foreach (XBMCProto.Genre m in mGenres)
            {
                if (m.label == "" || m.genreid < 1) continue;
                SrgsItem agItem = new SrgsItem(CleanString(m.label)+" music");
                agItem.Add(new SrgsNameValueTag("genreid", m.genreid));
                audioGenres.Add(agItem);
            }

            SrgsItem cmd_play_genre = new SrgsItem("play me some");
            cmd_play_genre.Add(new SrgsNameValueTag("command", "play me some"));
            SrgsOneOf cmd_genre = new SrgsOneOf(cmd_play_genre);

            SrgsRule rule_playgenre = new SrgsRule("playgenre");
            rule_playgenre.Add(cmd_genre);
            rule_playgenre.Add(audioGenres);
            doc.Rules.Add(rule_playgenre);
            ops.Add(new SrgsItem(new SrgsRuleRef(rule_playgenre)));



            //Other
            SrgsItem cmd_stop_media = new SrgsItem("stop media");
            cmd_stop_media.Add(new SrgsNameValueTag("command", "stop media"));
            SrgsItem cmd_pause_media = new SrgsItem("pause media");
            cmd_pause_media.Add(new SrgsNameValueTag("command", "pause media"));
            SrgsItem cmd_resume_media = new SrgsItem("resume media");
            cmd_resume_media.Add(new SrgsNameValueTag("command", "resume media"));
            ops.Add(new SrgsItem(cmd_stop_media));
            ops.Add(new SrgsItem(cmd_resume_media));
            ops.Add(new SrgsItem(cmd_pause_media));

            SrgsRule root_rule = new SrgsRule("rootrule");
            root_rule.Add(new SrgsItem("Holly"));
            root_rule.Add(ops);
            root_rule.Add(new SrgsItem("please"));
            doc.Rules.Add(root_rule);

            doc.Root = root_rule;

            XmlWriter xmlout = XmlWriter.Create(@"C:\Users\ian\Desktop\xbmc-grammar.xml");
            doc.WriteSrgs(xmlout);
            xmlout.Close();

            currentSrgsDoc = doc;
            return currentSrgsDoc;
        }
        public bool OnSpeechRecognised(string ID, System.Speech.Recognition.RecognitionResult result)
        {
            //ID: source of the audio. Normally IPaddress:port
            string cmd = "";
            if (result.Semantics.ContainsKey("command")) cmd = result.Semantics["command"].Value.ToString();

            if (cmd == "play show" || cmd == "resume show")
            {
                int tvshowid = 0;
                int episode = 0;
                int season = 0;
                foreach(KeyValuePair<String, SemanticValue> hit in result.Semantics){
                    if (hit.Key == "tvshowid") tvshowid = (int)hit.Value.Value;
                    else if (hit.Key.StartsWith("episodeval") && hit.Key != "episodeval") 
                        episode += (int)hit.Value.Value;
                    else if (hit.Key.StartsWith("seasonval") && hit.Key != "seasonval") 
                        season += (int)hit.Value.Value;
                }

                if (tvshowid != 0 && episode != 0 && season != 0)
                {
                    OnControllableEvent(this, new XBMCEventArgs(ID, cmd, tvshowid, season, episode));
                    return true;
                }
            }
            else if (cmd == "play movie" || cmd == "resume movie")
            {
                int movieid = 0;
                foreach (KeyValuePair<String, SemanticValue> hit in result.Semantics)
                {
                    if (hit.Key == "movieid")
                    {
                        movieid = (int)hit.Value.Value;
                        break;
                    }
                }
                if (movieid > 0)
                {
                    OnControllableEvent(this, new XBMCEventArgs(ID, cmd, movieid, 0, 0));
                    return true;
                }
            }
            else if (cmd == "stop media" || cmd == "pause media" || cmd == "resume media")
            {
                OnControllableEvent(this, new XBMCEventArgs(ID, cmd, 0, 0, 0));
                return true;
            }
            else if (cmd == "play me some")
            { //genre music
                int genreid = 0;
                foreach (KeyValuePair<String, SemanticValue> hit in result.Semantics)
                {
                    if (hit.Key == "genreid")
                    {
                        genreid = (int)hit.Value.Value;
                        break;
                    }
                }
                if (genreid > 0)
                {
                    OnControllableEvent(this, new XBMCEventArgs(ID, cmd, genreid, 0, 0));
                    return true;
                }
            }
            return false;
        }

        public event IControllableEventDelegate ControllableEvent;
        void OnControllableEvent(IControllable from, IControllableEventArgs Event)
        {
            if (ControllableEvent != null)
            {
                ControllableEvent(from, Event);
            }
        }


        public XBMC(string IPAddress, string Port, string user, string password)
        {
            mIPAddress = IPAddress;
            mPassword = password;
            mPort = Port;
            mUser = user;

            mClnt = new WebClient();
            mClnt.Credentials = new NetworkCredential(mUser, mPassword);
            mClnt.BaseAddress = "http://" + mIPAddress + ":" + mPort + "/jsonrpc";
        }

        public List<XBMCProto.TVShow> ListTVShows()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("VideoLibrary.GetTVShows",
                mClnt);

            XBMCProto.JSONRPCQueryResultRes jres = json.Execute_Obj();

            if (jres == null) return null;
            return jres.tvshows;
        }
        public List<XBMCProto.Movie> ListMovies()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("VideoLibrary.GetMovies",
                mClnt);

            XBMCProto.JSONRPCQueryResultRes jres = json.Execute_Obj();

            if (jres == null) return null;
            return jres.movies;
        }
        public List<XBMCProto.Season> ListSeasons(int tvshowid)
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("VideoLibrary.GetSeasons",
                mClnt);
            json.AddParameter("tvshowid", tvshowid);

            XBMCProto.JSONRPCQueryResultRes jres = json.Execute_Obj();

            if (jres == null) return null;
            return jres.seasons;
        }
        public List<XBMCProto.Episode> ListEpisodes(int tvshowid, int seasonidx)
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("VideoLibrary.GetEpisodes",
                mClnt);
            json.AddParameter("tvshowid", tvshowid);
            json.AddParameter("season", seasonidx);

            XBMCProto.JSONRPCQueryResultRes jres = json.Execute_Obj();

            if (jres == null) return null;
            return jres.episodes;
        }
        public List<XBMCProto.Genre> ListAudioGenres()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("AudioLibrary.GetGenres",
                mClnt);

            XBMCProto.JSONRPCQueryResultRes jres = json.Execute_Obj();

            if (jres == null) return null;
            return jres.genres;
        }

        string CleanString(string s)
        {
            string tmp = s;
            tmp = tmp.Replace("$#*!", "Shit");
            tmp = tmp.Replace(":", "");
            return tmp;
        }

        public bool PlayMovie(int movieid, bool resumeIfPossible)
        {
            if (movieid <= 0)
            {
                Form1.updateLog("ERR: Couldn't find episode", ELogLevel.Error, ELogType.XBMC);
                return false;
            }

            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.Open", mClnt);
            json.AddParameter("item", "\"movieid\":" + movieid.ToString(), "object");

            string status = json.Execute_Str();
            if (status == "OK") return true;
            return false;
        }
        public bool PlayTV(int tvshowid, int season, int episode, bool resumeIfPossible)
        {
            //find episodeid
            List<XBMCProto.Episode> eps = ListEpisodes(tvshowid, season);
            if (eps == null) return false;
            int episodeid = -1;
            Regex reg_se = new Regex(@"[sS](\d+)[eE](\d+)");
            Regex reg_x = new Regex(@"(\d+)x(\d+)");
            foreach (XBMCProto.Episode e in eps)
            {
                string s_ep = e.label;
                Match m = reg_se.Match(s_ep);
                if (m.Success)
                {
                    int epnum = -1;
                    if (Int32.TryParse(m.Groups[2].Value, out epnum))
                    {
                        if (epnum == episode) episodeid = e.episodeid;
                    }
                    else
                    {
                        Form1.updateLog("ERR: Failed to parse episode from: " + m.Groups[2].Value, ELogLevel.Error, 
                            ELogType.XBMC | ELogType.SpeechRecog);
                    }
                }
                else
                {
                    m = reg_x.Match(s_ep);
                    if (m.Success)
                    {
                        int epnum = -1;
                        if (Int32.TryParse(m.Groups[2].Value, out epnum))
                        {
                            if (epnum == episode) episodeid = e.episodeid;
                        }
                        else
                        {
                            Form1.updateLog("ERR: Failed to parse episode from: " + m.Groups[2].Value, ELogLevel.Error,
                                ELogType.XBMC | ELogType.SpeechRecog);
                        }
                    }
                    else
                    {
                        Form1.updateLog("ERR: Ep format unknown: " + s_ep, ELogLevel.Error, 
                            ELogType.XBMC | ELogType.SpeechRecog);
                    }
                }
                if (episodeid != -1) break;
            }
            if (episodeid == -1)
            {
                Form1.updateLog("ERR: Couldn't find episode", ELogLevel.Error, ELogType.XBMC);
                return false;
            }

            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.Open", mClnt);
            json.AddParameter("item", "\"episodeid\":"+episodeid.ToString(), "object");
            if (resumeIfPossible) json.AddParameter("resume", true);

            string status = json.Execute_Str();
            if (status == "OK") return true;
            return false;
        }
        public bool PlayGenre(int genreid)
        {
            if (genreid <= 0)
            {
                Form1.updateLog("ERR: Couldn't find genre", ELogLevel.Error, ELogType.XBMC);
                return false;
            }

            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.Open", mClnt);
            json.AddParameter("item", "\"genreid\":" + genreid.ToString(), "object");

            string status = json.Execute_Str();
            if (status == "OK") return true;
            return false;
        }
   
        public bool Stop()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.Stop", mClnt);
            json.AddParameter("playerid", 0);

            string status = json.Execute_Str();
            json = new XBMCProto.JSONParser("Player.Stop", mClnt);
            json.AddParameter("playerid", 1);

            status = json.Execute_Str();
            json = new XBMCProto.JSONParser("Player.Stop", mClnt);
            json.AddParameter("playerid", 2);

            status = json.Execute_Str();
            if (status == "OK") return true;
            return false;
        }
        public bool Pause()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.PlayPause", mClnt);
            json.AddParameter("playerid", 1);

            string status = json.Execute_None();
            if (status == "OK") return true;
            return false;
        }
        public bool Resume()
        {
            XBMCProto.JSONParser json = new XBMCProto.JSONParser("Player.PlayPause", mClnt);
            json.AddParameter("playerid", 1);

            string status = json.Execute_Str();
            if (status == "OK") return true;
            return false;
        }

    }
}
