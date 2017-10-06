using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GlobalConfig
{
    //=====================================
    //Compiler mode
    //0 default / 1 zeex

    public int compiler = 0;

    //=====================================
    //last opened files
    public string[] lastOpenedFiles = new string[6];

    public string noteblock;
    public int autobackup;
    public string includepath;

    //run string
    public string runCommand;



}
