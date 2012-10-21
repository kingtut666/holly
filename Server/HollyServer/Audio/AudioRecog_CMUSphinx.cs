using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HollyServer
{
    public class CMUSphinx_FSGState
    {
        public int StateID = -1;
        public List<CMUSphinx_FSGTransition> TransitionsOut;
        public List<CMUSphinx_FSGTransition> TransitionsIn;
        public CMUSphinx_FSGState EquivalentTo;
        public int EquivalentCount; //only valid when EquivalentTo is null. Used in prob calcs

        public CMUSphinx_FSGState(int stateID)
        {
            StateID = stateID;
            TransitionsOut = new List<CMUSphinx_FSGTransition>();
            TransitionsIn = new List<CMUSphinx_FSGTransition>();
            EquivalentTo = null;
            EquivalentCount = 1;
        }
    }
    public class CMUSphinx_FSGTransition
    {
        public string OnString;
        public CMUSphinx_FSGState From;
        public CMUSphinx_FSGState To;
        public float Probability;
        public int Count;
        public CMUSphinx_FSGTransition(CMUSphinx_FSGState from, CMUSphinx_FSGState to, string on)
        {
            From = from;
            To = to;
            OnString = on;
            from.TransitionsOut.Add(this);
            to.TransitionsIn.Add(this);
            Probability = 1.0f;
            Count = 1;
        }
    }

    public class CMUSphinx_GrammarDict
    {

        //TODO: Make this configurable
        static string DictFilePath = @"D:\devel\github\holly\Server\cmudict.0.7a_SPHINX_40.modded";

        public CMUSphinx_GrammarDict()
        {
            mJSGFRules = new Dictionary<string, string>();
            _validGrammar = false;
            words = new List<string>();
            dictDict = new Dictionary<string, string>();
        }

        public string GrammarName;
        public string Grammar;
        public string Dict;
        public string GrammarType; //should be an enum, but I can't be arsed
        bool _validGrammar;
        public bool GrammarIsValid
        {
            get { return _validGrammar; }
        }
        public bool DictIsValid
        {
            get { return _validDict; }
        }
        public List<string> MissingWords
        {
            get { return words; }
        }

        string mJSGFRootRule = "";
        Dictionary<string, string> mJSGFRules;
        bool _isValidJSGFRule(string token)
        {
            string s = token.Trim();
            if (!token.StartsWith("<") && !token.EndsWith(">")) return true;
            if (mJSGFRules.ContainsKey(s)) return true;
            return false;
        }
        public bool JSGFSetRootRule(string rule)
        {
            rule = rule.ToUpper();
            if (!mJSGFRules.ContainsKey(rule))
            {
                Form1.updateLog("(SetRootRule) No such rule:" + rule, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            mJSGFRootRule = rule;
            return true;
        }
        public bool JSGFRuleStart(string rule, StringBuilder bld)
        {
            rule = rule.Trim();
            rule = rule.ToUpper();
            if (!rule.StartsWith("<") || !rule.EndsWith(">"))
            {
                Form1.updateLog("(RuleStart) Rule name not correct format (<str>): "+rule, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (rule == "<NULL>" || rule == "<VOID>")
            {
                Form1.updateLog("(RuleStart) Rule name cannot be null or void ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (mJSGFRules.ContainsKey(rule))
            {
                Form1.updateLog("(RuleStart) Rule already exists: "+rule, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            bld.Clear();
            mJSGFRules.Add(rule, "");
            return true;
        }
        bool _JSGFRuleAddChoices(StringBuilder bld, List<string> opts)
        {
            for (int i = 0; i < opts.Count; i++)
            {
                opts[i] = opts[i].ToUpper();
                if (!_isValidJSGFRule(opts[i]))
                {
                    Form1.updateLog("(_RuleAddChoices) Invalid choice: " + opts[i], ELogLevel.Error, ELogType.SpeechRecog);
                    return false;
                }
                _AddToWordList(opts[i]);
                bld.Append(opts[i]);
                if (i != opts.Count - 1) bld.Append(" | ");
            }
            return true;
        }
        public bool JSGFRuleAddChoicesStart(StringBuilder bld, List<string> opts)
        {
            if (opts == null || opts.Count == 0)
            {
                Form1.updateLog("(RuleAddChoicesStart) Cannot have null or 0 choices ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (bld == null)
            {
                Form1.updateLog("(RuleAddChoicesStart) Invalid stringbuilder ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            bld.Append(" ( ");
            _JSGFRuleAddChoices(bld, opts);
            
            return true;
        }
        public bool JSGFRuleAddChoicesMore(StringBuilder bld, List<string> opts)
        {
            if (opts == null || opts.Count == 0)
            {
                Form1.updateLog("(RuleAddChoicesMore) Cannot have null or 0 choices ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (bld == null)
            {
                Form1.updateLog("(RuleAddChoicesMore) Invalid stringbuilder ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            bld.Append(" | ");
            _JSGFRuleAddChoices(bld, opts);
            return true;
        }
        public bool JSGFRuleAddChoicesEnd(StringBuilder bld)
        {
            if (bld == null)
            {
                Form1.updateLog("(RuleAddChoicesEnd) Invalid stringbuilder ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            bld.Append(" ) ");
            return true;
        }
        public bool JSGFRuleAddToken(StringBuilder bld, string token)
        {
            if (bld == null)
            {
                Form1.updateLog("(RuleAddToken) Invalid StringBuilder", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (token == null || token == "")
            {
                Form1.updateLog("(RuleAddToken) Token invalid, use <NULL> or <VOID> ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            token = token.ToUpper();
            _AddToWordList(token);
            bld.Append(" ( " + token + " ) ");
            return true;
        }
        public bool JSGFRuleUnaryOp(StringBuilder bld, bool optional, bool multiple)
        {
            if (bld == null)
            {
                Form1.updateLog("(RuleUnaryOp) Invalid StringBuilder", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if ((!optional && !multiple) || (optional && multiple))
            {
                Form1.updateLog("(RuleUnaryOp) Exactly one of optional or multiple must be set ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (optional) bld.Append("* ");
            else bld.Append("+ ");
            return true;
        }
        public bool JSGFRuleEnd(string rule, StringBuilder bld)
        {
            rule = rule.Trim();
            rule = rule.ToUpper();
            if (!mJSGFRules.ContainsKey(rule))
            {
                Form1.updateLog("(RuleEnd) Missing rulename - did you call RuleStart? Rule: "+rule, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (mJSGFRules[rule] != "")
            {
                Form1.updateLog("(RuleEnd) Rule already defined: "+rule+"="+mJSGFRules[rule], ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (bld == null)
            {
                Form1.updateLog("(RuleEnd) Invalid Stringbuilder ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            string s = bld.ToString();
            if (s == "") s = "<NULL>";
            mJSGFRules[rule] = s;
            return true;
        }
        public bool JSGFRuleCancel(string rule, StringBuilder bld)
        {
            rule = rule.Trim();
            rule = rule.ToUpper();
            if (!mJSGFRules.ContainsKey(rule))
            {
                Form1.updateLog("(RuleCancel) Missing rulename - did you call RuleStart? Rule: " + rule, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (mJSGFRules[rule] != "")
            {
                Form1.updateLog("(RuleCancel) Rule already defined: " + rule + "=" + mJSGFRules[rule], ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (bld == null)
            {
                Form1.updateLog("(RuleCancel) Invalid Stringbuilder ", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            bld.Clear();
            mJSGFRules.Remove(rule);
            return true;
        }
        public bool BuildJSGFGrammarAndDict()
        {
            if (GrammarName == "")
            {
                Form1.updateLog("(BuildGrammarAndDict) Missing GrammarName", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            if (mJSGFRootRule == "")
            {
                Form1.updateLog("(BuildGrammarAndDict) No root rule defined", ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }

            //Create Grammar
            StringBuilder bld = new StringBuilder();
            bld.AppendLine("#JSGF V1.0;");
            bld.AppendLine();
            bld.AppendLine("grammar " + GrammarName + ";");
            bld.AppendLine();
            foreach (string rulename in mJSGFRules.Keys)
            {
                if (rulename == mJSGFRootRule) bld.Append("public ");
                bld.AppendLine(rulename + " = " + mJSGFRules[rulename] + ";");
            }
            Grammar = bld.ToString();
            GrammarType = "JSGF";
            _validGrammar = true;

            //Load Dictionary file
            return _BuildDict();
        }

        CMUSphinx_FSGState rootFSGNode;
        CMUSphinx_FSGState endFSGNode;
        HashSet<CMUSphinx_FSGState> allFSGNodes;
        public CMUSphinx_FSGState FSGCreate(string name)
        {
            GrammarName = name;
            allFSGNodes = new HashSet<CMUSphinx_FSGState>();
            rootFSGNode = new CMUSphinx_FSGState(-1);
            allFSGNodes.Add(rootFSGNode);
            endFSGNode = new CMUSphinx_FSGState(-1);
            allFSGNodes.Add(endFSGNode);
            return rootFSGNode;
        }
        public CMUSphinx_FSGState FSGCreateOrphanState()
        {
            CMUSphinx_FSGState ret = new CMUSphinx_FSGState(-1);
            allFSGNodes.Add(ret);
            return ret;
        }
        public bool FSGLinkStates(CMUSphinx_FSGState from, CMUSphinx_FSGState to, string onString)
        {
            //need to split and uppercase the onString
            CMUSphinx_FSGState tempFrom = from;
            onString = onString.ToUpper();
            string[] toks = onString.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            for (i = 0; i < toks.Length - 1; i++)
            {
                _AddToWordList(toks[i]);
                tempFrom = FSGTransitionToNewState(tempFrom, toks[i]);
            }

            string endTok;
            if (toks.Length <= i) endTok = "";
            else endTok = toks[i];
            
                _AddToWordList(endTok);
                CMUSphinx_FSGTransition tr = new CMUSphinx_FSGTransition(tempFrom, to, endTok);
                if (tr != null) return true;
            
            return false;
        }
        public CMUSphinx_FSGState FSGTransitionToNewState(CMUSphinx_FSGState from, string onString)
        {
            CMUSphinx_FSGState ret = new CMUSphinx_FSGState(-1);
            allFSGNodes.Add(ret);
            FSGLinkStates(from, ret, onString);
            return ret;
        }
        public CMUSphinx_FSGState FSGGroupStates(List<CMUSphinx_FSGState> States)
        {
            if (States == null || States.Count == 0) return null;
            if (States.Count > 1)
            {
                for (int i = 1; i < States.Count; i++)
                {
                    States[i].EquivalentTo = States[0];
                }
            }
            return States[0];
        }
        public CMUSphinx_FSGState FSGGroupStates(CMUSphinx_FSGState State1, CMUSphinx_FSGState State2)
        {
            if (State1 == null || State2 == null) return null;
            State2.EquivalentTo = State1;
            return State1;
        }
        public CMUSphinx_FSGState FSGGetEndState()
        {
            return endFSGNode;
        }

        bool RemoveEquivalents()
        {
            List<CMUSphinx_FSGState> toDel = new List<CMUSphinx_FSGState>();
            foreach (CMUSphinx_FSGState s in allFSGNodes)
            {
                if (s.EquivalentTo == null) continue;
                    if (s.EquivalentTo == s)
                    {
                        Form1.updateLog("Bad state, marked as equivalent to itself", ELogLevel.Error, ELogType.SpeechRecog);
                        return false;
                    }
                    //equivalent, but it may already have been dealt with. If equivcount==-1, it has. Loop until find the
                    // ultimate target, but be careful of loops
                    CMUSphinx_FSGState equivTarget = s.EquivalentTo;
                    while (equivTarget.EquivalentCount == -1)
                    {
                        if (equivTarget.EquivalentTo == s)
                        {
                            Form1.updateLog("ERR: Equivalence loop for states", ELogLevel.Error, ELogType.SpeechRecog);
                        }
                        equivTarget = equivTarget.EquivalentTo;
                    }

                    //equivalent. Therefore, transfer transitions to the equivalent, update the equivalent count with self
                    //  and add self to delete list
                    s.EquivalentTo.EquivalentCount += s.EquivalentCount;
                    foreach (CMUSphinx_FSGTransition t in s.TransitionsIn) t.To = s.EquivalentTo;
                    foreach (CMUSphinx_FSGTransition t in s.TransitionsOut) t.From = s.EquivalentTo;
                    s.EquivalentTo.TransitionsIn.AddRange(s.TransitionsIn);
                    s.EquivalentTo.TransitionsOut.AddRange(s.TransitionsOut);
                    s.EquivalentCount = -1;
                    toDel.Add(s);
            }
            foreach (CMUSphinx_FSGState s in toDel) allFSGNodes.Remove(s);
            
            //remove duplicate transitions
            //TODO: This needs fixing
            foreach (CMUSphinx_FSGState s in allFSGNodes)
            {
                bool changed = false;
                Dictionary<CMUSphinx_FSGTransition, CMUSphinx_FSGTransition> ts =
                    new Dictionary<CMUSphinx_FSGTransition, CMUSphinx_FSGTransition>();
                foreach (CMUSphinx_FSGTransition t in s.TransitionsOut)
                {
                    if (ts.ContainsKey(t))
                    {
                        changed = true;
                        //already exists. We're going to replace the current list anyway, so just update
                        //  the count
                        ts[t].Count += t.Count;
                    }
                    else ts.Add(t, t);
                }
                if (changed) s.TransitionsOut = ts.Keys.ToList();
            }

            return false;
        }
        bool OptimiseForwards(CMUSphinx_FSGState n)
        {
            //TODO: equivalent if the previous state was the same and the transition string was the same
            return false;
        }
        bool OptimiseBackwards(CMUSphinx_FSGState n)
        {
            //TODO: equivalent if graph to the endNode is the same
            return false;
        }
        int nextStateID = 0;
        HashSet<CMUSphinx_FSGTransition> outTransitions;
        HashSet<CMUSphinx_FSGState> doneStates;
        bool RecurseForwardsAndFinalise(CMUSphinx_FSGState n)
        {
            //check for loops
            if(!doneStates.Add(n)) return true;
            
            n.StateID = nextStateID++;

            //Calculate Probabilities
            int nOut = 0;
            foreach (CMUSphinx_FSGTransition t in n.TransitionsOut)
            {
                //nOut += t.To.EquivalentCount;
                nOut += t.Count;
            }
            foreach (CMUSphinx_FSGTransition t in n.TransitionsOut)
            {
                //t.Probability = ((float)1.0 * t.To.EquivalentCount) / nOut;
                t.Probability = ((float)1.0 * t.Count) / nOut;
                outTransitions.Add(t);
                RecurseForwardsAndFinalise(t.To);
            }

            return true;
        }
        public bool BuildFSGGrammarAndDict()
        {
            //0) Insert blank entry leading to endNode - get crashes without this
            CMUSphinx_FSGState tempState = new CMUSphinx_FSGState(-1);
            allFSGNodes.Add(tempState);
            FSGLinkStates(endFSGNode, tempState, "");
            endFSGNode = tempState;

            //1) Optimise graph
            //optimise graph, check for loose ends, be careful of loops
            // states are equivalent if the previous state was the same and the transition string was the same
            //             or if graph to the endNode is the same
            //1a) Equiv lookforward
            OptimiseForwards(rootFSGNode);
            //1b) Equiv lookback
            OptimiseBackwards(endFSGNode.TransitionsIn[0].From);
            //1c) Remove equivalents
            RemoveEquivalents();

            //2) Number and count states, set probs, and compile list of transitions (out only)
            outTransitions = new HashSet<CMUSphinx_FSGTransition>();
            nextStateID = 0;
            doneStates = new HashSet<CMUSphinx_FSGState>(); //loop protection
            RecurseForwardsAndFinalise(rootFSGNode);


            //3) create output file header
            StringBuilder b = new StringBuilder();
            b.AppendLine("FSG_BEGIN <"+GrammarName+">");
            b.AppendLine("NUM_STATES " + nextStateID.ToString());
            b.AppendLine("START_STATE " + rootFSGNode.StateID.ToString());
            b.AppendLine("FINAL_STATE " + endFSGNode.StateID.ToString());

            //4) Dump transitions
            foreach (CMUSphinx_FSGTransition t in outTransitions)
            {
                b.AppendFormat("TRANSITION {0} {1} {2} {3}{4}",
                    t.From.StateID, t.To.StateID, t.Probability, t.OnString, Environment.NewLine);
            }
         
            //5)Footer and close out
            b.AppendLine("FSG_END");
            Grammar = b.ToString();
            GrammarType = "FSG";
            _validGrammar = true;
            return _BuildDict();
        }


        List<string> words;
        Dictionary<string, string> dictDict;
        bool _validDict;
        void _AddToWordList(string token)
        {
            string s = token.Trim();
            if (s.StartsWith("<") && s.EndsWith(">") && s.IndexOf(">") == s.Length - 1) return;
            if (s.StartsWith("{") && s.EndsWith("}") && s.IndexOf("}") == s.Length - 1) return;

            string[] toks = s.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (toks.Length > 1)
            {
                foreach (string ss in toks) _AddToWordList(ss);
            }
            else
            {
                if (s == "(" || s == ")" || s == "|" || s == "{" || s == "}" || s == "") return;
                if(!words.Contains(s)) words.Add(s);
            }
        }
        private bool _BuildDict()
        {
            words.Sort();
            StreamReader rdr = new StreamReader(DictFilePath);
            if (rdr == null)
            {
                Form1.updateLog("(BuildGrammarAndDict) Cannot find base dictionary: " + DictFilePath, ELogLevel.Error, ELogType.SpeechRecog);
                return false;
            }
            string line;
            bool hit;
            while ((line = rdr.ReadLine()) != null)
            {
                hit = false;
                string[] toks = line.Split(new Char[] { ' ', '\t' }, 2);
                if (toks.Length != 2)
                {
                    Form1.updateLog("ERR: Dictionary didn't have 2 parts: " + line, ELogLevel.Warning, ELogType.SpeechRecog);
                }
                else
                {
                    if (words.Contains(toks[0])) hit = true;
                }
                if (hit)
                {
                    dictDict.Add(toks[0], toks[1]);
                    while (words.Contains(toks[0])) words.Remove(toks[0]);
                }
            }
            rdr.Close();

            if (words.Count > 0)
            {
                //try the custom dictionary
                AddCustomDictionary();

                foreach (string wd in CustomDictionary.Keys)
                {
                    if (words.Contains(wd))
                    {
                        dictDict.Add(wd, CustomDictionary[wd]);
                        while (words.Contains(wd)) words.Remove(wd);
                    }
                }
                if (words.Count > 0)
                {

                    string s = "";
                    foreach (string wd in words) s += wd + ", ";
                    Form1.updateLog("ERR: Words remain without a dictionary entry: " + s, ELogLevel.Error, ELogType.SpeechRecog);
                    return false;
                }
            }

            string d = "";
            foreach (string wd in dictDict.Keys)
            {
                d += wd + " " + dictDict[wd] + "\n";
            }
            Dict = d;
            _validDict = true;

            return true;
        }
        Dictionary<string, string> CustomDictionary;
        bool AddCustomDictionary()
        {
            //TODO: Auto lookup at http://www.speech.cs.cmu.edu/cgi-bin/cmudict/ and http://www.speech.cs.cmu.edu/tools/lextool.html
            CustomDictionary = new Dictionary<string, string>();
            CustomDictionary.Add("ALLO", "AH L OW");
            CustomDictionary.Add("AMELIE", "AH M EH L IY");
            CustomDictionary.Add("AVENGERS", "AE V AH N JH ER Z");
            CustomDictionary.Add("AVP", "EY V IY P IY");
            CustomDictionary.Add("AVPR", "EY V IY P IY AA R");
            CustomDictionary.Add("AZKABAN", "AH Z K AH B AE N");
            CustomDictionary.Add("BASTERDS", "B AE S T ER D Z");
            CustomDictionary.Add("BATTLESTAR", "B AE T AH L S T AA R");
            CustomDictionary.Add("BOOSH", "B UW SH");
            CustomDictionary.Add("BRASSED", "B R AE S T");
            CustomDictionary.Add("BRITPOP", "B R IH T P AA P");
            CustomDictionary.Add("BUELLER'S", "B UW EH L ER Z");
            CustomDictionary.Add("BUSHCRAFT", "B UH SH K R AE F T");
            CustomDictionary.Add("CADFAEL", "K AE D F AY L");
            CustomDictionary.Add("CALIFORNICATION", "K AE L AH F AO R N AH K EY SH AH N");
            CustomDictionary.Add("CAPRICA", "K AE P R IH K AH");
            CustomDictionary.Add("CLOVERFIELD", "K L OW V ER F IY L D");
            CustomDictionary.Add("COLOURS", "K AH L ER Z");
            CustomDictionary.Add("CORELLI'S", "K AO R EH L IY Z");
            CustomDictionary.Add("CSI", "S IY S AY");
            CustomDictionary.Add("DARKO", "D AA R K OW");
            CustomDictionary.Add("DEMETRI", "D UH M IY T R IY");
            CustomDictionary.Add("DJANGO", "JH AH NG OW");
            CustomDictionary.Add("DODGEBALL", "D AA JH B AO L");
            CustomDictionary.Add("EXPENDABLES", "IH K S P EH N D ");
            CustomDictionary.Add("FANBOYS", "F AE N B OY Z");
            CustomDictionary.Add("FARSCAPE", "F AA R S K EY P");
            CustomDictionary.Add("FLASHFORWARD", "F L AE SH F AO R W ER D");
            CustomDictionary.Add("FOURTY", "F OW Y UH R T IY");
            CustomDictionary.Add("FUTURAMA", "F Y UW T Y UH R AH M AH");
            CustomDictionary.Add("GALACTICA", "G AH L AE K T AH K AH");
            CustomDictionary.Add("GALLIPOLI", "G AE L IH P AH L IY");
            CustomDictionary.Add("GATTACA", "G AE T AH K AH");
            CustomDictionary.Add("GOOD", "G UH D");
            CustomDictionary.Add("GOSFORD", "G OW Z F OW R D");
            CustomDictionary.Add("GRINDHOUSE", "G R AY N D HH AW S");
            CustomDictionary.Add("HELLBOY", "HH EH L B OY");
            CustomDictionary.Add("HELSING", "HH EH L S AH NG");
            CustomDictionary.Add("HONOUR", "AA N ER");
            CustomDictionary.Add("IDES", "AY D Z");
            CustomDictionary.Add("IGBY", "IH G IY");
            CustomDictionary.Add("INGLOURIOUS", "IH N G L AH Y UH R IY AH S");
            CustomDictionary.Add("JEDI", "JH EH D IY");
            CustomDictionary.Add("JEEVES", "JH IY V Z");
            CustomDictionary.Add("KALIFORNIA", "K AE L AH F AO R N IY AH");
            CustomDictionary.Add("LEBOWSKI", "L AH B OW S IY");
            CustomDictionary.Add("LOL", "EH L OW EH L");
            CustomDictionary.Add("MEARS'", "M IH R Z");
            CustomDictionary.Add("MOONRAKER", "M UW N R EY K ER");
            CustomDictionary.Add("MVI_0502", "EH M V IY AY Z IH R OW . F AY V . Z IH R OW . T UW ");
            CustomDictionary.Add("MVI", "EH M V IY AY");
            CustomDictionary.Add("MYTHBUSTERS", "M IH TH B AH S T ER Z");
            CustomDictionary.Add("NCIS", "EH N S IY AY EH S ");
            CustomDictionary.Add("NY", "EH N W AY");
            CustomDictionary.Add("PAPILLON", "P AE P IH L AO N");
            CustomDictionary.Add("PHILOSOPHER'S", "F AH L AA S AH F ER Z");
            CustomDictionary.Add("PHONESHOP", "F OW N SH AA P");
            CustomDictionary.Add("PRISCILLA", "P R IH S IH L AH");
            CustomDictionary.Add("PWNAGE", "OW N EY JH");
            CustomDictionary.Add("RJ", "AA R JH");
            CustomDictionary.Add("RUMPOLE", "R AH M P OW L");
            CustomDictionary.Add("SCRUBS", "S K R AH B Z");
            CustomDictionary.Add("SG", "EH S G");
            CustomDictionary.Add("SHAGGED", "SH AE G D");
            CustomDictionary.Add("SHREK", "SH R EH K");
            CustomDictionary.Add("SINCHRONICITY", "S IH N K R AA N IH K AH T IY");
            CustomDictionary.Add("SITH", "S IH TH");
            CustomDictionary.Add("SKA", "S K AA");
            CustomDictionary.Add("SLUMDOG", "S L AH M D AO G");
            CustomDictionary.Add("SPONGEBOB", "S P AH N JH B AA B");
            CustomDictionary.Add("SQUAREPANTS", "S K W EH R P AE N T S");
            CustomDictionary.Add("STALAG", "S T AA L AA G");
            CustomDictionary.Add("STARFIGHTER", "S T AA R F AY T ER");
            CustomDictionary.Add("STARSKY", "S T AA R S K AY");
            CustomDictionary.Add("STEWIE", "S T UW IY");
            CustomDictionary.Add("STROSZEK", "S T R AA Z AH K");
            CustomDictionary.Add("SUPERBAD", "S UW P ER B AE D");
            CustomDictionary.Add("TENENBAUMS", "T AH N EH N B AH M Z");
            CustomDictionary.Add("THRONES", "TH R OW N Z");
            CustomDictionary.Add("THUNDERDOME", "TH AH N D ER D OW M");
            CustomDictionary.Add("TINSELWORM", "T IH N S AH L W ER M");
            CustomDictionary.Add("TORCHWOOD", "T AO R CH W UH D");
            CustomDictionary.Add("TRAINSPOTTING", "T R EY N S P AA T IH NG");
            CustomDictionary.Add("TRAPDOOR", "T R AE P D OW R");
            CustomDictionary.Add("TRINIAN'S", "T R IH N IY AH N Z");
            CustomDictionary.Add("TUDORS", "T Y UW D AH R Z");
            CustomDictionary.Add("VII", "V IY");
            CustomDictionary.Add("VOL", "V AA L");
            CustomDictionary.Add("WALKABOUT", "W AO K AH B AW T");
            CustomDictionary.Add("WALLÃ‚Â·E", "W AO L IY");
            CustomDictionary.Add("WARGAMES", "W AO R G EY M Z");
            CustomDictionary.Add("XXX", "T R IH P AH L EH K S");
            CustomDictionary.Add("ZOHAN", "Z AA AH N");

            return true;
        }

    }



    public class AudioRecog_CMUSphinx : AudioRecog
    {
        CMUSphinx_Interop mSphinx;
        //string mRootPath = @"D:\devel\github\holly\Server\";

        public AudioRecog_CMUSphinx()
        {
            mSphinx = new CMUSphinx_Interop();
            CMUSphinx_Interop.RecognitionSuccessful += new CMUSphinx_Interop.RecognitionSuccessfulDelegate(CMUSphinx_Interop_RecognitionSuccessful);
            CMUSphinx_Interop.RecognitionComplete += new CMUSphinx_Interop.RecognitionCompleteDelegate(CMUSphinx_Interop_RecognitionComplete);
        }

        void CMUSphinx_Interop_RecognitionComplete(object sender, RecognitionCompleteEventArgs e)
        {
            OnRecognitionComplete(e.ID);
        }

        void CMUSphinx_Interop_RecognitionSuccessful(object sender, RecognitionSuccessfulEventArgs e)
        {
            OnRecognitionSuccessful(e.ID, e.res);
        }


        public override void RunRecognition(string file, string ID)
        {
            mSphinx.RunRecognition(null, ID, file);
        }
        public override void RunRecognition(Stream s, string ID)
        {
            mSphinx.RunRecognition(s, ID, null);
        }


        bool AddGrammar(string name, CMUSphinx_GrammarDict cgd)
        {
            if (cgd == null || !cgd.DictIsValid || !cgd.GrammarIsValid) return false;
            return mSphinx.AddGrammar(name, cgd.GrammarType, cgd.Grammar, cgd.Dict);
        }
        public bool RemoveGrammar(string name)
        {
            return mSphinx.DelGrammar(name);
        }
        public override bool AddControllable(IControllable c)
        {
            string nm = c.GetName();
            CMUSphinx_GrammarDict cmu = c.CreateGrammarDoc_FSG();
            if (nm != null && nm != "" && cmu != null && cmu.GrammarIsValid && cmu.DictIsValid)
                    
            {
                AddGrammar(nm, cmu);
                return true;
            }
            return false;
        }
        public override string GetName()
        {
            return AudioRecog_CMUSphinx.Name;
        }
        public static string Name
        {
            get
            {
                return "Sphinx";
            }
        }
        

    }
}
