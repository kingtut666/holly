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


    public class XBMC : IControllable, IDisposable
    {
        string mIPAddress;
        string mPort;
        string mUser;
        string mPassword;

        WebClient mClnt;
        List<XBMCProto.TVShow> mTVShows;
        List<XBMCProto.Movie> mMovies;
        List<XBMCProto.Genre> mGenres;


        SrgsItem CreateNumberSRGS(string prepend, SrgsDocument doc)
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
        bool CreateNumberJSGF(string rulename, CMUSphinx_GrammarDict cgd)
        {
            
            List<string> digit_single = new List<string>();
            digit_single.Add("one");
            digit_single.Add("two");
            digit_single.Add("three");
            digit_single.Add("four");
            digit_single.Add("five");
            digit_single.Add("six");
            digit_single.Add("seven");
            digit_single.Add("eight");
            digit_single.Add("nine");

            List<string> digit_tens = new List<string>();
            digit_tens.Add("twenty");
            digit_tens.Add("thirty");
            digit_tens.Add("fourty");
            digit_tens.Add("fifty");
            digit_tens.Add("sixty");

            List<string> digit_special = new List<string>();
            digit_special.Add("zero");
            digit_special.Add("ten");
            digit_special.Add("eleven");
            digit_special.Add("twelve");
            digit_special.Add("thirteen");
            digit_special.Add("fourteen");
            digit_special.Add("fifteen");
            digit_special.Add("sixteen");
            digit_special.Add("seventeen");
            digit_special.Add("eighteen");
            digit_special.Add("nineteen");

            StringBuilder bld_twodigit = new StringBuilder();
            cgd.JSGFRuleStart("<"+rulename+"_twodigit>", bld_twodigit);
            cgd.JSGFRuleAddChoicesStart(bld_twodigit, digit_tens);
            cgd.JSGFRuleAddChoicesEnd(bld_twodigit);
            cgd.JSGFRuleAddChoicesStart(bld_twodigit, digit_single);
            cgd.JSGFRuleAddChoicesEnd(bld_twodigit);
            cgd.JSGFRuleEnd("<"+rulename + "_twodigit>", bld_twodigit);

            StringBuilder bld_onedigit = new StringBuilder();
            cgd.JSGFRuleStart("<"+rulename + "_onedigit>", bld_onedigit);
            cgd.JSGFRuleAddChoicesStart(bld_onedigit, digit_single);
            cgd.JSGFRuleAddChoicesMore(bld_onedigit, digit_tens);
            cgd.JSGFRuleAddChoicesMore(bld_onedigit, digit_special);
            cgd.JSGFRuleAddChoicesEnd(bld_onedigit);
            cgd.JSGFRuleEnd("<"+rulename + "_onedigit>", bld_onedigit);

            StringBuilder bld = new StringBuilder();
            cgd.JSGFRuleStart("<"+rulename+">", bld);
            cgd.JSGFRuleAddChoicesStart(bld, new List<string>(new string[] { "<"+rulename + "_onedigit>", "<"+rulename + "_twodigit>" }));
            cgd.JSGFRuleAddChoicesEnd(bld);
            cgd.JSGFRuleEnd("<"+rulename+">", bld);

            return true;
        }
        CMUSphinx_FSGState CreateNumberFSG(CMUSphinx_FSGState startState, CMUSphinx_GrammarDict cgd)
        {
            CMUSphinx_FSGState start_special = cgd.FSGCreateOrphanState();
            CMUSphinx_FSGState start_digits = cgd.FSGCreateOrphanState();
            CMUSphinx_FSGState start_tens = cgd.FSGCreateOrphanState();
            CMUSphinx_FSGState end = cgd.FSGCreateOrphanState();
            CMUSphinx_FSGState end_tens = cgd.FSGCreateOrphanState();

            //fillers
            cgd.FSGLinkStates(startState, start_digits, "");
            cgd.FSGLinkStates(startState, start_special, "");
            cgd.FSGLinkStates(startState, start_tens, "");

            //digits
            cgd.FSGLinkStates(start_digits, end, "one");
            cgd.FSGLinkStates(start_digits, end, "two");
            cgd.FSGLinkStates(start_digits, end, "seven");
            //TODO

            
            //specials
            cgd.FSGLinkStates(start_special, end, "ten");
            cgd.FSGLinkStates(start_special, end, "eleven");
            cgd.FSGLinkStates(start_special, end, "twelve");
            //TODO: Finish these
            
            //tens
            cgd.FSGLinkStates(start_tens, end_tens, "twenty");
            cgd.FSGLinkStates(start_tens, end_tens, "thirty");
            //TODO: Finish the tens
            cgd.FSGLinkStates(end_tens, start_digits, "");
            cgd.FSGLinkStates(end_tens, end, "");


            return end;
        }

        public string GetName()
        {
            return "XBMC";
        }
        static SrgsDocument currentSrgsDoc = null;
        public System.Speech.Recognition.SrgsGrammar.SrgsDocument CreateGrammarDoc_SRGS()
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
                string temps = CleanString(s.label);
                temps = temps.Trim();
                if (temps == "" || s.tvshowid < 1) continue;
                SrgsItem showitem = new SrgsItem(temps);
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
            rule_playshow_episode.Add(CreateNumberSRGS("seasonval", doc));  //episode number
            rule_playshow_episode.Add(new SrgsItem("episode"));
            rule_playshow_episode.Add(CreateNumberSRGS("episodeval", doc));   //episode number
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
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.movieid < 1) continue;
                SrgsItem filmItem = new SrgsItem(temps);
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
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.genreid < 1) continue;
                SrgsItem agItem = new SrgsItem(temps+" music");
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
        static CMUSphinx_GrammarDict currentJSGFDoc = null;
        public CMUSphinx_GrammarDict CreateGrammarDoc_JSGF()
        {
            if (currentJSGFDoc != null) return currentJSGFDoc;
            CMUSphinx_GrammarDict ret = new CMUSphinx_GrammarDict();
            ret.GrammarName = GetName();
            StringBuilder bld = new StringBuilder();

            List<string> ops = new List<string>();


            //numbers
            CreateNumberJSGF("NUMS", ret);

            //TV Shows
            if (mTVShows == null)
            {
                mTVShows = ListTVShows();
            }
            if (mTVShows == null) return null;
            List<string> tvShowNames = new List<string>();
            foreach (XBMCProto.TVShow s in mTVShows)
            {
                string temps = CleanString(s.label);
                temps = temps.Trim();
                if (temps == "" || s.tvshowid < 1) continue;
                tvShowNames.Add(temps);
            }

            string tvshow_name = "<TVSHOW>";
            StringBuilder tvshow_bld = new StringBuilder();
            ret.JSGFRuleStart(tvshow_name, tvshow_bld);
            ret.JSGFRuleAddChoicesStart(tvshow_bld, new List<string>(new string[]{ "play show", "resume show"}));
            ret.JSGFRuleAddChoicesEnd(tvshow_bld);
            ret.JSGFRuleAddChoicesStart(tvshow_bld, tvShowNames);
            ret.JSGFRuleAddChoicesEnd(tvshow_bld);
            ret.JSGFRuleAddToken(tvshow_bld, "SEASON");
            ret.JSGFRuleAddToken(tvshow_bld, "<NUMS>");
            ret.JSGFRuleAddToken(tvshow_bld, "EPISODE");
            ret.JSGFRuleAddToken(tvshow_bld, "<NUMS>");
            ret.JSGFRuleEnd(tvshow_name, tvshow_bld);

            ops.Add(tvshow_name);
            

            //Movies
            if (mMovies == null)
            {
                mMovies = ListMovies();
            }
            if (mMovies == null) return null;
            List<string> movieNames = new List<string>();
            foreach (XBMCProto.Movie m in mMovies)
            {
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.movieid < 1) continue;
                movieNames.Add(temps);
            }

            string movie_name = "<MOVIE>";
            StringBuilder movie_bld = new StringBuilder();
            ret.JSGFRuleStart(movie_name, movie_bld);
            ret.JSGFRuleAddChoicesStart(movie_bld, new List<string>(new string[] { "play movie", "resume movie" }));
            ret.JSGFRuleAddChoicesEnd(movie_bld);
            ret.JSGFRuleAddChoicesStart(movie_bld, movieNames);
            ret.JSGFRuleAddChoicesEnd(movie_bld);
            ret.JSGFRuleEnd(movie_name, movie_bld);

            ops.Add(movie_name);
            


            //Music Genres
            SrgsOneOf audioGenres = new SrgsOneOf();
            if (mGenres == null)
            {
                mGenres = ListAudioGenres();
            }
            if (mGenres == null) return null;
            List<string> genreNames = new List<string>();
            foreach (XBMCProto.Genre m in mGenres)
            {
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.genreid < 1) continue;
                genreNames.Add(temps + " music");
            }

            string audiogenre_name = "<AUDIOGENRE>";
            StringBuilder audiogenre_bld = new StringBuilder();
            ret.JSGFRuleStart(audiogenre_name, audiogenre_bld);
            ret.JSGFRuleAddToken(audiogenre_bld, "play me some");
            ret.JSGFRuleAddChoicesStart(audiogenre_bld, genreNames);
            ret.JSGFRuleAddChoicesEnd(audiogenre_bld);
            ret.JSGFRuleEnd(audiogenre_name, audiogenre_bld);

            ops.Add(audiogenre_name);


            //Other
            ops.Add("stop media");
            ops.Add("pause media");
            ops.Add("resume media");
            

            StringBuilder bld_ops= new StringBuilder();
            ret.JSGFRuleStart("<OPS>", bld_ops);
            ret.JSGFRuleAddChoicesStart(bld_ops, ops);
            ret.JSGFRuleAddChoicesEnd(bld_ops);
            ret.JSGFRuleEnd("<OPS>", bld_ops);

            ret.JSGFRuleStart("<ROOT>", bld);
            ret.JSGFRuleAddToken(bld, "Holly");
            ret.JSGFRuleAddToken(bld, "<OPS>");
            ret.JSGFRuleAddToken(bld, "please");
            ret.JSGFRuleEnd("<ROOT>", bld);

            ret.JSGFSetRootRule("<ROOT>");

            ret.BuildJSGFGrammarAndDict();
            currentJSGFDoc = ret;
            return ret;
        }
        public CMUSphinx_GrammarDict CreateGrammarDoc_FSG()
        {
            if (currentJSGFDoc != null) return currentJSGFDoc;
            CMUSphinx_GrammarDict ret = new CMUSphinx_GrammarDict();
            ret.GrammarName = GetName();

            CMUSphinx_FSGState root = ret.FSGCreate(ret.GrammarName);
            CMUSphinx_FSGState end = ret.FSGGetEndState();

            //numbers
            CMUSphinx_FSGState epNumsStart = ret.FSGCreateOrphanState();
            CMUSphinx_FSGState epNumsEnd = CreateNumberFSG(epNumsStart, ret);
            CMUSphinx_FSGState seasonNumsStart = ret.FSGCreateOrphanState();
            CMUSphinx_FSGState seasonNumsEnd = CreateNumberFSG(seasonNumsStart, ret);

            //TV Shows
            if (mTVShows == null)
            {
                mTVShows = ListTVShows();
            }
            if (mTVShows == null) return null;
            List<string> tvShowNames = new List<string>();
            foreach (XBMCProto.TVShow s in mTVShows)
            {
                string temps = CleanString(s.label);
                temps = temps.Trim();
                if (temps == "" || s.tvshowid < 1) continue;
                tvShowNames.Add(temps);
            }

            CMUSphinx_FSGState play = ret.FSGTransitionToNewState(root, "Holly play show");
            CMUSphinx_FSGState temp1 = ret.FSGTransitionToNewState(root, "Holly resume show");
            ret.FSGGroupStates(play, temp1);
            temp1 = ret.FSGCreateOrphanState();
            CMUSphinx_FSGState temp2;
            foreach (string s in tvShowNames)
            {
                temp2 = ret.FSGTransitionToNewState(play, s);
                ret.FSGGroupStates(temp1, temp2);
            }
            temp1 = ret.FSGTransitionToNewState(temp1, "season");
            temp1 = CreateNumberFSG(temp1, ret);
            temp1 = ret.FSGTransitionToNewState(temp1, "episode");
            temp1 = CreateNumberFSG(temp1, ret);
            temp1 = ret.FSGTransitionToNewState(temp1, "please");
            ret.FSGGroupStates(end, temp1);


            //Movies
            if (mMovies == null)
            {
                mMovies = ListMovies();
            }
            if (mMovies == null) return null;
            List<string> movieNames = new List<string>();
            foreach (XBMCProto.Movie m in mMovies)
            {
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.movieid < 1) continue;
                movieNames.Add(temps);
            }

            play = ret.FSGTransitionToNewState(root, "Holly play movie");
            temp1 = ret.FSGTransitionToNewState(root, "Holly resume movie");
            ret.FSGGroupStates(play, temp1);
            temp1 = ret.FSGCreateOrphanState();
            foreach (string s in movieNames)
            {
                temp2 = ret.FSGTransitionToNewState(play, s);
                ret.FSGGroupStates(temp1, temp2);
            }
            temp1 = ret.FSGTransitionToNewState(temp1, "please");
            ret.FSGGroupStates(end, temp1);
            

            //Music Genres
            SrgsOneOf audioGenres = new SrgsOneOf();
            if (mGenres == null)
            {
                mGenres = ListAudioGenres();
            }
            if (mGenres == null) return null;
            List<string> genreNames = new List<string>();
            foreach (XBMCProto.Genre m in mGenres)
            {
                string temps = CleanString(m.label);
                temps = temps.Trim();
                if (temps == "" || m.genreid < 1) continue;
                genreNames.Add(temps);
            }

            play = ret.FSGTransitionToNewState(root, "Holly play me some");
            temp1 = ret.FSGCreateOrphanState();
            foreach (string s in movieNames)
            {
                temp2 = ret.FSGTransitionToNewState(play, s);
                ret.FSGGroupStates(temp1, temp2);
            }
            temp1 = ret.FSGTransitionToNewState(temp1, "music please");
            ret.FSGGroupStates(end, temp1);

           


            //Other
            play = ret.FSGTransitionToNewState(root, "Holly stop media please");
            ret.FSGGroupStates(end, play);
            play = ret.FSGTransitionToNewState(root, "Holly pause media please");
            ret.FSGGroupStates(end, play);
            play = ret.FSGTransitionToNewState(root, "Holly resume media please");
            ret.FSGGroupStates(end, play);

            ret.BuildFSGGrammarAndDict();
            currentJSGFDoc = ret;
            return ret;
        }
        
        public bool OnSpeechRecognised(string ID, RecognitionSuccess result)
        {
            //ID: source of the audio. Normally IPaddress:port
            string cmd = "";
            if (result.getSemanticValuesAsString("command") != null) cmd = result.getSemanticValuesAsString("command");

            if (cmd == "play show" || cmd == "resume show")
            {
                int tvshowid = result.getSemanticValueAsInt("tvshowid");
                int episode = result.getSemanticValueAsInt("episodeval");
                int season = result.getSemanticValueAsInt("seasonval");

                if (tvshowid != -1 && episode != -1 && season != -1)
                {
                    OnControllableEvent(this, new XBMCEventArgs(ID, cmd, tvshowid, season, episode));
                    return true;
                }
            }
            else if (cmd == "play movie" || cmd == "resume movie")
            {
                int movieid = result.getSemanticValueAsInt("movieid");
                if (movieid != -1)
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
                int genreid = result.getSemanticValueAsInt("genreid");
                if (genreid != -1)
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
            //TODO: Proper exception handling of all the JSON commands and networking
            mIPAddress = IPAddress;
            mPassword = password;
            mPort = Port;
            mUser = user;

            mClnt = new WebClient();
            mClnt.Credentials = new NetworkCredential(mUser, mPassword);
            mClnt.BaseAddress = "http://" + mIPAddress + ":" + mPort + "/jsonrpc";
        }
        public bool VerifyConnection()
        {
            try
            {
                ListAudioGenres();
            }
            catch (Exception e)
            {
                Form1.updateLog("XBMC: Connection failed: " + e.Message, ELogLevel.Warning, ELogType.XBMC);
                return false;
            }
            return true;
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
            //TODO: This should have a GUI
            string tmp = s;
            tmp = tmp.Replace("(2001)", "");
            tmp = tmp.Replace("(2003)", "");
            tmp = tmp.Replace("(2005)", "");
            tmp = tmp.Replace("(2006)", "");
            tmp = tmp.Replace("(2008)", "");
            tmp = tmp.Replace("(2009)", "");
            tmp = tmp.Replace("(2010)", "");
            tmp = tmp.Replace("(2012)", "");

            tmp = tmp.Replace("2001", " TWO THOUSAND AND ONE ");
            tmp = tmp.Replace("2010", " TWENTY TEN ");

            tmp = tmp.Replace("51ST", " FIFTY FIRST");
            tmp = tmp.Replace("633", " SIX THREE THREE ");
            tmp = tmp.Replace("300", " THREE HUNDRED ");
            tmp = tmp.Replace("39", " THIRTY NINE ");
            tmp = tmp.Replace("30", " THIRTY ");
            tmp = tmp.Replace("21", " TWENTY ONE ");
            tmp = tmp.Replace("24", " TWENTY FOUR ");
            tmp = tmp.Replace("28", " TWENTY EIGHT ");
            tmp = tmp.Replace("12", " TWELVE ");
            tmp = tmp.Replace("13", " THIRTEEN ");
            tmp = tmp.Replace("17", " SEVENTEEN ");
            tmp = tmp.Replace("0", " ZERO ");
            tmp = tmp.Replace("1", " ONE ");
            tmp = tmp.Replace("2", " TWO ");
            tmp = tmp.Replace("3", " THREE ");
            tmp = tmp.Replace("4", " FOUR ");
            tmp = tmp.Replace("5", " FIVE ");
            tmp = tmp.Replace("6", " SIX ");
            tmp = tmp.Replace("7", " SEVEN ");
            tmp = tmp.Replace("8", " EIGHT ");
            tmp = tmp.Replace("9", " NINE ");

            tmp = tmp.Replace("$#*!", " SHIT ");
            tmp = tmp.Replace("VOL.", " VOLUME ");
            tmp = tmp.Replace("-", " ");
            tmp = tmp.Replace("&", " AND ");
            tmp = tmp.Replace("(", "");
            tmp = tmp.Replace(")", "");
            tmp = tmp.Replace("!", "");
            tmp = tmp.Replace("/", " ");
            tmp = tmp.Replace(".", " ");
            tmp = tmp.Replace(" II", " I I ");
            tmp = tmp.Replace(" III", " I I I ");
            tmp = tmp.Replace(" IV ", " I V ");
            tmp = tmp.Replace(" VI ", " V I ");
            tmp = tmp.Replace(" VII ", " V I I ");
            tmp = tmp.Replace(",", " ");
            tmp = tmp.Replace(":", " ");
            tmp = tmp.Replace("?", " ");
            tmp = tmp.Replace("*", " ");
            tmp = tmp.Replace("_", " ");

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


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (mClnt != null) mClnt.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
