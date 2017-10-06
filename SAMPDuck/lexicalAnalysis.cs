using System.Collections.Generic;

public class lexer
{
    public static class lexicalHelper
    {
        /*Returns whether c is an identifer*/
        public static bool isIdentiferChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '#';
        }
        /*Returns whether c is an oprator*/
        public static bool isOperatorChar(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '&':
                case '|':
                case '!':
                    return true;
                default: return false;
            }
        }
        /*Returns whether c is a symbol*/
        public static bool isSymbol(char c)
        {
            switch (c)
            {
                case '#':
                case '<':
                case '>':
                case '(':
                case ')':
                case '{':
                case '}':
                case ':':
                case '[':
                case ']':
                case ';':
                case '=':
                case ',':
                    return true;
                default: return false;
            }
        }
        /*Returns whether c is a number*/
        public static bool isNumericChar(char c)
        {
            return char.IsNumber(c);
        }
        /*Returns whether s is a keyword that forces a parenthesis behind it*/
        public static bool isKeyword(string s)
        {
            if (s == "for" || s == "while" || s == "if" || s == "switch")
                return true;
            return false;
        }

        //IF s is a samp callback
        public static string[] sampCallbacks = {"OnActorStreamIn",             "OnPlayerClickMap",             "OnPlayerExitVehicle",
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
        public static bool isCallback(string s)
        {
            foreach(string _s in sampCallbacks)
            {
                if (_s.Equals(s))
                    return true;
            }
            return false;
        }









    };
    /*
     * Lexical tokens hold simple, uncategorized information about strings that are probably
     * identifers, operators, numbers or symbols
    */

    private enum tokenType
    {
        none,
        identifer,
        define,
        op,
        number,
        symbol,
        newline,
        parenthesis_open,
        parenthesis_close,
        brace_open,
        brace_close,
        semicolon,
        tstring,
        tenum
    };

    private class lexicalToken
    {
        public string value = "";
        public tokenType type = tokenType.none;
        public int line = 0;
        public lexicalToken(tokenType type, string value, int lineNumber)
        {
            this.type = type;
            if (type == tokenType.identifer && value == "#define")
                this.type = tokenType.define;
            else if (type == tokenType.identifer && value == "enum")
                this.type = tokenType.tenum;
            if (type == tokenType.symbol && value == "(")
                this.type = tokenType.parenthesis_open;
            else if (type == tokenType.symbol && value == ")")
                this.type = tokenType.parenthesis_close;
            else if (type == tokenType.symbol && value == "{")
                this.type = tokenType.brace_open;
            else if (type == tokenType.symbol && value == "}")
                this.type = tokenType.brace_close;
            else if (type == tokenType.symbol && value == ";")
                this.type = tokenType.semicolon;
            this.value = value;
            this.line = lineNumber;
        }
    };

    public class defineData
    {
        public string identifer = "";
        public string value = "";
        public defineData() { }
        public defineData(string identifer, string value)
        {
            this.identifer = identifer;
            this.value = value;
        }
    };
    public class functionData
    {
        public string identifer = "";
        public string fullIdentifer = "";
        public string fullIdentiferDataTypes = "";
        public List<string> arguments;
        public List<string> dataTypes;
        public int implementation = 0;
        public bool isImplemented = false;
        public List<int> occurences;
        public functionData()
        {
            arguments = new List<string>();
            dataTypes = new List<string>();
            occurences = new List<int>();
        }
    };
    public class functionCallData
    {
        public string identifer;
        public List<int> calls;
        public functionCallData()
        {
            calls = new List<int>();
        }
    };
    public class enumData
    {
        public List<string> content;
        public string identifer = "";
        public enumData()
        {
            content = new List<string>();
        }
    };

    public void codeAnalysis(string text, ref List<defineData> defineList, ref List<functionData> functionList, bool skipCallbacks = true)
    {
        List<string> errorList = new List<string>();
        List<lexicalToken> tokens = lexicalAnalysis(text, ref errorList);

        defineList = new List<defineData>();
        functionList = new List<functionData>();

        List<functionCallData> functionCallDataList = new List<functionCallData>();

        int max = tokens.Count;
        for (int i = 0; i != tokens.Count; i++)
        {
            lexicalToken t = tokens[i];
            if (t.type == tokenType.define)
            {
                //Create new define object:
                defineData define = new defineData();
                //Proceed to next token:
                i++; if (i == max) break;
                t = tokens[i];
                //If the token is a identifer, add that to the define object:
                if (t.type == tokenType.identifer)
                {
                    define.identifer = t.value;
                }
                else
                {
                    //Error: No define identifer found!
                    errorList.Add("Missing define identifer.");
                    continue;
                }
                // Add all following tokens to the define value:
                do
                {
                    i++; if (i == max) break;
                    t = tokens[i];
                    define.value += t.value + " ";
                }
                while (t.type != tokenType.newline);
                //Add the define object to the list:
                defineList.Add(define);
            }
            else if (t.type == tokenType.identifer)
            {
                //Abort if callback:
                if (skipCallbacks && lexicalHelper.isCallback(t.value))
                    continue;
                //Skip keywords (if they look like functions):
                if (lexicalHelper.isKeyword(t.value))
                {
                    //Skip until all () are closed again
                    i++; if (i == max) break;
                    t = tokens[i];
                    if (t.type == tokenType.parenthesis_open)
                    {
                        int parenthesis_count = 1;
                        while (parenthesis_count != 0)
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                            if (t.type == tokenType.parenthesis_open)
                                parenthesis_count++;
                            else if (t.type == tokenType.parenthesis_close)
                                parenthesis_count--;
                        }
                    }
                    else
                    {
                        //Error: Expected parenthesis
                    }
                }
                else
                {
                    //Check if the identifer is a function identifer:
                    //Create variables:
                    functionData function = new functionData();
                    function.identifer = t.value;
                    int beginning_token = i;

                    //Check if identifer is a function:
                    i++; if (i == max) break;
                    t = tokens[i];

                    //There is a ( following:
                    if (t.type == tokenType.parenthesis_open)
                    {
                        int identiferLine = t.line; //Line on which the identifer lies.
                        //Add all arguments until all () are closed again:
                        int parenthesis_count = 1;
                        while (parenthesis_count != 0)
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                            if (t.type == tokenType.parenthesis_open)
                                parenthesis_count++;
                            else if (t.type == tokenType.parenthesis_close)
                                parenthesis_count--;
                            else if (t.type == tokenType.identifer)
                                function.arguments.Add(t.value);
                            else if (t.type == tokenType.brace_open || t.type == tokenType.brace_close)
                                break;
                        }
                        if (parenthesis_count != 0)
                        {
                            if (i == max)
                            {
                                //Error: File ended unexpectedly!
                                break;
                            }
                            else //The loop broke because of a { or }
                            {
                                //Error: Probably missing at least one ")"
                            }
                        }
                        //Proceed to next tokens:
                        //Skip all unnessecary tokens:
                        do
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                        }
                        while (t.type == tokenType.newline);
                        functionData foundFunction = functionList.Find(item => item.identifer == function.identifer);
                        //Proceed to next tokens:
                        if (t.type == tokenType.brace_open)
                        {
                            //We found the implementation of the function:
                            if (foundFunction != null)
                            {
                                //Error: Function redefinition?
                                continue;
                            }
                            //If the function is a callback implementation, abort:
                            if (lexicalHelper.isCallback(function.identifer))
                            {
                                continue;
                            }
                            function.isImplemented = true;
                            function.implementation = identiferLine;
                        }
                        else if (t.type == tokenType.semicolon)
                        {
                            //We found no implementation of the function:
                            functionCallData foundFunctionData = functionCallDataList.Find(item => item.identifer == function.identifer);
                            if (foundFunctionData != null)
                                foundFunctionData.calls.Add(identiferLine);
                            else
                            {
                                foundFunctionData = new functionCallData();
                                foundFunctionData.identifer = function.identifer;
                                foundFunctionData.calls.Add(identiferLine);
                                functionCallDataList.Add(foundFunctionData);
                            }
                            continue;

                        }
                        else
                        {
                            //Error: Missing semicolon:
                        }
                        //Get tags and add to list:
                        int j = beginning_token;
                        List<string> dataTypeList = new List<string>();
                        do
                        {
                            j -= 2;
                            if (j <= 0) break;
                            t = tokens[j];
                            if (!((t.type == tokenType.newline) || (t.type == tokenType.semicolon)))
                                dataTypeList.Add(t.value);
                        }
                        while (j > 0 && t.type != tokenType.newline && t.type != tokenType.semicolon);
                        if (dataTypeList.Count > 0)
                            function.dataTypes = dataTypeList;
                        if (!function.isImplemented)
                            continue;
                        //Add to list or update on list:
                        //Process arguments:
                        string arg = "(";
                        foreach (string x in function.arguments)
                            arg += x + ",";
                        arg += ")";
                        arg = arg.Replace(",)", ")");
                        function.fullIdentifer = function.identifer + arg;
                        arg = "";
                        foreach (string x in function.dataTypes)
                        {
                            arg = arg.Replace("\n", "");
                            arg += x + " ";
                        }
                        function.fullIdentiferDataTypes = arg + function.fullIdentifer;
                        functionList.Add(function);
                    }
                    else
                    {
                        //There is no ( following to the identifer:
                        i--;
                    }
                }
            }
        }
        foreach (functionCallData i in functionCallDataList)
        {
            functionData f = functionList.Find(item => item.identifer == i.identifer);
            if (f != null)
            {
                f.occurences = i.calls;
            }
        }
    }
    public void includeAnalysis(string text, ref List<functionData> functionList)
    {
        List<string> errorList = new List<string>();
        List<lexicalToken> tokens = lexicalAnalysis(text, ref errorList);
        int max = tokens.Count;
        for (int i = 0; i != tokens.Count; i++)
        {
            lexicalToken t = tokens[i];
            if (t.type == tokenType.identifer)
            {
                //Abort if callback:
                if (lexicalHelper.isCallback(t.value))
                    continue;
                //Skip keywords like if (they look like functions):
                if (lexicalHelper.isKeyword(t.value))
                {
                    //Skip until all () are closed again
                    i++; if (i == max) break;
                    t = tokens[i];
                    if (t.type == tokenType.parenthesis_open)
                    {
                        int parenthesis_count = 1;
                        while (parenthesis_count != 0)
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                            if (t.type == tokenType.parenthesis_open)
                                parenthesis_count++;
                            else if (t.type == tokenType.parenthesis_close)
                                parenthesis_count--;
                        }
                    }
                    else
                    {
                        //Error: Expected parenthesis
                    }
                }
                else
                {
                    //Check if the identifer is a function identifer:
                    //Create variables:
                    functionData function = new functionData();
                    function.identifer = t.value;
                    int beginning_token = i;

                    //Check if identifer is a function:
                    i++; if (i == max) break;
                    t = tokens[i];
                    //There is a ( following:
                    if (t.type == tokenType.parenthesis_open)
                    {
                        //Add all arguments until all () are closed again:
                        int parenthesis_count = 1;
                        while (parenthesis_count != 0)
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                            if (t.type == tokenType.parenthesis_open)
                                parenthesis_count++;
                            else if (t.type == tokenType.parenthesis_close)
                                parenthesis_count--;
                            else if (t.type == tokenType.identifer)
                                function.arguments.Add(t.value);
                            else if (t.type == tokenType.brace_open || t.type == tokenType.brace_close)
                                break;
                        }
                        if (parenthesis_count != 0)
                        {
                            if (i == max)
                            {
                                //Error: File ended unexpectedly!
                                break;
                            }
                            else //The loop broke because of a { or }
                            {
                                //Error: Probably missing at least one ")"
                            }
                        }
                        //Proceed to next tokens:
                        //Skip all unnessecary tokens:
                        do
                        {
                            i++; if (i == max) break;
                            t = tokens[i];
                        }
                        while (t.type == tokenType.newline);
                        functionData foundFunction = functionList.Find(item => item.identifer == function.identifer);
                        //Proceed to next tokens:
                        if (t.type == tokenType.brace_open)
                        {
                            //We found the implementation of the function:
                            if (foundFunction != null)
                            {
                                //Error: Function redefinition?
                                continue;
                            }
                            //If the function is a callback implementation, abort:
                            if (lexicalHelper.isCallback(function.identifer))
                            {
                                continue;
                            }
                            function.isImplemented = true;
                            function.implementation = t.line - 1;
                        }
                        else if (t.type == tokenType.semicolon)
                        {
                            //We found no implementation of the function:
                            //Abort (there could be natives)
                            function.isImplemented = false;
                            function.implementation = -1;
                        }
                        else
                        {
                            //Error: Missing semicolon:
                        }
                        //Get tags and add to list:
                        int j = beginning_token;
                        List<string> dataTypeList = new List<string>();
                        do
                        {
                            j--;
                            if (j <= 0) break;
                            t = tokens[j];
                            if (!((t.type == tokenType.newline) || (t.type == tokenType.semicolon)))
                                dataTypeList.Add(t.value);
                        }
                        while (j > 0 && t.type != tokenType.newline && t.type != tokenType.semicolon);
                        if (dataTypeList.Count > 0)
                            function.dataTypes = dataTypeList;
                        //Add to list or update on list:
                        if (dataTypeList.Find(item => item.Contains("native")) != null)
                        {
                            string arg = "(";
                            foreach (string x in function.arguments)
                                arg += x + ",";
                            arg += ")";
                            arg = arg.Replace(",)", ")");
                            function.fullIdentifer = function.identifer + arg;
                            arg = "";
                            foreach (string x in function.dataTypes)
                            {
                                arg = arg.Replace("\n", "");
                                arg += x + " ";
                            }
                            function.fullIdentiferDataTypes = arg + function.fullIdentifer;
                            functionList.Add(function);
                        }
                        else continue;
                        //Else: Function is a prototype that is no native. Not interested in those.
                    }
                    else
                    {
                        //There is no ( following to the identifer:
                        i--;
                    }
                }
            }
        }
    }

    public lexer()
    {

    }

    List<lexicalToken> lexicalAnalysis(string text, ref List<string> errorList)
    {
        List<lexicalToken> tokens = new List<lexicalToken>();

        //Current character:
        int i = 0;
        //Different modes:
        const ushort mode_none = 0;
        const ushort mode_identifer = 1;
        const ushort mode_operator = 2;
        const ushort mode_number = 3;
        const ushort mode_symbol = 4;
        const ushort mode_comment_entry = 5;
        const ushort mode_comment_simple = 6;
        const ushort mode_comment_complex = 7;
        const ushort mode_string = 8;
        ushort mode = mode_none;
        //Temporary variables:
        string temp = "";
        char c;
        bool expectingEscapeCharacter = false;
        int currentLine = 0;
        //Scan the text:
        while (i != text.Length)
        {
            c = text[i];
            if (c == '\n')
                currentLine++;
            switch (mode)
            {
                case mode_none:
                    if (lexicalHelper.isIdentiferChar(c))   //Start reading identifers
                        mode = mode_identifer;
                    else if (lexicalHelper.isNumericChar(c))    //Start reading numbers
                        mode = mode_number;
                    else if (c == '/')  //There might be a comment following
                        mode = mode_comment_entry;
                    else if (lexicalHelper.isOperatorChar(c))   //Start reading operators
                        mode = mode_operator;
                    else if (lexicalHelper.isSymbol(c)) //Start reading symbols
                        mode = mode_symbol;
                    else if (c == '\n' || c == '\r')
                    {
                        lexicalToken t = new lexicalToken(tokenType.newline, "\n", currentLine);
                        tokens.Add(t);
                        i++;
                    }
                    else if (c == '\"')
                    {
                        mode = mode_string;
                        i++;
                    }
                    else i++;
                    break;
                /*
                 * Identifers
                 */
                case mode_identifer:
                    //If there are still identifer chars being read, continue
                    if (lexicalHelper.isIdentiferChar(c) || lexicalHelper.isNumericChar(c))
                    {
                        temp += c;
                        i++;
                    }
                    //Streak is broken, abort and save:
                    else
                    {
                        lexicalToken token = new lexicalToken(tokenType.identifer, temp, currentLine);
                        tokens.Add(token);
                        temp = "";
                        mode = mode_none;
                    }
                    break;
                case mode_number:
                    //If there are still numeric chars being read, continue
                    if (lexicalHelper.isNumericChar(c))
                    {
                        temp += c;
                        i++;
                    }
                    //Streak is broken, abort and save:
                    else
                    {
                        lexicalToken token = new lexicalToken(tokenType.number, temp, currentLine);
                        tokens.Add(token);
                        temp = "";
                        mode = mode_none;
                    }
                    break;
                case mode_comment_entry:
                    //A / was read! If there is another / oder a * following, we've got a comment!
                    i++;
                    if (i == text.Length) //Text suddenly ended...
                        break;
                    //Next character:
                    c = text[i];
                    if (c == '/')
                    {
                        //Simple one-line comment
                        mode = mode_comment_simple;
                        i++;
                    }
                    else if (c == '*')
                    {
                        //Multi line comment:
                        mode = mode_comment_complex;
                        i++;
                    }
                    else
                    {
                        //No comment. Let the operator mode handle the situation!
                        temp = "/";
                        mode = mode_operator;
                    }
                    break;
                case mode_comment_simple:
                    //Skip until newline char
                    if (c == '\n')
                    {
                        mode = mode_none;
                        i++;
                    }
                    else i++;
                    break;
                case mode_comment_complex:
                    //Skip until */ is found
                    if (c == '*')
                    {
                        i++;
                        if (i == text.Length)
                            break;
                        c = text[i];
                        if (c == '/')
                        {
                            mode = mode_none;
                            i++;
                        }
                    }
                    else i++;
                    break;
                case mode_operator:
                    if (lexicalHelper.isOperatorChar(c))
                    {
                        temp += c;
                        i++;
                    }
                    else
                    {
                        lexicalToken token = new lexicalToken(tokenType.op, temp, currentLine);
                        tokens.Add(token);
                        temp = "";
                        mode = mode_none;
                    }
                    break;
                case mode_symbol:
                    if (lexicalHelper.isSymbol(c))
                    {
                        lexicalToken token = new lexicalToken(tokenType.symbol, c.ToString(), currentLine);
                        tokens.Add(token);
                        i++;
                        mode = mode_none;
                    }
                    break;
                case mode_string:
                    if (c == '\"')
                    {
                        lexicalToken token = new lexicalToken(tokenType.tstring, temp, currentLine);
                        tokens.Add(token);
                        temp = "";
                        mode = mode_none;
                        i++;
                    }
                    else if (c == '\\')
                    {
                        if (!expectingEscapeCharacter)
                            expectingEscapeCharacter = true;
                        i++;
                    }
                    else
                    {
                        temp += c;
                        if (expectingEscapeCharacter)
                            expectingEscapeCharacter = false;
                        i++;
                    }
                    break;
            }
        }
        return tokens;
    }
};
