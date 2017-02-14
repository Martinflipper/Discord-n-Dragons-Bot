using System;
using System.IO;
using System.Xml;
using Discord;
using DnDbot;

class Program
{
    
    static void bot_MessageReceived(object sender, Discord.MessageEventArgs e) //Commands
    {
        char[] delimiterchars = { ' ', '+' };

        if (e.Message.RawText.StartsWith("/ability")) //Ability Commands -personal
        {    
            string[] command = e.Message.RawText.Split(delimiterchars);
            string charValueName = command[1];
            string charName;
            
            if (command.Length == 2) //uses nickname as charname
            {
                charName = e.User.Nickname;
            }
            else if (command.Length == 3) //uses given charname as charname
            {
                charName = command[2];
            }
            else
            {
                string errorMessage = "U FUCKED UP, command error"; //error in command syntax
                e.User.SendMessage(e.User.Mention + errorMessage);
                return;
            }

            int? charValue = charAbilities(charName, charValueName); //get abilityscore
            if (charValue == null)
            {
                string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                e.User.SendMessage(e.User.Mention + errorMessage);
                return;
            }

            e.Message.Delete(); //deleting command-message
            string message = String.Format("Your {0} score is {1}", charValueName,charValue);
            e.User.SendMessage(message); //sending user personal message with data
        }
        else if (e.Message.RawText.StartsWith("/r"))  //Roll Dem Dice
        {
            string[] command = e.Message.RawText.Split(delimiterchars);
            string userName = e.User.Nickname;
            string comment = "";
            int diceNumber;
            int addValue = 0;

            try
            {
                string str_diceNumber = command[1].Remove(0, 1);
                diceNumber = Int32.Parse(str_diceNumber);
            }
            catch
            {
                string errorMessage = "U FUCKED UP, command error";
                e.Channel.SendMessage(e.User.Mention + errorMessage);
                return;
            }

            
            if(command.Length > 2)
            {
                int i = 2; //indexer
                
                while(command.Length != i)
                {
                    int[] type = typeChecker(command[i], userName);

                    addValue = addValue + type[0];

                    if (type[1] == 1) //what to do with comment
                    {
                        char h = '#';
                        command[i] = command[i].TrimStart(h);
                        comment = comment + " " + command[i];
                    }

                    if (type[1] == 2)
                    {
                        string errorMessage = "U FUCKED UP, nameHandler error";
                        e.Channel.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }

                    if(type[1] == 3)
                    {
                        string abilMention = nameHandler(command[i]);
                        comment = comment + " " + abilMention;
                    }

                    i = i + 1;
                }

            }

            // Create Random value
            Random rnd = new Random();
            int dice = rnd.Next(1, diceNumber);

            // Calculate shit
            int totalValue = dice + addValue;
            string calculator = "";

            if (addValue != 0)
            {
                calculator = String.Format(" + {0}",addValue);
            }

            if (comment != "")
            {
                comment = String.Format("`{0}`",comment);
            }

            //return Dice Value
            string message = String.Format(" {0} = ({1}){2} = **{3}** {4}",command[1],dice,calculator,totalValue,comment); 
            e.Channel.SendMessage(e.User.Mention + message);
        }
        else if (e.Message.RawText.StartsWith("/hp")) //HP commands -personal
        {
            string[] command = e.Message.RawText.Split(delimiterchars);
            if (command.Length == 1) //the simple /hp command
            {
                int?[] healthPoints = new int?[2];
                healthPoints = charHp(e.User.Nickname);
                if (healthPoints[0] == null)
                {
                    string errorMessage = "U FUCKED UP, Nickname-parse error";
                    e.Message.Delete();
                    e.User.SendMessage(e.User.Mention + errorMessage);
                    return;
                }
                else
                {
                    string hpSender = string.Format("Your current hp is {0}/{1}", healthPoints[0], healthPoints[1]);
                    e.Message.Delete();
                    e.User.SendMessage(hpSender);
                }
            }
            else if (command.Length == 4) //add or remove hp from player
            {
                string playerName = command[1];
                string modifier = command[2];
                string hpScore = command[3];
                int hpModifier;

                string succesmessage;

                if (modifier == "add")
                {
                    try
                    {
                        hpModifier = Int32.Parse(hpScore);
                        succesmessage = string.Format("{0} healt voor {1}", playerName, hpScore);
                    }
                    catch
                    {
                        string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                        e.User.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }

                }
                else if (modifier == "del" || modifier == "delete")
                {
                    hpScore = string.Format("-{0}", hpScore);
                    try
                    {
                        hpModifier = Int32.Parse(hpScore);
                        succesmessage = string.Format("{0} neemt {1} damage", playerName, hpScore);
                    }
                    catch
                    {
                        string errorMessage = "U FUCKED UP, parse error"; //error parsing the abilityname
                        e.User.SendMessage(e.User.Mention + errorMessage);
                        return;
                    }
                }

                else
                {
                    string errorMessage = "U FUCKED UP, command error"; //error parsing the command
                    e.User.SendMessage(e.User.Mention + errorMessage);
                    return;
                }

                bool succes;

                succes = editCharHp(playerName, hpModifier);

                if (succes == true)
                {
                    e.Channel.SendMessage(e.User.Mention + succesmessage);
                    return;
                }

                else
                {
                    string errorMessage = "U FUCKED UP, editHp error"; //error parsing the abilityname
                    e.User.SendMessage(errorMessage);
                    return;
                }

            }
        }
       
    }

    static void Main() //Main Program
    {

        XmlDocument charSheet = new XmlDocument();
        charSheet.LoadXml(DnDbot.Properties.Resources.CharSheet);
        charSheet.Save(@".\CharSheet.xml");


        var bot = new Discord.DiscordClient();
                
        bot.MessageReceived += bot_MessageReceived;

        bot.ExecuteAndWait(async () =>
        {
            await bot.Connect("Mjc5MjkwNjgzMjY4MDA1ODg4.C4DXPg.31L5sU0R9yxYRWjDU0UQ8vE_1zQ", TokenType.Bot);

        });

    }

    static bool editCharHp (string charName, int hpModifier)
    {
        bool succes;
        int?[] currentHp = new int?[2];
        int newHpValue;
        string newHpValueString;

        //get current and max HP
        currentHp = charHp(charName);
        if(currentHp[0] == null)
        {
            succes = false;
            return succes;
        }

        //change it, and convert to string
        if (currentHp[0] + hpModifier > currentHp[1])
        {
            newHpValue = currentHp[1] ?? default(int);
        }
        else
        {
            newHpValue = hpModifier + currentHp[0] ?? default(int);
        }

        newHpValueString = newHpValue.ToString();

        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();
        charSheet.Load(@".\CharSheet.xml");
        string adress1 = string.Format("/csheets/{0}/hp/currenthp", charName);
        //XmlNode charCurrentHp = charSheet.DocumentElement.SelectSingleNode(adress1);

        charSheet.SelectSingleNode(adress1).InnerText = newHpValueString;

        //Change value in XML-sheet
        //charCurrentHp.Value = newHpValueString;
        charSheet.Save(@".\CharSheet.xml");

        succes = true;
        return succes;

    }

    static int?[] charHp(string charName)
    {
        int?[] charHp = new int?[2];
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@".\CharSheet.xml");
        
        //Verkrijgen van info uit de XML
        string adress1 = String.Format(" / csheets/{0}/hp/currenthp", charName);
        string adress2 = String.Format("/csheets/{0}/hp/maxhp", charName);
        XmlNode charCurrentHp = charSheet.DocumentElement.SelectSingleNode(adress1);
        XmlNode charMaxHp = charSheet.DocumentElement.SelectSingleNode(adress2);
        try
        {
            charHp[0] = Int32.Parse(charCurrentHp.InnerText);
        }
        catch
        {
            charHp[0] = null;
            Console.WriteLine("charHp has been terminated, Parse error. {0} {1} ", charName, charHp);
            return charHp;
        }

        try
        {
            charHp[1] = Int32.Parse(charMaxHp.InnerText);
        }
        catch
        {
            charHp[1] = null;
            Console.WriteLine("charHp has been terminated, Parse error. {0} {1} ", charName, charHp);
            return charHp;
        }

        Console.WriteLine("charHp initialized for {0}, value of: {1}/{2}", charName, charHp[0], charHp[1]);

        return charHp;

    }

    static int? charAbilities(string charName, string charValueName) //Collects ability score from xml
    {
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@".\CharSheet.xml");

        //Verkrijgen van info uit de XML
        string adress = String.Format("/csheets/{0}/abilities/{1}", charName, charValueName);

        XmlNode charInfo = charSheet.DocumentElement.SelectSingleNode(adress);

        //Try to return the value, otherwise return errorcode
        int? charValue = null;
        try
        {
            charValue = Int32.Parse(charInfo.InnerText);
        }
        catch
        {
            int? errorCode = null;
            Console.WriteLine("charAbilities has been terminated, Parse error. {0} {1} ", charValueName, charValue);
            return errorCode;
        }

        Console.WriteLine("charAbilities has been succesfully executed with a {0} score of {1}", charValueName, charValue);

        //Return de value
        return charValue;
    }

    static int? charSkills(string charName, string charValueName) //collects skill score from xml
    {
        //Laden van de XML-sheets
        XmlDocument charSheet = new XmlDocument();

        charSheet.Load(@".\CharSheet.xml");

        //Verkrijgen van info uit de XML
        string adress = String.Format("/csheets/{0}/skills/{1}", charName, charValueName);

        XmlNode charInfo = charSheet.DocumentElement.SelectSingleNode(adress);

        //Try to return the value, otherwise return errorcode
        int? charValue = null;
        try
        {
            charValue = Int32.Parse(charInfo.InnerText);
        }
        catch
        {
            int? errorCode = null;
            Console.WriteLine("charAbilities has been terminated, Parse error. {0} {1} {2} {3}", charValueName, charValue, charName, adress);
            return errorCode;
        }

        Console.WriteLine("charSkills has been succesfully executed with a {0} score of {1}", charValueName, charValue);

        //Return de value
        return charValue;
    }

    static string nameHandler(string namePart) //verandert afkorting naar volledige abilitynaam
    {
        string[] abilitiesList = { "Str", "Con", "Dex", "Int", "Wis", "Cha","Acr", "Arc", "Ath", "Blu", "Dip", "Dun","End","Hea","His","Ins","Itd","Nat","Per","Rel","Ste","Stw","Thi" };
        string[] abilitiesNames = { "strength", "constitution", "dexterity", "inteligence", "wisdom", "charisma","acrobatics","arcana","athletics","bluff","diplomacy","dungeoneering","endurance","heal","history","insight","intimidate","nature","perception","religion","stealth","streetwise","thievery" };

        int i = 0; //indexer

        if (namePart.Length == 3)
        {
            while (abilitiesList[i].Equals(namePart, StringComparison.OrdinalIgnoreCase) == false)
            {
                i = i + 1;
                if (i > (abilitiesList.Length - 1))
                {
                    return ("false");
                }
            }
        }

        else if (namePart.Length > 3)
        {
            while (abilitiesNames[i].Equals(namePart, StringComparison.OrdinalIgnoreCase) == false)
            {
                i = i + 1;
                if (i > (abilitiesList.Length - 1))
                {
                    return ("false");
                }
            }
        }


        string fullname = abilitiesNames[i];
        return fullname;

    }

    static int[] typeChecker(string commandPart, string userName) //returns int array {value to be added, typecode}
    {
        int functionValue;
        int[] typeChecker = new int[2];

        if (int.TryParse(commandPart, out functionValue)) //checks for integer value
        {
            typeChecker[0] = functionValue;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;
        }

        else if (commandPart.StartsWith("#")) //checks for comment
        {
            typeChecker[1] = 1;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;
        }

        else if (commandPart.Length >= 3)
        {

            string fullName = nameHandler(commandPart); //checks for ability check
            if(fullName == "false")
            {
                typeChecker[1] = 2;
                return typeChecker;
            }

            int? abilScore = charSkills(userName, fullName);

            typeChecker[0] = abilScore ?? default(int);
            typeChecker[1] = 3;

            Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

            return typeChecker;

            

        }

        typeChecker[1] = 4;

        Console.WriteLine("Typechecker exec:{0}{1}", typeChecker[0], typeChecker[1]);

        return typeChecker;


    }
}