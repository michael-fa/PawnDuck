using ScintillaNET;
using System;
using System.Drawing;
using System.Windows.Forms;
using AutocompleteMenuNS;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using ScintillaNET_FindReplaceDialog;

namespace SAMPDuck
{

    public partial class Form1 : Form
    {
        string VERSION = "0.01a";

        int lastBackup;
        int compileStart;

        //All the last used items
        List<ToolStripItem> lastFilesItems = new List<ToolStripItem>();


        //Just our config file
        GlobalConfig Config = null;

        //Temporary variables:
        List<lexer.functionData> tempIncludeFunctionList = null;

        List<lexer.functionData> includeFunctionList = null;
        List<lexer.functionData> userFunctionList = null;

        TreeNode tempIncludeFunctionContent = null;

        //Compiler errors and warnings
        List<ListViewItem> errorList = null;
        List<string> errorLines = null;

        //For automatic line margin setting
        private int maxLineNumberCharLength;

        //Some text editor things
        bool needSaveFile = false;
        string currentFilePath;

        //Used for invoking
        string scintillaText = null;


        //Just the last pos of our mouse
        int lastCaretPos;





        string[] startParams = null; //Params that maybe got passed through







        //===============================================================================================================================================================================================

        public string[] sampCallbacks = {"OnActorStreamIn",             "OnPlayerClickMap",             "OnPlayerExitVehicle",
                                    "OnActorStreamOut",          "OnPlayerClickPlayer",          "OnPlayerExitedMenu",
                                    "OnDialogResponse",          "OnPlayerClickPlayerTextDraw",  "OnPlayerGiveDamage",
                                    "OnEnterExitModShop",        "OnPlayerClickTextDraw",        "OnPlayerGiveDamageActor",
                                    "OnFilterScriptExit",        "OnPlayerCommandText",          "OnPlayerInteriorChange",
                                    "OnFilterScriptInit",        "OnPlayerConnect",              "OnPlayerKeyStateChange",
                                    "OnGameModeExit",            "OnPlayerDeath",                "OnPlayerLeaveCheckpoint",
                                    "OnGameModeInit",            "OnPlayerDisconnect",           "OnPlayerLeaveRaceCheckpoint",
                                    "OnIncomingConnection",      "OnPlayerEditAttachedObject",   "OnPlayerObjectMoved",
                                    "OnObjectMoved",             "OnPlayerEditObject",           "OnPlayerPickUpPickup",
                                                                 "OnPlayerEnterCheckpoint",      "OnPlayerPrivmsg",
                                                                 "OnPlayerEnterRaceCheckpoint",  "OnPlayerRequestClass",
                                                                 "OnPlayerEnterVehicle",         "OnPlayerRequestSpawn",
                                                                                                 "OnPlayerSelectObject",

                                    "OnUnoccupiedVehicleUpdate",           "OnPlayerSpawn",           "OnTrailerUpdate",
                                    "OnVehicleDamageStatusUpdate",         "OnPlayerStateChange",
                                    "OnVehicleDeath",                      "OnPlayerStreamIn",
                                    "OnVehicleMod",                        "OnPlayerStreamOut",
                                    "OnVehiclePaintjob",                   "OnPlayerTakeDamage",
                                    "OnVehicleRespray",                    "OnPlayerText",
                                    "OnVehicleSirenStateChange",           "OnPlayerUpdate",
                                    "OnVehicleSpawn",                      "OnPlayerWeaponShot",
                                    "OnVehicleStreamIn",                   "OnRconCommand",
                                    "OnVehicleStreamOut",                  "OnRconLoginAttempt",




         };

        //===============================================================================================================================================================================================






        FindReplace MyFindReplace;

        public Form1(string[] args)
        {
            InitializeComponent();

            startParams = args;
        }



        private void Form1_Load(object sender, EventArgs e)
        {

            this.Text = "PawnDuck " + VERSION;

            Config = new GlobalConfig();

            if (!File.Exists(Application.StartupPath + "\\config.ini"))
                File.Create(Application.StartupPath + "\\config.ini");
            IniFile ConfigFile = new IniFile(Application.StartupPath + "\\config.ini");



            //=====================================================================================================


            //If any needed key doesn't exist..
            if (!ConfigFile.KeyExists("compiler-mode"))
                ConfigFile.Write("compiler-mode", "0");

            if (!ConfigFile.KeyExists("runCommand"))
                ConfigFile.Write("runCommand", "cmd");

            if (!ConfigFile.KeyExists("notes"))
                ConfigFile.Write("notes", " ");

            if (!ConfigFile.KeyExists("autobackup"))
            {
                ConfigFile.Write("autobackup", "0");
                autoBackup_cb.Checked = false;
            }

            if(!ConfigFile.KeyExists("includedir"))
            {
                ConfigFile.Write("includedir", Application.StartupPath + "\\include");
                includedir_Tb.Text = Application.StartupPath + "\\include";
            }


            //Check if any key for last opened files doesnt exists
            for (int i = 0; i < 6; i++)
            {
                if (!ConfigFile.KeyExists("lastFile" + i))
                    ConfigFile.Write("lastFile" + i, "Ungespeichert");
            }


            //=====================================================================================================

            //Compiler mode
            int o;
            Int32.TryParse(ConfigFile.Read("compiler-mode"), out o);
            Config.compiler = o;
            if (o == 1)
            {
                label_compiler.Text = "Kompiler: Zeex";
                checkBox1.Checked = true;
            }
            else
            {
                label_compiler.Text = "Compiler: Standard";
                checkBox1.Checked = false;
            }
            //Run command
            toolStripTextBox1.Text = ConfigFile.Read("runCommand");
            Config.runCommand = ConfigFile.Read("runCommand");

            //Noteblock
            noteBlock.Text = ConfigFile.Read("notes");
            Config.noteblock = ConfigFile.Read("notes");

            //Include dir
            includedir_Tb.Text = ConfigFile.Read("includedir");
            Config.includepath = ConfigFile.Read("includedir");

            //AutoBackup
            Int32.TryParse(ConfigFile.Read("autobackup"), out o);
            autoBackup_cb.Checked = Convert.ToBoolean(o);
            Config.autobackup = o;
            if (o == 1)
                try
                {
                    t_Backup.RunWorkerAsync();
                }
                catch { }
            else autoBackup_cb.Checked = false;



            //To later load the last recent file
            string OPEN_FILE_STRING = null;

            //add all recent files
            int passme_i = 0;
            int actual_id = 0;
            for (int i = 0; i < 6; i++)
            {
                Config.lastOpenedFiles[i] = ConfigFile.Read("lastFile" + i);
                if (Config.lastOpenedFiles[i] != "Ungespeichert")
                {
                    if (actual_id == 0) passme_i = i;
                    if (!File.Exists(Config.lastOpenedFiles[i]))
                    {
                        Config.lastOpenedFiles[i] = "Ungespeichert";
                        continue;
                    }
                    ToolStripItem item = dateiToolStripMenuItem.DropDownItems.Add(Config.lastOpenedFiles[i]);
                    item.Click += new EventHandler(lastfile_Click);
                    lastFilesItems.Add(item);
                    actual_id++;
                }
            }


            //Open script when needed:
            if (startParams.Length > 0)
            {
                //Open the file:
                OPEN_FILE_STRING = startParams[0];
            }
            else
            {
                //Last opened
                if (OPEN_FILE_STRING == null)
                {
                    //Because the line-number margin doesn't get set on first load of a recent file here, pass file name to string
                    //We'll load it at the end of Form1_Load
                    OPEN_FILE_STRING = Config.lastOpenedFiles[passme_i];
                    this.Text = Config.lastOpenedFiles[passme_i] + "    |  PawnDuck " + VERSION;
                }

            }


            // Create instance of FindReplace with reference to a ScintillaNET control.
            MyFindReplace = new FindReplace(scintilla1);
            // Tie in FindReplace event
            MyFindReplace.KeyPressed += MyFindReplace_KeyPressed;

            // Tie in Scintilla event
            scintilla1.KeyDown += scintilla1_KeyDown;












            //=====================================================================================================


            //Set some stuff
            label_Args.Text = "";
            label_info.Text = "";
            toolTip1.SetToolTip(autoBackup_cb, "Diese Funktion erstellt im Dokumente\\PawnDuck -Ordner eine Sicherheitskopie der jeweiligen geöffneten Datei.");
            toolTip2.SetToolTip(checkBox1, "Wusstest du schon?\nEin sehr bekannter User in der SA-MP Szene bekannt als Zeex hat den im SA-MP Server beigelegten pawn-compiler verbessert, also eine verbesserte Version davon geschrieben.\n\nDiese Funktion lässt dein Skript über diesen Compiler kompilieren.");






            //Autocomplete:
            userFunctionList = new List<lexer.functionData>();
            includeFunctionList = new List<lexer.functionData>();

            errorList = new List<ListViewItem>();
            errorLines = new List<string>();

            autocompleteMenu1.TargetControlWrapper = new ScintillaWrapper(scintilla1);

            //For autocomplete and tree view
            UpdateIncludeFiles();




















            //Setting up LEXER (changed Cpp lexer)
            //================================================================================================

            //Yep use cpp and edit it
            scintilla1.Lexer = Lexer.Cpp;

            scintilla1.StyleResetDefault();
            scintilla1.Styles[Style.Default].Font = "Consolas";
            scintilla1.Styles[Style.Default].Size = 10;
            scintilla1.StyleClearAll();

            scintilla1.IndentationGuides = IndentView.LookBoth;
            scintilla1.Styles[Style.BraceLight].BackColor = Color.LightGray;
            scintilla1.Styles[Style.BraceLight].ForeColor = Color.BlueViolet;
            scintilla1.Styles[Style.BraceBad].ForeColor = Color.Red;

            //for line stuff - we later enlargen it further more if more line count is bigger then 3 digits
            scintilla1.Margins[0].Width = 16;

            //Comment BLOCK
            scintilla1.Styles[Style.Cpp.CommentLine].ForeColor = Color.Green;

            //Comment SINGLE
            scintilla1.Styles[Style.Cpp.Comment].ForeColor = Color.Green;

            //String
            scintilla1.Styles[Style.Cpp.String].ForeColor = Color.Red;

            //Numbers
            scintilla1.Styles[Style.Cpp.Number].ForeColor = Color.Brown;

            //Chars
            scintilla1.Styles[Style.Cpp.Character].ForeColor = Color.Red;

            //Default
            scintilla1.Styles[Style.Cpp.Default].ForeColor = Color.Black;

            //idents
            scintilla1.Styles[Style.Cpp.Identifier].ForeColor = Color.Black;

            //Operators
            scintilla1.Styles[Style.Cpp.Operator].ForeColor = Color.RosyBrown;

            //Pre processing
            scintilla1.Styles[Style.Cpp.Preprocessor].ForeColor = Color.Blue;

            //Words
            scintilla1.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
            scintilla1.Styles[Style.Cpp.Word2].ForeColor = Color.DarkCyan;

            //Pawn keywords 
            scintilla1.SetKeywords(0, "for while do stock public forward if else return new break continue false true stock case enum switch sizeof goto default");
            string tmp = "";
            //also use every sampcallback as keyword
            foreach(string str in sampCallbacks)
            {
                tmp = tmp + " " + str;
            }
            scintilla1.SetKeywords(1, tmp);

            if (File.Exists(OPEN_FILE_STRING))
                OpenFile(OPEN_FILE_STRING);










            //Load internal wikipedia stuff

            //-vehicles
            string content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\vehicles.data");
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_vehicles.Items.Add(lvitem);
            }

            //-weapons
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\weapons.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_weapons.Items.Add(lvitem);
            }

            //-playerkeys
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\pkeys.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_playerkeys.Items.Add(lvitem);
            }

            //-special actions
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\spactions.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_specialactions.Items.Add(lvitem);
            }

            //-limits
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\limits.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_limits.Items.Add(lvitem);
            }

            //-pikcup models
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\pmodels.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_pickupmodels.Items.Add(lvitem);
            }

            //-pickup types
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\ptypes.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_pickuptypes.Items.Add(lvitem);
            }

            //-weather
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\weather.txt");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_weather.Items.Add(lvitem);
            }

            //-bodyparts
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\bodyparts.txt");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_bodyparts.Items.Add(lvitem);
            }

            //-explosiontypes
            content = File.ReadAllText(Application.StartupPath + "\\resources\\lists\\explosiontypes.data");
            lines = content.Split('\n');

            foreach (string line in lines)
            {
                string[] split = line.Split('%');
                var lvitem = new ListViewItem(split);
                lv_explosiontypes.Items.Add(lvitem);
            }
            
        }







        //=======================================================================================================================================
        /*                                                       INCLUDE STUFF
        */
        //=======================================================================================================================================

        void ParseIncludeFiles(IEnumerable<string> includeFiles)
        {

            //RESETTEN
            if (tempIncludeFunctionContent != null)
                tempIncludeFunctionContent = null;

            if (tempIncludeFunctionList != null)
                tempIncludeFunctionList = null;


            tempIncludeFunctionContent = new TreeNode();
            tempIncludeFunctionList = new List<lexer.functionData>();
            foreach (string includeFile in includeFiles)
            {
                //Open the include file:
                StreamReader file = new StreamReader(includeFile);
                string text = file.ReadToEnd();
                file.Close();
                //Create the node:
                string[] splitted = includeFile.Split('\\');
                int where = splitted.Length;
                TreeNode node = tempIncludeFunctionContent.Nodes.Add(splitted[where - 1]);
                //Analyse the code:
                lexer lex = new lexer();
                List<lexer.functionData> functionList = new List<lexer.functionData>();
                lex.includeAnalysis(text, ref functionList);
                //Add function list to previous list:
                tempIncludeFunctionList.AddRange(functionList);
            }
        }

        private void thread_IncludeViewUpdate(object sender, DoWorkEventArgs e)
        {
            ParseIncludeFiles((IEnumerable<string>)e.Argument);
        }

        private void includeThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tv_libs.Nodes.Clear();
            foreach (TreeNode node in tempIncludeFunctionContent.Nodes)
                tv_libs.Nodes.Add(node);
            includeFunctionList = tempIncludeFunctionList;

            foreach (var item in includeFunctionList)
            {
                AutocompleteItem _tmp = new AutocompleteItem(item.identifer);
                autocompleteMenu1.AddItem(_tmp);
            }
        }

        private void UpdateIncludeFiles()
        {
            //Read include files:
            //Collect all include files:
            IEnumerable<string> includeFiles = Directory.EnumerateFiles(Application.StartupPath + "\\include");
            IEnumerable<string> directories = Directory.EnumerateDirectories(Application.StartupPath + "\\include");
            foreach (string i in directories)
            {
                IEnumerable<string> files = Directory.EnumerateFiles(i);
                includeFiles = includeFiles.Concat(files);
            }
            //Add include files to the tree view
            Invoke(new Action(() =>
            {
                t_includeTreeViewUpdater.RunWorkerAsync(includeFiles);
            }));
            
        }





















        //=======================================================================================================================================
        /*                                                       SCINTILLA/ TEXT UPDATE STUFF
        */
        //=======================================================================================================================================

        private void scintilla_TextChanged(object sender, EventArgs e)
        {
            // Did the number of characters in the line number display change?
            // i.e. nnn VS nn, or nnnn VS nn, etc...
            var maxLineNumberCharLength = scintilla1.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == this.maxLineNumberCharLength)
                return;

            // Calculate the width required to display the last line number
            // and include some padding for good measure.
            const int padding = 2;
            scintilla1.Margins[0].Width = scintilla1.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            this.maxLineNumberCharLength = maxLineNumberCharLength;

        }


        //compiler thread
        private void t_codeParsing_DoWork(object sender, DoWorkEventArgs e)
        {
            string compilerDir = Application.StartupPath + "\\compiler\\" + Config.compiler.ToString() + "\\";

            if (!File.Exists(compilerDir + "\\pawncc.exe"))
            {
                MessageBox.Show("Pawn Kompiler-Programmdatei in \"" + compilerDir + "\" nicht gefunden.");
            }
            else
            {
                string includeDir = Config.includepath;
                string[] tmp = currentFilePath.Split('\\');
                int index = 0;
                int idex = 0;

                foreach (string s in tmp)
                {
                    if (s.Contains(".pwn"))
                    {

                        idex = index;
                    }
                    index++;
                }
                string fileName = tmp[idex];


                //We bassically copy the PWN file thats loaded in the compiler direcory


                StreamWriter file = new StreamWriter(compilerDir + fileName.Remove(fileName.IndexOf('.')) + ".p", false, Encoding.Default);
                file.Write(scintillaText);
                file.Close();




                Process compiler = new Process();
                compiler.StartInfo.Arguments = fileName.Remove(fileName.IndexOf('.'), 4) + ".p" + " -e\"errors\" -i" + "\"" + includeDir + "\"" + " -d03";

                //for zeex' version
                if (Config.compiler == 1) compiler.StartInfo.Arguments = fileName.Remove(fileName.IndexOf('.'), 4) + ".p" + " -e\"errors\" -i" + "\"" + includeDir + "\" -Z " + " -d03";

                compiler.StartInfo.WorkingDirectory = compilerDir;
                compiler.StartInfo.FileName = "pawncc.exe";

                compiler.Start();

                while (!compiler.HasExited)
                {

                }

                //Clear both lists
                errorList.Clear();

                bool anyErrors = false;

                //Check for errors
                if (File.Exists(compilerDir + "\\errors"))
                {
                    StreamReader errorFile = new StreamReader(compilerDir + "\\errors");
                    string errors = errorFile.ReadToEnd();
                    errorFile.Close();

                    string[] lines = errors.Split('\n');

                    foreach (string line in lines)
                    {
                        if (line == "") continue;
                        if (line.Contains("error")) anyErrors = true;
                        //Add to error list
                        ListViewItem lv = new ListViewItem(line);
                        errorList.Add(lv);


                    }
                    File.Delete(compilerDir + "\\errors");
                }
                if(!anyErrors)
                {
                    File.Delete(compilerDir + "\\errors");
                    File.Delete(compilerDir + fileName.Remove(fileName.IndexOf('.'), 4) + ".p");
                    //Wait for the file to appear.
                    while (!File.Exists(compilerDir + fileName.Remove(fileName.IndexOf('.'), 4) + ".amx"))
                    {

                    }
                    //Check if a older file already exists in source folder and delete if so
                    if (File.Exists(Path.GetDirectoryName(currentFilePath) + "\\" + fileName.Remove(fileName.IndexOf('.'), 4) + ".amx"))
                        File.Delete(Path.GetDirectoryName(currentFilePath) + "\\" + fileName.Remove(fileName.IndexOf('.'), 4) + ".amx");
                    //move the new compiled file to source folder
                    File.Move(compilerDir + fileName.Remove(fileName.IndexOf('.'), 4) + ".amx", Path.GetDirectoryName(currentFilePath) + "\\" + fileName.Remove(fileName.IndexOf('.'), 4) + ".amx");
                }
            }
        }


        private void t_Compile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lv_error.Items.Clear();
            bool anyentry = false;
            foreach (var item in errorList)
            {
                lv_error.Items.Add(item);
                anyentry = true;
            }

            if(anyentry)
            {
                String time;

                time = DateTime.Now.ToString("HH:mm");
                output.Text = output.Text + "[" + time + "] Kompilieren mit Fehlern oder Warnungen abgeschlossen.\n";
                tabControl1.SelectTab(0);
            }
            else
            {
                String time;

                time = DateTime.Now.ToString("HH:mm");
                output.Text = output.Text + "[" + time + "] Datei wurde erfolgreich kompiliert (" + ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - compileStart) + "s).\n";
                tabControl1.SelectTab(1); //Select output tab
            }

            SaveFile();
        }

        private void lv_error_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (ListViewItem var in lv_error.Items)
            {

                if (var.Selected)
                {

                    //export line from string
                    string[] tokens = var.Text.Split('(', ')');
                    int line = 0;
                    try
                    {
                        int.TryParse(tokens[1], out line);
                    }
                    catch { }

                    if (var.Text.Contains("symbol is never used"))
                    {
                        MessageBox.Show("Du hast einen Fehler oder eine Warnung angeklickt, welche/r angeblich in der letzten Zeile ("+ line + ") sein soll.\nDas ist nicht der Fall!\nDas Symbol welches in der Meldung genannt wurde, ist sehr wahrscheinlich irgendwo im Script zu finden. Nutze die Suche (STRG+S)!");
                    }
                    scintilla1.Lines[line--].Goto();
                    scintilla1.SetSelection(scintilla1.Lines[line].Position, scintilla1.Lines[line].EndPosition);
                    break;
                }
            }

        }








        //=======================================================================================================================================
        /*                                                          AUTO COMPLETE ARGS
        */
        //=======================================================================================================================================

        private void autocompleteMenu1_Selected(object sender, SelectedEventArgs e)
        {
            foreach (var item in includeFunctionList)
            {
                if (e.Item.Text == item.identifer)
                {
                    label_Args.Text = "Verwendung: " + item.fullIdentifer;
                }
            }

        }

















        //=======================================================================================================================================
        /*                                                          Andere callbacks
        */
        //=======================================================================================================================================

        private void lastfile_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            foreach (ToolStripItem li in lastFilesItems)
            {

                if (li == item)
                {
                    OpenFile(li.Text);
                    break;
                }
            }
        }

        private void tabControl1_Enter(object sender, EventArgs e)
        {
            if(!t_includeTreeViewUpdater.IsBusy)
                UpdateIncludeFiles();



            //Check defines - easily
            tv_defines.Nodes.Clear();
            foreach (Match match in Regex.Matches(scintilla1.Text, @"#define(.*?)\n"))
            {
                tv_defines.Nodes.Add(Regex.Unescape(match.Value));
            }


            //For PVars
            var pvars = new List<string>();
            tv_pvars.Nodes.Clear();

            //==========================================================
            //                   INTEGERS
            foreach (Match match in Regex.Matches(scintilla1.Text, @"SetPVarInt\((.*?)\)"))
            {
                string adding = Regex.Unescape(match.Value);


                Match rx = Regex.Match(adding, "\\\"(.*?)\\\"");
                if (rx != null)
                {
                    adding = "[INT] " + rx.Value;
                }


                bool skip = false;
                foreach (string str in pvars)
                {
                    if (str.Equals(adding)) skip = true;
                }

                if (skip) continue;

                pvars.Add(adding);
                tv_pvars.Nodes.Add(adding);
            }

            //==========================================================
            //                   FLOATS
            foreach (Match match in Regex.Matches(scintilla1.Text, @"SetPVarFloat\((.*?)\)"))
            {
                string adding = Regex.Unescape(match.Value);


                Match rx = Regex.Match(adding, "\\\"(.*?)\\\"");
                if (rx != null)
                {
                    adding = "[FLOAT] " + rx.Value;
                }


                bool skip = false;
                foreach (string str in pvars)
                {
                    if (str.Equals(adding)) skip = true;
                }

                if (skip) continue;

                pvars.Add(adding);
                tv_pvars.Nodes.Add(adding);
            }
            //==========================================================
            //                   STRINGS
            foreach (Match match in Regex.Matches(scintilla1.Text, @"SetPVarString\((.*?)\)"))
            {
                string adding = Regex.Unescape(match.Value);


                Match rx = Regex.Match(adding, "\\\"(.*?)\\\"");
                if (rx != null)
                {
                    adding = "[STRING] " + rx.Value;
                }


                bool skip = false;
                foreach (string str in pvars)
                {
                    if (str.Equals(adding)) skip = true;
                }

                if (skip) continue;

                pvars.Add(adding);
                tv_pvars.Nodes.Add(adding);



            }


            tv_enums.Nodes.Clear();
            //==========================================================
            //                   ENUMS
           /* foreach (Match match in Regex.Matches(scintilla1.Text, "enum"))
            {
                string enumname = Regex.Unescape(match.Value);

                int end = scintilla1.Text.IndexOf('}', match.Index);

                string wholething = scintilla1.Text.Substring(match.Index, end);
                MessageBox.Show(wholething);

                //string[] splitted = match.Value.Split(new Char[] { ' ', ',', '\n' });


                TreeNode node = tv_enums.Nodes.Add(match.Value);
                //foreach (string car in splitted)
                    //tv_enums.Nodes[node.Index].Nodes.Add(child);

            }*/

        }

        private void scintilla1_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            // Has the caret changed position?
            var caretPos = scintilla1.CurrentPosition;
            if (lastCaretPos != caretPos)
            {
                lastCaretPos = caretPos;
                var bracePos1 = -1;
                var bracePos2 = -1;

                // Is there a brace to the left or right?
                if (caretPos > 0 && IsBrace(scintilla1.GetCharAt(caretPos - 1)))
                    bracePos1 = (caretPos - 1);
                else if (IsBrace(scintilla1.GetCharAt(caretPos)))
                    bracePos1 = caretPos;

                if (bracePos1 >= 0)
                {
                    // Find the matching brace
                    bracePos2 = scintilla1.BraceMatch(bracePos1);
                    if (bracePos2 == Scintilla.InvalidPosition)
                    {
                        scintilla1.BraceBadLight(bracePos1);
                        scintilla1.HighlightGuide = 0;
                    }
                    else
                    {
                        scintilla1.BraceHighlight(bracePos1, bracePos2);
                        scintilla1.HighlightGuide = scintilla1.GetColumn(bracePos1);
                    }
                }
                else
                {
                    // Turn off brace matching
                    scintilla1.BraceHighlight(Scintilla.InvalidPosition, Scintilla.InvalidPosition);
                    scintilla1.HighlightGuide = 0;
                }
            }
        }





        private void SaveFile(int type = 0)
        {
            if(type == 1)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Pawn Quelldatei|*.pwn|Include Datei|*.inc";
                saveFileDialog1.Title = "Skript speichern";
                saveFileDialog1.ShowDialog();







                // If the file name is not an empty string open it for saving.
                if (saveFileDialog1.FileName != "")
                {
                    StreamWriter file = new StreamWriter(saveFileDialog1.FileName, false, Encoding.Default);
                    file.Write(scintilla1.Text);
                    file.Close();


                    needSaveFile = false;


                    currentFilePath = saveFileDialog1.FileName;

                    //Update caption
                    this.Text = saveFileDialog1.FileName + "    |  PawnDuck " + VERSION;

                    if (lastFilesItems.Count() == 6)
                    {
                        foreach (ToolStripItem i in lastFilesItems)
                        {
                            dateiToolStripMenuItem.DropDownItems.Remove(i);
                        }
                        lastFilesItems.Clear();
                        for (int i = 0; i < 6; i++)
                            Config.lastOpenedFiles[i] = "Ungespeichert";
                    }

                    if (lastFilesItems.Count() < 6)
                    {
                        ToolStripItem item = dateiToolStripMenuItem.DropDownItems.Add(saveFileDialog1.FileName);
                        lastFilesItems.Add(item);
                        item.Click += new EventHandler(lastfile_Click);
                        Config.lastOpenedFiles[lastFilesItems.Count] = saveFileDialog1.FileName;
                    }
                }
            }


            //Is an already loaded file
            if(!needSaveFile)
            {
                //Save under the current file name:
                StreamWriter file = new StreamWriter(currentFilePath, false, Encoding.Default);
                file.Write(scintilla1.Text);
                file.Close();
                //We saved the file - no changes made since then:
                //Update status label:
            }
            else
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Pawn Quelldatei|*.pwn|Include Datei|*.inc";
                saveFileDialog1.Title = "Skript speichern";
                saveFileDialog1.ShowDialog();







                // If the file name is not an empty string open it for saving.
                if (saveFileDialog1.FileName != "")
                {
                    StreamWriter file = new StreamWriter(saveFileDialog1.FileName, false, Encoding.Default);
                    file.Write(scintilla1.Text);
                    file.Close();


                    needSaveFile = false;


                    currentFilePath = saveFileDialog1.FileName;

                    //Update caption
                    this.Text = saveFileDialog1.FileName + "    |  PawnDuck " + VERSION;

                    if (lastFilesItems.Count() == 6)
                    {
                        foreach (ToolStripItem i in lastFilesItems)
                        {
                            dateiToolStripMenuItem.DropDownItems.Remove(i);
                        }
                        lastFilesItems.Clear();
                        for (int i = 0; i < 6; i++)
                            Config.lastOpenedFiles[i] = "Ungespeichert";
                    }

                    if (lastFilesItems.Count() < 6)
                    {
                        ToolStripItem item = dateiToolStripMenuItem.DropDownItems.Add(saveFileDialog1.FileName);
                        lastFilesItems.Add(item);
                        item.Click += new EventHandler(lastfile_Click);
                        Config.lastOpenedFiles[lastFilesItems.Count-1] = saveFileDialog1.FileName;
                    }
                }
            }
        }






        void OpenFile(string path)
        {
            needSaveFile = false;
            label_info.Text = "";
            //Open the file:
            if (File.Exists(path))
            {
                if (lastFilesItems.Count() == 6)
                {
                    foreach (ToolStripItem i in lastFilesItems)
                    {
                        dateiToolStripMenuItem.DropDownItems.Remove(i);
                    }
                    lastFilesItems.Clear();

                    for (int i = 0; i < 6; i++)
                        Config.lastOpenedFiles[i] = "Ungespeichert";
                    
                }


                bool exists = false;
                if (lastFilesItems.Count() < 6)
                {
                    foreach (ToolStripItem s in dateiToolStripMenuItem.DropDownItems)
                    {
                        if (s.Text.Equals(path))
                        {
                            exists = true;
                            break;
                        }
                            
                    }
                }

                if (!exists)
                {
                    ToolStripItem item = dateiToolStripMenuItem.DropDownItems.Add(path);
                    lastFilesItems.Add(item);
                    item.Click += new EventHandler(lastfile_Click);
                    Config.lastOpenedFiles[lastFilesItems.Count] = path;
                }


                StreamReader file = new StreamReader(path, Encoding.Default);
                string text = file.ReadToEnd();
                scintilla1.Text = text;
                file.Close();

                currentFilePath = path;

                //Update caption
                this.Text = path + "    |  PawnDuck " + VERSION;
                if (path.Contains("new_gm"))
                    this.Text = "Neue Datei    |  PawnDuck " + VERSION;
            }
            else MessageBox.Show("Die Datei existiert nicht (mehr).");
        }

        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Multiselect = false;
            dialog.ShowDialog();

            if (File.Exists(dialog.FileName))
            {
                OpenFile(dialog.FileName);
            }




        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            IniFile config = new IniFile(@Application.StartupPath + "\\config.ini");
            int index = 0;
            foreach (string s in Config.lastOpenedFiles)
            {
                config.Write("lastFile" + index, s);
                index++;
            }

            config.Write("compiler-mode", Config.compiler.ToString());
            config.Write("runCommand", ausführenToolStripMenuItem.DropDownItems[0].Text);
            config.Write("notes", noteBlock.Text);
            config.Write("autobackup", Config.autobackup.ToString());
            config.Write("includedir", Config.includepath);
        }

        private void neuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(@Application.StartupPath + "\\resources\\new_gm.pwn"))
            {
                StreamReader file = new StreamReader(@Application.StartupPath + "\\resources\\new_gm.pwn", Encoding.Default);
                string text = file.ReadToEnd();
                scintilla1.Text = text;
                file.Close();
            }
            else
            {
                scintilla1.Text = "";
            }
            needSaveFile = true;
        }

        private void scintilla1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && e.Control)
            {
                SaveFile();
                return;
                
            }

            else if(e.KeyCode == Keys.R && e.Alt)
            {
                try
                {
                    System.Diagnostics.Process.Start(toolStripTextBox1.Text);
                }
                catch
                {
                    MessageBox.Show("Auszuführender Befehl nicht ausführbar.");
                }
            }

            else if(e.KeyCode == Keys.F5)
            {
                if (needSaveFile)
                    SaveFile();
                scintillaText = scintilla1.Text;
                if(!t_Compile.IsBusy)
                {
                    t_Compile.RunWorkerAsync();
                    compileStart = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                }
                    

            }

            else if(e.Control && e.KeyCode == Keys.O)
            {
                try
                {
                    Process.Start(Path.GetDirectoryName(currentFilePath));
                }
                catch { }
            }

            else if (e.Control && e.KeyCode == Keys.F)
            {
                MyFindReplace.ShowFind();
                e.SuppressKeyPress = true;
            }
            else if (e.Shift && e.KeyCode == Keys.F3)
            {
                MyFindReplace.Window.FindPrevious();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                MyFindReplace.Window.FindNext();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                MyFindReplace.ShowReplace();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                MyFindReplace.ShowIncrementalSearch();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                GoTo MyGoTo = new GoTo((Scintilla)sender);
                MyGoTo.ShowGoToDialog();
                e.SuppressKeyPress = true;
            }

        }

        private void MyFindReplace_KeyPressed(object sender, KeyEventArgs e)
        {
            scintilla1_KeyDown(sender, e);
        }

        private void scintilla1_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar < 32)
            {
                // Prevent control characters from getting inserted into the text buffer
                e.Handled = true;
                return;
            }

        }

        private void aktuellenOrdnerÖffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(Path.GetDirectoryName(currentFilePath));
            }
            catch { }
        }

        private void speichernToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void speichernUnterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(1);
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (needSaveFile)
            {
                SaveFile(1);
            }
            scintillaText = scintilla1.Text;
            t_Compile.RunWorkerAsync();
            if (!t_Compile.IsBusy)
            {
                t_Compile.RunWorkerAsync();
                compileStart = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }
        }

        private static bool IsBrace(int c)
        {
            switch (c)
            {
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '<':
                case '>':
                    return true;
            }

            return false;
        }

        private void scintilla1_CharAdded(object sender, CharAddedEventArgs e)
        {
            if (e.Char == '{')
            {

                string format = null;

                int count = scintilla1.Lines[scintilla1.CurrentLine].Text.Count(f => f == '\t') - 1;
                for (int i = 0; i != count + 1; i++)
                    format = format + "\t";

                if (count > 0)
                    scintilla1.InsertText(scintilla1.CurrentPosition, "\n" + "\t" + format + "\n");
                else scintilla1.InsertText(scintilla1.CurrentPosition, "\n" + "\t" + format + "\n");

                scintilla1.GotoPosition(scintilla1.Lines[scintilla1.CurrentLine + 1].EndPosition - 1);
                scintilla1.InsertText(scintilla1.Lines[scintilla1.CurrentLine + 1].Position, format + "}");

            }
        }

        private void scintilla1_InsertCheck(object sender, InsertCheckEventArgs e)
        {
            if ((e.Text.EndsWith("\r") || e.Text.EndsWith("\n")))
            {
                var curLine = scintilla1.LineFromPosition(e.Position);
                var curLineText = scintilla1.Lines[curLine].Text;

                var indent = Regex.Match(curLineText, @"^[ \t]*");
                e.Text += indent.Value; // Add indent following "\r\n"

                // Current line end with bracket?
                if (Regex.IsMatch(curLineText, @"{\s*$"))
                    e.Text += '\t'; // Add tab
            }
        }

        private void neuLadenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile(currentFilePath);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("PawnDuck " + VERSION + " by Michael Fa (aka lp_)\n\n" +
                "Dieser Skript Editor ist für das Skripten von PAWN Skripts gedacht.\n" +
                "Fehler? Melde es im Github/PawnDuck!\n" + 
                "\n\nDanke an:\n #ScintillaNET (C# Wrapper für Scintilla)\n #AutoCompleteMenu-ScintillaNET (AutoCompleteMenu Wrapper für ScintillaNET)\n #PawnFox (LexicalAnalysis und einige Vorgehensweisen)\n #Zeex' Modifierte Version des PAWN Compilers 3.2.3664"
                );
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            string text = null;
            if (checkBox1.Checked)
            {
                Config.compiler = 1;
                text = "Compiler: Zeex";
            }
            else
            {
                Config.compiler = 0;
                text = "Compiler: Standard";
            }

            label_compiler.Text = text;
        }

        private void suchenFindenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MyFindReplace.ShowFind();
        }

        private void suchenErsetzenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MyFindReplace.ShowReplace();
        }

        private void scintilla1_Click(object sender, EventArgs e)
        {
            label_info.Text = "Position " + scintilla1.CurrentPosition + " in Zeile " + scintilla1.CurrentLine + " | " + (scintilla1.SelectionEnd - scintilla1.SelectionStart) + " markiert.";
        }

        private void schließenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
            currentFilePath = null;
            scintilla1.Text = null;
            needSaveFile = true;
        }

        private void autoBackup_cb_CheckedChanged(object sender, EventArgs e)
        {
            if(autoBackup_cb.Checked)
            {
                t_Backup.RunWorkerAsync();
                Config.autobackup = 1;
            }
            else
            {
                t_Backup.CancelAsync();
                Config.autobackup = 0;
            }
        }

        private void t_Backup_DoWork(object sender, DoWorkEventArgs e)
        {
            while((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds < lastBackup)
            {

            }
            if(!needSaveFile)
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\PawnDuck"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\PawnDuck");
                }
                string[] spl = currentFilePath.Split('\\');

                StreamWriter file = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\PawnDuck\\backup_" + spl[spl.Length - 1] + ".pwn", false, Encoding.Default);
                file.Write(scintilla1.Text);
                file.Close();
            }
            
        }

        private void t_Backup_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(!needSaveFile)
            {
                String time;

                time = DateTime.Now.ToString("HH:mm");
                output.Text = output.Text + "[" + time + "] Ein Backup wurde erstellt.\n";
                lastBackup = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 120;
                if (autoBackup_cb.Checked) t_Backup.RunWorkerAsync();
            }
            
        }

        private void changeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();

            if(Directory.Exists(dialog.SelectedPath))
            {
                includedir_Tb.Text = dialog.SelectedPath;
                Config.includepath = dialog.SelectedPath;
            }
        }

        private void label_Args_Click(object sender, EventArgs e)
        {
            try
            {
                string rx = label_Args.Text.Substring(label_Args.Text.IndexOf('('));
                scintilla1.InsertText(scintilla1.CurrentPosition, rx);
            }
            catch { }
        }

        private void scintilla1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.P)
            {
                try
                {
                    string rx = label_Args.Text.Substring(label_Args.Text.IndexOf('('));
                    scintilla1.InsertText(scintilla1.CurrentPosition, rx);
                }
                catch { }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
